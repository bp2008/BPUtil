using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	[TestClass]
	public class TestEncryption
	{
		[TestInitialize()]
		public void Startup()
		{
			AsymmetricEncryption.keySize = 1024; // Small key size makes the tests run a lot faster.
		}

		[TestMethod]
		public void TestSymmetricEncryption()
		{
			Encryption e = new Encryption();
			byte[] buffer = new byte[300];
			new Random().NextBytes(buffer);

			byte[] cipher = e.Encrypt(buffer);
			byte[] plain = e.Decrypt(cipher);

			Assert.IsTrue(cipher.Length >= buffer.Length, "encrypted size should be greater than or equal to original size");
			Assert.AreEqual(string.Join(",", buffer), string.Join(",", plain), "decrypted buffer should match original buffer");

			// Test new Encryption instance based on existing Key and IV.
			e = new Encryption(e.Key, e.IV);
			byte[] cipher2 = e.Encrypt(buffer);
			byte[] plain2 = e.Decrypt(cipher2);

			Assert.AreEqual(string.Join(",", cipher2), string.Join(",", cipher), "encrypted buffer should match the encrypted buffer from earlier in the test");
			Assert.AreEqual(string.Join(",", buffer), string.Join(",", plain2), "decrypted buffer should match original buffer");
		}
		[TestMethod]
		public void TestAsymmetricEncryptionNonPersistedKey()
		{
			byte[] plainBytes = ByteUtil.Utf8NoBOM.GetBytes("Secret String For Testing");

			AsymmetricEncryption.GenerateNewKeys(out string publicKey, out string privateKey);

			byte[] encryptedBytes = AsymmetricEncryption.EncryptWithKey(publicKey, plainBytes);
			Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes));

			byte[] decryptedBytes = AsymmetricEncryption.DecryptWithKey(privateKey, encryptedBytes);
			Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes));

			// Try encrypting with the private key (usually done with only the public key).
			byte[] encryptedBytes2 = AsymmetricEncryption.EncryptWithKey(privateKey, plainBytes);
			Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes2));

			// Try decrypting with the public key (should fail)
			try
			{
				byte[] decryptedBytes2 = AsymmetricEncryption.DecryptWithKey(publicKey, encryptedBytes);
				Assert.Fail("Expected exception when trying to decrypt with public key.");
			}
			catch { }

			// Verify that private-key-encryption worked as intended
			byte[] decryptedBytes3 = AsymmetricEncryption.DecryptWithKey(privateKey, encryptedBytes);
			Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes3));
		}
		private const string MachineKeyContainerName = "BpUtil Asymmetric Machine Test Key #1";
		private const string UserKeyContainerName = "BpUtil Asymmetric Machine Test Key #1";
		private void CleanupKeystores()
		{
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.Machine, MachineKeyContainerName);
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.User, MachineKeyContainerName);
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.Machine, UserKeyContainerName);
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.User, UserKeyContainerName);
		}
		[TestMethod]
		public void TestAsymmetricEncryptionWithMachineKeystore()
		{
			CleanupKeystores();
			byte[] plainBytes = ByteUtil.Utf8NoBOM.GetBytes("Secret String For Testing");

			try
			{
				byte[] encryptedBytes = AsymmetricEncryption.EncryptWithKeyFromKeystore(Keystore.Machine, MachineKeyContainerName, plainBytes);
				Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes));

				byte[] decryptedBytes = AsymmetricEncryption.DecryptWithKeyFromKeystore(Keystore.Machine, MachineKeyContainerName, encryptedBytes);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes));

				// Key should be retrievable from correct keystore
				AsymmetricEncryption.GenerateNewKeysInKeystore(Keystore.Machine, MachineKeyContainerName, out string publicKey);
				string publicKeyLoaded = AsymmetricEncryption.GetPublicKeyFromKeystore(Keystore.Machine, MachineKeyContainerName);
				Assert.AreEqual(publicKey, publicKeyLoaded);

				// Key should NOT be retrievable from incorrect keystore
				string publicKeyFromWrongKeystore = AsymmetricEncryption.GetPublicKeyFromKeystore(Keystore.User, MachineKeyContainerName);
				Assert.IsNull(publicKeyFromWrongKeystore);

				Assert.IsTrue(KeystoreContainsKeyContainer(Keystore.Machine, MachineKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(Keystore.User, MachineKeyContainerName));

				// Test encryption using exported public key.
				byte[] encryptedBytes2 = AsymmetricEncryption.EncryptWithKey(publicKeyLoaded, plainBytes);
				Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes2));

				byte[] decryptedBytes2 = AsymmetricEncryption.DecryptWithKeyFromKeystore(Keystore.Machine, MachineKeyContainerName, encryptedBytes2);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes2));
			}
			finally
			{
				AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.Machine, MachineKeyContainerName);

				Assert.IsFalse(KeystoreContainsKeyContainer(Keystore.Machine, MachineKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(Keystore.User, MachineKeyContainerName));
			}
		}
		[TestMethod]
		public void TestAsymmetricEncryptionWithUserKeystore()
		{
			CleanupKeystores();
			byte[] plainBytes = ByteUtil.Utf8NoBOM.GetBytes("Secret String For Testing");

			try
			{
				// Test encryption and decryption
				byte[] encryptedBytes = AsymmetricEncryption.EncryptWithKeyFromKeystore(Keystore.User, UserKeyContainerName, plainBytes);
				Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes));

				byte[] decryptedBytes = AsymmetricEncryption.DecryptWithKeyFromKeystore(Keystore.User, UserKeyContainerName, encryptedBytes);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes));

				// Key should be retrievable from correct keystore
				AsymmetricEncryption.GenerateNewKeysInKeystore(Keystore.User, UserKeyContainerName, out string publicKey);
				string publicKeyLoaded = AsymmetricEncryption.GetPublicKeyFromKeystore(Keystore.User, UserKeyContainerName);
				Assert.AreEqual(publicKey, publicKeyLoaded);

				// Key should NOT be retrievable from incorrect keystore
				string publicKeyFromWrongKeystore = AsymmetricEncryption.GetPublicKeyFromKeystore(Keystore.Machine, UserKeyContainerName);
				Assert.IsNull(publicKeyFromWrongKeystore);

				Assert.IsFalse(KeystoreContainsKeyContainer(Keystore.Machine, MachineKeyContainerName));
				Assert.IsTrue(KeystoreContainsKeyContainer(Keystore.User, MachineKeyContainerName));

				// Test encryption using exported public key.
				byte[] encryptedBytes2 = AsymmetricEncryption.EncryptWithKey(publicKeyLoaded, plainBytes);
				Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes2));

				byte[] decryptedBytes2 = AsymmetricEncryption.DecryptWithKeyFromKeystore(Keystore.User, UserKeyContainerName, encryptedBytes2);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes2));
			}
			finally
			{
				AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.User, UserKeyContainerName);

				Assert.IsFalse(KeystoreContainsKeyContainer(Keystore.Machine, MachineKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(Keystore.User, MachineKeyContainerName));
			}
		}
		/// <summary>
		/// <para>If true, we determine if the keystore contains the key by simply trying to load it and seeing if it worked.</para>
		/// <para>If false, we determine if the keystore contains the key by loading the names of all keys in the keystore. This is much slower.</para>
		/// </summary>
		bool fastKeystoreCheck = true;
		/// <summary>
		/// Returns true if the given keystore contains the given key container.
		/// </summary>
		/// <param name="useMachineKeyStore">Specify which keystore to look in.</param>
		/// <param name="keyContainerName">Name of the key container to find.</param>
		/// <returns></returns>
		private bool KeystoreContainsKeyContainer(Keystore keystore, string keyContainerName)
		{
			if (fastKeystoreCheck)
			{
				return AsymmetricEncryption.GetPublicKeyFromKeystore(keystore, keyContainerName) != null;
			}
			else
			{
				List<string> containerNames = Helpers.IterateOverKeyContainers.GetKeyContainerNames(keystore == Keystore.Machine);
				return containerNames.Contains(keyContainerName);
			}
		}
	}
}
