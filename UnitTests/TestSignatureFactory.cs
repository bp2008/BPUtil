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
	public class TestSignatureFactory
	{
		[TestMethod]
		public void TestSignatureFactoryByteArrayInput()
		{
			SignatureFactory f = new SignatureFactory();
			byte[] buffer = new byte[300];
			new Random().NextBytes(buffer);

			byte[] signature1 = f.Sign(buffer);
			Assert.AreNotEqual(string.Join(",", buffer), string.Join(",", signature1), "signature should not be the same as original data");
			Assert.IsTrue(f.Verify(buffer, signature1), "(1) correct signature verification");
			Assert.IsFalse(f.Verify(buffer.Reverse().ToArray(), signature1), "(2) incorrect signature verification");
			Assert.IsFalse(f.Verify(buffer, signature1.Reverse().ToArray()), "(3) incorrect signature verification");

			// Test new Encryption instance based on existing Key and IV.
			f = new SignatureFactory(f.ExportPrivateKey());
			byte[] signature2 = f.Sign(buffer);
			Assert.IsTrue(f.Verify(buffer, signature1), "(4) correct signature verification ");
			Assert.IsTrue(f.Verify(buffer, signature2), "(5) correct signature verification ");
			Assert.IsFalse(f.Verify(buffer.Reverse().ToArray(), signature2), "(6) incorrect signature verification");
			Assert.IsFalse(f.Verify(buffer, signature2.Reverse().ToArray()), "(7) incorrect signature verification");

			Assert.AreNotEqual(buffer, signature1, "(8) signature should not be the same as original data");
		}
		[TestMethod]
		public void TestSignatureFactoryStringInput()
		{
			SignatureFactory f = new SignatureFactory();
			string buffer = "abcdefghijklmnopqrstuvwxyz";

			string signature1 = f.Sign(buffer);
			Assert.IsTrue(f.Verify(buffer, signature1), "(1) correct signature verification");
			Assert.IsFalse(f.Verify(new string(buffer.Reverse().ToArray()), signature1), "(2) incorrect signature verification");

			// Test new Encryption instance based on existing Key and IV.
			f = new SignatureFactory(f.ExportPrivateKey());
			string signature2 = f.Sign(buffer);
			Assert.IsTrue(f.Verify(buffer, signature1), "(3) correct signature verification ");
			Assert.IsTrue(f.Verify(buffer, signature2), "(4) correct signature verification ");
			Assert.IsFalse(f.Verify(new string(buffer.Reverse().ToArray()), signature2), "(5) incorrect signature verification");

			Assert.AreNotEqual(buffer, signature1, "(6) signature should not be the same as original data");
		}
	}
}
