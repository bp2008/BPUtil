using System;

namespace BPUtil
{
	/// <summary>
	/// Provides an interface for parsing strings.
	/// </summary>
	public class StringParser
	{
		private string strLocal;
		private int iNow = 0;
		private static uint iContextStringRadiusDefault = 40;
		public StringParser(string str)
		{
			this.strLocal = str;
		}
		/// <summary>
		/// Gets the current parsing index.
		/// </summary>
		/// <returns></returns>
		public int CurrentIndex
		{
			get
			{
				return iNow;
			}
		}
		/// <summary>
		/// Gets the string associated with this StringParser.
		/// </summary>
		/// <returns></returns>
		public string MyString
		{
			get
			{
				return strLocal;
			}
		}
		/// <summary>
		/// Returns true if the current parsing index is > 0.
		/// </summary>
		/// <returns></returns>
		public bool IsStarted()
		{
			return iNow > 0;
		}
		/// <summary>
		/// Returns true if the entire string has been parsed.
		/// </summary>
		/// <returns></returns>
		public bool IsFinished()
		{
			return iNow >= strLocal.Length;
		}
		/// <summary>
		/// Returns a single character without consuming it.
		/// </summary>
		/// <param name="offset">Offset from current parsing index. If this offset yields an invalid index, (char)0 is returned.</param>
		/// <returns></returns>
		public char Peek(int offset = 0)
		{
			int idx = iNow + offset;
			if (idx < strLocal.Length)
				return strLocal[idx];
			return (char)0;
		}
		/// <summary>
		/// Returns the next [count] characters without consuming them.
		/// </summary>
		/// <param name="count">Number of characters to return. If this many characters are not available, "" is returned.</param>
		/// <returns></returns>
		public string PeekString(int count)
		{
			if (iNow < strLocal.Length)
			{
				if (strLocal.Length < iNow + count)
					count = strLocal.Length - iNow;
				return CppSubstr(strLocal, iNow, count);
			}
			return "";
		}
		/// <summary>
		/// Consumes the specified number of characters.
		/// </summary>
		/// <returns></returns>
		public void ThrowAway(uint count = 1)
		{
			iNow += (int)count;
		}
		/// <summary>
		/// Consumes a single character and returns it. If there are no characters left, returns (char)0.
		/// </summary>
		/// <returns></returns>
		public char Get()
		{
			if (iNow < strLocal.Length)
				return strLocal[iNow++];
			return (char)0;
		}
		/// <summary>
		/// Consumes the specified number of characters (up to the number available before end of string) and returns a copy of what was consumed.
		/// </summary>
		/// <param name="count">The requested number of characters. The returned string may be shorter (or empty) if the end of the string is reached.</param>
		/// <returns></returns>
		public string Get(uint count)
		{
			int cnt = (int)count;
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				iNow += cnt;
				if (strLocal.Length < iStart + cnt)
					cnt = strLocal.Length - iStart;
				if (cnt == 0)
					return "";
				return CppSubstr(strLocal, iStart, cnt);
			}
			return "";
		}
		/// <summary>
		/// Consumes characters up to but not including the specified character.  If this character is not found, the remainder of the string is consumed.
		/// </summary>
		/// <param name="c">Character to consume up to, but not through.</param>
		public void ThrowAwayUntil(char c)
		{
			if (iNow < strLocal.Length)
			{
				int idx = strLocal.IndexOf(c, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else
					iNow = idx;
			}
		}
		/// <summary>
		/// Consumes characters up to but not including the specified substring.  If the substring is not found, the remainder of the string is consumed.
		/// </summary>
		/// <param name="subStr">String to consume up to, but not through.</param>
		public void ThrowAwayUntil(string subStr)
		{
			if (iNow < strLocal.Length)
			{
				int idx = strLocal.IndexOf(subStr, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else
					iNow = idx;
			}
		}
		/// <summary>
		/// Consumes characters until one of the specified characters is found.  If none are found, the remainder of the string is consumed.
		/// </summary>
		/// <param name="chars">Characters to end consumption upon reaching.</param>
		public void ThrowAwayUntilAny(params char[] chars)
		{
			if (iNow < strLocal.Length)
			{
				int idx = strLocal.IndexOfAny(chars, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else
					iNow = idx;
			}
		}
		/// <summary>
		/// Consumes characters up to and including the specified character.  If this character is not found, the remainder of the string is consumed.
		/// </summary>
		/// <param name="c">Character to consume through.</param>
		public void ThrowAwayThrough(char c)
		{
			if (iNow < strLocal.Length)
			{
				int idx = strLocal.IndexOf(c, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else
					iNow = idx + 1;
			}
		}
		/// <summary>
		/// Consumes characters up to and including the specified substring.  If the substring is not found, the remainder of the string is consumed.
		/// </summary>
		/// <param name="subStr">String to consume through.</param>
		public void ThrowAwayThrough(string subStr)
		{
			if (iNow < strLocal.Length)
			{
				int idx = strLocal.IndexOf(subStr, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else
					iNow = idx + subStr.Length;
			}
		}
		/// <summary>
		/// Consumes characters until one of the specified characters is found, consuming that character too.  If none are found, the remainder of the string is consumed.
		/// </summary>
		/// <param name="chars">Consumption ends after one of these is consumed.</param>
		public void ThrowAwayThroughAny(params char[] chars)
		{
			if (iNow < strLocal.Length)
			{
				int idx = strLocal.IndexOfAny(chars, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else
					iNow = idx + 1;
			}
		}
		/// <summary>
		/// Consumes all characters up to but not including the specified character.  If this character is not found, the remainder of the string is consumed.  A copy of the consumed value is returned.
		/// </summary>
		/// <param name="c">Character to consume up to, but not through.</param>
		/// <returns></returns>
		public string GetUntil(char c)
		{
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				int idx = strLocal.IndexOf(c, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else if (idx == iStart)
					return "";
				else
					iNow = idx;

				return CppSubstr(strLocal, iStart, iNow - iStart);
			}
			return "";
		}
		/// <summary>
		/// Consumes all characters up to but not including the specified substring.  If the substring is not found, the remainder of the string is consumed.  A copy of the consumed value is returned.
		/// </summary>
		/// <param name="subStr">String to consume up to, but not through.</param>
		/// <returns></returns>
		public string GetUntil(string subStr)
		{
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				int idx = strLocal.IndexOf(subStr, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else if (idx == iStart)
					return "";
				else
					iNow = idx;

				return CppSubstr(strLocal, iStart, iNow - iStart);
			}
			return "";
		}
		/// <summary>
		/// Consumes characters until one of the specified characters is found.  If none are found, the remainder of the string is consumed.  A copy of the consumed value is returned.
		/// </summary>
		/// <param name="chars">Characters to end consumption upon reaching.</param>
		public string GetUntilAny(params char[] chars)
		{
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				int idx = strLocal.IndexOfAny(chars, iNow);
				if (idx == -1)
					iNow = strLocal.Length;
				else if (idx == iStart)
					return "";
				else
					iNow = idx;

				return CppSubstr(strLocal, iStart, iNow - iStart);
			}
			return "";
		}
		/// <summary>
		/// Consumes characters until one of the specified characters is found or the specified index is reached.  If none are found and the index is not reached, the remainder of the string is consumed.  A copy of the consumed value is returned.
		/// </summary>
		/// <param name="chars">Characters to end consumption upon reaching.</param>
		/// <param name="stopAt">Index to end consumption upon reaching.</param>
		public string GetUntilAny(char[] chars, int stopAt)
		{
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				int idx = strLocal.IndexOfAny(chars, iNow);
				// If we were given a valid stopAt index that we haven't yet passed, make sure we don't pass it.
				if (stopAt < idx && stopAt >= iStart)
					idx = stopAt;
				if (idx == -1)
					iNow = strLocal.Length;
				else if (idx == iStart)
					return "";
				else
					iNow = idx;

				return CppSubstr(strLocal, iStart, iNow - iStart);
			}
			return "";
		}
		/// <summary>
		/// Consumes all characters up to and including the specified character.  If this character is not found, the remainder of the string is consumed.  A copy of the consumed value is returned.
		/// </summary>
		/// <param name="c">Character to consume through.</param>
		/// <returns></returns>
		public string GetThrough(char c)
		{
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				int idx = strLocal.IndexOf(c, iNow) + 1;
				if (idx == 0)
					iNow = strLocal.Length;
				else if (idx == iStart)
					return "";
				else
					iNow = idx;

				return CppSubstr(strLocal, iStart, iNow - iStart);
			}
			return "";
		}
		/// <summary>
		/// Consumes all characters up to and including the specified substring.  If the substring is not found, the remainder of the string is consumed.  A copy of the consumed value is returned.
		/// </summary>
		/// <param name="subStr">String to consume through.</param>
		/// <returns></returns>
		public string GetThrough(string subStr)
		{
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				int idx = strLocal.IndexOf(subStr, iNow) + subStr.Length;
				if (idx == subStr.Length - 1)
					iNow = strLocal.Length;
				else if (idx == iStart)
					return "";
				else
					iNow = idx;

				return CppSubstr(strLocal, iStart, iNow - iStart);
			}
			return "";
		}
		/// <summary>
		/// Consumes characters until one of the specified characters is found, consuming that character too.  If none are found, the remainder of the string is consumed.  A copy of the consumed value is returned.
		/// </summary>
		/// <param name="chars">Consumption ends after one of these is consumed.</param>
		public string GetThroughAny(params char[] chars)
		{
			if (iNow < strLocal.Length)
			{
				int iStart = iNow;
				int idx = strLocal.IndexOfAny(chars, iNow) + 1;
				if (idx == 0)
					iNow = strLocal.Length;
				else if (idx == iStart)
					return "";
				else
					iNow = idx;

				return CppSubstr(strLocal, iStart, iNow - iStart);
			}
			return "";
		}
		/// <summary>
		/// Consumes any whitespace chars starting at the current parsing index until a non-whitespace char is reached or the end of the string is reached.
		/// </summary>
		public void ThrowAwayWhitespace()
		{
			while (iNow < strLocal.Length && (strLocal[iNow] == ' ' || strLocal[iNow] == '\t' || strLocal[iNow] == '\r' || strLocal[iNow] == '\n'))
				iNow++;
		}
		/// <summary>
		/// Consumes any of the specified chars starting at the current parsing index until an unspecified char is reached or the end of the string is reached.
		/// </summary>
		/// <param name="chars">Characters to consume.</param>
		public void ThrowAwayChars(params char[] chars)
		{
			bool found = true;
			int i;
			while (found && iNow < strLocal.Length)
			{
				found = false;
				for (i = 0; i < chars.Length; i++)
					if (strLocal[iNow] == chars[i])
					{
						found = true;
						iNow++;
						break;
					}
			}
		}
		/// <summary>
		/// Returns a string up to (<see cref="iContextStringRadiusDefault"/> * 2) characters long containing the characters around the current parsing index.
		/// </summary>
		/// <returns></returns>
		public string GetContextForErrorMessages()
		{
			return GetContextForErrorMessages(iContextStringRadiusDefault);
		}
		/// <summary>
		/// Returns a string up to (n * 2) characters long containing the characters around the current parsing index.
		/// </summary>
		/// <returns></returns>
		public string GetContextForErrorMessages(uint n)
		{
			int iStart = iNow - (int)n;
			if (iNow < n)
				iStart = 0;
			return CppSubstr(strLocal, iStart, (int)n * 2);
		}

#pragma warning disable CS0419 // Ambiguous reference in cref attribute
		/// <summary>
		/// Sets the default length of context string used when no length is specified in <see cref="GetContextForErrorMessages"/>. This value is initialized to 40.
		/// </summary>
		/// <param name="defRadius">Maximum length of context string used when no length is specified in <see cref="GetContextForErrorMessages"/>.</param>
		public void SetContextStringRadiusDefault(uint defRadius)
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
		{
			iContextStringRadiusDefault = defRadius;
		}
		/// <summary>
		/// Behaves like std::string::substr from C++.
		/// </summary>
		/// <param name="str">String to get a substring from.</param>
		/// <param name="pos">
		/// <para>Position of the first character to be copied as a substring.</para>
		/// <para>If this is equal to the string length, the function returns an empty string.</para>
		/// <para>If this is greater than the string length, it throws IndexOutOfRangeException.</para>
		/// <para>Note: The first character is denoted by a value of 0 (not 1).</para>
		/// </param>
		/// <param name="len">
		/// <para>Number of characters to include in the substring (if the string is shorter, as many characters as possible are used).</para>
		/// <para>A value of int.MaxValue indicates all characters until the end of the string.</para>
		/// </param>
		/// <returns></returns>
		private string CppSubstr(string str, int pos = 0, int len = int.MaxValue)
		{
			if (str == null)
				throw new ArgumentException("str must not be null", "str");
			if (pos < 0)
				throw new ArgumentException("pos must be non-negative", "pos");
			if (len < 0)
				throw new ArgumentException("len must be non-negative", "len");
			if (pos == str.Length)
				return "";
			if (pos > str.Length)
				throw new IndexOutOfRangeException("Index " + pos + " is out of range of string with length " + str.Length);
			if (pos + len > str.Length)
				len = str.Length - pos;
			return str.Substring(pos, len);
		}
	}
}