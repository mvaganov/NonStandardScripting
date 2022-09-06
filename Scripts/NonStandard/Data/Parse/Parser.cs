using NonStandard.Extension;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NonStandard.Data.Parse {
	public class Parser {
		/// current data being parsed
		protected object memberValue;
		/// the object being parsed into, the final result
		public object result { get; protected set; }
		public object scope;
		/// the type that the result needs to be
		protected Type resultType;
		/// the type that the next value needs to be
		protected Type memberType;
		public ParserStackOfTokens tokenStack = new ParserStackOfTokens();
		protected Tokenizer tok;
		// for parsing a list
		protected List<object> listData;
		// for objects and dictionaries
		protected object memberId;
		protected Token memberToken;
		// for parsing an object
		protected MemberReflectionTable reflectTable = new MemberReflectionTable();
		protected FieldInfo field;
		protected PropertyInfo prop;
		// for parsing a dictionary
		protected KeyValuePair<Type, Type> dictionaryTypes;
		protected MethodInfo dictionaryAdd;

		public List<TokenLayerBeingParsed> _stackOfTokensBeingParsed => tokenStack._stackOfTokensBeingParsed;
		// TODO cache this value instead of calling it so much...
		internal TokenLayerBeingParsed Current => tokenStack.Current;//_stackOfTokensBeingParsed[_stackOfTokensBeingParsed.Count - 1];

		protected bool AdvanceToken() => SkipComments(true);
		protected bool SkipComments(bool incrementAtLeastOnce = false) {
			SyntaxTree syntax = incrementAtLeastOnce ? SyntaxTree.None : null;
			do {
				if (syntax != null && !tokenStack.Increment()) return false;
				syntax = Current.Token.GetAsSyntaxNode();
			} while (syntax != null && syntax.IsComment);
			return true;
		}

		public void SetResultType(Type type) {
			resultType = type;
			if (type != null) {
				reflectTable.SetType(type);
			}
		}

		protected Type SetResultType(string typeName) {
			Type t = Type.GetType(typeName);
			if (t == null) {
				Type[] childTypes = resultType.GetSubClasses();
				string[] typeNames = Array.ConvertAll(childTypes, ty => ty.ToString());
				char wildcard = ReflectionParseExtension.Wildcard;
				string nameSearch = typeName[0] != wildcard ? wildcard + typeName : typeName;
				int index = ReflectionParseExtension.FindIndexWithWildcard(typeNames, nameSearch, false);
				if (index >= 0) { t = childTypes[index]; }
			}
			if (t != null && (result == null || result.GetType() != t)) {
				SetResultType(t);
				result = resultType.GetNewInstance();
			}
			return t;
		}

		public bool CalculateResultType(Type type) {
			SetResultType(type);
			if (type != null) {
				memberType = type.GetIListType();
			}
			memberToken.Invalidate();
			if (memberType != null) {
				listData = new List<object>();
			} else {
				try {
					// explicitly parsing a primitive is sort of a poor use of the parser, _except_ when a script resolves to a primitive.
					//if (resultType.IsPrimitive || resultType == typeof(string)) {
					//	Show.Error("need to parse primitive! see TryGetValue()? "+resultType);
					//}
					if (result == null && !resultType.IsAbstract) { result = type.GetNewInstance(); }
				} catch (Exception e) {
					AddError("failed to create " + type + "\n" + e.ToString());
					return false;
				}
				dictionaryTypes = type.GetIDictionaryType();
				if (dictionaryTypes.Value != null) {
					memberType = dictionaryTypes.Value;
					dictionaryAdd = resultType.GetMethod("Add", new Type[] { dictionaryTypes.Key, dictionaryTypes.Value });
				}
			}
			return true;
		}

		public bool Init(Type type, List<Token> tokens, object dataStructure, Tokenizer tokenizer, object scope) {
			resultType = type;
			tok = tokenizer;
			_stackOfTokensBeingParsed.Clear();
			tokenStack.PushTokensBeingParsed(tokens);
			result = dataStructure;
			this.scope = scope;
			return CalculateResultType(type);
		}

		protected Type FindScriptedTypecast() {
			if (!Current.IsValid) return null;
			if (!SkipComments()) { AddError("failed skipping comment for initial type"); return null; }
			Token token = Current.Token;
			Delim d = token.GetAsDelimiter();
			if (d != null) {
				if (d.text == "=" || d.text == ":") {
					AdvanceToken();
					memberType = typeof(string);
					if (!TryGetValue()) { return null; }
					memberType = null;
					AdvanceToken();
					string typeName = memberValue.ToString();
					//Show.Log("looking for member type "+typeName);
					try {
						Type t = SetResultType(typeName);
						//Show.Log("internal type " + typeName + " (" + typeName + ")");
						if (t == null) { AddError("unknown type " + typeName); }
						return t;
					} catch (Exception e) {
						AddError(typeName + " failed to evaluate to a type:" + e);
						return null;
					}
				} else {
					AddError("unexpected beginning token " + d.text);
				}
			}
			return null;
		}
		public bool TryParse(Type targetType = null) {
			Token token = Current.Token;
			SyntaxTree syntax = token.GetAsSyntaxNode();
			if (IsCurrentTokenLayer(syntax)) { tokenStack.Increment(); } // skip past the opening bracket
			if (targetType != null) { SetResultType(targetType); }
			FindScriptedTypecast(); // first, check if this has a more correct internal type defined
			if (resultType == typeof(object)) {
				// if it has colons or equals signs, it's a Dictionary<string,object>
				bool hasNameBreaks = false;
				List<Token> tokens = Current.Tokens;
				for (int i = 0; i < tokens.Count; ++i) {
					string s = tokens[i].GetAsSmallText();
					if (s == ":" || s == "=") { hasNameBreaks = true; break; }
				}
				// if it has commas or generally more than one term, it's a List<object>
				if (hasNameBreaks) {
					result = new Dictionary<string, object>();
				} else {
					result = new List<object>();
				}
				CalculateResultType(result.GetType());
			}
			if (result == null && listData == null) {
				AddError("need specific " + resultType + ", eg: \"" + resultType.GetSubClasses().JoinToString("\", \"") + "\"");
				return false;
			}
			return TryParse_internal();
		}
		private bool IsCurrentTokenLayer(SyntaxTree syntax) => syntax != null && syntax.tokens == Current.Tokens;
		internal bool TryParse_internal() {
			if (!SkipComments()) { return true; }
			while (_stackOfTokensBeingParsed.Count > 0 && Current._currentTokenIndex < Current.Tokens.Count) {
				Token token = Current.Token;
				SyntaxTree syntax = token.GetAsSyntaxNode();
				if (IsCurrentTokenLayer(syntax)) {
					if (!token.IsContextEnding()) { AddError("unexpected state. we should never see this. ever."); }
					break;
				} // found the closing bracket!
				if (listData == null) {
					if (memberId == null) {
						if (!GetMemberNameAndAssociatedType()) { return false; }
					} else {
						if (!TryGetValue()) { return false; }
						if (!IsMemberParseCancelled) AssignValueToMember();
					}
				} else {
					if (!TryGetValue()) { return false; }
					if (!IsMemberParseCancelled) listData.Add(memberValue);
				}
				AdvanceToken();
			}
			FinalParseDataCompile();
			return true;
		}
		private bool IsMemberParseCancelled {
			get => memberValue == _stackOfTokensBeingParsed;
			set {
				if (value) { memberValue = _stackOfTokensBeingParsed; } else {
					throw new Exception("incorrect use.");
				}
			}
		}
		protected void FinalParseDataCompile() {
			if (listData == null) { return; }
			//result = ConvertIList(listData, resultType, memberType);
			object ilist = listData;
			if (!CodeConvert.TryConvertIList(ref ilist, resultType, memberType)) {
				throw new Exception("convert failed");
			}
			result = ilist;
		}
		protected bool GetMemberNameAndAssociatedType() {
			memberToken = Current.Token;
			if (SkipStructuredDelimiters(memberToken.GetAsDelimiter())) { memberToken.Invalidate(); return true; }
			memberId = null;
			SyntaxTree syntax = memberToken.GetAsSyntaxNode();
			if (syntax != null) {
				if (dictionaryAdd == null) {
					if (syntax.IsTextLiteral) {
						memberId = syntax.GetText();
					} else {
						AddError("unable to parse token (" + syntax.rules.name + "), expected member name for " + resultType);
					}
				} else {
					memberId = syntax.Resolve(tok, scope);// "dictionary member value will be resolved later";
				}
				if (IsCurrentTokenLayer(syntax)) {
					Current._currentTokenIndex += syntax.tokenCount - 1;
				}
			} else {
				memberId = memberToken.GetAsBasicToken();
			}
			if (memberId == null) {
				memberToken.index = -1;
				IsMemberParseCancelled = true;
				//memberValue = _stackOfTokensBeingParsed;
				return true;
			}
			memberValue = null;
			return CalculateMemberTypeBasedOnName();
		}
		protected bool CalculateMemberTypeBasedOnName() {
			if (dictionaryAdd != null) { return true; } // dictionary has no field to find
			string memberName = memberId as string;
			if (!reflectTable.TryGetMemberDetails(memberName, out memberType, out field, out prop)) {
				AddError("could not find \"" + memberName + "\" in " + result.GetType() +
					". possbile valid values: " + reflectTable);
				return false;
			}
			return true;
		}
		protected bool SkipStructuredDelimiters(Delim delim) {
			if (delim == null) return false;
			switch (delim.text) {
			// skip these delimiters as though they were whitespace.
			case "=": case ":": case ",": break;
			default:
				AddError("unexpected delimiter \"" + delim.text + "\"");
				return false;
			}
			IsMemberParseCancelled = true;
			//memberValue = _stackOfTokensBeingParsed;
			return true;
		}
		public static int AssignDictionaryMember(KeyValuePair<Type, Type> dType, MethodInfo dictionaryAddMethod,
			object dict, object key, object value) {
			if (!dType.Key.IsAssignableFrom(key.GetType())) { return 1; }
			if (!dType.Value.IsAssignableFrom(value.GetType())) { return 2; }
			dictionaryAddMethod.Invoke(dict, new object[] { key, value });
			return 0;
		}
		protected void AssignValueToMember() {
			if (memberValue != null) {
				switch (memberValue) {
				case SyntaxTree syntax:
					memberValue = syntax.Resolve(tok, scope);
					break;
				case Token t:
					if (memberType != typeof(Token)) {
						memberValue = t.Resolve(tok, scope);
					}
					break;
				}
			}
			if (dictionaryAdd != null) {
				string s = memberId as string;
				ReflectionParseExtension.TrySetValue_Dictionary(result, ref memberId, memberValue);
				//string error = AssignValueToDictionary(dictionaryTypes, dictionaryAdd, result, memberId, memberValue, memberType);
				//if (error != null) { AddError(error); }
			} else {
				if (field != null) {
					ReflectionParseExtension.TrySetValueCompiled(result, field, memberValue);
				} else if (prop != null) {
					ReflectionParseExtension.TrySetValueCompiled(result, prop, memberValue);
				} else {
					throw new Exception("huh? how did we get here?");
				}
				field = null; prop = null; memberType = dictionaryTypes.Value;
			}
			memberId = null;
			memberToken.Invalidate();
		}
		public static string AssignValueToDictionary(KeyValuePair<Type, Type> dictionaryTypes, MethodInfo dictionaryAdd, object result, object memberId, object memberValue, Type memberType) {
			try {
				switch (AssignDictionaryMember(dictionaryTypes, dictionaryAdd, result, memberId, memberValue)) {
				case 1: return ("unable to convert key \"" + memberId + "\" (" + memberId.GetType() + ") to " + dictionaryTypes.Key);
				case 2: return ("unable to convert \"" + memberId + "\" value (" + memberValue.GetType() + ") \"" + memberValue + "\" to type " + memberType);
				}
			} catch (Exception e) {
				return (memberId + " dictionaryAdd:" + e);
			}
			return null;
		}

		/// <summary>
		/// parse <see cref="memberType"/> out of the next token
		/// </summary>
		/// <returns>true if a <see cref="memberType"/> value was parsed into <see cref="memberValue"/> out of the next token</returns>
		protected bool TryGetValue() {
			memberValue = null;
			Token token = Current.Token;
			object meta = token.meta;
			if (SkipStructuredDelimiters(meta as Delim)) { return true; }
			// if we're looking for an unparsed token, we got it! lets go!
			if (memberType == typeof(Token)) { memberValue = token; return true; }
			switch (meta) {
			case SyntaxTree syntax: return TryGetValue_Syntax(token, syntax);
			case string basicString: return TryGetValue_String(token);
			case TokenSubstitution s: return TryGetValue_TokenSubstitution(token, s);
			case Delim d:
				if (memberType == typeof(object)) {
					memberValue = d;
					return true;
				}
				break;
			}
			AddError("unable to parse token with meta data " + meta);
			return false;
		}

		private bool TryGetValue_Syntax(Token token, SyntaxTree syntax) {
			bool subContextUsingSameList = IsCurrentTokenLayer(syntax);
			if (syntax.IsTextLiteral) {
				memberValue = syntax.GetText();
			} else {
				int index = Current._currentTokenIndex;
				List<Token> parseNext = subContextUsingSameList
						? Current.Tokens.GetRange(index, syntax.TokenCount)
						: syntax.tokens;
				if (memberType == typeof(Expression)) {
					Expression expr = new Expression(new List<Token>() { token });
					if (scope != null) {
						List<object> resolved = expr.Resolve(tok, scope);
						if (resolved.Count == 1) {
							switch (resolved[0]) {
							case string partiallyResolvedExpressionAsString:
								expr = new Expression(partiallyResolvedExpressionAsString);
								break;
							case Expression partiallyResolvedExpression:
								expr = partiallyResolvedExpression;
								break;
							}
						}
						//Show.Log(resolved.JoinToString() + " " + resolved[0].GetType()+ " >><< " + expr+ " " + expr.GetType());
					}
					memberValue = expr;
				} else {
					if (CodeConvert.IsConvertable(memberType)) {
						//Show.Log(memberId + " :: " + memberValue);
						memberValue = syntax.Resolve(tok, scope);
					} else {
						//Show.Log(memberId+" : "+memberValue);
						if (!CodeConvert.TryParseTokens(memberType, parseNext, ref memberValue, scope, tok)) { return false; }
					}
				}
			}
			if (subContextUsingSameList) {
				Current._currentTokenIndex += syntax.TokenCount - 1; // -1 because increment happens after this method
			}
			return true;
		}
		private bool TryGetValue_String(Token token) {
			//memberValue = token.ToString(s);
			CodeRules.op_ResolveToken(tok, token, scope, out memberValue, out Type _memberType);
			if (memberType == null || memberValue == null || (!memberType.IsAssignableFrom(memberValue.GetType()) && !CodeConvert.TryConvert(ref memberValue, memberType))) {
				AddError("unable to convert (" + memberValue + ") to type '" + memberType + "'");
				return false;
			}
			return true;
		}
		private bool TryGetValue_TokenSubstitution(Token token, TokenSubstitution sub) {
			memberValue = sub.value;
			if (!memberType.IsAssignableFrom(memberValue.GetType()) && !CodeConvert.TryConvert(ref memberValue, memberType)) {
				AddError("unable to convert substitution (" + memberValue + ") to type '" + memberType + "'");
				return false;
			}
			return true;
		}

		//public static bool TryConvert(Type memberType, List<Token> tokens, ParseRuleSet.Entry context, object scope, Tokenizer tok, ref object memberValue) {
		//	if (CodeConvert.IsConvertable(memberType)) {
		//		//Show.Log(memberId + " :: " + memberValue);
		//		memberValue = context.Resolve(tok, scope);
		//	} else {
		//		//Show.Log(memberId+" : "+memberValue);
		//		if (!CodeConvert.TryParseTokens(memberType, tokens, ref memberValue, scope, tok)) { return false; }
		//	}
		//	return true;
		//}

		protected void AddError(string message) { tok.AddError(Current.Token, message); }
	}

}
