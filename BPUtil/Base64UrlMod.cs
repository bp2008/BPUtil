using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A class which provides an alternate form of Base64 encoding and decoding, using only characters which are valid, un-encoded, in URLs.
	/// </summary>
	public static class Base64UrlMod
	{
		private static Dictionary<char, char> encodeReplacementMap;
		private static Dictionary<char, char> decodeReplacementMap;
		static Base64UrlMod()
		{
			encodeReplacementMap = new Dictionary<char, char>();
			encodeReplacementMap['+'] = '-';
			encodeReplacementMap['/'] = '_';

			decodeReplacementMap = new Dictionary<char, char>();
			decodeReplacementMap['-'] = '+';
			decodeReplacementMap['_'] = '/';
		}
		/// <summary>
		/// <para>Returns a modified base64-encoded string using characters which can be used in URLs without URL-encoding.</para>
		/// <para>The output format is the same as standard Base64 except the characters '+' and '/' are respectively replaced with '-' and '_', while padding characters ('=') are simply removed from the end.</para>
		/// </summary>
		/// <param name="data">Data to encode.</param>
		/// <returns></returns>
		public static string ToBase64UrlMod(byte[] data)
		{
			return ToBase64UrlMod(Convert.ToBase64String(data));
		}
		/// <summary>
		/// <para>Returns a modified base64-encoded string using characters which can be used in URLs without URL-encoding.</para>
		/// <para>The output format is the same as standard Base64 except the characters '+' and '/' are respectively replaced with '-' and '_', while '=' padding characters ('=') are simply removed from the end.</para>
		/// </summary>
		/// <param name="base64">Base64-encoded string</param>
		/// <returns></returns>
		public static string ToBase64UrlMod(string base64)
		{
			return StringUtil.ReplaceMultiple(base64.TrimEnd('='), encodeReplacementMap);
		}
		/// <summary>
		/// <para>Decodes a string that was originally encoded via `ToBase64UrlMod`, returning a copy of the original byte array.</para>
		/// <para>This method can also handle standard Base64 input strings, however with reduced efficiency.</para>
		/// <para>This method can handle input strings which have had their trailing padding characters removed.</para>
		/// <para>This method can handle input strings with certain unwanted punctuation or whitespace appended to the start or end.</para>
		/// </summary>
		/// <param name="base64UrlMod">A string that was originally encoded via `ToBase64UrlMod`.</param>
		/// <returns></returns>
		public static byte[] FromBase64UrlMod(string base64UrlMod)
		{
			string b64 = Base64UrlModToBase64(base64UrlMod);
			return Convert.FromBase64String(b64);
		}
		/// <summary>
		/// <para>Decodes a string that was originally encoded via `ToBase64UrlMod`, returning a copy in regular Base64 format.</para>
		/// <para>This method can also handle standard Base64 input strings, however with reduced efficiency.</para>
		/// <para>This method can handle input strings which have had their trailing padding characters removed.</para>
		/// <para>This method can handle input strings with certain unwanted punctuation or whitespace appended to the start or end.</para>
		/// </summary>
		/// <param name="base64UrlMod">A string that was originally encoded via `ToBase64UrlMod`.</param>
		/// <returns></returns>
		public static string Base64UrlModToBase64(string base64UrlMod)
		{
			string b64 = StringUtil.ReplaceMultiple(base64UrlMod, decodeReplacementMap);
			b64 = b64.Trim('.', ',', '(', ')', '[', ']', '{', '}', '<', '>', '!', '\t', '\r', '\n', ' '); // Remove punctuation and whitespace from both ends.
			b64 = StringUtil.RepairBase64Padding(b64);
			return b64;
		}
	}
}
