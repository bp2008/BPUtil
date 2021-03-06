﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides utilities for working with strings.
	/// </summary>
	public static class StringUtil
	{
		/// <summary>
		/// Gets a random character from the ranges 0-9, A-Z, a-z. There are 62 possible characters this method will return.
		/// </summary>
		/// <returns></returns>
		public static char GetRandomAlphaNumericChar()
		{
			int i = SecureRandom.Next(62);
			if (i < 10)
				return (char)(48 + i);
			if (i < 36)
				return (char)(65 + (i - 10));
			return (char)(97 + (i - 36));
		}
		/// <summary>
		/// Gets a string of random characters from the ranges 0-9, A-Z, a-z. There are 62 possible characters this method will return.
		/// </summary>
		/// <returns></returns>
		public static string GetRandomAlphaNumericString(ushort length)
		{
			StringBuilder sb = new StringBuilder(length);
			for (int i = 0; i < length; i++)
				sb.Append(GetRandomAlphaNumericChar());
			return sb.ToString();
		}
		/// <summary>
		/// <para>Encodes the characters '&lt;', '&gt;', '"', '&amp;', and apostrophe as html entities so that the resulting string may be inserted into an html attribute.</para>
		/// </summary>
		/// <remarks>
		/// <para>Compared to HttpUtility.HtmlAttributeEncode, this method encodes the same characters plus the &gt; character.</para>
		/// <para>Also, until .NET 4.5, HttpUtility.HtmlAttributeEncode does not encode the apostrophe.</para>
		/// <para>This method encodes the same characters as System.Security.SecurityElement.Escape(), with the difference being that the SecurityElement.Escape() method is intended for XML encoding and encodes apostrophe as &amp;apos; while this method encodes apostrophe as &amp;#39; which is more appropriate for HTML.</para>
		/// </remarks>
		/// <param name="str">The string to encode.</param>
		/// <returns></returns>
		public static string HtmlAttributeEncode(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in str)
				switch (c)
				{
					case '"':
						sb.Append("&quot;");
						break;
					case '\'':
						sb.Append("&#39;");
						break;
					case '&':
						sb.Append("&amp;");
						break;
					case '<':
						sb.Append("&lt;");
						break;
					case '>':
						sb.Append("&gt;");
						break;
					default:
						sb.Append(c);
						break;
				}
			return sb.ToString();
		}
		/// <summary>
		/// <para>Encodes the characters '"' and apostrophe as html entities so that the resulting string may be inserted into an html attribute. '&lt;', '&gt;', and '&amp;' characters are left alone.</para>
		/// </summary>
		/// <param name="str">The string to encode.</param>
		/// <returns></returns>
		public static string HtmlAttributeEncodeMinimal(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in str)
				switch (c)
				{
					case '"':
						sb.Append("&quot;");
						break;
					case '\'':
						sb.Append("&#39;");
						break;
					default:
						sb.Append(c);
						break;
				}
			return sb.ToString();
		}

		/// <summary>
		/// Returns true if the string contains only characters from the set: A-Z, a-z, 0-9. Null returns true.
		/// </summary>
		/// <param name="str">String to test.</param>
		/// <returns></returns>
		public static bool IsAlphaNumeric(string str)
		{
			if (str == null)
				return true;
			foreach (char c in str)
			{
				if ((c >= 'a' && c <= 'z')
					|| (c >= 'A' && c <= 'Z')
					|| (c >= '0' && c <= '9'))
				{
					// Character is OK
				}
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns true if the string contains only characters from the set: A-Z, a-z, 0-9,_. Null returns true.
		/// </summary>
		/// <param name="str">String to test.</param>
		/// <returns></returns>
		public static bool IsAlphaNumericOrUnderscore(string str)
		{
			if (str == null)
				return true;
			foreach (char c in str)
			{
				if ((c >= 'a' && c <= 'z')
					|| (c >= 'A' && c <= 'Z')
					|| (c >= '0' && c <= '9')
					|| c == '_')
				{
					// Character is OK
				}
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns true if the string meets minimum reasonable criteria for a printable display name, meaning it consists of at least one alphanumeric character among any number of spaces or other ASCII-printable characters.
		/// </summary>
		/// <param name="str">String to test.</param>
		/// <returns></returns>
		public static bool IsPrintableName(string str)
		{
			if (str == null)
				return false;
			bool containsAlphaNumeric = false;
			foreach (char c in str)
			{
				if ((c >= 'a' && c <= 'z')
					|| (c >= 'A' && c <= 'Z')
					|| (c >= '0' && c <= '9'))
				{
					containsAlphaNumeric = true;
					// Character is OK
				}
				else if (c >= 32 && c <= 126)
				{
					// Character is OK
				}
				else
					return false;
			}
			return containsAlphaNumeric;
		}

		/// <summary>
		/// Returns a copy of the string that has certain special characters replaced with special visualizing characters.
		/// Note that it is possible for these replacement characters to also exist in the source string and thereby cause some ambiguity.
		/// </summary>
		/// <param name="str">A string which may contain special characters.</param>
		/// <returns>A string with certain special characters replaced.</returns>
		public static string VisualizeSpecialCharacters(string str)
		{
			if (str == null)
				return null;
			StringBuilder sb = new StringBuilder(str.Length);
			foreach (char c in str)
			{
				if (c == 0) // Null
					sb.Append('␀');
				else if (c == '\t') // Horizontal Tab (Dec 9)
					sb.Append('→');
				else if (c == '\r' || c == '\n') // CR, LF
					sb.Append(c);
				else if (c == 8) // Backspace
					sb.Append('⌫');
				else if (c == 127) // Delete
					sb.Append('⌦');
				else if (c >= 1 && c <= 31) // Other ASCII non-printable characters without special case above.
					sb.Append('�');
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>
		/// <para>Encodes a string so it can be safely placed into a JavaScript string literal.</para>
		/// <para>From .NET reference source.</para>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string JavaScriptStringEncode(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "";
			}

			StringBuilder b = null;
			int startIndex = 0;
			int count = 0;
			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];

				// Append the unhandled characters (that do not require special treament)
				// to the string builder when special characters are detected.
				if (CharRequiresJavaScriptEncoding(c))
				{
					if (b == null)
					{
						b = new StringBuilder(value.Length + 5);
					}

					if (count > 0)
					{
						b.Append(value, startIndex, count);
					}

					startIndex = i + 1;
					count = 0;
				}

				switch (c)
				{
					case '\r':
						b.Append("\\r");
						break;
					case '\t':
						b.Append("\\t");
						break;
					case '\"':
						b.Append("\\\"");
						break;
					case '\\':
						b.Append("\\\\");
						break;
					case '\n':
						b.Append("\\n");
						break;
					case '\b':
						b.Append("\\b");
						break;
					case '\f':
						b.Append("\\f");
						break;
					default:
						if (CharRequiresJavaScriptEncoding(c))
						{
							AppendCharAsUnicodeJavaScript(b, c);
						}
						else
						{
							count++;
						}
						break;
				}
			}

			if (b == null)
			{
				return value;
			}

			if (count > 0)
			{
				b.Append(value, startIndex, count);
			}

			return b.ToString();
		}
		/// <summary>
		/// Based on .NET reference source.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		private static bool CharRequiresJavaScriptEncoding(char c)
		{
			return c < 0x20 // control chars always have to be encoded
				|| c == '\"' // chars which must be encoded per JSON spec
				|| c == '\\'
				|| c == '\'' // HTML-sensitive chars encoded for safety
				|| c == '<'
				|| c == '>'
				|| c == '\u0085' // newline chars (see Unicode 6.2, Table 5-1 [http://www.unicode.org/versions/Unicode6.2.0/ch05.pdf]) have to be encoded (DevDiv #663531)
				|| c == '\u2028'
				|| c == '\u2029';
		}
		/// <summary>
		/// From .NET reference source.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="c"></param>
		private static void AppendCharAsUnicodeJavaScript(StringBuilder builder, char c)
		{
			builder.Append("\\u");
			builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
		}
		public static string DeNullify(object maybeNull)
		{
			if (maybeNull == null)
				return "";
			else if (maybeNull is string)
				return (string)maybeNull;
			else
				return maybeNull.ToString();
		}

		/// <summary>
		/// Returns a number of bytes formatted with an appropriate unit for disk / file sizes.  E.g. 1234567 => 1.18 MiB
		/// </summary>
		/// <param name="bytes">Number of bytes</param>
		/// <param name="decimals">Maximum number of decimal places to expose.</param>
		/// <returns></returns>
		public static string FormatDiskBytes(long bytes, int decimals = 2)
		{
			return FormatDataSize(bytes,
				new int[] { decimals, decimals, decimals, decimals, decimals, decimals, decimals, decimals, decimals },
				1024,
				new string[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" });
		}
		/// <summary>
		/// Returns a number of bytes formatted with an appropriate unit for network transfer sizes.  E.g. 1234567 => 1.23 MB
		/// </summary>
		/// <param name="bytes">Number of bytes</param>
		/// <param name="decimals">Maximum number of decimal places to expose.</param>
		/// <returns></returns>
		public static string FormatNetworkBytes(long bytes, int decimals = 2)
		{
			return FormatDataSize(bytes,
				new int[] { decimals, decimals, decimals, decimals, decimals, decimals, decimals, decimals, decimals },
				1000,
				new string[] { "B", "K", "M", "G", "T", "PB", "EB", "ZB", "YB" });
		}
		/// <summary>
		/// <para>Returns a number of bits formatted with an appropriate unit for network transfer sizes.  E.g. 1234567 => 1.2 Mb</para>
		/// <para>An appropriate number of decimal places is chosen automatically based on the size of the input.</para>
		/// </summary>
		/// <param name="bits">Number of bits</param>
		/// <returns></returns>
		public static string FormatNetworkBits(long bits)
		{
			return FormatDataSize(bits, new int[] { 0, 0, 1, 2, 2, 2, 2, 2, 2 }, 1000, new string[] { "b", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb", "Zb", "Yb" });
		}

		private static string FormatDataSize(long input, int[] decimals, int factor, string[] sizes)
		{
			if (input == 0) return "0 " + sizes[0];
			bool negative = input < 0;
			if (negative)
				input = -input;

			// Decide which unit we'll use (index 0-8)
			int unitIndex = (int)Math.Floor(Math.Log(input) / Math.Log(factor));
			unitIndex = unitIndex.Clamp(0, 8);

			// Build string formatting string
			StringBuilder decimalStringFormat = new StringBuilder();
			decimalStringFormat.Append("0");
			int dm = decimals[unitIndex];
			if (dm > 0)
				decimalStringFormat.Append(".");
			for (int n = 0; n < dm; n++)
				decimalStringFormat.Append("#");

			// Return final value
			return (negative ? "-" : "") + (input / Math.Pow(factor, unitIndex)).ToString(decimalStringFormat.ToString()) + ' ' + sizes[unitIndex];
		}
		/// <summary>
		/// Performs multiple character-to-character replacements on a string.
		/// </summary>
		/// <param name="source">The string to perform replacements on.</param>
		/// <param name="replacements">A dictionary of replacement mappings.</param>
		/// <returns></returns>
		public static string ReplaceMultiple(string source, IDictionary<char, char> replacements)
		{
			StringBuilder sb = new StringBuilder();
			char replacement;
			foreach (char c in source)
			{
				if (replacements.TryGetValue(c, out replacement))
					sb.Append(replacement);
				else
					sb.Append(c);
			}
			return sb.ToString();
		}
		/// <summary>
		/// Repairs Base64 padding by appending '=' characters to the end of the string until its length is divisible by 4.
		/// </summary>
		/// <param name="b64">A base64 string which may be missing its padding characters.</param>
		/// <returns></returns>
		public static string RepairBase64Padding(string b64)
		{
			int rem = b64.Length % 4;
			if (rem == 2)
				return b64 + "==";
			else if (rem == 3)
				return b64 + "=";
			else
				return b64; // If remainder is 0, the base64 string needs no padding. If remainder is 1, the base64 string is invalid.
		}
	}
}
