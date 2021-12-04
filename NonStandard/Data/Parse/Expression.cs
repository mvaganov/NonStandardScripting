using System;
using System.Collections.Generic;

namespace NonStandard.Data.Parse {
	public class Expression {
		private List<Token> tokens;
		public Expression(List<Token> tokens) { this.tokens = tokens; }
		public Expression(string textToTokenize, Tokenizer tok = null) {
			if (tok == null) { tok = new Tokenizer(); }
			tok.Tokenize(textToTokenize);
			tokens = tok.tokens;
		}
		public override string ToString() {
			return SyntaxTree.PrintAll(tokens);
		}
		public string Stringify() { return ToString(); }
		public string DebugPrint(int depth = 0, string indent = "  ", string separator = ", ") {
			return Tokenizer.DebugPrint(tokens, depth, indent, separator);
		}
		public List<object> Resolve(ITokenErrLog errLog, object scope = null) {
			List<object> results = new List<object>();
			SyntaxTree.ResolveTerms(errLog, scope, tokens, 0, tokens.Count, results);
			return results;
		}

		public bool TryResolve<T>(out T value, Tokenizer tok, object scope = null) {
			List<object> results = new List<object>();
			//Show.Log(Tokenizer.DebugPrint(tokens));
			SyntaxTree.ResolveTerms(tok, scope, tokens, 0, tokens.Count, results);
			//Show.Log(results.Join("]["));
			if(results == null || results.Count != 1) {
				tok.AddError(-1, "missing results");
				value = default(T); return false;
			}
			object obj = results[0];
			if(obj.GetType() == typeof(T)) { value = (T)obj; return true; }
			if(!CodeConvert.TryConvert(ref obj, typeof(T))) {
				if(obj.GetType() == typeof(string)) {
					Expression deepExpression = new Expression(obj as string, tok);
					return deepExpression.TryResolve(out value, tok, scope);
				}
				tok.AddError("unable to parse ("+obj.GetType()+")\""+obj+"\" as " + typeof(T).ToString());
				value = default(T);
				return false;
			}
			value = (T)obj;
			return true;
		}
	}
}
