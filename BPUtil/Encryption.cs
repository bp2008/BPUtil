using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Performs AES encryption and Decryption using <see cref="CipherMode.CBC"/>. This class is thread-safe.
	/// </summary>
	public class Encryption
	{
		public readonly byte[] Key = null;
		public readonly byte[] IV = null;

		/// <summary>
		/// Creates a new Encryption instance with a random Initialization Vector and 256-bit Key
		/// </summary>
		public Encryption()
		{
			using (Aes aes = Aes.Create())
			{
				if (aes.KeySize != 256 && aes.ValidKeySize(256))
					aes.KeySize = 256;
				Key = aes.Key;
				IV = aes.IV;
			}
		}
		/// <summary>
		/// Creates a new Encryption instance with a predefined Key and Initialization Vector
		/// </summary>
		/// <param name="key">Encryption Key, Base64-encoded</param>
		/// <param name="iv">Initialization Vector, Base64-encoded</param>
		public Encryption(string key, string iv)
		{
			this.Key = Convert.FromBase64String(key);
			this.IV = Convert.FromBase64String(iv);
		}
		/// <summary>
		/// Creates a new Encryption instance with a predefined Key and Initialization Vector
		/// </summary>
		/// <param name="key">Encryption Key</param>
		/// <param name="iv">Initialization Vector</param>
		public Encryption(byte[] key, byte[] iv)
		{
			this.Key = key;
			this.IV = iv;
		}

		public byte[] Encrypt(byte[] plain)
		{
			using (Aes aes = Aes.Create())
			{
				aes.Mode = CipherMode.CBC;
				aes.IV = IV;
				aes.Key = Key;
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
						cs.Write(plain, 0, plain.Length);
					return ms.ToArray();
				}
			}
		}
		public byte[] Decrypt(byte[] cipher)
		{
			using (Aes aes = Aes.Create())
			{
				aes.Mode = CipherMode.CBC;
				aes.IV = IV;
				aes.Key = Key;
				using (MemoryStream ms = new MemoryStream())
				{
					byte[] buf = new byte[2048];
					using (MemoryStream input = new MemoryStream(cipher))
					using (CryptoStream cs = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
					{
						int read = cs.Read(buf, 0, buf.Length);
						while (read > 0)
						{
							ms.Write(buf, 0, read);
							read = cs.Read(buf, 0, buf.Length);
						}
					}
					return ms.ToArray();
				}
			}
		}
	}
}
