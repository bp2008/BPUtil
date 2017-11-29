using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace BPUtil
{
	public class Hash
	{
		/// <summary>
		/// Computes the SHA512 hash of the specified binary data, optionally appending a binary salt value.
		/// </summary>
		/// <param name="data">Binary data to hash.</param>
		/// <param name="salt">A salt value to append directly to the end of the data before hashing.</param>
		/// <returns></returns>
		public static byte[] GetSHA512Bytes(byte[] data, byte[] salt = null)
		{
			if (salt != null && salt.Length > 0)
			{
				byte[] salted = new byte[data.Length + salt.Length];
				Array.Copy(data, 0, salted, 0, data.Length);
				Array.Copy(salt, 0, salted, data.Length, salt.Length);
				data = salted;
			}
			SHA512 sha = new SHA512CryptoServiceProvider();
			byte[] result = sha.ComputeHash(data);
			return result;
		}
		/// <summary>
		/// Computes the SHA512 hash of the specified string, optionally appending a binary salt value.
		/// </summary>
		/// <param name="s">A UTF8-encoded string.</param>
		/// <param name="salt">A salt value to append directly to the end of the string's binary data before hashing.</param>
		/// <returns></returns>
		public static byte[] GetSHA512Bytes(string s, byte[] salt = null)
		{
			byte[] data = UTF8Encoding.UTF8.GetBytes(s);
			return GetSHA512Bytes(data, salt);
		}
		/// <summary>
		/// Encodes the specified string as UTF8 and calculates the SHA512 hash of the data.
		/// The hash is returned as a lower-case hexidecimal string 128 characters long.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string GetSHA512Hex(string s)
		{
			return BitConverter.ToString(GetSHA512Bytes(s)).Replace("-", "").ToLower();
		}
		/// <summary>
		/// Computes the SHA256 hash of the specified binary data, optionally appending a binary salt value.
		/// </summary>
		/// <param name="data">Binary data to hash.</param>
		/// <param name="salt">A salt value to append directly to the end of the data before hashing.</param>
		/// <returns></returns>
		public static byte[] GetSHA256Bytes(byte[] data, byte[] salt = null)
		{
			if (salt != null && salt.Length > 0)
			{
				byte[] salted = new byte[data.Length + salt.Length];
				Array.Copy(data, 0, salted, 0, data.Length);
				Array.Copy(salt, 0, salted, data.Length, salt.Length);
				data = salted;
			}
			SHA256 sha = new SHA256CryptoServiceProvider();
			byte[] result = sha.ComputeHash(data);
			return result;
		}
		/// <summary>
		/// Computes the SHA256 hash of the specified string, optionally appending a binary salt value.
		/// </summary>
		/// <param name="s">A UTF8-encoded string.</param>
		/// <param name="salt">A salt value to append directly to the end of the string's binary data before hashing.</param>
		/// <returns></returns>
		public static byte[] GetSHA256Bytes(string s, byte[] salt = null)
		{
			byte[] data = UTF8Encoding.UTF8.GetBytes(s);
			return GetSHA256Bytes(data, salt);
		}
		/// <summary>
		/// Encodes the specified string as UTF8 and calculates the SHA256 hash of the data.
		/// The hash is returned as a lower-case hexidecimal string 64 characters long.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string GetSHA256Hex(string s)
		{
			return BitConverter.ToString(GetSHA256Bytes(s)).Replace("-", "").ToLower();
		}
		public static byte[] GetSHA1Bytes(string s, byte[] salt = null)
		{
			byte[] data = UTF8Encoding.UTF8.GetBytes(s);
			if (salt != null && salt.Length > 0)
			{
				byte[] salted = new byte[data.Length + salt.Length];
				Array.Copy(data, 0, salted, 0, data.Length);
				Array.Copy(salt, 0, salted, data.Length, salt.Length);
				data = salted;
			}
			SHA1 sha = new SHA1CryptoServiceProvider();
			byte[] result = sha.ComputeHash(data);
			return result;
		}
		/// <summary>
		/// Encodes the specified string as UTF8 and calculates the SHA1 hash of the data.
		/// The hash is returned as a lower-case hexidecimal string 40 characters long.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string GetSHA1Hex(string s)
		{
			return BitConverter.ToString(GetSHA1Bytes(s)).Replace("-", "").ToLower();
		}
		public static byte[] GetMD5Bytes(string s, byte[] salt = null)
		{
			byte[] data = UTF8Encoding.UTF8.GetBytes(s);
			if (salt != null && salt.Length > 0)
			{
				byte[] salted = new byte[data.Length + salt.Length];
				Array.Copy(data, 0, salted, 0, data.Length);
				Array.Copy(salt, 0, salted, data.Length, salt.Length);
				data = salted;
			}
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] result = md5.ComputeHash(data);
			return result;
		}
		/// <summary>
		/// Encodes the specified string as UTF8 and calculates the MD5 hash of the data.
		/// The hash is returned as a lower-case hexidecimal string 32 characters long.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string GetMD5Hex(string s)
		{
			return BitConverter.ToString(GetMD5Bytes(s)).Replace("-", "").ToLower();
		}
	}
}
