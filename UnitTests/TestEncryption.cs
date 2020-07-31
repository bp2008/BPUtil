using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	[TestClass]
	public class TestEncryption
	{
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
	}
}
