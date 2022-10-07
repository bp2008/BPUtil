using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Enumerates cryptographic keystores available in Windows.
	/// </summary>
	public enum Keystore
	{
		/// <summary>
		/// The key is kept at the machine level so that all users can access it (in theory)
		/// </summary>
		Machine,
		/// <summary>
		/// The key is kept at the user level so only the user who created the key can access it (in theory).
		/// </summary>
		User
	}
	/// <summary>
	/// <para>Performs asymmetric encryption and decryption using RSACryptoServiceProvider and OAEP padding. This class is thread-safe.</para>
	/// <para>This class can also be used to generate and export keys for later use.</para>
	/// <para>Based on testing, the payload size limits are as follows:</para>
	/// <para>* 1024-bit key: 86-byte payload limit</para>
	/// <para>* 2048-bit key: 214-byte payload limit</para>
	/// <para>* 3072-bit key: 342-byte payload limit</para>
	/// <para>* 4096-bit key: 470-byte payload limit</para>
	/// <para>This class can also be used to sign and verify using the same keys that are used for encryption and decryption.</para>
	/// </summary>
	public static class AsymmetricEncryption
	{
		/// <summary>
		/// RSA key size.  Defaults to 4096.  Key size mainly affects speed of key generation, and 4096 takes a couple seconds but is more secure than smaller key sizes. 
		/// </summary>
		public static int keySize = 4096;
		/// <summary>
		/// Generates new 4096-bit RSA keys without persisting them in the operating system's keystore.
		/// </summary>
		/// <param name="publicKeyBase64">CspBlob containing public key information, base64 encoded. Can be used for encrypting using <see cref="EncryptWithKey"/>.</param>
		/// <param name="privateKeyBase64">The private (and public) components of the RSA key, base64 encoded. Can be used for encrypting using <see cref="EncryptWithKey"/> and decrypting using <see cref="DecryptWithKey"/>.</param>
		public static void GenerateNewKeys(out string publicKeyBase64, out string privateKeyBase64)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
			{
				publicKeyBase64 = Convert.ToBase64String(rsa.ExportCspBlob(false));
				privateKeyBase64 = Convert.ToBase64String(rsa.ExportCspBlob(true));
			}
		}
		/// <summary>
		/// <para>Generates new 4096-bit RSA keys, saving them in a key container in the operating system's keystore. Any existing key container with this name is deleted first.</para>
		/// <para>The public key is exported via an out string parameter, but the private key information is not exported.</para>
		/// <para>If you need to export the private key, you shouldn't bother using the operating system's keystore.</para>
		/// </summary>
		/// <param name="keystore">Specify which keystore the key should be saved in.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.  You'll use it again to access the encryption key later.  E.g. "MyKeyForThisSpecificPurpose".</param>
		/// <param name="publicKeyBase64">CspBlob containing public key information, base64 encoded. Can be used for encrypting using <see cref="EncryptWithKey"/>.</param>
		public static void GenerateNewKeysInKeystore(Keystore keystore, string keyContainerName, out string publicKeyBase64)
		{
			DeletePublicKeyFromKeystore(keystore, keyContainerName);
			using (RSACryptoServiceProvider rsa = GetRsaCspWithKeystore(keystore, keyContainerName))
			{
				publicKeyBase64 = Convert.ToBase64String(rsa.ExportCspBlob(false));
			}
		}
		/// <summary>
		/// Returns the RSA key from the specified key container. Behavior if the key does not already exist is configured via argument.
		/// </summary>
		/// <param name="keystore">Specify which keystore the key should be loaded from.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		/// <param name="createIfNoExist">If true, the key is created if it does not exist (can fail and throw exception).  If false, returns null if the key does not exist or is not accessible (not expected to throw).</param>
		/// <param name="publicOnly">If true, only the public key is returned. If false, the private key is returned.</param>
		/// <returns></returns>
		public static string GetKeyFromKeystore(Keystore keystore, string keyContainerName, bool createIfNoExist, bool publicOnly = true)
		{
			if (createIfNoExist)
			{
				using (RSACryptoServiceProvider rsa = GetRsaCspWithKeystore(keystore, keyContainerName))
				{
					return Convert.ToBase64String(rsa.ExportCspBlob(!publicOnly));
				}
			}
			else
			{
				CspParameters cspParams = CreateCspParameters(keystore, keyContainerName);
				cspParams.Flags |= CspProviderFlags.UseExistingKey;
				RSACryptoServiceProvider rsa;
				try
				{
					rsa = new RSACryptoServiceProvider(cspParams);
				}
				catch (CryptographicException)
				{
					return null; // Thrown if the key does not exist, because of CspProviderFlags.UseExistingKey
				}
				try
				{
					rsa.PersistKeyInCsp = true;
					return Convert.ToBase64String(rsa.ExportCspBlob(!publicOnly));
				}
				finally
				{
					rsa.Dispose();
				}
			}
		}
		/// <summary>
		/// Returns an RSAParameters object containing the key parameters from the given base64 CspBlob.
		/// </summary>
		/// <param name="keyBase64">CspBlob containing public or private key information, base64 encoded.</param>
		/// <returns></returns>
		public static RSAParameters ConvertKeyBase64ToRSAParameters(string keyBase64)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
			{
				rsa.ImportCspBlob(Convert.FromBase64String(keyBase64));
				return rsa.ExportParameters(!rsa.PublicOnly);
			}
		}
		/// <summary>
		/// Deletes the RSA key with the specified key container name, if it exists.
		/// </summary>
		/// <param name="keystore">Specify which keystore the key should be deleted from.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		public static void DeletePublicKeyFromKeystore(Keystore keystore, string keyContainerName)
		{
			CspParameters cspParams = CreateCspParameters(keystore, keyContainerName);
			cspParams.Flags |= CspProviderFlags.UseExistingKey;
			RSACryptoServiceProvider rsa;
			try
			{
				// Try to open existing key
				rsa = new RSACryptoServiceProvider(keySize, cspParams);
			}
			catch (CryptographicException)
			{
				return; // Key does not exist or is inaccessible.  No further action can be done.
			}
			try
			{
				// Delete existing key
				rsa.PersistKeyInCsp = false;
				rsa.Clear();
			}
			finally
			{
				rsa.Dispose();
			}
		}
		#region Encrypt/Decrypt With Given Key
		/// <summary>
		/// Encrypts the given data using the public key from the given base64-encoded CspBlob.
		/// </summary>
		/// <param name="publicKeyBase64">CspBlob containing public key information, base64 encoded, as exported using the <see cref="GenerateNewKeys"/> method. Can also accept the private key because RSA private keys typically include public key information too.</param>
		/// <param name="data">Data to encrypt.</param>
		/// <returns></returns>
		public static byte[] EncryptWithKey(string publicKeyBase64, byte[] data)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
			{
				rsa.ImportCspBlob(Convert.FromBase64String(publicKeyBase64));
				return rsa.Encrypt(data, true);
			}
		}
		/// <summary>
		/// Decrypts the given data using the private key from the given base64-encoded CspBlob.
		/// </summary>
		/// <param name="privateKeyBase64">CspBlob containing private key information, as a base64 string, as exported using the <see cref="GenerateNewKeys"/> method.</param>
		/// <param name="data">Data to decrypt.</param>
		/// <returns></returns>
		public static byte[] DecryptWithKey(string privateKeyBase64, byte[] data)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
			{
				rsa.ImportCspBlob(Convert.FromBase64String(privateKeyBase64));
				if (rsa.PublicOnly)
					throw new ApplicationException("The given key string did not contain private key components, and therefore cannot be used for decrypting.");
				return rsa.Decrypt(data, true);
			}
		}
		#endregion
		#region Encrypt/Decrypt With Keystore
		/// <summary>
		/// Encrypts the given data using a public key from the operating system's keystore. If the key does not already exist, a new one is created.
		/// </summary>
		/// <param name="keystore">Specify which keystore the key should be loaded from.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		/// <param name="data">Data to encrypt.</param>
		/// <returns></returns>
		public static byte[] EncryptWithKeyFromKeystore(Keystore keystore, string keyContainerName, byte[] data)
		{
			using (RSACryptoServiceProvider rsa = GetRsaCspWithKeystore(keystore, keyContainerName))
			{
				return rsa.Encrypt(data, true);
			}
		}
		/// <summary>
		/// Decrypts the given data using a private key from the operating system's keystore. If the key does not already exist, a new one is created.
		/// </summary>
		/// <param name="keystore">Specify which keystore the key should be loaded from.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		/// <param name="data">Data to decrypt.</param>
		/// <returns></returns>
		public static byte[] DecryptWithKeyFromKeystore(Keystore keystore, string keyContainerName, byte[] data)
		{
			using (RSACryptoServiceProvider rsa = GetRsaCspWithKeystore(keystore, keyContainerName))
			{
				if (rsa.PublicOnly)
					throw new ApplicationException("The given key container did not contain private key components, and therefore cannot be used for decrypting.");
				return rsa.Decrypt(data, true);
			}
		}
		#endregion
		#region Sign/Verify With Given Key
		/// <summary>
		/// Verifies the given signature against a hash of the data using the public key from the given base64-encoded CspBlob. Returns true if the signature is verified.
		/// </summary>
		/// <param name="publicKeyBase64">CspBlob containing public key information, base64 encoded, as exported using the <see cref="GenerateNewKeys"/> method. Can also accept the private key because RSA private keys typically include public key information too.</param>
		/// <param name="data">Data to verify.</param>
		/// <param name="signature">Signature to verify.</param>
		/// <param name="hashAlg">Hash algorithm to use when hashing the data. Default is SHA256.</param>
		/// <returns></returns>
		public static bool VerifyWithKey(string publicKeyBase64, byte[] data, byte[] signature, HashAlgSelector hashAlg = HashAlgSelector.SHA256)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
			{
				rsa.ImportCspBlob(Convert.FromBase64String(publicKeyBase64));
				data = Hash.GetSHA256Bytes(data);
				return rsa.VerifyHash(data, hashAlg.ToString(), signature);
			}
		}
		/// <summary>
		/// Signs a hash of the given data using the private key from the given base64-encoded CspBlob. Returns the signature.
		/// </summary>
		/// <param name="privateKeyBase64">CspBlob containing private key information, as a base64 string, as exported using the <see cref="GenerateNewKeys"/> method.</param>
		/// <param name="data">Data to sign.</param>
		/// <param name="hashAlg">Hash algorithm to use when hashing the data. Default is SHA256.</param>
		/// <returns></returns>
		public static byte[] SignWithKey(string privateKeyBase64, byte[] data, HashAlgSelector hashAlg = HashAlgSelector.SHA256)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
			{
				rsa.ImportCspBlob(Convert.FromBase64String(privateKeyBase64));
				if (rsa.PublicOnly)
					throw new ApplicationException("The given key string did not contain private key components, and therefore cannot be used for signing.");
				data = Hash.GetSHA256Bytes(data);
				return rsa.SignHash(data, hashAlg.ToString());
			}
		}
		#endregion
		#region Sign/Verify With Keystore
		/// <summary>
		/// Verifies the given signature against a hash of the data using a public key from the operating system's keystore. If the key does not already exist, verification will fail. Returns true if the signature is verified.
		/// </summary>
		/// <param name="keystore">Specify which keystore the key should be loaded from.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		/// <param name="data">Data to verify.</param>
		/// <param name="signature">Signature to verify.</param>
		/// <param name="hashAlg">Hash algorithm to use when hashing the data. Default is SHA256.</param>
		/// <returns></returns>
		public static bool VerifyWithKeyFromKeystore(Keystore keystore, string keyContainerName, byte[] data, byte[] signature, HashAlgSelector hashAlg = HashAlgSelector.SHA256)
		{
			// .NET has a weird limitation where SHA1 is the only supported hash algorithm if we load the keys via GetRsaCspWithKeystore.  We must provide the key as a string instead.
			string publicKey = GetKeyFromKeystore(keystore, keyContainerName, false, true);
			return VerifyWithKey(publicKey, data, signature, hashAlg);
		}
		/// <summary>
		/// Signs a hash of the given data using a private key from the operating system's keystore. If the key does not already exist, a new one is created. Returns the signature.
		/// </summary>
		/// <param name="keystore">Specify which keystore the key should be loaded from.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		/// <param name="data">Data to sign.</param>
		/// <param name="hashAlg">Hash algorithm to use when hashing the data. Default is SHA256.</param>
		/// <returns></returns>
		public static byte[] SignWithKeyFromKeystore(Keystore keystore, string keyContainerName, byte[] data, HashAlgSelector hashAlg = HashAlgSelector.SHA256)
		{
			// .NET has a weird limitation where SHA1 is the only supported hash algorithm if we load the keys via GetRsaCspWithKeystore.  We must provide the key as a string instead.
			string privateKey = GetKeyFromKeystore(keystore, keyContainerName, false, false);
			return SignWithKey(privateKey, data, hashAlg);
		}
		#endregion
		/// <summary>
		/// Returns a new CspParameters object configured for the specified keystore and key container name.
		/// </summary>
		/// <param name="keystore">Specify which keystore should be used.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		/// <returns></returns>
		private static CspParameters CreateCspParameters(Keystore keystore, string keyContainerName)
		{
			CspParameters cspParams = new CspParameters(1); // 1 for RSA.  This is also the default if omitted.
			cspParams.KeyContainerName = keyContainerName;
			cspParams.Flags = CspProviderFlags.NoFlags;
			if (keystore == Keystore.Machine)
			{
				cspParams.Flags |= CspProviderFlags.UseMachineKeyStore;
				CryptoKeyAccessRule rule = new CryptoKeyAccessRule("everyone", CryptoKeyRights.FullControl, AccessControlType.Allow);
				cspParams.CryptoKeySecurity = new CryptoKeySecurity();
				cspParams.CryptoKeySecurity.SetAccessRule(rule);
			}
			return cspParams;
		}
		/// <summary>
		/// Creates an RSACryptoServiceProvider with a persistent key using the given keystore and key container name. The key is created if it does not exist.
		/// </summary>
		/// <param name="keystore">Specify which keystore should be used.</param>
		/// <param name="keyContainerName">A string which uniquely identifies this encryption key among all other keys in the keystore.</param>
		/// <returns></returns>
		private static RSACryptoServiceProvider GetRsaCspWithKeystore(Keystore keystore, string keyContainerName)
		{
			// X509Store is an interesting alternative way to access OS keystores, but I haven't learned it.
			CspParameters cspParams = CreateCspParameters(keystore, keyContainerName);
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize, cspParams);
			try
			{
				rsa.PersistKeyInCsp = true;
				return rsa;
			}
			catch
			{
				rsa.Dispose();
				throw;
			}
		}
	}
	/// <summary>
	/// Identifies a hash algorithm for use in AsymmetricEncryption Sign and Verify methods.
	/// </summary>
	public enum HashAlgSelector
	{
		MD5,
		SHA1,
		/// <summary>
		/// The default hash algorithm, and the only one I have tested.
		/// </summary>
		SHA256,
		SHA384,
		SHA512
	}
}
