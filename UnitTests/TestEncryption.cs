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

			// Test Sign/Verify
			{
				byte[] signature = AsymmetricEncryption.SignWithKey(privateKey, plainBytes);
				Assert.IsTrue(AsymmetricEncryption.VerifyWithKey(publicKey, plainBytes, signature));

				// You can't Sign with a public key
				try
				{
					AsymmetricEncryption.SignWithKey(publicKey, plainBytes);
					Assert.Fail("Expected exception when trying to sign with public key.");
				}
				catch { }

				// But you can Verify with a private key because it contains public key parameters.
				Assert.IsTrue(AsymmetricEncryption.VerifyWithKey(privateKey, plainBytes, signature));

				// Change one byte in the signature, verification should now fail.
				signature[0] = (byte)(signature[0] + 1);
				Assert.IsFalse(AsymmetricEncryption.VerifyWithKey(publicKey, plainBytes, signature));
			}
		}

		[TestMethod]
		public void TestAsymmetricEncryptionWithMachineKeystore()
		{
			TestAsymmetricEncryptionWithKeystore(Keystore.Machine, MachineKeyContainerName, Keystore.User, UserKeyContainerName);
		}
		[TestMethod]
		public void TestAsymmetricEncryptionWithUserKeystore()
		{
			TestAsymmetricEncryptionWithKeystore(Keystore.User, UserKeyContainerName, Keystore.Machine, MachineKeyContainerName);
		}
		private const string MachineKeyContainerName = "BpUtil Asymmetric Machine Test Key #1";
		private const string UserKeyContainerName = "BpUtil Asymmetric User Test Key #1";
		private void TestAsymmetricEncryptionWithKeystore(Keystore correctKeystore, string correctKeyContainerName, Keystore wrongKeystore, string wrongKeyContainerName)
		{
			CleanupKeystores();
			byte[] plainBytes = ByteUtil.Utf8NoBOM.GetBytes("Secret String For Testing");

			try
			{
				// Key should be automatically generated
				byte[] encryptedBytes = AsymmetricEncryption.EncryptWithKeyFromKeystore(correctKeystore, correctKeyContainerName, plainBytes);
				Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes));

				byte[] decryptedBytes = AsymmetricEncryption.DecryptWithKeyFromKeystore(correctKeystore, correctKeyContainerName, encryptedBytes);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes));

				// Key should be retrievable from correct keystore
				string publicKeyLoaded = AsymmetricEncryption.GetKeyFromKeystore(correctKeystore, correctKeyContainerName, false);
				Assert.IsNotNull(publicKeyLoaded);

				// Key should NOT be retrievable from incorrect keystore
				string publicKeyFromWrongKeystore = AsymmetricEncryption.GetKeyFromKeystore(wrongKeystore, correctKeyContainerName, false);
				Assert.IsNull(publicKeyFromWrongKeystore);

				Assert.IsTrue(KeystoreContainsKeyContainer(correctKeystore, correctKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(wrongKeystore, correctKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(correctKeystore, wrongKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(wrongKeystore, wrongKeyContainerName));

				// Test encryption using exported public key.
				byte[] encryptedBytes2 = AsymmetricEncryption.EncryptWithKey(publicKeyLoaded, plainBytes);
				Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes2));

				byte[] decryptedBytes2 = AsymmetricEncryption.DecryptWithKeyFromKeystore(correctKeystore, correctKeyContainerName, encryptedBytes2);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes2));

				// Test Sign/Verify
				{
					// Sign and verify using keystore
					byte[] signature = AsymmetricEncryption.SignWithKeyFromKeystore(correctKeystore, correctKeyContainerName, plainBytes);
					Assert.IsTrue(AsymmetricEncryption.VerifyWithKeyFromKeystore(correctKeystore, correctKeyContainerName, plainBytes, signature));

					// Verify with public key we loaded earlier
					Assert.IsTrue(AsymmetricEncryption.VerifyWithKey(publicKeyLoaded, plainBytes, signature));

					// Sign with extracted private key, then verify again with keystore
					string privateKeyLoaded = AsymmetricEncryption.GetKeyFromKeystore(correctKeystore, correctKeyContainerName, false, false);
					signature = AsymmetricEncryption.SignWithKey(privateKeyLoaded, plainBytes);
					Assert.IsTrue(AsymmetricEncryption.VerifyWithKeyFromKeystore(correctKeystore, correctKeyContainerName, plainBytes, signature));

					// Change one byte in the signature, verification should now fail.
					signature[0] = (byte)(signature[0] + 1);
					Assert.IsFalse(AsymmetricEncryption.VerifyWithKeyFromKeystore(correctKeystore, correctKeyContainerName, plainBytes, signature));
				}

				// Should be possible to replace existing keys by calling GenerateNewKeysInKeystore
				AsymmetricEncryption.GenerateNewKeysInKeystore(correctKeystore, correctKeyContainerName, out string publicKey2);
				Assert.AreNotEqual(publicKeyLoaded, publicKey2);

				// Getting the key should now return the new key
				string publicKeyLoaded2 = AsymmetricEncryption.GetKeyFromKeystore(correctKeystore, correctKeyContainerName, false);
				Assert.AreEqual(publicKey2, publicKeyLoaded2);

				// Delete the key
				AsymmetricEncryption.DeletePublicKeyFromKeystore(correctKeystore, correctKeyContainerName);
				Assert.IsNull(AsymmetricEncryption.GetKeyFromKeystore(correctKeystore, correctKeyContainerName, false));

				// Try to generate a new one using the "Get" method.
				string publicKeyLoaded3 = AsymmetricEncryption.GetKeyFromKeystore(correctKeystore, correctKeyContainerName, true);
				Assert.AreNotEqual(publicKeyLoaded, publicKeyLoaded3);
				Assert.AreNotEqual(publicKey2, publicKeyLoaded3);
			}
			finally
			{
				AsymmetricEncryption.DeletePublicKeyFromKeystore(correctKeystore, correctKeyContainerName);
				Assert.IsFalse(KeystoreContainsKeyContainer(correctKeystore, correctKeyContainerName));

				// Confirm the delete can be done redundantly without negative effect
				AsymmetricEncryption.DeletePublicKeyFromKeystore(correctKeystore, correctKeyContainerName);

				Assert.IsFalse(KeystoreContainsKeyContainer(correctKeystore, correctKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(wrongKeystore, correctKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(correctKeystore, wrongKeyContainerName));
				Assert.IsFalse(KeystoreContainsKeyContainer(wrongKeystore, wrongKeyContainerName));
			}
		}
		private void CleanupKeystores()
		{
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.Machine, MachineKeyContainerName);
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.User, MachineKeyContainerName);
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.Machine, UserKeyContainerName);
			AsymmetricEncryption.DeletePublicKeyFromKeystore(Keystore.User, UserKeyContainerName);
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
				return AsymmetricEncryption.GetKeyFromKeystore(keystore, keyContainerName, false) != null;
			}
			else
			{
				List<string> containerNames = Helpers.IterateOverKeyContainers.GetKeyContainerNames(keystore == Keystore.Machine);
				return containerNames.Contains(keyContainerName);
			}
		}
		[TestMethod]
		public void TestRSAPayloadSizeLimit()
		{
			// It takes about 21 seconds to find the limits for all 4 key sizes here, starting from a 1-byte payload.
			// To speed things up, we instead start the tests at the expected limit.
			TestRSAPayloadSizeLimitAtKeySize(1024, 86);
			TestRSAPayloadSizeLimitAtKeySize(2048, 214);
			TestRSAPayloadSizeLimitAtKeySize(3072, 342);
			TestRSAPayloadSizeLimitAtKeySize(4096, 470);
		}
		private void TestRSAPayloadSizeLimitAtKeySize(int keySize, int expectedPayloadSizeLimit, int startTestAt = -1)
		{
			AsymmetricKeypair keys = GetStaticKeys(keySize);

			if (startTestAt < 1)
				startTestAt = expectedPayloadSizeLimit;
			int expectedFailureAt = expectedPayloadSizeLimit + 1;

			for (int i = startTestAt; i <= expectedFailureAt; i++)
			{
				byte[] plainBytes = new byte[i];
				SecureRandom.NextBytes(plainBytes);

				byte[] encryptedBytes = null;
				try
				{
					encryptedBytes = AsymmetricEncryption.EncryptWithKey(keys.publicKey, plainBytes);
				}
				catch (Exception ex)
				{
					if (i == expectedFailureAt)
						return;
					Assert.Fail(keySize + "-bit key failed at payload size " + i + " bytes. Expected failure at " + expectedFailureAt + "-byte payload size. Exception: " + ex.ToString());
				}
				if (i == expectedFailureAt)
					Assert.Fail("Expected exception when encrypting " + expectedFailureAt + "-byte payload size. Did not get exception. " + keySize + "-bit key test failed.");
				Assert.IsFalse(ByteUtil.ByteArraysMatch(plainBytes, encryptedBytes));

				byte[] decryptedBytes = AsymmetricEncryption.DecryptWithKey(keys.privateKey, encryptedBytes);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(plainBytes, decryptedBytes));
			}
		}
		struct AsymmetricKeypair
		{
			public string publicKey;
			public string privateKey;

			public AsymmetricKeypair(string publicKey, string privateKey) : this()
			{
				this.publicKey = publicKey;
				this.privateKey = privateKey;
			}
		}
		/// <summary>
		/// Provides hard-coded asymmetric keys to speed up tests where key generation is not being tested.
		/// </summary>
		/// <param name="keySize"></param>
		/// <returns></returns>
		private AsymmetricKeypair GetStaticKeys(int keySize)
		{
			if (keySize == 1024)
				return new AsymmetricKeypair("BgIAAACkAABSU0ExAAQAAAEAAQCpeV3tAWVyxUugKVvPl6YfRxzYBQDZILv6A9dj/LsIoqZUdTrklVl8mG6jjBQI30MtH+9AhB9nO7sX60"
							+ "TykUVfZnWO7r0NiWXV8GhJkNMkp3zfTpvNhZKuTEzXBrQTDgut8D4XEdUt+EK+NspPx4HHPv6NJB0F0eTwS3mCo/a57A==", "BwIAAACkAABSU0EyAAQAAAE"
							+ "AAQCpeV3tAWVyxUugKVvPl6YfRxzYBQDZILv6A9dj/LsIoqZUdTrklVl8mG6jjBQI30MtH+9AhB9nO7sX60TykUVfZnWO7r0NiWXV8GhJkNMkp3zfTpvNhZKu"
							+ "TEzXBrQTDgut8D4XEdUt+EK+NspPx4HHPv6NJB0F0eTwS3mCo/a57KugTjQOiqS3qNMiOeyqdARYOO3Y8lSFgnYbgAiS/75cSkF7lvwSWWXJKrS76JXyKjY9V"
							+ "2MbkZRsuSfo3y4a++371uWTSTg4xtN1XkE+IEO8qhGcgXlhAHPfsOBZzr3lyql2buOTW8NUHzFC7PBNdifXKdLWKbgPOxO/hbKqi6b+EyEU6sALjQkIt49Lu3"
							+ "1lYg3iU3Bf/RQRJpkDTOMBTXLnEfn23zScOQqKljDS9NvMHkz4oUA1ehWrS0nSy6JlXOWmUsTWxK6laOVvxuC6QZjhJCEFfd8NNn9PAaHgBiEwGC238LmLejx"
							+ "OM9OeZK+1hGKtatCk2NbFTK14KLd0TWemClw0qz38nblu1OXaPGlEAX4x8trLtsAQVL8fLCT47ha+JpgzcqSp+/SZG5Ad/ESArZOG+uWldT1vTevanfWNRSd6"
							+ "pQRdS8002FoxRHgnc+or1E51awR0y/iDZCFISVTBHSSDlopTqgC5Lr9vesnBipPYVwzX5id0Ayp+hurYbbH/jrA0lzz+hcNZstIOxjGSE89iv6g2W3suszVGQ"
							+ "H+e+J55DV1iZ2zpoqF1V7vldFAqEjNHCFoT+qwlN0OIWdA=");
			else if (keySize == 2048)
				return new AsymmetricKeypair("BgIAAACkAABSU0ExAAgAAAEAAQDJ5BCDdho7IkZAI8Ay6F1ZtzNTbcEy4U1vlvHpBvnoD4MU1ZyN33wpkjA44rrII5CIOU01MhlKtcYKdp"
							+ "LfMlFwPhyk+62g+aEQKHA671v/8cp6IDrfN0tAE+TW4a84QasXGyHGhxCLUYHRmdghamkFAb2jOMoeXFtWjFRd00BVYolu1i5D+oAc0GUvYw6YXOZXVDg86Nn"
							+ "F2P9Vz++V3JYXNwoBiATLTxpO8iUs3wvVtv0ISAChi+JhpujzB4M7WogS/APGs603a6ITMdFggZebKntrxkVgVfHnMv429QhBmIgiY+yallqtx44d8dpskeAv"
							+ "ngsgPjdRe2yX5HgU7PK4", "BwIAAACkAABSU0EyAAgAAAEAAQDJ5BCDdho7IkZAI8Ay6F1ZtzNTbcEy4U1vlvHpBvnoD4MU1ZyN33wpkjA44rrII5CIOU01M"
							+ "hlKtcYKdpLfMlFwPhyk+62g+aEQKHA671v/8cp6IDrfN0tAE+TW4a84QasXGyHGhxCLUYHRmdghamkFAb2jOMoeXFtWjFRd00BVYolu1i5D+oAc0GUvYw6YXO"
							+ "ZXVDg86NnF2P9Vz++V3JYXNwoBiATLTxpO8iUs3wvVtv0ISAChi+JhpujzB4M7WogS/APGs603a6ITMdFggZebKntrxkVgVfHnMv429QhBmIgiY+yallqtx44"
							+ "d8dpskeAvngsgPjdRe2yX5HgU7PK42zD1ZMzpBoLccQtItHxAPhFw73IERuyG18m2/u/kHZrOu1NUc3i2P+nMwWUASuziY0qw9OQxWI9UNPADrfi/U6QzKDCD"
							+ "vit1QLCbPKvOzepp7pMCQLIMQEF02563Cpx38Qm6CotwHN4DylzhOGyu990pktfElW/B8qeoTIednuErELwuYdvMgL8X2jcttXMNMdLTgFNNCVcW+e3YXNzh8"
							+ "ZZ0Zzm4IvNQLHS3R3IgFc6L2PBZbhOEkciOpa7Y4eExFeVQ/iao3/GQy2cZ2r69rkG5NTaV5wXU13VR6drWKZl79k1KkCMkJNM9XkCCZ96+hKpEvXgAoxSStE"
							+ "yIsFba0enOk1F94HTIZtYjgAPLTuDdMTSL/yH+2yAL+dPAM7ZC4MEqHXPoIrlwziMhwYhP5bWebHmxTZzSXpBaNCzksDpu4CefCUpuGsDYM/3Y6vMRzx4tKt9"
							+ "Oou1SGYP0jR4V5ah97qCwtdfJlvCbDXaxxakK3IR4jkU3kqrOFYWzMzMEu/hPmtU0Otx7W374fZp5vngRf7XvGs8elt7asJxYegYopzN0GJpOq37iZJNf2e9u"
							+ "uLkwcEM/BVBwGJHBKNijynY4TS3OGqbGBMcrb+2Eb7ErXaCf50yKpU4UYFZbAorLwjRLYQfTymJyM23L7s2iD5z0nsKe6M2MiRbGk5ua8Y0xlGceH6WoHvL9F"
							+ "G/iHm6sAzRu/yhGA/nNbWXZDDMVdLS5bNGo2O9oITUyYS8gSShlbJbU9LlXxmuXxdWsiPbQU7PZ9SozfEG/W7TpnhYqv6Zq29OmXpqw0Jk2e9qq98MTXvR+vd"
							+ "GTn1SEXsBTGmE5ZARh5JuGCzQ0W3dkIrxshtlR9p7SgS+v7emBuVocFuOYxjL8XekBffH0/kIP3xVVMMNXZJqHOsAcP2dAyNG19QQnwSJ+ulJwIvAsDnsRKCg"
							+ "NL7JuLBT5Fp0J0RXWbqZWsQBN9x8Uhsirj6Qt/uowBuyMNeYfGs4+cGK2JSkHgNBeLGE9E26sTCrRUqnUtRInAtbjWmkf2/q2J6RXCS3x0DRkw50fsg9ntF3P"
							+ "grDHVpFrqIzbFXKa0oypT1VBj0A30XyH3keF0fLid32XWQ9FeVjw3r9tAtvE72+oWSiTFaHpHZr6ENSknJ3dPq1YQh+Wy5h1nn4gqJ65EjTsA7DeWjrY6ydsS"
							+ "UYzMmf36wKpQpM=");
			else if (keySize == 3072)
				return new AsymmetricKeypair("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");
			else if (keySize == 4096)
				return new AsymmetricKeypair("BgIAAACkAABSU0ExABAAAAEAAQCxeABg+dzl81qPhYU5AvcXojGkCJL69RC/bTrpVSTawVcZu9A1zpNGQDby02iusJjwHJ12YiIeHCdCVH"
							+ "92MpOWk+R2sMoQTOJbAPYjzqHJeW9IQa64fNg4X+RfWFoH++C3SoYUn5Ott1qROMJBGvUOjpdb9PNjiJGVfxPE1TP5c1mMXXQaWlLvl84dxDQkeVXVWVKDV5z"
							+ "gR/vGg/Ce2RwOWFQjNwC9+FsK7wEyWYiiSF0AyZpnz0eiXzHu0WlusxfWNXtShCvrSTLZkbfC8zc7O0GhaZOBVOxa8qKpBabS5ZMdpcGK/G+Fn1RNHb99rc0V"
							+ "2exTWF5Ig14juMMArv4vc0v9ipxyTWtejrUvna0cw1UwAPEnuOpgrN7Nur0yZHelRUbwU1+6PRbCNp49PDCAh7OU7Qpe8MTTdIqssbMNYy7/p5DVgzAuVzOyK"
							+ "7Io/OecoVl+nFuFQB+oibMCpSsWeyn/wtHAGUn4NQeUXFkdwg81eGEnVCOu6JqkPJ8YzAFgstNx8B4RaARskfvryAhevoPEoS6B4idIQG8zdt6fjHwVs4qcdO"
							+ "6EGXP7sco9X+LmLHN0jkgeyjYbwZg8bUe5kSP38tXxr/cphJ8XfABEQMNV9OYtF8BNpCyYkxhn6c753HJUlAbdSvBdP5xijARMJDX9gmZkwWvgrw7jWBFTyQ="
							+ "=", "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");
			else
			{
				throw new ApplicationException("Static keys are not available at " + keySize + "-bit key size.");
				//int tmp = AsymmetricEncryption.keySize;
				//AsymmetricEncryption.keySize = keySize;
				//AsymmetricEncryption.GenerateNewKeys(out string publicKey, out string privateKey);
				//AsymmetricEncryption.keySize = tmp;
				//return new AsymmetricKeypair(publicKey, privateKey);
			}
		}
	}
}
