using NonStandard.Extension;
using System;
using System.Collections.Generic;

namespace NonStandard.Data.Parse {
	public class DelimCtx : Delim {
		public ParseRuleSet Context {
			get {
				return foundContext != null ? foundContext : foundContext = ParseRuleSet.GetContext(contextName);
			}
		}
		private ParseRuleSet foundContext = null;
		public string contextName;
		public bool isStart, isEnd;
		public DelimCtx(string delim, string name = null, string desc = null, ParseRule parseRule = null,
			string ctx = null, bool start = false, bool end = false, SyntaxRequirement addReq = null, bool printable = true, bool breaking = true)
			: base(delim, name, desc, parseRule, addReq, printable, breaking) {
			contextName = ctx; isStart = start; isEnd = end;
		}
	}
	public class DelimOp : Delim {
		public delegate object TokenResolver(ITokenErrLog errLog, SyntaxTree syntax, object variableContext, ResolvedEnoughDelegate isItResolvedEnough);
		public delegate SyntaxTree SyntaxGenerator(Tokenizer tokenizer, List<Token> tokens, int index);

		public int order;
		public SyntaxGenerator isSyntaxValid = null;
		public TokenResolver resolve = null;
		public DelimOp(string delim, string name = null, string desc = null, ParseRule parseRule = null, SyntaxRequirement addReq = null, int order = 100, SyntaxGenerator syntax = null, TokenResolver resolve = null, bool printable = true, bool breaking = true)
			: base(delim, name, desc, parseRule, addReq, printable, breaking) {
			this.order = order; isSyntaxValid = syntax; this.resolve = resolve;
		}
	}
	public class Delim : IComparable<Delim> {
		public delegate bool SyntaxRequirement(string text, int index);
		public delegate ParseResult ParseRule(string text, int index);

		public string text, name, description;
		public ParseRule parseRule = null;
		public SyntaxRequirement extraReq = null;
		public bool printable = true;
		/// <summary>
		/// if not breaking, will ignore this delimiter if a token has started being parsed. For example, setting breaking to false for the delimiter "True" means "isItTrue" should be one token, instead of 2 ("isIt" and "True")
		/// </summary>
		public bool breaking = true;
		public Delim(string delim, string name = null, string desc = null, ParseRule parseRule = null, SyntaxRequirement addReq = null, bool printable = true, bool breaking = true) {
			text = delim; this.name = name; description = desc; this.parseRule = parseRule; extraReq = addReq; this.printable = printable; this.breaking = breaking;
		}
		/// <summary>
		/// checks if this delimiter is found in the given string, at the given index
		/// </summary>
		/// <param name="str"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsAt(string str, int index) {
			if (!str.IsSubstringAt(text, index)) { return false; }
			if (extraReq != null) { return extraReq.Invoke(str, index); }
			return true;
		}
		public override string ToString() { return printable?text:""; }
		public static implicit operator Delim(string s) { return new Delim(s); }

		public int CompareTo(Delim other) {
			int len = Math.Min(text.Length, other.text.Length);
			for (int i = 0; i < len; ++i) { int comp = text[i] - other.text[i]; if (comp != 0) return comp; }
			if (text.Length > other.text.Length) return -1;
			if (text.Length < other.text.Length) return 1;
			if (extraReq != null && other.extraReq == null) return -1;
			if (extraReq == null && other.extraReq != null) return 1;
			return 0;
		}
	}
}
