using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BPUtil
{
	/// <summary>
	/// Contains helpful methods for working with binary data.
	/// </summary>
	public static class ByteUtil
	{
		/// <summary>
		/// Returns true of the content of the specified byte arrays exactly match each other, or if both arrays are null.
		/// </summary>
		/// <param name="a">An array to compare.</param>
		/// <param name="b">An array to compare.</param>
		/// <returns></returns>
		public static bool ByteArraysMatch(byte[] a, byte[] b)
		{
			if (a == null && b == null)
				return true;
			else if (a == null || b == null || a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}
		/// <summary>
		/// <para>Returns true if the specified byte arrays `a` and `b` are the same length and if every bit which is set in `mask` is the same between `a` and `b`.</para>
		/// <para>e.g.</para>
		/// <para>a = 01</para>
		/// <para>b = 00</para>
		/// <para>If mask = 10 or mask = 00 then true. Because the second bit is not set in mask, it does not get compared.  If mask = 01 or mask = 11 then false.</para>
		/// </summary>
		/// <param name="a">An array to compare.</param>
		/// <param name="b">An array to compare.</param>
		/// <param name="mask">An array of equal or lesser length to `a` and `b`.</param>
		/// <returns></returns>
		public static bool CompareWithMask(byte[] a, byte[] b, byte[] mask)
		{
			if (a == null && b == null)
				return true;
			else if (a == null || b == null || a.Length != b.Length || mask.Length > a.Length)
				return false;
			for (int i = 0; i < mask.Length; i++)
			{
				if ((a[i] & mask[i]) != (b[i] & mask[i]))
					return false;
			}
			return true;
		}
		/// <summary>
		/// Returns a new array containing the values of the first array XORed with the values of the second array.
		/// </summary>
		/// <param name="a">An array.</param>
		/// <param name="b">An array.</param>
		/// <returns></returns>
		public static byte[] XORByteArrays(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				throw new ArgumentException("Array lengths do not match! (" + a.Length + ", " + b.Length + ")");
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(a[i] ^ b[i]);
			return result;
		}
		/// <summary>
		/// Computes the "bitwise and" of the values in each array, and returns a new array containing the results.
		/// e.g. [0,1,1] &amp; [1,0,1] = [0,0,1]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <param name="b">An array.</param>
		/// <returns></returns>
		public static byte[] BitwiseAnd(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				throw new ArgumentException("Array lengths do not match! (" + a.Length + ", " + b.Length + ")");
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(a[i] & b[i]);
			return result;
		}
		/// <summary>
		/// Computes the "bitwise or" of the values in each array, and returns a new array containing the results.
		/// e.g. [0,1,1] | [1,0,1] = [1,1,1]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <param name="b">An array.</param>
		/// <returns></returns>
		public static byte[] BitwiseOr(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				throw new ArgumentException("Array lengths do not match! (" + a.Length + ", " + b.Length + ")");
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(a[i] | b[i]);
			return result;
		}
		/// <summary>
		/// Inverts every byte in the array. e.g. [0001] => [1110]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <returns></returns>
		public static void InvertBits(byte[] a)
		{
			if (a == null)
				return;
			for (int i = 0; i < a.Length; i++)
				a[i] = (byte)~(a[i]);
		}
		/// <summary>
		/// Returns a new byte array containing the inverse of the values of the source array. e.g. [0001] => [1110]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <returns></returns>
		public static byte[] GetInverse(byte[] a)
		{
			if (a == null)
				return null;
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)~(a[i]);
			return result;
		}
		/// <summary>
		/// Generates a byte array of the specified length, filled with cryptographically strong random values.
		/// </summary>
		/// <param name="numBytes"></param>
		/// <returns></returns>
		public static byte[] GenerateRandomBytes(int numBytes)
		{
			byte[] salt = new byte[numBytes];
			System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(salt);
			return salt;
		}
	}
	[Obsolete("C# 7.0 supports binary literals in the format '0b0000_0001', rendering this enum obsolete.  Happily, any number of underscores may be inserted between the 0 and 1 digits, for readability purposes, and the compiler will ignore them.")]
	[Flags]
	public enum ByteFlagConstants : byte
	{
		b0000_0000 = 0
		, b0000_0001 = 1
		, b0000_0010 = 2
		, b0000_0100 = 4
		, b0000_1000 = 8
		, b0001_0000 = 16
		, b0010_0000 = 32
		, b0100_0000 = 64
		, b1000_0000 = 128
	}
}
