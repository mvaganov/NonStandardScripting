using NonStandard.Extension;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NonStandard.Data.Parse {
	public delegate bool ResolvedEnoughDelegate(object currentStateOfData);
	public class ParseRuleSet {
		protected static Dictionary<string, ParseRuleSet> allContexts = new Dictionary<string, ParseRuleSet>();
		public string name = "default";
		protected char[] whitespace;
		internal Delim[] delimiters;
		private List<ParseRuleSet> delimiterFallback = new List<ParseRuleSet>();
		/// <summary>
		/// an optional function to simplify results
		/// </summary>
		public Func<List<object>, object> Simplify;

		//public static Dictionary<string, ParseRuleSet> GetAllContexts() { return allContexts; }
		public static ParseRuleSet GetContext(string name) { allContexts.TryGetValue(name, out ParseRuleSet value); return value; }
		public char[] Whitespace {
			get => whitespace;
			set {
				whitespace = value;
				if(whitespace == null || whitespace.Length == 0) {
					minWhitespace = maxWhitespace = (char)0;
				} else {
					minWhitespace = whitespace.Min();
					maxWhitespace = whitespace.Max();
				}
			}
		}
		public Delim[] Delimiters {
			get => delimiters;
			set {
				delimiters = value;
				SetDelimiters(delimiters);
			}
		}

		private char minWhitespace = char.MinValue, maxWhitespace = char.MaxValue;
		public bool IsWhitespace(char c) {
			return (c < minWhitespace || c > maxWhitespace) ? false : whitespace.IndexOf(c) >= 0;
		}
		/// <summary>
		/// data used to make delimiter searching very fast
		/// </summary>
		private char minDelim = char.MaxValue, maxDelim = char.MinValue; private int[] delimTextLookup;
		public ParseRuleSet(string name, Delim[] defaultDelimiters = null, char[] defaultWhitespace = null) {
			this.name = name;
			allContexts[name] = this;
			if (defaultDelimiters != null && !defaultDelimiters.IsSorted()) { Array.Sort(defaultDelimiters); }
			if (defaultWhitespace != null && !defaultWhitespace.IsSorted()) { Array.Sort(defaultWhitespace); }
			Delimiters = defaultDelimiters;
			Whitespace = defaultWhitespace;
		}

		public Delim[] GetDefaultDelimiters(ParseRuleSet possibleRuleSet) {
			return possibleRuleSet != null ? possibleRuleSet.Delimiters : CodeRules.Default.Delimiters;
		}
		public char[] GetDefaultWhitespace(ParseRuleSet possibleRuleSet) {
			return possibleRuleSet != null ? possibleRuleSet.Whitespace : CodeRules.Default.Whitespace;
		}

		/// <summary>
		/// set the delimiters of this Context, also calculating a simple lookup table
		/// </summary>
		/// <param name="delims"></param>
		public void SetDelimiters(Delim[] delims) {
			if(delims == null || delims.Length == 0) {
				minDelim = maxDelim = (char)0;
				delimTextLookup = new int[] { -1 };
				return;
			}
			char c, last = delims[0].text[0];
			for (int i = 0; i < delims.Length; ++i) {
				c = delims[i].text[0];
				if (c < last) { Array.Sort(delims); SetDelimiters(delims); return; }
				if (c < minDelim) minDelim = c;
				if (c > maxDelim) maxDelim = c;
			}
			delimTextLookup = new int[maxDelim + 1 - minDelim];
			for (int i = 0; i < delimTextLookup.Length; ++i) { delimTextLookup[i] = -1; }
			for (int i = 0; i < delims.Length; ++i) {
				c = delims[i].text[0];
				int lookupIndex = c - minDelim; // where in the delimiters list this character can be found
				if (delimTextLookup[lookupIndex] < 0) { delimTextLookup[lookupIndex] = i; }
			}
		}
		public int IndexOfDelimeterAt(string str, int index) {
			char c = str[index];
			if (c < minDelim || c > maxDelim) return -1;
			int i = delimTextLookup[c - minDelim];
			if (i < 0) return -1;
			while (i < delimiters.Length) {
				if (delimiters[i].text[0] != c) break;
				if (delimiters[i].IsAt(str, index)) return i;
				++i;
			}
			return -1;
		}
		public Delim GetDelimiter(string str) { return GetDelimiterAt(str, 0, -1); }
		public Delim GetDelimiterAt(string str, int index, int currentTokenStartedAt) {
			int i = IndexOfDelimeterAt(str, index);
			Delim delim = (delimiters != null && i >= 0) ? delimiters[i] : null;
			// if this is a non-breaking delimeter...
			if (delim != null && !delim.breaking) { // TODO put this body in a nicely named function
				//Show.Log(delim.text);
				// ...that has been found within a non-delimiter token
				if (currentTokenStartedAt >= 0) {
					delim = null; // nope, not a delimeter
				} else {
					int nextIndex = index + delim.text.Length;
					if (str.Length > nextIndex) {
						bool whitespaceIsNext = IsWhitespace(str[nextIndex]);
						// ...that has a non-breaking delimiter immediately after it
						//Show.Log("checking after " + delim.text + ": " + str.Substring(index + delim.text.Length));
						Delim nextPossibleDelim = GetDelimiterAt(str, nextIndex, index);
						if (!whitespaceIsNext && (nextPossibleDelim == null || !nextPossibleDelim.breaking)) {
							delim = null; // nope, not a delimiter
						}
					}
				}
			}
			// if a delimiter could not be found and there are fall-back delimiter parsers to check
			if (delim == null && delimiterFallback != null && delimiterFallback.Count > 0) {
				for(i = 0; i < delimiterFallback.Count; ++i) {
					Delim d = delimiterFallback[i].GetDelimiterAt(str, index, currentTokenStartedAt);
					if (d != null) {
						return d;
					}
				}
			}
			return delim;
		}
		public void AddDelimiterFallback(ParseRuleSet ruleSet) {
			delimiterFallback.Add(ruleSet);
			List<ParseRuleSet> stack = new List<ParseRuleSet>();
			if (RecursionFound(stack)) {
				delimiterFallback.Remove(ruleSet);
				throw new Exception("can't add " + ruleSet.name + " as fallback to " + name + ", recursion: " +
					stack.JoinToString("->", rs => rs.name));
			}
		}
		private bool RecursionFound(List<ParseRuleSet> stack = null) {
			if (stack == null) { stack = new List<ParseRuleSet>(); }
			if (stack.Contains(this)) { return true; }
			if (delimiterFallback != null && delimiterFallback.Count > 0) {
				stack.Add(this);
				for (int i = 0; i < delimiterFallback.Count; ++i) {
					if (delimiterFallback[i].RecursionFound(stack)) { return true; }
				}
				stack.Remove(this);
			}
			return false;
		}
		public SyntaxTree GetEntry(List<Token> tokens, int startTokenIndex, object meta, SyntaxTree parent = null) {
			SyntaxTree syntax = new SyntaxTree (this, tokens, startTokenIndex, -1, meta);
			syntax.SetParent(parent);
			return syntax;
		}
	}
}
