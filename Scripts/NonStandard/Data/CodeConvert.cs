// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using NonStandard.Data.Parse;
using NonStandard.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NonStandard.Data {
	public class CodeConvert {
		public delegate bool TryParseFunction(string text, out object result);

		public static Dictionary<Type, TryParseFunction> Deserialization =
			new Dictionary<Type, TryParseFunction>();
		public static string Stringify(object obj) {
			return StringifyExtension.Stringify(obj, false, showBoundary: false);
		}
		/// <summary>
		/// used to fill an already allocated object with data derived from a JSON-like script
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="text">JSON-like source text</param>
		/// <param name="data">output</param>
		/// <param name="scope">where to search for variables when resolving unescaped-string tokens</param>
		/// <param name="tokenizer">optional tokenizer, useful if you want to get errors</param>
		/// <returns></returns>
		public static bool TryFill<T>(string text, ref T data, object scope = null, Tokenizer tokenizer = null) {
			object value = data;
			bool result = TryParseType(typeof(T), text, ref value, scope, tokenizer);
			data = (T)value;
			return result;
		}
		/// <summary>
		/// used to parse JSON-like objects, and output the results to a new object made of nested <see cref="Dictionary{string, object}"/>, <see cref="List{object}"/>, and primitive value objects
		/// </summary>
		/// <param name="text">JSON-like source text</param>
		/// <param name="data">output</param>
		/// <param name="scope">where to search for variables when resolving unescaped-string tokens</param>
		/// <param name="tokenizer">optional tokenizer, useful if you want to get errors</param>
		/// <param name="parsingRules">rules used to parse. if null, will use Default rules. another example rule set: CommandLine</param>
		/// <returns></returns>
		public static bool TryParse(string text, out object data, object scope = null, Tokenizer tokenizer = null, ParseRuleSet parsingRules = null) {
			object value = null;
			bool result = TryParseType(typeof(object), text, ref value, scope, tokenizer, parsingRules);
			data = value;
			return result;
		}

		public static bool TryParse(string text, out object data) { return TryParse(text, out data, null, null, null); }

		/// <summary>
		/// used to parse JSON-like objects, and output the results to a new object made of nested <see cref="Dictionary{string, object}"/>, <see cref="List{object}"/>, and primitive value objects
		/// </summary>
		/// <param name="text">JSON-like source text</param>
		/// <param name="data">output</param>
		/// <param name="scope">where to search for variables when resolving unescaped-string tokens</param>
		/// <param name="tokenizer">optional tokenizer, useful if you want to get errors</param>
		/// <param name="parsingRules">rules used to parse. if null, will use Default rules. another example rule set: CommandLine</param>
		/// <returns>always a list of objects or null. if text would be a single object, it's in a list of size 1</returns>
		public static bool TryParseArgs(string text, out List<object> data, object scope = null, Tokenizer tokenizer = null, ParseRuleSet parsingRules = null) {
			bool result = TryParse(text, out object d, scope, tokenizer, parsingRules);
			if (!result) { data = null; return false; }
			switch (d) {
			case List<object> list: data = list; break;
			default:
				data = new List<object>();
				if (d is IList<object> ilist) {
					data.Capacity = ilist.Count;
					for (int i = 0; i < ilist.Count; ++i) { data.Add(ilist[i]); }
				} else {
					data.Add(d);
				}
				break;
			}
			return true;
		}
		/// <summary>
		/// used to compile an object out of JSON
		/// </summary>
		/// <typeparam name="T">the type being parsed</typeparam>
		/// <param name="text">JSON-like source text</param>
		/// <param name="data">output object</param>
		/// <param name="scope">where to search for variables when resolving unescaped-string tokens</param>
		/// <param name="tokenizer">optional tokenizer, useful if you want to get errors</param>
		/// <returns>true if data was parsed without error. any errors can be reviewed in the 'tokenizer' parameter</returns>
		public static bool TryParse<T>(string text, out T data, object scope = null, Tokenizer tokenizer = null, ParseRuleSet parseRules = null) {
			object value = null;
			bool result = TryParseType(typeof(T), text, ref value, scope, tokenizer, parseRules);
			data = (T)value;
			return result;
		}
		/// <summary>
		/// used to resolve tokens
		/// </summary>
		/// <param name="token"></param>
		/// <param name="tokenizer"></param>
		/// <param name="scope"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool TryParse(Token token, out object data, object scope, ITokenErrLog tokenizer) {
			CodeRules.op_ResolveToken(tokenizer, token, scope, out data, out Type resultType);
			return resultType != null;
		}
		/// <summary>
		/// used to parse when Type is known
		/// </summary>
		/// <param name="type"></param>
		/// <param name="text"></param>
		/// <param name="data"></param>
		/// <param name="scope"></param>
		/// <param name="tokenizer"></param>
		/// <returns></returns>
		public static bool TryParseType(Type type, string text, ref object data, object scope, Tokenizer tokenizer = null, ParseRuleSet parsingRules = null) {
			// object data wrapped by curly braces happens often enough that this is worth it for convenience.
			if (type.IsClass && text != null && text.StartsWith("{") && text.EndsWith("}") && !type.IsArray && type.GetIListType() == null) {
				text = text.Substring(1, text.Length - 2);
			}
			if (text == null || text.Trim().Length == 0) return false;
			try {
				if (tokenizer == null) { tokenizer = new Tokenizer(); }
				tokenizer.Tokenize(text, parsingRules);
			} catch(Exception e){
				tokenizer.AddError("Tokenize: " + e + "\n" + tokenizer.DebugPrint());
				return false;
			}
			//if(tokenizer.errors.Count > 0) { Show.Error(tokenizer.errors.JoinToString("\n")); }
			//Show.Log(Show.GetStack(4));
			//Show.Log(tokenizer.DebugPrint(-1));
			return TryParseTokens(type, tokenizer.tokens, ref data, scope, tokenizer);
		}
		public static bool TryParseTokens(Type type, List<Token> tokens, ref object data, object scope, Tokenizer tokenizer) {
			bool result = false;
			Parser p = new Parser();
			p.Init(type, tokens, data, tokenizer, scope);
			try {
				result = p.TryParse(type);
				data = p.result;
			} catch (Exception e) {
				tokenizer.AddError("TryParseTokens:" + e + "\n" + p.tokenStack.GetCurrentTokenIndex().JoinToString(", ") + "\n" + tokenizer.DebugPrint());
			}
			return result;
		}

		public static bool IsConvertable(Type typeToGet) {
			switch (Type.GetTypeCode(typeToGet)) {
			case TypeCode.Boolean:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Char:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Single:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Double:
			case TypeCode.String:
				return true;
			}
			return typeToGet.IsEnum;
		}
		/// <summary>
		/// does convert, will throw <see cref="FormatException"/> if convert fails
		/// </summary>
		/// <param name="value"></param>
		/// <param name="typeToConvertTo"></param>
		public static bool Convert(ref object value, Type typeToConvertTo) {
			if (!TryConvert(value, out object result, typeToConvertTo)) {
				value = result;
				//throw new FormatException("could not convert \"" + value + "\" to type " + typeToConvertTo);
				return false;
			}
			value = result;
			return true;
		}
		public static bool IsIntegral(Type t) {
			switch (Type.GetTypeCode(t)) {
			case TypeCode.Boolean:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Char:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return true;
			}
			return false;
		}
		public static bool IsNumeric(Type t) {
			switch (Type.GetTypeCode(t)) {
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Single:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Double:
				return true;
			}
			return false;
		}
		public static bool TryConvert<TYPE>(object value, out TYPE desiredValue) {
			Type typeToGet = typeof(TYPE);
			if (TryConvert(value, out object result, typeToGet)) {
				desiredValue = (TYPE)result;
				return true;
			}
			desiredValue = default;
			return false;
		}
		public static bool TryConvert(ref object value, Type typeToGet) {
			if (TryConvert(value, out object result, typeToGet)) {
				value = result;
				return true;
			}
			return false;
		}
		public static bool TryConvert(object value, out object desiredValue, Type typeToGet) {
		//public static bool TryConvert(ref object value, Type typeToGet) {
		//	if (value != null && value.GetType() == typeToGet) return true;
			if (value != null && value.GetType() == typeToGet) { desiredValue = value; return true; }
			desiredValue = default;
			try {
				if (typeToGet.IsEnum) {
					string str = value as string;
					if (str != null && ReflectionParseExtension.TryConvertEnumWildcard(typeToGet, str, out desiredValue)) {
						return true;
					}
					return false;
				}
				switch (Type.GetTypeCode(typeToGet)) {
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Single:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Double:
				case TypeCode.String:
					desiredValue = System.Convert.ChangeType(value, typeToGet);
					break;
				default:
					if (value is string s && Deserialization.TryGetValue(typeToGet, out TryParseFunction f)) {
						return f.Invoke(s, out desiredValue);
					}
					if (TryConvertIList(value, out desiredValue, typeToGet)) {
						return true;
					}
					return false;
				}
			} catch { return false; }
			return true;
		}
		public static bool TryConvertIList(ref object value, Type resultListType, Type resultElementType = null) {
			if(TryConvertIList(value, out object result, resultListType, resultElementType)) {
				value = result;
				return true;
			}
			return false;
		}
		public static bool TryConvertIList(object valueIn, out object valueOut, Type resultListType, Type resultElementType = null) {
			valueOut = null; 
			Type outputListElementType = resultElementType != null ? resultElementType : resultListType.GetIListType();
			if (outputListElementType == null) { return false; }
			IList ilist = (IList)valueIn;
			if (resultListType.IsArray) {
				//try {
					Array oArray = Array.CreateInstance(outputListElementType, ilist.Count);
					for (int i = 0; i < ilist.Count; ++i) {
						object element = ilist[i];
						if (outputListElementType.IsAssignableFrom(element.GetType()) || TryConvert(ref element, outputListElementType)) {
							oArray.SetValue(element, i);
						}
					}
					valueOut = oArray;
				//} catch (Exception e) {
				//	Show.Error("array creation:" + e);
				//	return false;
				//}
			} else if (resultListType.IsGenericType) {
				//try {
					object result = resultListType.GetNewInstance();
					IList olist = result as IList;
					for (int i = 0; i < ilist.Count; ++i) {
						object element = ilist[i];
						if (outputListElementType.IsAssignableFrom(element.GetType()) || TryConvert(ref element, outputListElementType)) {
							olist.Add(element);
						}
					}
					valueOut = olist;
				//} catch (Exception e) {
				//	Show.Error("List creation:" + e);
				//	return false;
				//}
			}
			return true;
		}

		public static string Format(string format, object scope, Tokenizer tokenizer = null) {
			if (tokenizer == null) { tokenizer = new Tokenizer(); }
			tokenizer.Tokenize(format, CodeRules.CodeInString);
			StringBuilder sb = new StringBuilder();
			for(int i = 0; i < tokenizer.tokens.Count; ++i) {
				object obj;
				Type type;
				Token token = tokenizer.tokens[i];
				CodeRules.op_ResolveToken(tokenizer, token, scope, out obj, out type);
				sb.Append(obj.ToString());
			}
			return sb.ToString();
		}
	}
}
