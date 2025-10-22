using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		static StringUtil()
		{
#if NET6_PLUS_LINUX
			if (Platform.IsUnix())
			{
				RegisterCodepagesProvider();
			}
#endif
		}
#if NET6_PLUS_LINUX
		private static void RegisterCodepagesProvider()
		{
			// This, along with the nuget package `System.Text.Encoding.CodePages`, allows windows-1252 to be loadable in Linux.
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
#endif
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
		/// Gets a random character from the ranges 0-9, A-Z. There are 36 possible characters this method will return.
		/// </summary>
		/// <returns></returns>
		public static char GetRandomAlphaNumericCapitalChar()
		{
			int i = SecureRandom.Next(36);
			if (i < 10)
				return (char)(48 + i);
			return (char)(65 + (i - 10));
		}
		/// <summary>
		/// Gets a string of random characters from the ranges 0-9, A-Z. There are 36 possible characters this method will return.
		/// </summary>
		/// <returns></returns>
		public static string GetRandomAlphaNumericCapitalString(ushort length)
		{
			StringBuilder sb = new StringBuilder(length);
			for (int i = 0; i < length; i++)
				sb.Append(GetRandomAlphaNumericCapitalChar());
			return sb.ToString();
		}
		/// <summary>
		/// Gets a random character from the range A-Z. There are 26 possible characters this method will return.
		/// </summary>
		/// <returns></returns>
		public static char GetRandomCapitalChar()
		{
			int i = SecureRandom.Next(26);
			return (char)(65 + i);
		}
		/// <summary>
		/// Gets a string of random characters from the range A-Z. There are 26 possible characters this method will return.
		/// </summary>
		/// <returns></returns>
		public static string GetRandomCapitalString(ushort length)
		{
			StringBuilder sb = new StringBuilder(length);
			for (int i = 0; i < length; i++)
				sb.Append(GetRandomCapitalChar());
			return sb.ToString();
		}
		/// <summary>
		/// Gets a random character between ASCII values 32 (inclusive) and 127 (exclusive).
		/// </summary>
		/// <returns></returns>
		public static char GetRandomAsciiPrintableChar()
		{
			return (char)SecureRandom.Next(32, 127);
		}
		/// <summary>
		/// Gets a random character between ASCII values 32 (inclusive) and 127 (exclusive).
		/// </summary>
		/// <returns></returns>
		public static string GetRandomAsciiPrintableString(ushort length)
		{
			StringBuilder sb = new StringBuilder(length);
			for (int i = 0; i < length; i++)
				sb.Append(GetRandomAsciiPrintableChar());
			return sb.ToString();
		}
		/// <summary>
		/// Returns true if the given string consists of only ASCII characters between 32 (inclusive) and 127 (exclusive).
		/// </summary>
		/// <param name="str">String to test.</param>
		/// <returns></returns>
		public static bool IsAsciiPrintable(string str)
		{
			for (int i = 0; i < str.Length; i++)
				if (str[i] < 32 || str[i] >= 127)
					return false;
			return true;
		}
		/// <summary>
		/// This string contains one of every alphanumeric character except:
		/// B 8 G 6 I 1 l O 0 Q D S 5 Z 2
		/// </summary>
		private const string unambiguousPasswordAlphabet = "3479ACEFHJKLMNPRTUVWXYabcdefghijkmnopqrstuvwxyz";
		/// <summary>
		/// Gets a random alphanumeric character from an alphabet that omits the most commonly confused characters.
		/// </summary>
		/// <returns></returns>
		public static char GetUnambiguousPasswordChar()
		{
			return unambiguousPasswordAlphabet[SecureRandom.Next(unambiguousPasswordAlphabet.Length)];
		}
		/// <summary>
		/// Gets a random alphanumeric string from an alphabet that omits the most commonly confused characters such as B and 8, O and 0.
		/// </summary>
		/// <param name="length">Length in characters of the string to generate.</param>
		/// <returns></returns>
		public static string GenerateUnambiguousPassword(ushort length)
		{
			StringBuilder sb = new StringBuilder(length);
			for (int i = 0; i < length; i++)
				sb.Append(GetUnambiguousPasswordChar());
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
		/// Returns "" if <paramref name="count"/> is 1, otherwise returns "s".
		/// </summary>
		/// <param name="count">Number of items being labeled.</param>
		/// <returns>"" or "s" as appropriate for <paramref name="count"/></returns>
		public static string PluralSuffix(int count)
		{
			return count == 1 ? "" : "s";
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
		/// Encodes HTML-reserved characters as the appropriate entities so the string can appear safely in HTML source.
		/// </summary>
		/// <param name="str">String which may contain HTML-reserved characters.</param>
		/// <returns></returns>
		public static string HtmlEncode(string str)
		{
			return System.Web.HttpUtility.HtmlEncode(str);
		}
		/// <summary>
		/// Decodes a string of HTML markup, returning the text representation.  Some information, such as HTML tags, can be lost in the conversion.
		/// </summary>
		/// <param name="str">String of HTML markup.</param>
		/// <returns></returns>
		public static string HtmlDecode(string str)
		{
			return System.Web.HttpUtility.HtmlDecode(str);
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
		/// Returns true if the character is from the set: A-Z, a-z, 0-9,_.
		/// </summary>
		/// <param name="c">Character to test.</param>
		/// <returns></returns>
		public static bool IsAlphaNumericOrUnderscore(char c)
		{
			return (c >= 'a' && c <= 'z')
					|| (c >= 'A' && c <= 'Z')
					|| (c >= '0' && c <= '9')
					|| c == '_';
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
		/// Returns true if the string is eligible to be used as a systemd service name on linux.
		/// </summary>
		/// <param name="str">String to test. E.g. "BPUtil" -> True. "BP Util" -> False.</param>
		/// <returns></returns>
		public static bool IsValidSystemdServiceName(string str)
		{
			if (str == null
				|| str.Length == 0
				|| str.Length + ".service".Length > 255)
				return false;
			foreach (char c in str)
			{
				if ((c >= 'a' && c <= 'z')
					|| (c >= 'A' && c <= 'Z')
					|| (c >= '0' && c <= '9')
					|| c == '_'
					|| c == ':'
					|| c == '-'
					|| c == '.'
					|| c == '\\')
				{
					// Character is OK
				}
				else
					return false;
			}
			return true;
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
		/// <para>From .NET reference source.  Modified by bp2008.</para>
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
					case '`':
						b.Append("\\`");
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
		/// Replaces all line breaks in the string with '\n' for best linux compatibility.
		/// </summary>
		/// <param name="str">String to replace line breaks in.</param>
		/// <returns>A copy of the string with all line breaks replaced.</returns>
		public static string LinuxLineBreaks(string str)
		{
			return str.Replace("\r\n", "\n").Replace("\r", "\n");
		}

		/// <summary>
		/// Replaces all line breaks in the string with '\r' for best Mac compatibility.
		/// </summary>
		/// <param name="str">String to replace line breaks in.</param>
		/// <returns>A copy of the string with all line breaks replaced.</returns>
		public static string MacLineBreaks(string str)
		{
			return str.Replace("\r\n", "\r").Replace("\n", "\r");
		}

		/// <summary>
		/// Replaces all line breaks in the string with "\r\n" for best Windows compatibility.
		/// </summary>
		/// <param name="str">String to replace line breaks in.</param>
		/// <returns>A copy of the string with all line breaks replaced.</returns>
		public static string WindowsLineBreaks(string str)
		{
			return LinuxLineBreaks(str).Replace("\n", "\r\n");
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
				new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" });
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
		/// Performs multiple character-to-string replacements on a string.
		/// </summary>
		/// <param name="source">The string to perform replacements on.</param>
		/// <param name="replacements">A dictionary of replacement mappings.</param>
		/// <returns></returns>
		public static string ReplaceMultiple(string source, IDictionary<char, string> replacements)
		{
			StringBuilder sb = new StringBuilder();
			string replacement;
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

		/// <summary>
		/// These characters are not allowed in file names on any common platform (Windows has the most restricted characters).
		/// </summary>
		private static HashSet<char> InvalidFileNameChars = GetInvalidFileNameChars();
		/// <summary>
		/// These file names are not allowed on Windows, either by themselves or with any extension, in any character case.
		/// </summary>
		private static HashSet<string> InvalidWindowsFileNames = new HashSet<string>(new string[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" });
		private static HashSet<char> GetInvalidFileNameChars()
		{
			HashSet<char> invalid = new HashSet<char>("\\/<>:\"|?*".ToCharArray());
			foreach (char c in System.IO.Path.GetInvalidFileNameChars())
				invalid.Add(c);
			for (int i = 0; i <= 31; i++)
				invalid.Add((char)i);
			return invalid;
		}

		/// <summary>
		/// Returns a copy of the string that should be valid for use as a file name on any platform.
		/// Based on https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names?rq=1
		/// </summary>
		/// <param name="text">A string that may not be safe to use as a file name.</param>
		/// <param name="replacementStr">Invalid characters should be replaced with this string. May be empty string.</param>
		/// <returns></returns>
		public static string MakeSafeForFileName(string text, string replacementStr = "")
		{
			if (replacementStr == null)
				replacementStr = "";
			StringBuilder sb = new StringBuilder();
			foreach (char c in text)
			{
				if (InvalidFileNameChars.Contains(c))
					sb.Append(replacementStr);
				else
					sb.Append(c);
			}
			string fileName = sb.ToString().TrimEnd('.', ' '); // dot and space characters are not allowed at the end of a Windows filename.
			string nameNoExt = fileName.Substring(0, fileName.Length - System.IO.Path.GetExtension(fileName).Length);
			if (InvalidWindowsFileNames.Contains(nameNoExt.ToUpper()))
				fileName = (!string.IsNullOrWhiteSpace(replacementStr) ? replacementStr : "_") + fileName;
			return fileName;
		}
		/// <summary>
		/// Given the input text, returns a string that is guaranteed safe to use as a C# variable name by replacing invalid characters with underscore, prepending an underscore if necessary (if the input string begins with a number), and limiting its length to 511 characters.
		/// </summary>
		/// <param name="text">A string which may or may not already be safe to use as a C# variable name.</param>
		/// <returns>A string, derived from the input string, that is guaranteed safe to use as a C# variable name.</returns>
		public static string MakeSafeForCsVariableName(string text)
		{
			if (string.IsNullOrEmpty(text))
				return "_";

			StringBuilder sb = new StringBuilder();
			foreach (char c in text)
			{
				if (char.IsLetterOrDigit(c) || c == '_')
					sb.Append(c);
				else
					sb.Append('_');
			}

			if (!char.IsLetter(sb[0]) && sb[0] != '_')
				sb[0] = '_';

			if (sb.Length > 511)
				sb.Length = 511;

			return sb.ToString();
		}
		/// <summary>
		/// Creates a Data URI from a given mime type string and data blob.
		/// </summary>
		/// <param name="mime">Mime Type (e.g. "image/jpeg")</param>
		/// <param name="data">Data to encode into the Data URI.</param>
		/// <returns></returns>
		public static string CreateDataUri(string mime, byte[] data)
		{
			return "data:" + mime + ";base64," + Convert.ToBase64String(data);
		}
		/// <summary>
		/// Splits the input string into an ordered list of substrings that could be concatenated together to recreate the original input string.  The produced strings will have a maximum length of [maxSegmentLength].  Instead of returning a list of strings, this method passes each substring into a callback method.
		/// 
		/// This method is designed to split a large text file into smaller chunks.
		/// </summary>
		/// <param name="fullText">The string to split.</param>
		/// <param name="maxSegmentLength">Maximum length of each substring. Minimum value is 1.</param>
		/// <param name="smartBreak">Try to end substrings early at characters ['\r', '\n', '.', '\t', ' '], to yield cleaner output.</param>
		/// <param name="callback">A callback method that will receive each string that is split out of the original string.</param>
		/// <returns></returns>
		public static void SplitIntoSegments(string fullText, int maxSegmentLength, bool smartBreak, Action<string> callback)
		{
			if (maxSegmentLength < 1)
				maxSegmentLength = 1;
			int idxStart = 0;
			while (idxStart < fullText.Length)
			{
				int subStrLength;
				int remaining = fullText.Length - idxStart;
				if (remaining <= maxSegmentLength)
					subStrLength = remaining;
				else
				{
					// More text remains than the max doc length.
					if (smartBreak)
					{
						int idxEnd = fullText.LastIndexOfAny(new char[] { '\r', '\n' }, (idxStart + maxSegmentLength) - 1, maxSegmentLength);
						if (idxEnd == -1)
							idxEnd = fullText.LastIndexOfAny(new char[] { '.', '\t', ' ' }, (idxStart + maxSegmentLength) - 1, maxSegmentLength);
						if (idxEnd > 0)
						{
							subStrLength = (idxEnd + 1) - idxStart;
							if (subStrLength <= 0)
								subStrLength = maxSegmentLength;
						}
						else
							subStrLength = maxSegmentLength;
					}
					else
						subStrLength = maxSegmentLength;
				}
				string text = fullText.Substring(idxStart, subStrLength);
				callback(text);
				idxStart += subStrLength;
			}
		}
		/// <summary>
		/// Splits the input string into an ordered list of substrings that could be concatenated together to recreate the original input string.  The produced strings will have a maximum length of [maxSegmentLength].
		/// 
		/// This method is designed to split a large text file into smaller chunks.
		/// </summary>
		/// <param name="fullText">The string to split.</param>
		/// <param name="maxSegmentLength">Maximum length of each substring. Minimum value is 1.</param>
		/// <param name="smartBreak">Try to end substrings early at characters ['\r', '\n', '.', '\t', ' '], to yield cleaner output.</param>
		/// <returns></returns>
		public static List<string> SplitIntoSegments(string fullText, int maxSegmentLength, bool smartBreak)
		{
			List<string> segments = new List<string>();
			SplitIntoSegments(fullText, maxSegmentLength, smartBreak, s => segments.Add(s));
			return segments;
		}
		/// <summary>
		/// Adds the appropriate English suffix to the specified number.  i.e.
		/// -3  ->  -3nd
		/// -2  ->  -2nd
		/// -1  ->  -1st
		/// 0  ->  0th
		/// 1  ->  1st
		/// 2  ->  2nd
		/// 3  ->  3rd
		/// 4  ->  4th
		/// 11 -> 11th
		/// 12 -> 12th
		/// 13 -> 13th
		/// 14 -> 14th
		/// 20 -> 20th
		/// 21 -> 21st
		/// 22 -> 22nd
		/// 23 -> 23rd
		/// 24 -> 24th
		/// </summary>
		/// <param name="baseNumber"></param>
		/// <returns></returns>
		public static string ToOrdinal(int baseNumber)
		{
			return baseNumber + GetOrdinalSuffix(baseNumber);
		}
		/// <summary>
		/// Returns the appropriate English suffix for the specified number.  i.e.
		/// 0  -> th
		/// 1  -> st
		/// 2  -> nd
		/// 3  -> rd
		/// 4  -> th
		/// 11 -> th
		/// 12 -> th
		/// 13 -> th
		/// 14 -> th
		/// 20 -> th
		/// 21 -> st
		/// 22 -> nd
		/// 23 -> rd
		/// 24 -> th
		/// </summary>
		/// <param name="baseNumber"></param>
		/// <returns></returns>
		public static string GetOrdinalSuffix(int baseNumber)
		{
			int abs = Math.Abs(baseNumber);
			int tensPlace = (abs / 10) % 10;
			if (tensPlace == 1)
				return "th";
			int onesPlace = abs % 10;
			switch (onesPlace)
			{
				case 1: return "st";
				case 2: return "nd";
				case 3: return "rd";
				default: return "th";
			}
		}
		/// <summary>
		/// Removes the file extension from a path and appends a new extension.
		/// </summary>
		/// <param name="path">Path to change the file extension in, e.g. "C:\Folder\File.ext".</param>
		/// <param name="newExtension">New extension, e.g. ".txt".</param>
		/// <returns></returns>
		public static string ReplaceFileExtension(string path, string newExtension)
		{
			string oldExtension = System.IO.Path.GetExtension(path);
			return path.Substring(0, path.Length - oldExtension.Length) + newExtension;
		}
		/// <summary>
		/// Indents each line of the given string.
		/// </summary>
		/// <param name="str">String that needs indented.</param>
		/// <param name="indentationString">Indentation string to insert at the beginning of each line.</param>
		/// <returns></returns>
		public static string Indent(string str, string indentationString = "\t")
		{
			if (str == null)
				throw new ArgumentException("Cannot indent a null string", "str");
			StringBuilder sb = new StringBuilder();
			sb.Append(indentationString);
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];
				sb.Append(c);
				if (c == '\r' || c == '\n')
				{
					if (c == '\r' && i + 1 < str.Length && str[i + 1] == '\n')
					{
						i++;
						sb.Append('\n');
					}
					sb.Append(indentationString);
				}
			}
			return sb.ToString();
		}
		/// <summary>
		/// <para>Given a terminal command, splits the command into its component substrings such that you could join them together again using a space as a separator to yield the original string.</para>
		/// <para>For example, given this raw input:</para>
		/// <para>"C:\Program Files\VideoLAN\VLC\vlc.exe" --rtsp-tcp "rtsp://127.0.0.1/"</para>
		/// <para>... The method returns ...</para>
		/// <para>["\"C:\\Program Files\\VideoLAN\\VLC\\vlc.exe\"", "--rtsp-tcp", "\"rtsp://127.0.0.1/\""]</para>
		/// <para>... And calling string.Join(" ", ...) on the array above would yield the original input string.</para>
		/// <para>Note that not all command strings may be entirely compatible with this method.</para>
		/// </summary>
		/// <param name="command">Command string that could be run in a text terminal.</param>
		/// <returns></returns>
		public static string[] ParseCommandLine(string command)
		{
			List<string> parts = new List<string>();
			char escapeChar = Platform.IsUnix() ? '\\' : '^';
			StringParser p = new StringParser(command);
			StringBuilder sb = new StringBuilder();
			bool isInQuote = false;
			while (!p.IsFinished())
			{
				sb.Append(p.GetUntilAny('"', ' ', escapeChar));
				char c = p.Get();
				if (c == '"')
				{
					sb.Append(c);
					isInQuote = !isInQuote;
				}
				else if (c == ' ')
				{
					if (isInQuote)
						sb.Append(c);
					else
					{
						parts.Add(sb.ToString());
						sb.Clear();
					}
				}
				else if (c == escapeChar)
				{
					sb.Append(c);
					if (!p.IsFinished())
						sb.Append(p.Get());
				}
			}
			parts.Add(sb.ToString());
			return parts.ToArray();
		}

		/// <summary>
		/// <para>Attempts to discover the character encoding of the data in the given stream.</para>
		/// <para>For the most dependable result, files without a byte order mark (BOM) will be read fully, possibly multiple times as different encodings are tried.  The stream will always be seeked to the beginning before returning.</para>
		/// <para>UTF-7 encoding will be converted to UTF-8.</para>
		/// <para>If the file data is not formatted using any known text encoding, returns null.</para>
		/// </summary>
		/// <param name="stream">A MemoryStream containing text data with an unknown encoding.</param>
		/// <param name="fullString">(output) the full string form of the data in the stream. May be null if the full string was not read.</param>
		/// <returns>The detected character encoding of the data in the stream, or null.</returns>
		public static Encoding DetectTextEncodingFromStream(MemoryStream stream, out string fullString)
		{
			byte[] fileData = stream.ToArray();
			fullString = null;

			// Read the BOM
			byte[] bom = new byte[4];
			stream.Seek(0, SeekOrigin.Begin);
			stream.Read(bom, 0, 4);
			stream.Seek(0, SeekOrigin.Begin);

			// Analyze the BOM
			Encoding detectedEncoding = GetEncodingFromBOM(bom);

			if (detectedEncoding != null)
			{
				fullString = detectedEncoding.GetString(fileData);
				if (detectedEncoding.CodePage == 65000)
					detectedEncoding = new UTF8Encoding(false);
				return detectedEncoding;
			}

			// Attempt to decode using various encodings
			foreach (Encoding enc in _EncodingDetection_GetEncodingsToTry())
			{
				try
				{
					if (enc != null)
					{
						fullString = enc.GetString(fileData);
						return enc;
					}
				}
				catch { }
			}
			return null;

			//			Encoding Utf8EncodingVerifier = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
			//			using (StreamReader reader = new StreamReader(stream, Utf8EncodingVerifier, true, 8 * 1024, true))
			//			{
			//				try
			//				{
			//					fullString = reader.ReadToEnd();
			//					detectedEncoding = reader.CurrentEncoding;
			//					fullString = detectedEncoding.GetString(stream.ToArray());
			//				}
			//				catch
			//				{
			//					// Failed to decode the file using the BOM/UT8. 
			//					// Assume it's local ANSI
			//#if NET6_0_OR_GREATER
			//					detectedEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);
			//#else
			//					detectedEncoding = Encoding.GetEncoding("windows-1252");
			//#endif
			//					if (detectedEncoding == null)
			//						detectedEncoding = Encoding.GetEncoding("ISO-8859-1");
			//					if (detectedEncoding == null)
			//						detectedEncoding = Encoding.Default;
			//					try
			//					{
			//						fullString = detectedEncoding.GetString(stream.ToArray());
			//					}
			//					catch { return null; }
			//				}
			//				finally
			//				{
			//					stream.Seek(0, SeekOrigin.Begin);
			//				}
			//				return detectedEncoding;
			//			}
		}
		private static IEnumerable<Encoding> _EncodingDetection_GetEncodingsToTry()
		{
			yield return new UTF8Encoding(false, true);
			yield return new UTF32Encoding(true, false, true);
			yield return new UTF32Encoding(false, false, true);
			yield return new UnicodeEncoding(false, false, true);
			yield return new UnicodeEncoding(true, false, true);
			yield return Encoding.GetEncoding("windows-1252");
			yield return Encoding.GetEncoding("ISO-8859-1");
			yield return Encoding.Default;
		}
		/// <summary>
		/// Determines a text file's encoding by analyzing its byte order mark (BOM).  If there is no identifiable BOM, returns null.
		/// </summary>
		/// <param name="bom">The byte order mark (BOM) to analyze (4 bytes).</param>
		/// <returns>The detected encoding.</returns>
		private static Encoding GetEncodingFromBOM(byte[] bom)
		{
#pragma warning disable SYSLIB0001
			if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
#pragma warning restore
			if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
			if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
			if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
			if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
			if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE
			return null;
		}
	}
}
