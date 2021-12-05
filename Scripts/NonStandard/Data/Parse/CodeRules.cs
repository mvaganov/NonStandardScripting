using NonStandard.Extension;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// TODO enable square braces to attempt to seek an array-element/array-sublist/table-element/object-element if possible, and use that behavior by default (a new list is explicitly started by a comma)
namespace NonStandard.Data.Parse {
	class CodeRules {

		public static ParseRuleSet
			String, Char, Number, Hexadecimal, Boolean, Expression, SquareBrace, GenericArgs, CodeBody,
			CodeInString, Sum, Difference, Product, Quotient, Modulus, Power, LogicalAnd, LogicalOr,
			Assignment, Equal, LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual, MembershipOperator,
			IfStatement, MaybeModifier, NotModifier, NotEqual, XmlCommentLine, CommentLine, CommentBlock, Default, CommandLine;

		public static Delim[] _string_delimiter = new Delim[] { new DelimCtx("\"", ctx: "string", start: true, end: true), };
		public static Delim[] _char_delimiter = new Delim[] { new DelimCtx("\'", ctx: "char", start: true, end: true), };
		public static Delim[] _char_escape_sequence = new Delim[] { new Delim("\\", parseRule: StringExtension.UnescapeStringSequenceAt) };
		public static Delim[] _expression_delimiter = new Delim[] {
			new DelimCtx("(", ctx: "()", start: true),
			new DelimCtx(")", ctx: "()", end: true)
		};
		public static Delim[] _code_body_delimiter = new Delim[] { new DelimCtx("{", ctx: "{}", start: true), new DelimCtx("}", ctx: "{}", end: true) };
		public static Delim[] _string_code_body_delimiter = new Delim[] {
			//new DelimCtx("\"", ctx: "codeInString", s: true, e: true), 
			new Delim("{{",parseRule:(str,i)=>new ParseResult(2,"{")),
			new Delim("}}",parseRule:(str,i)=>new ParseResult(2,"}")),
			new DelimCtx("{", ctx: "{}", start: true),
			new DelimCtx("}", ctx: "{}", end: true) };
		public static Delim[] _square_brace_delimiter = new Delim[] { new DelimCtx("[", ctx: "[]", start: true), new DelimCtx("]", ctx: "[]", end: true) };
		public static Delim[] _triangle_brace_delimiter = new Delim[] { new DelimCtx("<", ctx: "<>", start: true), new DelimCtx(">", ctx: "<>", end: true) };
		public static Delim[] _ternary_operator_delimiter = new Delim[] { "?", ":", "??" };
		public static Delim[] _instruction_finished_delimiter = new Delim[] { ";" };
		public static Delim[] _instruction_finished_delimiter_ignore_rest = new Delim[] { new Delim(";", parseRule: IgnoreTheRestOfThis) };
		public static Delim[] _list_item_delimiter = new Delim[] { "," };
		public static Delim[] _membership_operator = new Delim[] {
			new DelimOp(".", "member",syntax:CodeRules.syntax_mem,resolve:CodeRules.op_member, order:10),
			new DelimOp("->", "pointee",syntax:CodeRules.syntax_mem,resolve:CodeRules.op_member, order:11),
			new DelimOp("::", "scope resolution",syntax:CodeRules.syntax_mem,resolve:CodeRules.op_member, order:12),
			new DelimOp("?.", "null conditional",syntax:CodeRules.syntax_mem,resolve:CodeRules.op_member, order:13)
		};
		public static Delim[] _conditional_operator = new Delim[] {
			new DelimOp("if", "if statement",syntax:CodeRules.syntax_if_,resolve:CodeRules.op_if_statement, order:10, breaking:false),
		};
		public static Delim[] _maybe_operator = new Delim[] {
			new DelimOp("maybe", "maybe modifier",syntax:CodeRules.syntax_mayB,resolve:CodeRules.op_mayB, order:210, breaking:false),
		};
		public static Delim[] _not_operator = new Delim[] {
			new DelimOp("not", "not modifier",syntax:CodeRules.syntax_not,resolve:CodeRules.op_not, order:220, breaking:false),
		};
		public static Delim[] _prefix_unary_operator = new Delim[] { "++", "--", "!", "-", "~" };
		public static Delim[] _postfix_unary_operator = new Delim[] { "++", "--" };
		public static Delim[] _binary_operator = new Delim[] { "&", "|", "<<", ">>", "^" };
		// https://en.wikipedia.org/wiki/Order_of_operations#:~:text=In%20mathematics%20and%20computer%20programming,evaluate%20a%20given%20mathematical%20expression.
		public static Delim[] _binary_logic_operatpor = new DelimOp[] {
			new DelimOp("==",syntax:CodeRules.syntax_equ,resolve:CodeRules.op_equ, order:70),
			new DelimOp("!=",syntax:CodeRules.syntax_neq,resolve:CodeRules.op_neq, order:71),
			new DelimOp("<", syntax:CodeRules.syntax_lt_,resolve:CodeRules.op_lt_, order:60),
			new DelimOp(">", syntax:CodeRules.syntax_gt_,resolve:CodeRules.op_gt_, order:61),
			new DelimOp("<=",syntax:CodeRules.syntax_lte,resolve:CodeRules.op_lte, order:62),
			new DelimOp(">=",syntax:CodeRules.syntax_gte,resolve:CodeRules.op_gte, order:63),
			new DelimOp("&&",syntax:CodeRules.syntax_and,resolve:CodeRules.op_and, order:110),
			new DelimOp("||",syntax:CodeRules.syntax_or_,resolve:CodeRules.op_or_, order:120)
		};
		public static Delim[] _assignment_operator = new Delim[] { "+=", "-=", "*=", "/=", "%=", "|=", "&=", "<<=", ">>=", "??=", "=" };
		public static Delim[] _lambda_operator = new Delim[] { "=>" };
		public static Delim[] _math_operator = new DelimOp[] {
			new DelimOp("+", syntax:CodeRules.syntax_add,resolve:CodeRules.op_add, order:40),
			new DelimOp("-", syntax:CodeRules.syntax_dif,resolve:CodeRules.op_dif, order:41),
			new DelimOp("*", syntax:CodeRules.syntax_mul,resolve:CodeRules.op_mul, order:30),
			new DelimOp("/", syntax:CodeRules.syntax_div,resolve:CodeRules.op_div, order:31),
			new DelimOp("%", syntax:CodeRules.syntax_mod,resolve:CodeRules.op_mod, order:32),
			new DelimOp("^^",syntax:CodeRules.syntax_pow,resolve:CodeRules.op_pow, order:20),
		};
		public static Delim[] _hex_number_prefix = new Delim[] { new DelimCtx("0x", ctx: "0x", parseRule: StringExtension.HexadecimalParse) };
		public static Delim[] _number = new Delim[] {
			new DelimCtx("-",ctx:"number",parseRule:StringExtension.NumericParse,addReq:IsNextCharacterBase10NumericOrDecimal),
			new DelimCtx(".",ctx:"number",parseRule:StringExtension.NumericParse,addReq:IsNextCharacterBase10Numeric),
			new DelimCtx("0",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("1",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("2",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("3",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("4",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("5",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("6",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("7",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("8",ctx:"number",parseRule:StringExtension.NumericParse),
			new DelimCtx("9",ctx:"number",parseRule:StringExtension.NumericParse) };
		public static Delim[] _boolean = new Delim[] {
			new DelimCtx("False",ctx:"bool",parseRule:(str,i)=>new ParseResult(5,false),breaking:false),
			new DelimCtx("True",ctx:"bool",parseRule:(str,i)=>new ParseResult(4,true),breaking:false),
		};
		public static Delim[] _block_comment_delimiter = new Delim[] { new DelimCtx("/*", ctx: "/**/", start: true), new DelimCtx("*/", ctx: "/**/", end: true) };
		public static Delim[] _line_comment_delimiter = new Delim[] { new DelimCtx("//", ctx: "//", start: true) };
		public static Delim[] _XML_line_comment_delimiter = new Delim[] { new DelimCtx("///", ctx: "///", start: true) };
		public static Delim[] _end_of_line_comment = new Delim[] { new DelimCtx("\n", ctx: "//", end: true, printable: false), new DelimCtx("\r", ctx: "//", end: true, printable: false) };
		public static Delim[] _erroneous_end_of_string = new Delim[] { new DelimCtx("\n", ctx: "string", end: true, printable: false), new DelimCtx("\r", ctx: "string", end: true, printable: false) };
		public static Delim[] _end_of_XML_line_comment = new Delim[] { new DelimCtx("\n", ctx: "///", end: true, printable: false), new DelimCtx("\r", ctx: "///", end: true, printable: false) };
		public static Delim[] _line_comment_continuation = new Delim[] { new Delim("\\", parseRule: CommentEscape) };
		public static Delim[] _data_keyword = new Delim[] { "null", "true", "false", "bool", "int", "short", "string", "long", "byte",
			"float", "double", "uint", "ushort", "sbyte", "char", "if", "else", "void", "var", "new", "as", };
		public static Delim[] _data_c_sharp_keyword = new Delim[] {
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
			"const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
			"explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
			"implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
			"object", "operator", "out", "override", "params", "private", "protected", "public", "readonly",
			"ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
			"switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
			"ushort", "using", "virtual", "void", "volatile", "while"
		};
		public static Delim[] None = new Delim[] { };
		public static char[] StandardWhitespace = new char[] { ' ', '\t', '\n', '\r' };
		public static char[] WhitespaceNone = new char[] { };

		public static Delim[] CharLiteralDelimiters = CombineDelims(_char_escape_sequence, _char_delimiter);
		public static Delim[] StringLiteralDelimiters = CombineDelims(_char_escape_sequence, _string_delimiter, _erroneous_end_of_string);
		public static Delim[] StandardDelimiters = CombineDelims(_string_delimiter, _char_delimiter,
			_expression_delimiter, _code_body_delimiter, _square_brace_delimiter, _ternary_operator_delimiter,
			_instruction_finished_delimiter, _list_item_delimiter, _membership_operator, _prefix_unary_operator,
			_binary_operator, _binary_logic_operatpor, _assignment_operator, _lambda_operator, _math_operator,
			_block_comment_delimiter, _line_comment_delimiter, _number, _boolean, _conditional_operator, _maybe_operator, _not_operator);
		public static Delim[] CommandLineDelimiters = CombineDelims(_string_delimiter, _char_delimiter,
			_expression_delimiter, _code_body_delimiter, _square_brace_delimiter,//_ternary_operator_delimiter,
			_instruction_finished_delimiter, _list_item_delimiter, //_membership_operator, _prefix_unary_operator,
																   //_binary_operator, _binary_logic_operatpor, _assignment_operator, _lambda_operator, //_math_operator,
			_block_comment_delimiter, _line_comment_delimiter //, _conditional_operator
			);
		public static Delim[] LineCommentDelimiters = CombineDelims(_line_comment_continuation, _end_of_line_comment);
		public static Delim[] XmlCommentDelimiters = CombineDelims(_line_comment_continuation,
			_end_of_XML_line_comment);
		public static Delim[] CommentBlockDelimiters = CombineDelims(_block_comment_delimiter);

		static CodeRules() {
			Default = new ParseRuleSet("default", StandardDelimiters, StandardWhitespace);
			CommandLine = new ParseRuleSet("command line", CodeRules.CommandLineDelimiters, StandardWhitespace);
			String = new ParseRuleSet("string");
			Char = new ParseRuleSet("char");
			Number = new ParseRuleSet("number");
			Hexadecimal = new ParseRuleSet("0x");
			Boolean = new ParseRuleSet("bool");
			Expression = new ParseRuleSet("()");
			SquareBrace = new ParseRuleSet("[]");
			GenericArgs = new ParseRuleSet("<>");
			CodeBody = new ParseRuleSet("{}");
			CodeInString = new ParseRuleSet("codeInString");
			Sum = new ParseRuleSet("sum", CodeRules.None);
			Difference = new ParseRuleSet("difference", CodeRules.None);
			Product = new ParseRuleSet("product", CodeRules.None);
			Quotient = new ParseRuleSet("quotient", CodeRules.None);
			Modulus = new ParseRuleSet("modulus", CodeRules.None);
			Power = new ParseRuleSet("power", CodeRules.None);
			LogicalAnd = new ParseRuleSet("logical and", CodeRules.None);
			LogicalOr = new ParseRuleSet("logical or", CodeRules.None);
			Assignment = new ParseRuleSet("assignment", CodeRules.None);
			Equal = new ParseRuleSet("equal", CodeRules.None);
			LessThan = new ParseRuleSet("less than", CodeRules.None);
			GreaterThan = new ParseRuleSet("greater than", CodeRules.None);
			MembershipOperator = new ParseRuleSet("membership operator", CodeRules.None);
			IfStatement = new ParseRuleSet("if statement", CodeRules.None);
			MaybeModifier = new ParseRuleSet("maybe modifier", CodeRules.None);
			NotModifier = new ParseRuleSet("not modifier", CodeRules.None);
			LessThanOrEqual = new ParseRuleSet("less than or equal", CodeRules.None);
			GreaterThanOrEqual = new ParseRuleSet("greater than or equal", CodeRules.None);
			NotEqual = new ParseRuleSet("not equal", CodeRules.None);
			XmlCommentLine = new ParseRuleSet("///");
			CommentBlock = new ParseRuleSet("/**/");
			CommentLine = new ParseRuleSet("//");

			CodeInString.Delimiters = CombineDelims(_string_code_body_delimiter);
			CodeInString.Whitespace = CodeRules.WhitespaceNone;
			XmlCommentLine.Delimiters = CodeRules.XmlCommentDelimiters;
			CommentLine.Delimiters = CodeRules.LineCommentDelimiters;
			CommentBlock.Delimiters = CodeRules.CommentBlockDelimiters;
			CommentLine.Whitespace = CodeRules.WhitespaceNone;
			String.Whitespace = CodeRules.WhitespaceNone;
			String.Delimiters = CodeRules.StringLiteralDelimiters;
			//			Boolean.Delimiters = CombineDelims(_boolean);
			//Char.whitespace = CodeRules.WhitespaceNone;
			//Char.delimiters = CodeRules.StringLiteralDelimiters;
			Number.Whitespace = CodeRules.WhitespaceNone;
			XmlCommentLine.Simplify = CodeRules.SimplifyComment;
			CommentBlock.Simplify = CodeRules.SimplifyComment;
			CommentLine.Simplify = CodeRules.SimplifyComment;
			Expression.Simplify = CodeRules.SimplifySingleTermParenthesis;
			CodeBody.Simplify = CodeRules.SimplifySingleTermParenthesis;
			String.Simplify = CodeRules.SimplifyCompositeStringLiteral;
			Char.Simplify = CodeRules.SimplifyCompositeStringLiteral;

			Type t = typeof(CodeRules);
			MemberInfo[] mInfo = t.GetMembers();
			for (int i = 0; i < mInfo.Length; ++i) {
				MemberInfo mi = mInfo[i];
				FieldInfo fi = mi as FieldInfo;
				if (fi != null && mi.Name.StartsWith("_") && fi.FieldType == typeof(Delim[]) && fi.IsStatic) {
					Delim[] delims = fi.GetValue(null) as Delim[];
					GiveDesc(delims, fi.Name.Substring(1).Replace('_', ' '));
				}
			}

			//StringBuilder sb = new StringBuilder();
			//for (int i = 0; i < CodeRules.StandardDelimiters.Length; ++i) {
			//	sb.Append(CodeRules.StandardDelimiters[i].text + " " + CodeRules.StandardDelimiters[i].description).Append("\n");
			//}
			//Show.Log(sb);
		}
		public static bool IsNextCharacterBase10Numeric(string str, int index) {
			const int numberBase = 10;
			if (index < -1 || index + 1 >= str.Length) return false;
			int i = str[index + 1].ToNumericValue(numberBase); return (i >= 0 && i < numberBase);
		}
		public static bool IsNextCharacterBase10NumericOrDecimal(string str, int index) {
			const int numberBase = 10;
			if (index < -1 || index + 1 >= str.Length) return false;
			char c = str[index + 1]; if (c == '.') return true;
			int i = c.ToNumericValue(numberBase); return (i >= 0 && i < numberBase);
		}
		public static ParseResult CommentEscape(string str, int index) { return str.UnescapeStringSequenceAt(index); }

		private static void GiveDesc(Delim[] delims, string desc) {
			for (int i = 0; i < delims.Length; ++i) { if (delims[i].description == null) { delims[i].description = desc; } }
		}
		public static Delim[] CombineDelims(params Delim[][] delimGroups) {
			List<Delim> delims = new List<Delim>();
			for (int i = 0; i < delimGroups.Length; ++i) { delims.AddRange(delimGroups[i]); }
			delims.Sort();
			return delims.ToArray();
		}
		private static readonly string[] _if_operand_names = new string[] { "syntax", "conditional perand", "true case" };
		private static readonly string[] _ifelse_operand_names = new string[] { "syntax", "conditional perand", "true case", "else clause", "false case" };
		private static readonly string[] _binary_operand_names = new string[] { "left operand", "syntax", "right operand" };
		private static readonly string[] _unary_operand_names = new string[] { "syntax", "operand" };
		public static SyntaxTree syntax_IfStatementGeneral(List<Token> tokens, Tokenizer tok, int index, string contextName) {
			if (tokens.Count > index + 4 && tokens[index + 3].StringifySmall() == "else") {
				return syntax_GenericOp(tokens, tok, index, contextName, 0, _ifelse_operand_names);
			}
			// TODO if there is an else, use _ifelse_operand_names
			return syntax_GenericOp(tokens, tok, index, contextName, 0, _if_operand_names);
		}
		public static SyntaxTree syntax_Maybe(List<Token> tokens, Tokenizer tok, int index, string contextName) {
			return syntax_GenericOp(tokens, tok, index, contextName, 0, _unary_operand_names);
		}
		public static SyntaxTree syntax_Not(List<Token> tokens, Tokenizer tok, int index, string contextName) {
			return syntax_GenericOp(tokens, tok, index, contextName, 0, _unary_operand_names);
		}
		public static SyntaxTree syntax_BinaryOp(List<Token> tokens, Tokenizer tok, int index, string contextName) {
			return syntax_GenericOp(tokens, tok, index, contextName, -1, _binary_operand_names);
		}
		public static SyntaxTree syntax_GenericOp(List<Token> tokens, Tokenizer tok, int index, string contextName,
			int whereContextBegins, string[] operandNames) {
			Token t = tokens[index];
			SyntaxTree syntax = tokens[index].GetAsSyntaxNode();
			if (syntax != null) {
				if (syntax.rules.name != contextName) {
					throw new Exception(tok.AddError(t, "expected context: " + contextName + ", found " + syntax.rules.name).ToString());
				}
				return syntax;
			}
			for (int i = 0; i < operandNames.Length; ++i) {
				int id = whereContextBegins + i;
				if (id == 0) { continue; }
				id += index;
				if (id < 0 || id >= tokens.Count) { tok.AddError(t, "missing " + operandNames[i]); return null; }
			}
			ParseRuleSet foundContext = ParseRuleSet.GetContext(contextName);
			if (foundContext == null) { throw new Exception(tok.AddError(t, "context '" + contextName + "' does not exist").ToString()); }
			SyntaxTree parent = null; int pIndex;
			for (pIndex = 0; pIndex < tokens.Count; ++pIndex) {
				syntax = tokens[pIndex].GetAsSyntaxNode();
				if (syntax != null && syntax.tokens == tokens) { parent = syntax; break; }
			}
			if (pIndex == index) { throw new Exception(tok.AddError(t, "parent context recursion").ToString()); }
			syntax = foundContext.GetEntry(tokens, index + whereContextBegins, t.meta, parent);
			syntax.tokenCount = operandNames.Length;
			t.meta = syntax;
			tokens[index] = t;
			tok.ExtractContextAsSubTokenList(syntax);
			return syntax;
		}
		public static ParseResult IgnoreTheRestOfThis(string str, int index) { return new ParseResult(str.Length - index, null); }
		public static SyntaxTree syntax_add(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "sum"); }
		public static SyntaxTree syntax_dif(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "difference"); }
		public static SyntaxTree syntax_mul(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "product"); }
		public static SyntaxTree syntax_div(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "quotient"); }
		public static SyntaxTree syntax_mod(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "modulus"); }
		public static SyntaxTree syntax_pow(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "power"); }
		public static SyntaxTree syntax_and(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "logical and"); }
		public static SyntaxTree syntax_or_(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "logical or"); }
		public static SyntaxTree syntax_asn(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "assign"); }
		public static SyntaxTree syntax_equ(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "equal"); }
		public static SyntaxTree syntax_neq(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "not equal"); }
		public static SyntaxTree syntax_lt_(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "less than"); }
		public static SyntaxTree syntax_gt_(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "greater than"); }
		public static SyntaxTree syntax_lte(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "less than or equal"); }
		public static SyntaxTree syntax_gte(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "greater than or equal"); }
		public static SyntaxTree syntax_mem(Tokenizer tok, List<Token> tokens, int index) { return syntax_BinaryOp(tokens, tok, index, "membership operator"); }
		public static SyntaxTree syntax_if_(Tokenizer tok, List<Token> tokens, int index) { return syntax_IfStatementGeneral(tokens, tok, index, "if statement"); }
		public static SyntaxTree syntax_mayB(Tokenizer tok, List<Token> tokens, int index) { return syntax_Maybe(tokens, tok, index, "maybe modifier"); }
		public static SyntaxTree syntax_not(Tokenizer tok, List<Token> tokens, int index) { return syntax_Not(tokens, tok, index, "not modifier"); }

		public static object SimplifySingleTermParenthesis(List<object> terms) {
			if (terms != null) { switch (terms.Count) { case 0: return null; case 1: return terms[0]; } }
			return terms;
		}
		public static object SimplifyCompositeStringLiteral(List<object> terms) {
			StringBuilder sb = new StringBuilder();
			//if (terms.Count > 1 && terms.Count < 5) { Show.Log(terms.JoinToString()); }
			for (int i = 0; i < terms.Count; ++i) { sb.Append(terms[i].ToString()); }
			return sb.ToString();

		}
		public static object SimplifyComment(List<object> terms) { return null; }

		/// <param name="tok"></param>
		/// <param name="token"></param>
		/// <param name="scope"></param>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <param name="simplify">if true, and the result is a list with one item, the list is stripped away and the single item is returned</param>
		public static void op_ResolveToken(ITokenErrLog tok, Token token, object scope, out object value, out Type type, ResolvedEnoughDelegate isItResolvedEnough = null) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(token)) { value = token; type = token.GetType(); return; }
			//			Show.Log("resolving: " + token.ToString());
			value = token.Resolve(tok, scope, isItResolvedEnough);
			type = (value != null) ? value.GetType() : null;
			if (scope == null || type == null) { return; } // no scope, or no data, easy. we're done.
			string name = value as string;
			if (name == null) {  // data not a string (can't be a reference from scope), also easy. done.
				op_Resolve_SimplifyListOfArguments(tok, ref value, scope);
				return;
			}
			SyntaxTree syntax = token.GetAsSyntaxNode();
			if (syntax != null && syntax.IsTextLiteral) { return; } // data is explicitly meant to be a string, done.
			Type t;
			object v;
			if (op_SearchForMember(name, out v, out t, scope)) {
				value = v;
				type = t;
			}
		}
		public static bool op_SearchForMember(string name, out object value, out Type type, object scope) {
			switch (name) { // TODO can this switch be removed because the tokens are in CodeRules?
			case "null": value = null; type = null; return true;
			case "True": value = true; type = typeof(bool); return true;
			case "False": value = false; type = typeof(bool); return true;
			}
			type = typeof(string);
			if (ReflectionParseExtension.TryGetValue(scope, name, out value, out object _)) {
				type = (value != null) ? value.GetType() : null;
				return type != null;
			}
			return false;
		}
		public static bool op_Resolve_SimplifyListOfArguments(ITokenErrLog tok, ref object value, object scope) {
			List<object> args = value as List<object>;
			if (args != null) {
				for (int i = 0; i < args.Count; ++i) {
					bool remove = false;
					op_ResolveToken(tok, new Token(args[i], -1, -1), scope, out value, out Type type);
					switch (value as string) { case ",": remove = true; break; }
					if (remove) { args.RemoveAt(i--); } else { args[i] = value; }
				}
				value = args;
				return true;
			}
			return false;
		}

		public static void op_BinaryArgs(ITokenErrLog tok, SyntaxTree syntax, object scope, out object left, out object right, out Type lType, out Type rType) {
			SingleArgPreferDouble(tok, syntax.tokens[0], scope, out left, out lType, null);
			bool missingRightArgument = false;
			if (syntax.TokenCount >= 3 && syntax.Length > 3) {
				Token r = syntax.tokens[2];
				if (r.IsContextEnding()) { missingRightArgument = true; } // prevent a right-parenthesis from being read as a valid token
			} else {
				missingRightArgument = true;
			}
			if (missingRightArgument) {
				tok.AddError(syntax.tokens[1], "missing right argument for binary operator " + syntax.rules.name);
				right = null;
				rType = null;
				return;
			}
			SingleArgPreferDouble(tok, syntax.tokens[2], scope, out right, out rType, null);
		}
		public static void SingleArgPreferDouble(ITokenErrLog tok, Token token, object scope, out object value, out Type valType, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(token)) { value = token; valType = token.GetType(); return; }
			op_ResolveToken(tok, token, scope, out value, out valType, isItResolvedEnough);
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(value)) { return; }
			if (valType != typeof(string) && valType != typeof(double) && CodeConvert.IsConvertable(valType)) {
				CodeConvert.TryConvert(ref value, typeof(double)); valType = typeof(double);
			}
		}
		public static object op_asn(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			return "=";
		}
		public static object op_mul(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			do {
				bool lString = lType == typeof(string);
				bool rString = rType == typeof(string);
				// if one of them is a string, there is some string multiplication logic to do!
				if (lString != rString) {
					string meaningfulString;
					double meaningfulNumber;
					if (lString) {
						if (!CodeConvert.IsConvertable(rType)) { break; }
						meaningfulString = left.ToString();
						CodeConvert.TryConvert(ref right, typeof(double));
						meaningfulNumber = (double)right;
					} else {
						if (!CodeConvert.IsConvertable(lType)) { break; }
						meaningfulString = right.ToString();
						CodeConvert.TryConvert(ref left, typeof(double));
						meaningfulNumber = (double)left;
					}
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < meaningfulNumber; ++i) {
						sb.Append(meaningfulString);
					}
					meaningfulNumber -= (int)meaningfulNumber;
					int count = (int)(meaningfulString.Length * meaningfulNumber);
					if (count > 0) {
						sb.Append(meaningfulString.Substring(0, count));
					}
					return sb.ToString();
				}
				if (CodeConvert.IsConvertable(lType) && CodeConvert.IsConvertable(rType)) {
					CodeConvert.TryConvert(ref left, typeof(double));
					CodeConvert.TryConvert(ref right, typeof(double));
					return ((double)left) * ((double)right);
				}
			} while (false);
			tok.AddError(syntax.tokens[1], "unable to multiply " + lType + " and " + rType);
			return syntax;
		}
		public static object op_add(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			if (lType == typeof(string) || rType == typeof(string)) { return left.ToString() + right.ToString(); }
			if (CodeConvert.IsConvertable(lType) && CodeConvert.IsConvertable(rType)) {
				CodeConvert.TryConvert(ref left, typeof(double));
				CodeConvert.TryConvert(ref right, typeof(double));
				return ((double)left) + ((double)right);
			}
			tok.AddError(syntax.tokens[1], "unable to add " + lType + " and " + rType + " : " + left + " + " + right);
			return syntax;
		}
		public static object op_dif(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			do {
				if (lType == typeof(string) || rType == typeof(string)) { break; }
				if (CodeConvert.IsConvertable(lType) && CodeConvert.IsConvertable(rType)) {
					CodeConvert.TryConvert(ref left, typeof(double));
					CodeConvert.TryConvert(ref right, typeof(double));
					return ((double)left) - ((double)right);
				}
			} while (false);
			tok.AddError(syntax.tokens[1], "unable to subtract " + lType + " and " + rType + " : " + left + " - " + right);
			return syntax;
		}
		public static object op_div(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			do {
				if (lType == typeof(string) || rType == typeof(string)) { break; }
				if (CodeConvert.IsConvertable(lType) && CodeConvert.IsConvertable(rType)) {
					CodeConvert.TryConvert(ref left, typeof(double));
					CodeConvert.TryConvert(ref right, typeof(double));
					return ((double)left) / ((double)right);
				}
			} while (false);
			tok.AddError(syntax.tokens[1], "unable to divide " + lType + " and " + rType + " : " + left + " / " + right);
			return syntax;
		}
		public static int FindEndOfNextToken(ITokenErrLog tok, int startI, string str, int index, out int started, out int tokenId) {
			started = -1;
			tokenId = -1;
			for (int i = index; i < str.Length; ++i) {
				char c = str[i];
				switch (c) {
				case '{':
					if (i + 1 >= str.Length) {
						tok.AddError(startI + i, "unexpected end of format token" +
							(tokenId < 0 ? "" : (" " + tokenId.ToString())));
						return -1;
					}
					if (str[i + 1] != '{') {
						if (started >= 0) {
							tok.AddError(startI + i, "unexpected beginning of new format token" +
								(tokenId < 0 ? "" : (" " + tokenId.ToString())));
							return -1;
						} else {
							started = i;
							ParseResult pr = StringExtension.IntegerParse(str, i + 1);
							if (pr.IsError) {
								pr.error.OffsetBy(startI + i, tok.GetTextRows());
								tok.AddError(pr.error);
							} else {
								tokenId = (int)(long)pr.replacementValue;
							}
						}
					} else {
						++i;
					}
					break;
				case '}':
					if (started >= 0) {
						if (tokenId < 0) {
							tok.AddError(startI + i, "token missing leading base 10 integer index" +
								(tokenId < 0 ? "" : (" " + tokenId.ToString())));
						}
						return i + 1;
					}
					break;
				}
			}
			if (started >= 0) {
				tok.AddError(startI + started, "expected end of format token" +
					(tokenId < 0 ? "" : (" " + tokenId.ToString())));
			}
			return -1;
		}
		public static string Format(string format, List<object> args, object scope, ITokenErrLog tok, int tIndex) {
			StringBuilder sb = new StringBuilder();
			int index = 0, start, end, tokenId;
			do {
				end = FindEndOfNextToken(tok, tIndex, format, index, out start, out tokenId);
				if (end < 0) {
					sb.Append(format.Substring(index));
					index = format.Length;
				} else {
					if (tokenId < 0 || tokenId >= args.Count) {
						ParseError err = tok.AddError(tIndex, "invalid format token index (limit " +
							args.Count + ")" + (tokenId < 0 ? "" : (" " + tokenId.ToString()))); err.col += start;
						sb.Append(format.Substring(index, end - index));
					} else {
						if (index != start) { sb.Append(format.Substring(index, start - index)); }
						string str = format.Substring(start, end - start);
						str = str.Replace("{" + tokenId, "{" + 0);
						sb.AppendFormat(str, args[tokenId]);
					}
					index = end;
				}
			} while (index < format.Length);
			return sb.ToString();
		}
		public static object op_mod(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			do {
				if (lType == typeof(string)) {
					string format = left as string;
					List<object> args;
					if (rType != typeof(List<object>)) {
						args = new List<object>();
						args.Add(right);
					} else {
						args = right as List<object>;
					}
					return Format(format, args, scope, tok, syntax.tokens[0].index);
				}
				if (lType == typeof(string) || rType == typeof(string)) { break; }
				if (CodeConvert.IsConvertable(lType) && CodeConvert.IsConvertable(rType)) {
					CodeConvert.TryConvert(ref left, typeof(double));
					CodeConvert.TryConvert(ref right, typeof(double));
					return ((double)left) % ((double)right);
				}
			} while (false);
			tok.AddError(syntax.tokens[1], "unable to modulo " + lType + " and " + rType + " : " + left + " % " + right);
			return syntax;
		}
		public static object op_pow(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			do {
				if (lType == typeof(string) || rType == typeof(string)) { break; }
				if (CodeConvert.IsConvertable(lType) && CodeConvert.IsConvertable(rType)) {
					CodeConvert.TryConvert(ref left, typeof(double));
					CodeConvert.TryConvert(ref right, typeof(double));
					return Math.Pow((double)left, (double)right);
				}
			} while (false);
			tok.AddError(syntax.tokens[1], "unable to exponent " + lType + " and " + rType + " : " + left + " ^^ " + right);
			return syntax;
		}
		public static bool op_reduceToBoolean(object obj, Type type) {
			if (obj == null) return false;
			if (type == typeof(string)) return ((string)obj).Length != 0;
			if (!CodeConvert.TryConvert(ref obj, typeof(double))) { return true; }
			double d = (double)obj;
			return d != 0;
		}
		public struct DefaultString {
			public object str;
			public override string ToString() { return str.ToString(); }
			public DefaultString(object str) { this.str = str; }
		}
		
		/// <summary>
		/// returns false if the operand is false or an error
		/// </summary>
		/// <param name="tok"></param>
		/// <param name="syntax"></param>
		/// <param name="scope"></param>
		/// <param name="isItResolvedEnough"></param>
		/// <returns></returns>
		public static object op_mayB(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			Token operand = syntax.tokens[1];
			//string what = operand.ToString();
			//Show.Log(what);
			op_ResolveToken(tok, operand, scope, out object result, out Type cType);
			result = (result is bool tb && tb != false);
			if (result is bool f && f == false) {
				tok.ClearErrors();
			}
			return result;
		}
		public static object op_not(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			Token operand = syntax.tokens[1];
			op_ResolveToken(tok, operand, scope, out object result, out Type cType);
			if (result is bool boolean) {
				result = !boolean;
			}
			return result;
		}
		public static object op_if_statement(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object conditionResult, resultEvaluated; Type cType, rType;
			Token condition = syntax.tokens[1];
			Token resultToEvaluate = syntax.tokens[2];
			op_ResolveToken(tok, condition, scope, out conditionResult, out cType);
			//Show.Log("this is where the if statement gets parsed, and returns "+resultToEvaluate.StringifySmall()+" if "+condition.StringifySmall()+" is true");
			bool convertedToDouble = false;
			double td = 0;
			if (conditionResult != null && (
				(conditionResult is string ts && ts != "") ||
				(conditionResult is bool tb && tb != false) ||
				((convertedToDouble = CodeConvert.TryConvert(conditionResult, out td)) && td != 0)
			)) {
				op_ResolveToken(tok, resultToEvaluate, scope, out resultEvaluated, out rType, isItResolvedEnough);
				return resultEvaluated;
			}
			if (syntax.tokens.Count <= 4) { return null; }
			resultToEvaluate = syntax.tokens[4];
			if (conditionResult == null || 
				(conditionResult is string fs && fs == "") ||
				(conditionResult is bool fb && fb == false) ||
				((convertedToDouble || CodeConvert.TryConvert(conditionResult, out td)) && td == 0)
			) {
				op_ResolveToken(tok, resultToEvaluate, scope, out resultEvaluated, out rType, isItResolvedEnough);
				//Show.Log("resolved else: " + resultToEvaluate.StringifySmall()+" as "+resultEvaluated);
				return resultEvaluated;
			}
			//Show.Log(condition.StringifySmall()+" resolves to "+ conditionResult.StringifySmall() + ", which is neither True, nor False. what is it?");
			op_ResolveToken(tok, condition, scope, out conditionResult, out cType);
			//if (e.tokens.Count > 2 && e.tokens[3].GetAsBasicToken() == "else") {
			//	Show.Log("and it has an else to return if the condition failed");
			//}
			return null;
		}
		public static object op_member(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left; Type lType;
			SingleArgPreferDouble(tok, syntax.tokens[0], scope, out left, out lType, isItResolvedEnough); //op_BinaryArgs(tok, e, scope, out left, out right, out lType, out rType);
			//Show.Log(e.tokens[0].ToString()+ e.tokens[1].ToString()+ e.tokens[2].ToString()+"~~~"+ lType +" "+left+" . "+rType+" "+right);
			object variableName;
			SyntaxTree possiblyComplexSyntax = syntax.tokens[2].GetAsSyntaxNode();
			if (possiblyComplexSyntax != null) {
				variableName = possiblyComplexSyntax.Resolve(tok, scope, isItResolvedEnough);
			} else {
				variableName = syntax.tokens[2].ToString();
			}
			object val = ReflectionParseExtension.GetValue(left, variableName, new DefaultString(variableName));
			if (val == null || val is DefaultString) {
				SyntaxTree parentSyntax = syntax.GetParent();
				int index = parentSyntax.tokens.FindIndex(t => t.meta == syntax);
				if (index < 0) {
					throw new Exception("problem with parent syntax branch, it is missing it's child");
				}
				// look for the argument list right after this and use that to find the correct method from the list of possibilities
				SyntaxTree argSyntax = parentSyntax.tokens.Count > index + 2 ? parentSyntax.tokens[index + 1].GetAsSyntaxNode() : null;
				if (parentSyntax != null && argSyntax != null) {//ReflectionParseExtension.TryGetMethod(left, variableName.ToString(), out MethodInfo[] methods)) {
					//Token arguments = p.tokens[index + 1];
					//if (SyntaxTree.TryExecuteFunction(left, variableName, arguments, out object funcResult, tok, isItResolvedEnough)) {
					//	return funcResult;
					//}
					return new Invocation(left, variableName);
				}
				val = syntax.tokens[0].ToString()+syntax.tokens[1]+syntax.tokens[2];
			}
			return val;
		}
		public static object op_and(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			return op_reduceToBoolean(left, lType) && op_reduceToBoolean(right, rType);
		}
		public static object op_or_(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			return op_reduceToBoolean(left, lType) || op_reduceToBoolean(right, rType);
		}
		// spaceship operator
		public static bool op_Compare(ITokenErrLog tok, SyntaxTree syntax, object scope, out int compareValue) {
			object left, right; Type lType, rType;
			op_BinaryArgs(tok, syntax, scope, out left, out right, out lType, out rType);
			if (lType == rType) { return lType.TryCompare(left, right, out compareValue); }
			compareValue = 0;
			tok.AddError(syntax.tokens[1].index, "can't operate ("+lType+")"+left+" "+syntax.tokens[1]+" ("+rType+")"+right+" with scope("+scope+")");
			return false;
		}
		public static object op_equ(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			int comp; if (op_Compare(tok, syntax, scope, out comp)) { return comp == 0; }
			return syntax;
		}
		public static object op_neq(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			int comp; if (op_Compare(tok, syntax, scope, out comp)) { return comp != 0; }
			return syntax;
		}
		public static object op_lt_(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			int comp; if (op_Compare(tok, syntax, scope, out comp)) { return comp < 0; }
			return syntax;
		}
		public static object op_gt_(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			int comp; if (op_Compare(tok, syntax, scope, out comp)) { return comp > 0; }
			return syntax;
		}
		public static object op_lte(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			int comp; if (op_Compare(tok, syntax, scope, out comp)) { return comp <= 0; }
			return syntax;
		}
		public static object op_gte(ITokenErrLog tok, SyntaxTree syntax, object scope, ResolvedEnoughDelegate isItResolvedEnough) {
			if (isItResolvedEnough != null && isItResolvedEnough.Invoke(syntax)) { return syntax; }
			int comp; if (op_Compare(tok, syntax, scope, out comp)) { return comp >= 0; }
			return syntax;
		}
	}
}
