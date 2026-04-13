#if NETFRAMEWORK || NET6_PLUS_WIN

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
		/// The key size in bits for the elliptic curve used by this instance.
		/// </summary>
		public int KeySize { get; }
		/// <summary>
		/// Creates a new SignatureFactory with a random P-521 key.
		/// </summary>
		public SignatureFactory() : this(521) { }
		/// <summary>
		/// Creates a new SignatureFactory with a random key of the specified curve size.
		/// </summary>
		/// <param name="keySize">Elliptic curve key size in bits (256, 384, or 521).</param>
		public SignatureFactory(int keySize)
		{
			KeySize = keySize;
			dsa = new ECDsaCng(keySize);
		}
		/// <summary>
		/// Creates a new SignatureFactory from the specified private key, inferring curve size from the blob.
		/// </summary>
		/// <param name="key">Base64 string of the private key previously exported from a SignatureFactory.</param>
		public SignatureFactory(string key) : this(key, true) { }
		/// <summary>
		/// Creates a new SignatureFactory from the specified key, inferring curve size from the blob.
		/// </summary>
		/// <param name="key">Base64 string previously exported from a SignatureFactory.</param>
		/// <param name="isPrivateKey">If true, this key is treated as a private key. If false, this key is treated as a public key and signing will not be available.</param>
		public SignatureFactory(string key, bool isPrivateKey)
		{
			dsa = new ECDsaCng(CngKey.Import(Convert.FromBase64String(key), isPrivateKey ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob));
			KeySize = dsa.KeySize;
		}
		/// <summary>
		/// Disposes the underlying ECDsaCng instance. After calling this method, the SignatureFactory should not be used for signing or verifying. This method is thread-safe.
		/// </summary>
		public void Dispose()
		{
			lock (dsa)
				dsa.Dispose();
		}
		/// <summary>
		/// Signs a hash of the specified data and returns the signature.
		/// </summary>
		/// <param name="data">The data to sign.</param>
		/// <returns>The signature as a byte array.</returns>
		public byte[] Sign(byte[] data)
		{
			lock (dsa)
				return dsa.SignData(data);
		}
		/// <summary>
		/// Returns the minimum expected signature length for this instance's curve.
		/// ECDSA signatures in IEEE P1363 format are exactly 2 * ceil(keySize / 8) bytes,
		/// but we use a safety margin of 75% of that value as the minimum sanity threshold.
		/// </summary>
		private int MinimumSignatureLength
		{
			get { return (int)(2.0 * Math.Ceiling(KeySize / 8.0) * 0.75); }
		}
		/// <summary>
		/// Verifies a signature.
		/// </summary>
		/// <param name="data">The data that was signed.</param>
		/// <param name="signature">The signature data to be verified.</param>
		/// <returns>True if the signature is valid; otherwise, false.</returns>
		public bool Verify(byte[] data, byte[] signature)
		{
			if (signature == null || signature.Length < MinimumSignatureLength)
				return false; // ECDsa 521 signatures are typically length 132, although implementations can vary. The first half of the signature is the "R" value, the second half is the "S" value.
			lock (dsa)
				return dsa.VerifyData(data, signature);
		}
		/// <summary>
		/// Computes a cryptographic signature of a string.
		/// Later, the signature can be provided along with the data string to the <see cref="Verify(string, string)"/> method to verify that the data string has not been tampered with.
		/// Returns a string in <see cref="Base64UrlMod"/> format.
		/// </summary>
		/// <param name="data">A string to sign.</param>
		/// <returns>Returns the signature as a string in <see cref="Base64UrlMod"/> format.</returns>
		public string Sign(string data)
		{
			return Base64UrlMod.ToBase64UrlMod(Sign(Encoding.UTF8.GetBytes(data)));
		}
		/// <summary>
		/// Verifies that the signature matches the data string. If the data and/or signature have been tampered with, this method will return false.
		/// </summary>
		/// <param name="data">The string to verify the signature of.</param>
		/// <param name="signature">The signature used for verification.  (Base64 or <see cref="Base64UrlMod"/> format)</param>
		/// <returns>True if the signature is valid; otherwise, false.</returns>
		public bool Verify(string data, string signature)
		{
			return Verify(Encoding.UTF8.GetBytes(data), Base64UrlMod.FromBase64UrlMod(signature));
		}
		private string privateKeyLastExported = null;
		/// <summary>
		/// Returns the private key in base64 format so it can be reused in future SignatureFactory instances.  This method is very efficient to call again after the first time.
		/// </summary>
		/// <returns>The private key in base64 format.</returns>
		public string ExportPrivateKey()
		{
			if (privateKeyLastExported == null)
			{
				byte[] key;
				lock (dsa)
					key = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
				privateKeyLastExported = Convert.ToBase64String(key);
			}
			return privateKeyLastExported;
		}
		private string publicKeyLastExported = null;
		/// <summary>
		/// Returns the public key in base64 format so it can be reused in future SignatureFactory instances.  This method is very efficient to call again after the first time.
		/// </summary>
		/// <returns>The public key in base64 format.</returns>
		public string ExportPublicKey()
		{
			if (publicKeyLastExported == null)
			{
				byte[] key;
				lock (dsa)
					key = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);
				publicKeyLastExported = Convert.ToBase64String(key);
			}
			return publicKeyLastExported;
		}
	}
}

#endif