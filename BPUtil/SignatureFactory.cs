using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Performs sign and verify tasks using ECDsaCng. This class is thread-safe.
	/// </summary>
	public class SignatureFactory : IDisposable
	{
		private readonly ECDsaCng dsa;
		/// <summary>
		/// Creates a new SignatureFactory with a random key.
		/// </summary>
		public SignatureFactory()
		{
			// Valid key sizes are 256, 384, and 521 bits.
			dsa = new ECDsaCng(521);
		}
		/// <summary>
		/// Creates a new SignatureFactory from the specified private key.
		/// </summary>
		/// <param name="key">Base64 string of the private key previously exported from a SignatureFactory.</param>
		public SignatureFactory(string key) : this(key, true) { }
		/// <summary>
		/// Creates a new SignatureFactory from the specified key.
		/// </summary>
		/// <param name="key">Base64 string previously exported from a SignatureFactory.</param>
		/// <param name="isPrivateKey">If true, this key is treated as a private key. If false, this key is treated as a public key and signing will not be available.</param>
		public SignatureFactory(string key, bool isPrivateKey)
		{
			dsa = new ECDsaCng(CngKey.Import(Convert.FromBase64String(key), isPrivateKey ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob));
		}
		public void Dispose()
		{
			lock (dsa)
				dsa.Dispose();
		}
		/// <summary>
		/// Signs a hash of the specified data and returns the signature.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public byte[] Sign(byte[] data)
		{
			lock (dsa)
				return dsa.SignData(data);
		}
		/// <summary>
		/// Verifies a signature.
		/// </summary>
		/// <param name="data">The data that was signed.</param>
		/// <param name="signature">The signature data to be verified.</param>
		/// <returns></returns>
		public bool Verify(byte[] data, byte[] signature)
		{
			if (signature == null || signature.Length < 100)
				return false; // ECDsa signatures are typically length 132, although implementations can vary.
			lock (dsa)
				return dsa.VerifyData(data, signature);
		}
		/// <summary>
		/// Computes a cryptographic signature of a string.
		/// Later, the signature can be provided along with the data string to the <see cref="Verify(string, string)"/> method to verify that the data string has not been tampered with.
		/// Returns a string in <see cref="Base64UrlMod"/> format.
		/// </summary>
		/// <param name="data">A string to sign.</param>
		/// <returns>Returns a string in <see cref="Base64UrlMod"/> format.</returns>
		public string Sign(string data)
		{
			return Base64UrlMod.ToBase64UrlMod(Sign(Encoding.UTF8.GetBytes(data)));
		}
		/// <summary>
		/// Verifies that the signature matches the data string. If the data and/or signature have been tampered with, this method will return false.
		/// </summary>
		/// <param name="data">The string to verify the signature of.</param>
		/// <param name="signature">The signature used for verification.  (Base64 or <see cref="Base64UrlMod"/> format)</param>
		/// <returns></returns>
		public bool Verify(string data, string signature)
		{
			return Verify(Encoding.UTF8.GetBytes(data), Base64UrlMod.FromBase64UrlMod(signature));
		}
		/// <summary>
		/// Returns the private key in base64 format so it can be reused in future SignatureFactory instances.
		/// </summary>
		/// <returns></returns>
		public string ExportPrivateKey()
		{
			byte[] key;
			lock (dsa)
				key = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
			return Convert.ToBase64String(key);
		}
		/// <summary>
		/// Returns the public key in base64 format so it can be reused in future SignatureFactory instances.
		/// </summary>
		/// <returns></returns>
		public string ExportPublicKey()
		{
			byte[] key;
			lock (dsa)
				key = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);
			return Convert.ToBase64String(key);
		}
	}
}
