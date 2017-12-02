using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
