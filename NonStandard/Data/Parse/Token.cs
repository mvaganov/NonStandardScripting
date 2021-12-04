using NonStandard.Extension;
using System;
using System.Collections.Generic;
using System.Text;

namespace NonStandard.Data.Parse {
	public struct Token : IEquatable<Token>, IComparable<Token> {
		public int index, length; // 32 bits x2
		/// <summary>
		/// if this field is a different type, it changes the behavior of this class in dramatic and meaningful ways
		/// <see cref="string"/> - means this is some kind of basic string, either a string literal, or an unescaped 'probably a string' situation. these could be a variable token that should be resolved later, when better context is given
		/// <see cref="SyntaxTree"/> - means this token is a container of other tokens. things inside of parenthesis, square braces, quotes, etc. binary operators also fall into this category.
		/// <see cref="Delim"/> - means this token is a standard piece of syntax, like a constant, or a default type
		/// <see cref="TokenSubstitution"/> - means this token should have it's value semantically replaced by <see cref="TokenSubstitution.value"/>, even though it is literally a sequence of characters. used when resolving alphanumeric tokens into numbers, enums, or constants
		/// </summary>
		public object meta; // 64 bits
		public Token(object meta, int i, int len) { this.meta = meta; index = i; length = len; }
		public Token(string text) { this.meta = text; index = 0; length = text.Length; }
		private static Token _None = new Token(null, -1, -1);
		public static Token None => _None;
		/// <summary>
		/// a token can be valid without meta data, as a simple marker. but if it has invalid marks, and no data, it's bad.
		/// </summary>
		public bool IsValid { get { return index >= 0 && length >= 0; } }
		public bool IsSyntax => meta != null && meta.GetType() == typeof(SyntaxTree);
		public bool IsSyntaxBoundary {
			get {
				/// <see cref="SyntaxTree"> boundaries, like "(" and ")" or "\"" or "{" and "}" have a valid index and length
				if (!IsValid) return false;
				/// and point at a <see cref="SyntaxTree"/> instead of at a string or <see cref="Delim"/> or <see cref="TokenSubstitution"/>
				return IsSyntax;// meta != null && meta.GetType() == typeof(SyntaxTree);
			}
		}
		public bool IsSimpleString { get { return index >= 0 && length >= 0 && 
					(meta is string || meta is SyntaxTree syntax && syntax.TextRaw != null); } }
		public bool IsDelim { get { return meta is Delim; } }
		public int GetBeginIndex() { return index; }
		public int GetEndIndex() { return index + length; }
		public string ToString(string s) { return s.Substring(index, length); }

		public string Stringify() { return GetAsSmallText(); }
		public override string ToString() { return GetAsBasicToken(); }
		public string ToDebugString() {
			SyntaxTree syntax = meta as SyntaxTree;
			if (syntax == null) { return Resolve(null, null).ToString(); }
			Delim d = syntax.sourceMeta as Delim;
			if(d != null) { return d.ToString(); }
			if(IsValid) return ToString(syntax.TextRaw);
			string output = syntax.rules.name;
			if (syntax.IsTextLiteral) {
				output += "(" + syntax.GetText() + ")";
			}
			return output;
		}
		public void DebugOut(StringBuilder sb = null, int depth = 0, List<Token> recursionGuard = null) {
			if (recursionGuard == null) { recursionGuard = new List<Token>(); }
			if (recursionGuard.Contains(this)) { return; }
			recursionGuard.Add(this);
			if (sb == null) { sb = new StringBuilder(); }
			sb.Append(StringExtension.Indentation(depth, "  "));
			sb.Append(meta.GetType().ToString() + ":" + GetAsSmallText()+"@"+index);
			SyntaxTree syntax = meta as SyntaxTree;
			if (syntax != null) {
				sb.Append(syntax.rules.name);
			}
			sb.Append("\n");
			if (syntax != null) {
				for (int i = 0; i < syntax.TokenCount; ++i) {
					Token t = syntax.GetToken(i);
					if (t.index == index && t.meta.GetType() == meta.GetType()) continue;
					t.DebugOut(sb, depth + 1, recursionGuard);
				}
			}
		}
		/// <summary>
		/// gathers a linear list of the tokens contained in this token
		/// </summary>
		public void FlattenInto(List<Token> tokens) {
			if (tokens.Contains(this)) { return; }
			tokens.Add(this);
			SyntaxTree syntax = meta as SyntaxTree;
			if(syntax != null) {
				for(int i = 0; i < syntax.TokenCount; ++i) {
					Token t = syntax.GetToken(i);
					// binary operators insert a copy of themselves (but not themselves exactly) as the middle of 3 tokens
					if (t.index == index && t.meta.GetType() == meta.GetType()) { continue; }
					t.FlattenInto(tokens);
				}
			}
		}
		public object Resolve(ITokenErrLog tok, object scope, ResolvedEnoughDelegate isItResolvedEnough = null) {
			if (isItResolvedEnough != null && isItResolvedEnough(this)) return this;
			if (index == -1 && length == -1) return meta;
			if (meta == null) throw new NullReferenceException("can't resolve NULL token");
			switch (meta) {
			case string s: {
				string str = ToString(s);
				//Show.Log("@@@  "+str+" "+scope);
				if (scope != null && (isItResolvedEnough == null || isItResolvedEnough.Invoke(str))) {
					if (CodeRules.op_SearchForMember(str, out object value, out Type type, scope)) {
						//Show.Log(str+" "+foundIt+" "+value);
						return value;
					}
				}
				return str;
			}
			case TokenSubstitution ss: return ss.value;
			case Delim d: return d.text;
			case SyntaxTree pce: return pce.Resolve(tok, scope, isItResolvedEnough);
			}
			throw new DecoderFallbackException();
		}
		public string ResolveString(ITokenErrLog tok, object scope, ResolvedEnoughDelegate isItResolvedEnough = null) {
			object result = Resolve(tok, scope, isItResolvedEnough);
			if (result == null) return null;
			return result.ToString();
		}
		public string GetAsSmallText() {
			SyntaxTree e = GetAsSyntaxNode();
			if (e != null) {
				if (IsContextBeginning()) { return e.beginDelim.ToString(); }
				if (IsContextEnding()) { return e.endDelim.ToString(); }
				return e.TextEnclosed;
			}
			return ToDebugString();
		}
		public string GetAsBasicToken() {
			string src = null;
			int len = length, beginIndex = index;
			switch (meta) {
			case string s: src = s; break;
			case Delim d: return d.text;
			case TokenSubstitution ts: return ts.value.ToString();
			case SyntaxTree syntax:
				Token end = syntax.GetEndToken();
				Token begin = syntax.GetBeginToken();
				//if (len < 0) {
				beginIndex = begin.index;
				string endTokenText = end.GetAsSmallText();
				len = end.index + endTokenText.Length - beginIndex;
				//}
				src = begin.meta as string;
				if (src == null) { src = end.meta as string; }
				if (src != null) { break; }
				while (src == null && syntax != null) {
					if (syntax.sourceMeta is string es) {
						src = es;
						break;
					}
					syntax = syntax.GetParent();
				}
				break;
			}
			if (src != null) { return src.Substring(beginIndex, len); }
			return null;
		}
		public Delim GetAsDelimiter() { return meta as Delim; }
		public SyntaxTree GetAsSyntaxNode() { return meta as SyntaxTree; }
		public List<Token> GetTokenSublist() {
			SyntaxTree e = GetAsSyntaxNode();
			if(e != null) {
				return e.tokens;
			}
			return null;
		}
		public bool IsContextBeginning() {
			SyntaxTree ctx = GetAsSyntaxNode(); if (ctx != null) { return ctx.GetBeginToken() == this; }
			return false;
		}
		public bool IsContextEnding() {
			SyntaxTree ctx = GetAsSyntaxNode(); if (ctx != null) { return ctx.GetEndToken() == this; }
			return false;
		}
		public bool IsContextBeginningOrEnding() {
			SyntaxTree ctx = GetAsSyntaxNode();
			if (ctx != null) { return ctx.GetEndToken() == this || ctx.GetBeginToken() == this; }
			return false;
		}
		public void Invalidate() { length = -1; }
		public bool Equals(Token other) { return index == other.index && length == other.length && meta == other.meta; }
		public override bool Equals(object obj) { if (obj is Token) return Equals((Token)obj); return false; }
		public override int GetHashCode() { return meta.GetHashCode() ^ index ^ length; }
		public int CompareTo(Token other) {
			int comp = index.CompareTo(other.index);
			if (comp != 0) return comp;
			return -length.CompareTo(other.length); // bigger one should go first
		}
		public static bool operator ==(Token lhs, Token rhs) { return lhs.Equals(rhs); }
		public static bool operator !=(Token lhs, Token rhs) { return !lhs.Equals(rhs); }
	}
	public class TokenSubstitution {
		public string origMeta; public object value;
		public TokenSubstitution(string o, object v) { origMeta = o; value = v; }
	}
}
