using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	[TestClass]
	public class TestXXHash
	{
		[TestMethod]
		public void TestXXHashMethods()
		{

			byte[] input = BPUtil.ByteUtil.Utf8NoBOM.GetBytes("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
			uint reference1 = xxHash.CalculateHash(input);
			uint reference2 = xxHash.CalculateHash(new MemoryStream(input));
			Assert.AreEqual(reference1, reference2);

			BPUtil.xxHash hasher = new BPUtil.xxHash();
			hasher.Init();
			hasher.Update(input, input.Length);
			uint reference3 = hasher.Digest();
			Assert.AreEqual(reference1, reference3);

			//BPUtil.BasicEventTimer bet = new BPUtil.BasicEventTimer("0.000");

			//{
			//	BPUtil.xxHash hasher = new BPUtil.xxHash();

			//	bet.Start("xxHash static byte[]");
			//	for (int i = 0; i < 100000; i++)
			//	{
			//		xxHash.CalculateHash(input);
			//	}

			//	bet.Start("xxHash static MemoryStream");
			//	MemoryStream ms = new MemoryStream(input);
			//	for (int i = 0; i < 100000; i++)
			//	{
			//		xxHash.CalculateHash(ms);
			//		ms.Seek(0, SeekOrigin.Begin);
			//	}

			//	bet.Start("xxHash instance");
			//	for (int i = 0; i < 100000; i++)
			//	{
			//		hasher.Init();
			//		hasher.Update(input, input.Length);
			//		hasher.Digest();
			//	}

			//	bet.Start("SHA1");
			//	for (int i = 0; i < 100000; i++)
			//	{
			//		BPUtil.Hash.GetSHA1Bytes(input);
			//	}

			//	bet.Start("MD5");
			//	for (int i = 0; i < 100000; i++)
			//	{
			//		BPUtil.Hash.GetMD5Bytes(input);
			//	}

			//	bet.Start("SHA256");
			//	for (int i = 0; i < 100000; i++)
			//	{
			//		BPUtil.Hash.GetSHA256Bytes(input);
			//	}

			//	bet.Stop();

			//	Assert.Fail(bet.ToString("\r\n"));
			//}
		}
	}
}
