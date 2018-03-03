using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BPUtil
{
	public static class Hex
	{
		public static string ToHex(byte[] array)
		{
			return BitConverter.ToString(array).Replace("-", "").ToLower();
		}
		public static byte[] ToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}
		/// <summary>
		/// Converts a prefixed hex string (e.g. "0x185b8ae584") to a long.
		/// </summary>
		/// <param name="prefixedHex"></param>
		/// <returns></returns>
		public static long PrefixedHexToLong(string prefixedHex)
		{
			return Convert.ToInt64(prefixedHex, 16);
		}
	}
}
