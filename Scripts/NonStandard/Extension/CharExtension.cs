using System.Collections.Generic;

namespace NonStandard.Extension {
	public static class CharExtension {
		public static int ToNumericValue(this char c, int numberBase = 10) {
			if (numberBase < 36) {
				return GetDigitValueHexadecimalPattern(c);
			} else if (numberBase <= 64) {
				return GetDigitValueBase64Pattern(c);
			}
			return -1;
		}
		/// <param name="c">an alpha numeric character</param>
		/// <returns>-1 if invalid digit</returns>
		public static int GetDigitValueHexadecimalPattern(this char c) {
			if (c >= '0' && c <= '9') return (byte)(c - '0');
			if (c >= 'A' && c <= 'Z') return (byte)((c - 'A') + 10);
			if (c >= 'a') return (byte)((c - 'a') + 10);
			return -1;
		}
		public static char ConvertToHexadecimalPattern(int n) {
			if (n < 10) { return (char)('0' + n); }
			return (char)('a' + (n-10));
		}
		public const string Base64Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789​+/=";
		private static Dictionary<char, int> _Base64Characters = null;
		/// <param name="c">a character from the <see cref="Base64Characters"/> set</param>
		/// <returns>-1 if invalid digit</returns>
		public static int GetDigitValueBase64Pattern(this char c) {
			if (_Base64Characters == null) {
				_Base64Characters = new Dictionary<char, int>();
				for (int i = 0; i < Base64Characters.Length; ++i) { _Base64Characters[Base64Characters[i]] = i; }
			}
			return _Base64Characters[c];
		}
		public static char ConvertToBase64Pattern(int n) { return Base64Characters[n]; }
		public static bool IsValidNumber(this char c, int numberBase) {
			int h = ToNumericValue(c);
			return h >= 0 && h < numberBase;
		}
		public static char LiteralUnescape(this char c) {
			switch (c) {
			case 'a': return '\a';
			case 'b': return '\b';
			case 'n': return '\n';
			case 'r': return '\r';
			case 'f': return '\f';
			case 't': return '\t';
			case 'v': return '\v';
			}
			return c;
		}

		public static string LiteralEscape(this char c) {
			switch (c) {
			case '\a': return ("\\a");
			case '\b': return ("\\b");
			case '\n': return ("\\n");
			case '\r': return ("\\r");
			case '\f': return ("\\f");
			case '\t': return ("\\t");
			case '\v': return ("\\v");
			case '\'': return ("\\\'");
			case '\"': return ("\\\"");
			case '\\': return ("\\\\");
			}
			return null;
		}

	}
}
