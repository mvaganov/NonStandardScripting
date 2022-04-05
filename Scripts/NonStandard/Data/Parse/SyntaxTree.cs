// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using System.Collections.Generic;
using System.Text;

namespace NonStandard.Data.Parse {
	/// <summary>
	/// a node in a NonStadard syntax tree
	/// </summary>
	public class SyntaxTree {
		public ParseRuleSet rules = null;
		protected SyntaxTree parent = null;
		public List<Token> tokens;
		/// <summary>
		/// where in the list of tokens this container pulls its children from. might be non-zero it the token list is borrowed from elsewhere
		/// </summary>
		internal int tokenStart;
		/// <summary>
		/// how many tokens from the token list this container is claiming. they will be contiguous following <see cref="tokenStart"/>
		/// </summary>
		internal int tokenCount = -1;
		//public SyntaxTree(List<Token> tokenList, int indexStart, int count) {
		//	tokens = tokenList; tokenStart = indexStart; tokenCount = count;
		//}
		public SyntaxTree(ParseRuleSet rule, List<Token> tokenList, int indexStart, int count, object meta) {
			rules = rule; tokens = tokenList; tokenStart = indexStart; tokenCount = count; sourceMeta = meta;
		}
		public Token GetToken(int index) { return tokens[tokenStart + index]; }
		public Delim beginDelim, endDelim;
		/// <summary>
		/// what this <see cref="SyntaxTree"/> is describing
		/// </summary>
		public object sourceMeta;
		public int TokenCount => tokenCount;
		public int Depth { get { SyntaxTree p = parent; int n = 0; while (p != null) { p = p.parent; ++n; } return n; } }
		public readonly static SyntaxTree None = new SyntaxTree(null, null, 0, -1, null);
		public SyntaxTree GetParent() { return parent; }
		public void SetParent(SyntaxTree p) { parent = p; }
		public static string PrintAll(List<Token> tokens) {
			StringBuilder sb = new StringBuilder();
			List<List<Token>> stack = new List<List<Token>>();
			PrintAll(tokens, sb, stack);
			return sb.ToString();
		}
		protected static void PrintAll(List<Token> tokens, StringBuilder sb, List<List<Token>> stack) {
			int recurse = stack.IndexOf(tokens);
			stack.Add(tokens);
			if (recurse >= 0) { sb.Append("/* recurse " + stack.Count + " */"); return; }
			for (int i = 0; i < tokens.Count; ++i) {
				Token t = tokens[i];
				SyntaxTree e = t.GetAsSyntaxNode();
				if (e != null && !t.IsValidText) {
					PrintAll(e.tokens, sb, stack);
				} else {
					sb.Append(t);
				}
			}
		}
		public string TextEnclosed {
			get {
				switch (sourceMeta) {
				case DelimOp d: return TextRaw;
				}
				int start = tokens[tokenStart].index;
				int limit = tokens[tokenStart + tokenCount - 1].GetEndIndex();
				//Show.Log(parseRules.name + " " +sourceMeta.GetType()+" "+start+", "+limit+" "+TextRaw);
				if (TextRaw == null) {
					throw new System.Exception("dunno how to print this...");
				}
				return TextRaw.Substring(start, limit - start);
			}
		}
		public string TextRaw {
			get {
				SyntaxTree e = this; string str;
				Delim d;
				do {
					str = e.sourceMeta as string;
					if (str == null) { d = e.sourceMeta as Delim; if (d != null) { str = d.text; } }
					if (str == null) { e = e.sourceMeta as SyntaxTree; }
				} while (str == null && e != null);
				if (str == null) {
					for(int i = 0; i < tokens.Count; ++i) {
						if (tokens[i].IsSimpleString) { str = tokens[i].meta.ToString(); break; }
						SyntaxTree syntax = tokens[i].GetAsSyntaxNode();
						str = syntax.TextRaw;
						if (str != null) { break; }
					}
				}
				return str;
			}
		}
		public string GetText() { return Unescape(); }
		public object Resolve(ITokenErrLog tok, object scope, ResolvedEnoughDelegate isItResolvedEnough = null) {
			DelimOp op = sourceMeta as DelimOp;
			if (op != null) {
				return op.resolve.Invoke(tok, this, scope, isItResolvedEnough);
			}
			List<object> finalTerms = ResolveTerms(tok, scope, tokens, isItResolvedEnough);
			object result = finalTerms;
			if (rules != null && rules.Simplify != null) {
				if (isItResolvedEnough != null && isItResolvedEnough.Invoke(result)) { return result; }
				result = rules.Simplify.Invoke(finalTerms);
			}
			return result;
		}
		public static List<object> ResolveTerms(ITokenErrLog tok, object scope, List<Token> tokens, ResolvedEnoughDelegate isItResolvedEnough = null) {
			List<object> results = new List<object>();
			ResolveTerms(tok, scope, tokens, 0, tokens.Count, results, isItResolvedEnough);
			return results;
		}
		public static void ResolveTerms(ITokenErrLog tok, object scope, List<Token> tokens, int start, int length, List<object> results, ResolvedEnoughDelegate isItResolvedEnough = null) {
			List<int> found = new List<int>();
			FindTerms(tokens, start, length, found);
			for (int i = 0; i < found.Count; ++i) {
				Token t = tokens[found[i]];
				object result = t.Resolve(tok, scope, isItResolvedEnough);
				results.Add(result);
				// if this token is probably a method call, or there are arguments immediately after this token (so maybe a method call)
				Invocation mc = result as Invocation;
				if (mc != null || (i < found.Count - 1 && found[i + 1] == found[i] + 1)) {
					object target = mc != null ? mc.target : scope;
					object methodName = mc != null ? mc.methodName : result;
					Token methodArgs = tokens[found[i + 1]];
					if (Invocation.TryExecuteFunction(target, methodName, methodArgs, out result, tok, isItResolvedEnough)) {
						++i;
						results[results.Count - 1] = result;
					}
				}
			}
		}

		public static void FindTerms(IList<Token> tokens, int start, int length, List<int> found) {
			SyntaxTree syntax;
			for (int i = 0; i < length; ++i) {
				Token t = tokens[start + i];
				if (t.IsSyntaxBoundary || ((syntax = t.GetAsSyntaxNode()) != null && syntax.IsComment)) { continue; } // skip entry tokens (count sub-syntax trees)
				found.Add(i);
			}
		}

		public static List<int> FindTermIndexes(IList<Token> tokens, int start = 0, int length = -1) {
			if(length < 0) { length = tokens.Count; }
			List<int> tokenIndexes = new List<int>();
			FindTerms(tokens, start, length, tokenIndexes);
			return tokenIndexes;
		}
		public static List<Token> FindSubstantiveTerms(IList<Token> tokens, int start = 0, int length = -1) {
			List<int> tokenIndexes = FindTermIndexes(tokens, start, length);
			List<Token> substantiveTokens = new List<Token>();
			for(int i = 0; i < tokenIndexes.Count; ++i) {
				substantiveTokens.Add(tokens[tokenIndexes[i]]);
			}
			return substantiveTokens;
		}
		public bool IsTextLiteral { get { return rules == CodeRules.String || rules == CodeRules.Char; } }
		public bool IsEnclosure { get { return rules == CodeRules.Expression || rules == CodeRules.CodeBody || rules == CodeRules.SquareBrace; } }
		public bool IsComment { get { return rules == CodeRules.CommentLine || rules == CodeRules.XmlCommentLine || rules == CodeRules.CommentBlock; } }
		public bool IsMembershipOperation { get { return rules == CodeRules.MembershipOperator; } }
		public List<string> ConvertMembershipOperationToMemberSequence() {
			if (!IsMembershipOperation) return null;
			List<string> sequence = new List<string>();
			if (tokens[0].IsSyntax) {
				SyntaxTree st = tokens[0].GetAsSyntaxNode();
				sequence.AddRange(st.ConvertMembershipOperationToMemberSequence());
			} else {
				sequence.Add(tokens[0].ToString());
			}
			if (tokens[2].IsSyntax) {
				SyntaxTree st = tokens[2].GetAsSyntaxNode();
				sequence.AddRange(st.ConvertMembershipOperationToMemberSequence());
			} else {
				sequence.Add(tokens[2].ToString());
			}
			return sequence;
		}
		public Token GetBeginToken() { return tokens[tokenStart]; }
		public Token GetEndToken() { return tokens[tokenStart + tokenCount - 1]; }
		public int GetIndexBegin() { return GetBeginToken().GetBeginIndex(); }
		public int GetIndexEnd() { return GetEndToken().GetEndIndex(); }
		public bool IsBegin(Token t) { return t == GetBeginToken(); }
		public bool IsEnd(Token t) { return t == GetEndToken(); }
		public bool IsBeginOrEnd(Token t) { return t == GetBeginToken() || t == GetEndToken(); }
		public int Length { get { return GetIndexEnd() - GetIndexBegin(); } }
		public string Unescape() {
			if (rules != CodeRules.String && rules != CodeRules.Char) { return TextRaw.Substring(GetIndexBegin(), Length); }
			StringBuilder sb = new StringBuilder();
			for (int i = tokenStart + 1; i < tokenStart + tokenCount - 1; ++i) {
				sb.Append(tokens[i].ToString());
			}
			return sb.ToString();
		}
		public void RemoveTokenRange(int index, int count) {
			if (count <= 0) return;
			List<Token> tok = tokens as List<Token>;
			if (tok != null) { tok.RemoveRange(index, count); } else {
				Token[] tArr = new Token[tokens.Count - count];
				int end = index + count, length = tokens.Count - end;
				for (int i = 0; i < index; ++i) { tArr[i] = tokens[i]; }
				for (int i = 0; i < length; ++i) { tArr[index + i] = tokens[end + i]; }
				tokens = new List<Token>(tArr);
			}
			tokenCount -= count;
		}
	}
}