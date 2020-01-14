using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
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
	}
}
