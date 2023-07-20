using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class StringSorting
	{
		/// <summary>
		/// <para>Compare character-by-character, but whenever we encounter a digit, grab all consecutive digits and treat them as a single integer.</para>
		/// <para>Integers shall be considered greater than whitespace characters but less than any other type of character (a,b,c,$,%, etc).</para>
		/// <para>Additionally, null strings are less than non-null strings.</para>
		/// <para>Thus, the set ["a50", "a3", "a400", "a s", "as", "as", null, null] would be sorted to:</para>
		/// <para>[null, null, "a s", "a3", "a50", "a400", "as", "as"]</para>
		/// </summary>
		/// <param name="a">One of two strings to compare.</param>
		/// <param name="b">One of two strings to compare.</param>
		/// <returns></returns>
		public static int CompareStringsContainingIntegers(string a, string b)
		{
			if (a == null && b == null)
				return 0;
			else if (a == null)
				return -1;
			else if (b == null)
				return 1;
			int min = Math.Min(a.Length, b.Length);
			int iA = 0, iB = 0;
			while (iA < a.Length && iB < b.Length)
			{
				string str_numA, str_numB;
				int? numA = GetNumberAtStringIndex(a, iA, out str_numA);
				int? numB = GetNumberAtStringIndex(b, iB, out str_numB);
				if (numA == null && numB == null)
				{
					// We're looking at two characters.
					char cA = char.ToLower(a[iA]);
					char cB = char.ToLower(b[iB]);
					if (cA < cB)
						return -1;
					else if (cA > cB)
						return 1;
				}
				else if (numA == null)
				{
					// Character from [a], number from [b]
					return char.IsWhiteSpace(a[iA]) ? -1 : 1;
				}
				else if (numB == null)
				{
					// Number from [a], character from [b]
					return char.IsWhiteSpace(b[iB]) ? 1 : -1;
				}
				else
				{
					// We're looking at two numbers.
					if (numA > numB)
						return 1;
					else if (numA < numB)
						return -1;
				}
				iA += str_numA == null ? 1 : str_numA.Length;
				iB += str_numB == null ? 1 : str_numB.Length;
			}
			// We've reached the end of at least one string without finding a difference.
			if (iA == a.Length && iB == b.Length)
				return 0; // both strings were compared to their ends.
			else if (iA == a.Length)
				return -1; // [b] has characters remaining, making [a] lesser.
			else
				return 1; // [a] has characters remaining, making [a] greater.
		}
		private static int? GetNumberAtStringIndex(string str, int startIndex, out string str_num)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = startIndex; i < str.Length; i++)
			{
				if (char.IsDigit(str[i]))
					sb.Append(str[i]);
				else
					break;
			}
			if (sb.Length > 0)
			{
				int num;
				str_num = sb.ToString();
				if (int.TryParse(str_num, out num))
					return num;
			}
			str_num = null;
			return null;
		}
	}
}
