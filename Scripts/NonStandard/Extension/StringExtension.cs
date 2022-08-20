using System;
using System.Collections.Generic;
using System.Text;
using NonStandard.Data.Parse;

namespace NonStandard.Extension {
	public static class StringExtension {
		public static bool IsSubstringAt(this string str, string substring, int index) {
			if (index < 0 || index + substring.Length > str.Length) { return false; }
			for (int i = 0; i < substring.Length; ++i) {
				if (str[index + i] != substring[i]) { return false; }
			}
			return true;
		}
		public static int IsSubstringAt(this string str, IList<string> substrings, int index) {
			for (int i = 0; i < substrings.Count; ++i) {
				if (IsSubstringAt(str, substrings[i], index)) { return i; }
			}
			return -1;
		}
		public static string RemoveFromFront(this string str, string trimMe) {
			if (str.StartsWith(trimMe)) { return str.Substring(trimMe.Length); }
			return str;
		}

		public static readonly string[] whitespace = new string[] { " ", "\t", "\n", "\r" };
		public static int IndexOfFirst(this string str, string[] delimiter, out int firstDelimiter, int startIndex = 0) {
			firstDelimiter = -1;
			for (int i = startIndex; i < str.Length; ++i) {
				for (int j = 0; j < delimiter.Length; ++j) {
					if (str.IsSubstringAt(delimiter[j], i)) {
						firstDelimiter = j;
						return i;
					}
				}
			}
			return -1;
		}
		public static int IndexOfLast(this string str, string[] delimiter, out int firstDelimeter) {
			firstDelimeter = -1;
			for (int i = str.Length - 1; i >= 0; --i) {
				for (int j = 0; j < delimiter.Length; ++j) {
					if (str.IsSubstringAt(delimiter[j], i)) {
						firstDelimeter = j;
						return i;
					}
				}
			}
			return -1;
		}
		public static string SubstringBeforeWhitespace(this string str, int startIndex = 0) {
			return SubstringBeforeFirst(str, whitespace, startIndex);
		}
		public static string SubstringBeforeFirst(this string str, string delimiter, int startIndex = 0) {
			return SubstringBeforeFirst(str, new string[] { delimiter }, startIndex);
		}
		public static string SubstringBeforeFirst(this string str, string[] delimiter, int startIndex = 0) {
			if (delimiter == null) { delimiter = whitespace; }
			int index = IndexOfFirst(str, delimiter, out int which, startIndex);
			if (index >= 0) { return str.Substring(0, index); }
			return null;
		}
		public static string SubstringAfterLast(this string str, string delimiter) {
			return SubstringAfterLast(str, new string[] { delimiter });
		}
		public static string SubstringAfterLast(this string str, string[] delimiter) {
			if (delimiter == null) { delimiter = whitespace; }
			int index = IndexOfLast(str, delimiter, out int which);
			if (index >= 0) { return str.Substring(index + delimiter[which].Length); }
			return null;
		}

		public static string DefaultIndentation = "  ";
		/// <param name="indent">if null, use <see cref="DefaultIndentation"/></param>
		/// <param name="depth"></param>
		public static string Indentation(int depth, string indent = null) {
			if (indent == null) { indent = DefaultIndentation; }
			StringBuilder sb = new StringBuilder();
			while (depth-- > 0) { sb.Append(indent); }
			return sb.ToString();
		}

		/// <param name="str">a string with one or more lines, separated by '\n'</param>
		/// <param name="indent">if null, use <see cref="DefaultIndentation"/></param>
		/// <param name="depth"></param>
		public static string Indent(this string str, string indent = null, int depth = 1) {
			string indentation = Indentation(depth, indent);
			return ForEachBetween(str, s => indentation + s);
		}

		/// <param name="str"></param>
		/// <param name="indent">if null, use <see cref="DefaultIndentation"/></param>
		/// <param name="depth"></param>
		public static string Unindent(this string str, string indent = null, int depth = 1) {
			string indentation = Indentation(depth, indent);
			return ForEachBetween(str, s => RemoveFromFront(s, indentation));
		}

		public static string ForEachBetween(this string str, Func<string, string> manipulateSubstring, string delimiter = "\n") {
			List<string> lines = new List<string>(str.Split(delimiter));
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < lines.Count; ++i) {
				if (i > 0) sb.Append(delimiter);
				sb.Append(manipulateSubstring(lines[i]));
			}
			return sb.ToString();
		}

		public static List<string> Split(this string str, string delimiter) {
			return Split(str, new string[] { delimiter });
		}
		public enum DelimiterOption { Omit, IncludeSeparate, Append, Prepend }
		public static List<string> Split(this string str, IList<string> delimiters, DelimiterOption option = DelimiterOption.Omit, bool keepEmptyTokens = false) {
			List<KeyValuePair<int,int>> lineEndings = GenerateIndexTable(str, delimiters);
			List<string> lines = new List<string>();
			if (lineEndings.Count == 0) { lines.Add(str); return lines; }
			int delimLenMin = int.MaxValue, delimLenMax = int.MinValue;
			for (int i = 0; i < lineEndings.Count; ++i) {
				delimLenMin = Math.Min(delimiters[lineEndings[i].Value].Length, delimLenMin);
				delimLenMax = Math.Max(delimiters[lineEndings[i].Value].Length, delimLenMax);
			}
			int nextTokenStart = 0;
			bool uniformDelimiterSize = delimLenMin == delimLenMax;
			string token;
			for (int i = 0; i < lineEndings.Count; ++i) {
				int tokenStart = nextTokenStart;
				int tokenLength = lineEndings[i].Key - tokenStart;
				int delimLen = uniformDelimiterSize ? delimLenMin : delimiters[lineEndings[i].Value].Length;
				nextTokenStart = lineEndings[i].Key + ((option != DelimiterOption.Prepend) ? delimLen : 0);
				if (option == DelimiterOption.Append) { tokenLength += delimLen; }
				token = str.Substring(tokenStart, tokenLength);
				if (keepEmptyTokens || !string.IsNullOrEmpty(token)) { lines.Add(token); }
				if (option == DelimiterOption.IncludeSeparate) { lines.Add(delimiters[lineEndings[i].Value]); }
			}
			token = str.Substring(nextTokenStart, str.Length - nextTokenStart);
			if (keepEmptyTokens || !string.IsNullOrEmpty(token)) { lines.Add(token); }
			return lines;
		}

		/// <summary>
		/// converts a string from it's code to it's compiled form, with processed escape sequences
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string Unescape(this string str) {
			StringBuilder sb = new StringBuilder();
			int stringStarted = 0;
			for (int i = 0; i < str.Length; ++i) {
				char c = str[i];
				if (c == '\\') {
					if (stringStarted != i) { sb.Append(str.Substring(stringStarted, i - stringStarted)); }
					++i;
					if (i >= str.Length) { break; }
					//sb.Append(str[i].LiteralUnescape());
					ParseResult parseResult = str.UnescapeStringSequenceAt(i);
					if (!parseResult.IsError) { sb.Append(parseResult.replacementValue.ToString()); } else { throw new FormatException(parseResult.error.ToString()); }
					stringStarted = i + 1;
				}
			}
			sb.Append(str.Substring(stringStarted, str.Length - stringStarted));
			return sb.ToString();
		}
		public static string Escape(this string str) {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < str.Length; ++i) {
				char c = str[i];
				string escaped = c.LiteralEscape();
				if (escaped != null) {
					sb.Append(escaped);
				} else {
					if (c < 32 || (c > 127 && c < 512)) {
						sb.Append("\\").Append(Convert.ToString((int)c, 8));
					} else if (c >= 512) {
						sb.Append("\\u").Append(((int)c).ToString("X4"));
					} else {
						sb.Append(c);
					}
				}
			}
			return sb.ToString();
		}
		public static bool ContainsNonAlphaCharacters(this string str) {
			for (int i = 0; i < str.Length; ++i) {
				char c = str[i];
				if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) return true;
			}
			return false;
		}
		public static int CountNumericCharactersAt(this string str, int index, int numberBase, bool includeNegativeSign, bool includeSingleDecimal) {
			return CountNumericCharactersAt(str, index, numberBase, includeNegativeSign, includeSingleDecimal, out int _);
		}
		public static int CountNumericCharactersAt(this string str, int index, int numberBase, bool includeNegativeSign, bool includeSingleDecimal, out int foundDecimal) {
			int numDigits = 0;
			foundDecimal = -1;
			bool stillGood;
			while (index + numDigits < str.Length) {
				char c = str[index + numDigits];
				if (c.IsValidNumber(numberBase)) {
					stillGood = true;
				} else {
					stillGood = false;
					if (includeNegativeSign && numDigits == 0 && c == '-') {
						stillGood = true;
					} else if (includeSingleDecimal && c == '.' && foundDecimal < 0) {
						foundDecimal = index;
						stillGood = true;
					}
				}
				if (stillGood) { numDigits++; } else break;
			}
			return numDigits;
		}
		public static ParseResult NumberParse(this string str, int index, int characterCount, int numberBase, bool includeDecimal) {
			ParseResult pr = new ParseResult(0, null);
			long sum = 0;
			char c = str[index];
			bool isNegative = c == '-';
			if (isNegative) { ++index; }
			bool isDecimal = c == '.';
			int numDigits;
			if (!isDecimal) {
				numDigits = str.CountNumericCharactersAt(index, numberBase, false, false);
				int b = 1, onesPlace = index + numDigits - 1;
				for (int i = 0; i < numDigits; ++i) {
					sum += str[onesPlace - i].ToNumericValue() * b;
					b *= numberBase;
				}
				if (isNegative) sum *= -1;
				pr.replacementValue = (sum < int.MaxValue) ? (int)sum : sum;
				index += numDigits;
			}
			++index;
			double fraction = 0;
			if (includeDecimal && index < str.Length && str[index - 1] == '.') {
				numDigits = str.CountNumericCharactersAt(index, numberBase, false, false);
				if (numDigits == 0) { pr.SetError("decimal point with no subsequent digits", index, 1, index); }
				long b = numberBase;
				for (int i = 0; i < numDigits; ++i) {
					fraction += str[index + i].ToNumericValue() / (double)b;
					b *= numberBase;
				}
				if (isNegative) fraction *= -1;
				pr.replacementValue = (sum + fraction);
			}
			pr.lengthParsed = characterCount;
			return pr;
		}
		public static ParseResult HexadecimalParse(this string str, int index) { return NumberParse(str, index + 2, 16, false); }
		public static ParseResult NumericParse(this string str, int index) { return NumberParse(str, index, 10, true); }
		public static ParseResult IntegerParse(this string str, int index) { return NumberParse(str, index, 10, false); }
		public static ParseResult NumberParse(this string str, int index, int numberBase, bool includeDecimal) {
			return str.NumberParse(index, CountNumericCharactersAt(str, index, numberBase, true, true), numberBase, includeDecimal);
		}
		public static ParseResult UnescapeStringSequenceAt(this string str, int index) {
			ParseResult r = new ParseResult(0, null); // by default, nothing happened
			if (str.Length <= index) { return r.SetError("invalid arguments"); }
			if (str[index] != '\\') { return r.SetError("expected escape sequence starting with '\\'"); }
			if (str.Length <= index + 1) { return r.SetError("unable to parse escape sequence at end of string", 1, 0, 1); }
			char c = str[index + 1];
			switch (c) {
			case '\n': return new ParseResult(index + 2, "");
			case '\r':
				if (str.Length <= index + 2 || str[index + 2] != '\n') {
					return new ParseResult(index, "", "expected windows line ending", 2, 0, 2);
				}
				return new ParseResult(index + 3, "");
			case 'a': return new ParseResult(2, "\a");
			case 'b': return new ParseResult(2, "\b");
			case 'e': return new ParseResult(2, ((char)27).ToString());
			case 'f': return new ParseResult(2, "\f");
			case 'r': return new ParseResult(2, "\r");
			case 'n': return new ParseResult(2, "\n");
			case 't': return new ParseResult(2, "\t");
			case 'v': return new ParseResult(2, "\v");
			case '\\': return new ParseResult(2, "\\");
			case '\'': return new ParseResult(2, "\'");
			case '\"': return new ParseResult(2, "\"");
			case '?': return new ParseResult(2, "?");
			case 'x': return str.NumberParse(index + 2, 2, 16, false).AddToLength(2).ForceCharSubstitute();
			case 'u': return str.NumberParse(index + 2, 4, 16, false).AddToLength(2).ForceCharSubstitute();
			case 'U': return str.NumberParse(index + 2, 8, 16, false).AddToLength(2).ForceCharSubstitute();
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7': {
				int digitCount = 1;
				do {
					if (str.Length <= index + digitCount + 1) break;
					c = str[index + digitCount + 1];
					if (c < '0' || c > '7') break;
					++digitCount;
				} while (digitCount < 3);
				return str.NumberParse(index + 1, digitCount, 8, false).AddToLength(1).ForceCharSubstitute();
			}
			}
			return r.SetError("unknown escape sequence", 1, 0, 1);
		}

		public static bool TryParseEnum<T>(this string value, out T result) where T : struct, IConvertible {
			try {
				result = (T)Enum.Parse(typeof(T), value, true);
				return true;
			} catch (Exception) {
				//string[] names = Enum.GetNames(typeof(T));
				//string errorMessage = $"failed conversion \"{value}\" into {typeof(T)}\nvalid values: {names.JoinToString()}\n" + e;
				//Show.Error(errorMessage);
				result = default;
				return false;
			}
		}

		/// <returns>the indexes of the given substring in this string as the Key, and the substring's index as the Value</returns>
		public static List<KeyValuePair<int, int>> GenerateIndexTable(this string haystack, string needle = "\n") {
			return GenerateIndexTable(haystack, new string[] { needle });
		}

		/// <returns>the indexes of the given substring in this string as the Key, and the substring's index as the Value</returns>
		public static List<KeyValuePair<int,int>> GenerateIndexTable(this string haystack, IList<string> needles) {
			List<KeyValuePair<int, int>> found = new List<KeyValuePair<int, int>>();
			//if (haystack == null || needle == null) return found;
			int limit = haystack.Length;
			for (int i = 0; i < limit; ++i) {
				int whichNeedleIsHere = IsSubstringAt(haystack, needles, i);
				if (whichNeedleIsHere >= 0) {
					found.Add(new KeyValuePair<int, int>(i, whichNeedleIsHere));
				}
			}
			return found;
		}

		/// <summary>
		/// searches for the given file name in the exe's directory, as well as parent directories
		/// </summary>
		/// <param name="filePathAndName"></param>
		/// <param name="numDirectoriesBackToLook">how many parent directories backward to look</param>
		public static string StringFromFile(string filePathAndName, int numDirectoriesBackToLook = 3) {
			string text = null;
			int directoriesSearched = 0;
			//string originalDir = System.IO.Directory.GetCurrentDirectory();
			do {
				try {
					text = System.IO.File.ReadAllText(filePathAndName);
				} catch (System.IO.FileNotFoundException e) {
					if (directoriesSearched < numDirectoriesBackToLook) {
						System.IO.Directory.SetCurrentDirectory("..");
						directoriesSearched++;
					} else {
						//System.Console.WriteLine($"{filePathAndName} not found @{originalDir}");
						throw e;
					}
				}
			} while (text == null);
			text = text.Replace("\r\n", "\n");
			text = text.Replace("\n\r", "\n");
			text = text.Replace("\r", "\n");
			return text;
		}

		/// <summary>
		/// Damereau-Levenshein Distance algorithm
		/// </summary>
		public static int ComputeDistance(this string s, string t) {
			if (string.IsNullOrEmpty(s)) {
				if (string.IsNullOrEmpty(t))
					return 0;
				return t.Length;
			}
			if (string.IsNullOrEmpty(t)) {
				return s.Length;
			}
			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1, m + 1];
			// initialize the top and right of the table to 0, 1, 2, ...
			for (int i = 0; i <= n; d[i, 0] = i++) { }
			for (int j = 1; j <= m; d[0, j] = j++) { }
			for (int i = 1; i <= n; i++) {
				for (int j = 1; j <= m; j++) {
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
					int min1 = d[i - 1, j] + 1;
					int min2 = d[i, j - 1] + 1;
					int min3 = d[i - 1, j - 1] + cost;
					d[i, j] = Math.Min(Math.Min(min1, min2), min3);
				}
			}
			return d[n, m];
		}

		public static string GetClosestString(this string needle, IList<string> haystack) {
			int i = GetClosestStringIndex(needle, haystack);
			return i < 0 ? null : haystack[i];
		}
		public static string GetClosestString(this string needle, Func<int> haystackSize, Func<int, string> haystack) {
			int i = GetClosestStringIndex(needle, haystackSize, haystack);
			return i < 0 ? null : haystack.Invoke(i);
		}
		public static int GetClosestStringIndex(this string needle, IList<string> haystack) {
			return GetClosestStringIndex(needle, () => haystack.Count, i => haystack[i]);
		}
		public static int GetClosestStringIndex(this string needle, Func<int> haystackSize, Func<int,string> haystack) {
			int bestIndex = -1;
			int bestDist = -1;
			for (int i = 0; i < haystackSize.Invoke(); ++i) {
				int d = ComputeDistance(needle, haystack.Invoke(i));
				if (bestDist < 0 || d < bestDist) {
					bestIndex = i;
					bestDist = d;
				}
			}
			return bestIndex;
		}
	}
}