using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class NumberUtil
	{
		public static sbyte ParseSByte(string str, sbyte defaultValue = 0)
		{
			if (sbyte.TryParse(str, out sbyte result))
				return result;
			return defaultValue;
		}
		public static byte ParseByte(string str, byte defaultValue = 0)
		{
			if (byte.TryParse(str, out byte result))
				return result;
			return defaultValue;
		}
		public static short ParseShort(string str, short defaultValue = 0)
		{
			if (short.TryParse(str, out short result))
				return result;
			return defaultValue;
		}
		public static ushort ParseUShort(string str, ushort defaultValue = 0)
		{
			if (ushort.TryParse(str, out ushort result))
				return result;
			return defaultValue;
		}
		public static int ParseInt(string str, int defaultValue = 0)
		{
			if (int.TryParse(str, out int result))
				return result;
			return defaultValue;
		}
		public static uint ParseUInt(string str, uint defaultValue = 0)
		{
			if (uint.TryParse(str, out uint result))
				return result;
			return defaultValue;
		}
		public static long ParseLong(string str, long defaultValue = 0)
		{
			if (long.TryParse(str, out long result))
				return result;
			return defaultValue;
		}
		public static ulong ParseULong(string str, ulong defaultValue = 0)
		{
			if (ulong.TryParse(str, out ulong result))
				return result;
			return defaultValue;
		}
		public static decimal ParseDecimal(string str, decimal defaultValue = 0)
		{
			if (decimal.TryParse(str, out decimal result))
				return result;
			return defaultValue;
		}
		public static float ParseFloat(string str, float defaultValue = 0)
		{
			if (float.TryParse(str, out float result))
				return result;
			return defaultValue;
		}
		public static double ParseDouble(string str, double defaultValue = 0)
		{
			if (double.TryParse(str, out double result))
				return result;
			return defaultValue;
		}

		#region First Number
		private static Regex rxFindSignedInteger = new Regex("(-?\\d+)", RegexOptions.Compiled);
		/// <summary>
		/// Returns the first 16-bit integer found inside the string, or null if none are found.  Note that hyphens may be incorrectly interpreted as minus signs.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static short? FirstShort(string str)
		{
			Match m = rxFindSignedInteger.Match(str);
			if (m.Success && short.TryParse(m.Groups[1].Value, out short result))
				return result;
			return null;
		}
		/// <summary>
		/// Returns the first 32-bit integer found inside the string, or null if none are found.  Note that hyphens may be incorrectly interpreted as minus signs.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static int? FirstInt(string str)
		{
			Match m = rxFindSignedInteger.Match(str);
			if (m.Success && int.TryParse(m.Groups[1].Value, out int result))
				return result;
			return null;
		}
		/// <summary>
		/// Returns the first 64-bit signed integer found inside the string, or null if none are found.  Note that hyphens may be incorrectly interpreted as minus signs.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static long? FirstLong(string str)
		{
			Match m = rxFindSignedInteger.Match(str);
			if (m.Success && long.TryParse(m.Groups[1].Value, out long result))
				return result;
			return null;
		}
		private static Regex rxFindUnsignedInteger = new Regex("(\\d+)", RegexOptions.Compiled);
		/// <summary>
		/// Returns the first 16-bit unsigned integer found inside the string, or null if none are found.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static ushort? FirstUShort(string str)
		{
			Match m = rxFindUnsignedInteger.Match(str);
			if (m.Success && ushort.TryParse(m.Groups[1].Value, out ushort result))
				return result;
			return null;
		}
		/// <summary>
		/// Returns the first 32-bit unsigned integer found inside the string, or null if none are found.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static uint? FirstUInt(string str)
		{
			Match m = rxFindUnsignedInteger.Match(str);
			if (m.Success && uint.TryParse(m.Groups[1].Value, out uint result))
				return result;
			return null;
		}
		/// <summary>
		/// Returns the first 64-bit unsigned integer found inside the string, or null if none are found.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static ulong? FirstULong(string str)
		{
			Match m = rxFindUnsignedInteger.Match(str);
			if (m.Success && ulong.TryParse(m.Groups[1].Value, out ulong result))
				return result;
			return null;
		}
		private static Regex rxFindFloat = new Regex("(-?\\d+(\\.\\d+)?)", RegexOptions.Compiled);
		/// <summary>
		/// Returns the first 32-bit floating point number found inside the string, or null if none are found.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static float? FirstFloat(string str)
		{
			Match m = rxFindFloat.Match(str);
			if (m.Success && float.TryParse(m.Groups[1].Value, out float result))
				return result;
			return null;
		}
		/// <summary>
		/// Returns the first 64-bit floating point number found inside the string, or null if none are found.
		/// </summary>
		/// <param name="str">str</param>
		/// <returns></returns>
		public static double? FirstDouble(string str)
		{
			Match m = rxFindFloat.Match(str);
			if (m.Success && double.TryParse(m.Groups[1].Value, out double result))
				return result;
			return null;
		}
		#endregion

		/// <summary>
		/// Converts the specified number of bytes to GiB (Gibibytes -- used in RAM and DISK sizes mostly).
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static double BytesToGiB(long bytes)
		{
			return bytes / 1073741824.0D;
		}
		/// <summary>
		/// Converts the specified double-precision number to a string with at most 1 decimal place.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static string ToFixed1(double n)
		{
			return n.ToString("0.#");
		}
		/// <summary>
		/// Converts the specified double-precision number to a string with at most 2 decimal places.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static string ToFixed2(double n)
		{
			return n.ToString("0.##");
		}
		/// <summary>
		/// Converts the specified double-precision number to a string with at most 3 decimal places.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static string ToFixed3(double n)
		{
			return n.ToString("0.###");
		}
		/// <summary>
		/// Converts the specified double-precision number to a string with at most 4 decimal places.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static string ToFixed4(double n)
		{
			return n.ToString("0.####");
		}

		public static int Clamp(int n, int min, int max)
		{
			if (n < min)
				return min;
			else if (n > max)
				return max;
			else
				return n;
		}
		public static T Clamp<T>(T n, T min, T max) where T : IComparable<T>
		{
			T result = n;
			if (n.CompareTo(max) > 0)
				result = max;
			if (n.CompareTo(min) < 0)
				result = min;
			return result;
		}
	}
}
