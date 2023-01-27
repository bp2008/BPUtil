using System;

namespace BPUtil
{
	public static class Hex
	{
		/// <summary>
		/// Converts a byte array to a hexidecimal string.
		/// </summary>
		/// <param name="array">Byte array to convert</param>
		/// <param name="uppercase">If true, A-F shall be uppercase, and the conversion will be slightly faster. Default is false for backwards compatibility.</param>
		/// <returns></returns>
		public static string ToHex(byte[] array, bool uppercase = false)
		{
			if (uppercase)
				return BitConverter.ToString(array).Replace("-", "");
			else
				return BitConverter.ToString(array).Replace("-", "").ToLower();
		}
		/// <summary>
		/// Converts a hexidecimal string to a byte array. Based on https://stackoverflow.com/a/14332574/814569
		/// </summary>
		/// <param name="hex">Hex string</param>
		/// <returns></returns>
		public static byte[] ToByteArray(string hex)
		{
			if ((hex.Length & 1) != 0)
				throw new ArgumentException("Input must have even number of characters");

			int length = hex.Length / 2;
			byte[] ret = new byte[length];
			for (int i = 0, j = 0; i < length; i++)
			{
				int high = ParseNybble(hex[j++]);
				int low = ParseNybble(hex[j++]);
				ret[i] = (byte)((high << 4) | low);
			}

			return ret;
		}
		private static int ParseNybble(char c)
		{
			if (c >= '0' && c <= '9')
			{
				return c - '0';
			}
			c = (char)(c & ~0x20);
			if (c >= 'A' && c <= 'F')
			{
				return c - ('A' - 10);
			}
			throw new ArgumentException("Invalid nybble: " + c);
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
