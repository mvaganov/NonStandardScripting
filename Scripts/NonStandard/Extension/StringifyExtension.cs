using NonStandard.Data.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NonStandard.Extension {
	public static class StringifyExtension {
		public static string JoinToString<T>(this IList<T> source, string separator = ", ", Func<T, string> toString = null) {
			string[] strings = new string[source.Count];
			if (toString == null) { toString = o => o != null ? o.ToString() : ""; }
			for (int i = 0; i < strings.Length; ++i) { strings[i] = toString.Invoke(source[i]); }
			return string.Join(separator, strings);
		}
		public static void JoinToString<T>(this IList<T> source, StringBuilder sb, string separator, Func<T, string> toString = null) {
			if (toString == null) { toString = o => o.ToString(); }
			bool somethingPrinted = false;
			for (int i = 0; i < source.Count; ++i) {
				if (source[i] != null) {
					if (somethingPrinted) sb.Append(separator);
					sb.Append(toString.Invoke(source[i]));
				}
			}
		}
		public static string JoinToString<T>(this IEnumerable<T> source, string separator = ", ", Func<T, string> toString = null) {
			List<string> strings = new List<string>();
			if (toString == null) { toString = o => o.ToString(); }
			IEnumerator<T> e = source.GetEnumerator();
			while (e.MoveNext()) { strings.Add(toString.Invoke(e.Current)); }
			e.Dispose();
			return strings.JoinToString(separator);
		}

		/// <summary>
		/// the way that Stringify writes a dictionary's type 
		/// </summary>
		public static string StringifyTypeOfDictionary(Type t) { return "=\"" + t.ToString() + "\""; }

		public static string StringifySmall(this object obj) {
			return Stringify(obj, pretty: false, showNulls: true);
		}
		/// <summary>
		/// stringifies an object using custom NonStandard rules
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="pretty"></param>
		/// <param name="showType">include "=TypeName" if there could be ambiguity because of inheritance</param>
		/// <param name="depth"></param>
		/// <param name="rStack">used to prevent recursion stack overflows</param>
		/// <param name="filter">object0 is the object, object1 is the member, object2 is the value. if it returns null, print as usual. if returns "", skip print.</param>
		/// <param name="howToMarkStrongType">if null, use <see cref="StringifyTypeOfDictionary"/>, which is expected by <see cref="NonStandard.Data.CodeConvert.TryParse"/></param>
		/// <returns></returns>
		public static string Stringify(this object obj, bool pretty = true, bool showType = true, bool showNulls = false, bool showBoundary = true, int depth = 0, List<object> rStack = null, KeyValuePairToStringFilter filter = null, Func<Type, string> howToMarkStrongType = null) {
			if (obj == null) return showNulls ? "null" : "";
			if (filter != null) { string res = filter.Invoke(obj, null, null); if (res != null) { return res; } }
			Type t = obj.GetType();
			MethodInfo stringifyMethod = t.GetMethod("Stringify", Type.EmptyTypes);
			if (stringifyMethod != null) { return stringifyMethod.Invoke(obj, Array.Empty<object>()) as string; }
			StringBuilder sb = new StringBuilder();
			bool showTypeHere = showType; // no need to print type if there isn't type ambiguity
			if (showType) {
				Type b = t.BaseType; // if the parent class is a base class, there isn't any ambiguity
				if (b == typeof(ValueType) || b == typeof(System.Object) || b == typeof(Array) ||
					t.GetCustomAttributes(false).FindIndex(o => o.GetType() == typeof(StringifyHideTypeAttribute)) >= 0) { showTypeHere = false; }
			}
			if (obj is string || t.IsPrimitive || t.IsEnum) {
				return StringifiedSimple(sb, obj, showBoundary).ToString();
			}
			if (rStack == null) { rStack = new List<object>(); }
			int recursionIndex = rStack.IndexOf(obj);
			if (recursionIndex >= 0) {
				return sb.Append("/* recursed " + (rStack.Count - recursionIndex) + " */").ToString();
			}
			rStack.Add(obj);
			Type listT = t.GetIListType();
			if (t.IsArray || listT != null) {
				StringifiedList(sb, obj, listT, depth, howToMarkStrongType, filter, rStack, pretty, showBoundary, showType, showTypeHere, showNulls);
			} else {
				StringifiedObject(sb, t, obj, depth, howToMarkStrongType, filter, rStack, pretty, showBoundary, showType, showTypeHere, showNulls);
			}
			if (sb.Length == 0) { sb.Append(obj.ToString()); }
			return sb.ToString();
		}

		private static void AppendType(StringBuilder sb, object obj, int depth, Func<Type, string> howToMarkStrongType, bool pretty) {
			if (pretty) { sb.Append("\n" + StringExtension.Indentation(depth + 1)); }
			if (howToMarkStrongType == null) { howToMarkStrongType = StringifyTypeOfDictionary; }
			sb.Append(howToMarkStrongType(obj.GetType()));
		}

		private static StringBuilder StringifiedSimple(StringBuilder sb, object obj, bool showBoundary) {
			string s = obj as string;
			if (s == null) {
				sb.Append(obj.ToString());
				return sb;
			}
			if (!showBoundary) { showBoundary |= s.ContainsNonAlphaCharacters(); }
			if (showBoundary) { sb.Append("\""); }
			sb.Append(s.Escape());
			if (showBoundary) { sb.Append("\""); }
			return sb;
		}

		private static void StringifiedList(StringBuilder sb, object obj, Type iListElement, int depth, Func<Type, string> howToMarkStrongType, KeyValuePairToStringFilter filter, List<object> rStack,
			bool pretty, bool showBoundary, bool showType, bool showTypeHere, bool showNulls) {
			if (showBoundary) sb.Append("[");
			if (showTypeHere) { AppendType(sb, obj, depth, howToMarkStrongType, pretty); }
			IList list = obj as IList;
			for (int i = 0; i < list.Count; ++i) {
				if (!showNulls && list[i] == null) continue;
				if (i > 0) { sb.Append(","); }
				if (pretty && !iListElement.IsPrimitive) { sb.Append("\n" + StringExtension.Indentation(depth + 1)); }
				if (filter == null) {
					sb.Append(Stringify(list[i], pretty, showType, showNulls, true, depth + 1, rStack));
				} else {
					FilterElement(sb, obj, i, list[i], pretty, showType, showNulls, true, depth, rStack, filter);
				}
			}
			if (pretty) { sb.Append("\n" + StringExtension.Indentation(depth)); }
			if (showBoundary) sb.Append("]");
		}
		private static void StringifiedObject(StringBuilder sb, Type t, object obj, int depth, Func<Type, string> howToMarkStrongType, KeyValuePairToStringFilter filter, List<object> rStack, bool pretty, bool showBoundary, bool showType, bool showTypeHere, bool showNulls) {
			KeyValuePair<Type, Type> kvp = t.GetIDictionaryType();
			bool isDict = kvp.Key != null;
			if (showBoundary) sb.Append("{");
			if (showTypeHere) { AppendType(sb, obj, depth, howToMarkStrongType, pretty); }
			if (!isDict) {
				AppendStringifiedObjectReflection(sb, t, obj, depth, howToMarkStrongType, filter, rStack, pretty, showType, showTypeHere, showNulls);
			} else {
				AppendStringifiedDictionary(sb, t, obj, depth, filter, rStack, pretty, showType, showTypeHere, showNulls);
			}
			if (pretty) { sb.Append("\n" + StringExtension.Indentation(depth)); }
			if (showBoundary) sb.Append("}");
		}
		private static void AppendStringifiedDictionary(StringBuilder sb, Type t, object obj, int depth, KeyValuePairToStringFilter filter, List<object> rStack, bool pretty, bool showType, bool showTypeHere, bool showNulls) {
			MethodInfo getEnum = t.GetMethod("GetEnumerator", new Type[] { });
			MethodInfo getKey = null, getVal = null;
			object[] noparams = Array.Empty<object>();
			IEnumerator e = getEnum.Invoke(obj, noparams) as IEnumerator;
			bool printed = false;
			while (e.MoveNext()) {
				object o = e.Current;
				if (getKey == null) { getKey = o.GetType().GetProperty("Key").GetGetMethod(); }
				if (getVal == null) { getVal = o.GetType().GetProperty("Value").GetGetMethod(); }
				if (printed || showTypeHere) { sb.Append(","); }
				if (pretty) { sb.Append("\n" + StringExtension.Indentation(depth + 1)); }
				object k = getKey.Invoke(o, noparams);
				object v = getVal.Invoke(o, noparams);
				if (!showNulls && v == null) { continue; }
				if (filter == null) {
					string keyToString = k.ToString();
					if (k is string && (keyToString.ContainsNonAlphaCharacters() || CodeRules.Default.GetDelimiter(keyToString) != null)) {
						keyToString = keyToString.StringifySmall();
					}
					sb.Append(keyToString).Append(pretty ? " : " : ":");
					sb.Append(Stringify(v, pretty, showType, showNulls, showBoundary:true, depth + 1, rStack));
					printed = true;
				} else {
					printed = FilterElement(sb, obj, k, v, pretty, showType, showNulls, false, depth, rStack, filter);
				}
			}
		}
		private static void AppendStringifiedObjectReflection(StringBuilder sb, Type t, object obj, int depth, Func<Type, string> howToMarkStrongType, KeyValuePairToStringFilter filter, List<object> rStack,
			bool pretty, bool showType, bool showTypeHere, bool showNulls) {
			FieldInfo[] fi = t.GetFields();
			for (int i = 0; i < fi.Length; ++i) {
				object val = fi[i].GetValue(obj);
				if (!showNulls && val == null) continue;
				if (i > 0 || showTypeHere) { sb.Append(","); }
				if (pretty) { sb.Append("\n" + StringExtension.Indentation(depth + 1)); }
				if (filter == null) {
					sb.Append(fi[i].Name).Append(pretty ? " : " : ":");
					sb.Append(Stringify(val, pretty, showType, showNulls, true, depth + 1, rStack));
				} else {
					FilterElement(sb, obj, fi[i].Name, val,
						pretty, showType, showNulls, false, depth, rStack, filter);
				}
			}
		}
		public delegate string KeyValuePairToStringFilter(object obj, object key, object value);
		private static bool FilterElement(StringBuilder sb, object obj, object key, object val,
			bool pretty, bool includeType, bool showNulls, bool isArray, int depth, List<object> recursionStack,
			KeyValuePairToStringFilter filter = null) {
			bool unfiltered = true;
			if (filter != null) {
				string result = filter.Invoke(obj, key, val);
				unfiltered = result == null;
				if (!unfiltered && result.Length != 0) { sb.Append(result); return true; }
			}
			if (unfiltered) {
				if (!isArray) { sb.Append(key).Append(pretty ? " : " : ":"); }
				sb.Append(Stringify(val, pretty, includeType, showNulls, true, depth + 1, recursionStack));
				return true;
			}
			return false;
		}
	}
	public class StringifyHideTypeAttribute : System.Attribute { }
}