using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestHex
	{
		[TestMethod]
		public void TestHexBytesConversion()
		{
			byte[] bytes = new byte[512];
			for (int i = 0; i < 256; i++)
			{
				bytes[i] = (byte)i;
				bytes[bytes.Length - 1 - i] = (byte)i;
			}
			string expectedHex = "000102030405060708090a0b0c0d0e0f"
				+ "101112131415161718191a1b1c1d1e1f"
				+ "202122232425262728292a2b2c2d2e2f"
				+ "303132333435363738393a3b3c3d3e3f"
				+ "404142434445464748494a4b4c4d4e4f"
				+ "505152535455565758595a5b5c5d5e5f"
				+ "606162636465666768696a6b6c6d6e6f"
				+ "707172737475767778797a7b7c7d7e7f"
				+ "808182838485868788898a8b8c8d8e8f"
				+ "909192939495969798999a9b9c9d9e9f"
				+ "a0a1a2a3a4a5a6a7a8a9aaabacadaeaf"
				+ "b0b1b2b3b4b5b6b7b8b9babbbcbdbebf"
				+ "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf"
				+ "d0d1d2d3d4d5d6d7d8d9dadbdcdddedf"
				+ "e0e1e2e3e4e5e6e7e8e9eaebecedeeef"
				+ "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"
				+ "fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0"
				+ "efeeedecebeae9e8e7e6e5e4e3e2e1e0"
				+ "dfdedddcdbdad9d8d7d6d5d4d3d2d1d0"
				+ "cfcecdcccbcac9c8c7c6c5c4c3c2c1c0"
				+ "bfbebdbcbbbab9b8b7b6b5b4b3b2b1b0"
				+ "afaeadacabaaa9a8a7a6a5a4a3a2a1a0"
				+ "9f9e9d9c9b9a99989796959493929190"
				+ "8f8e8d8c8b8a89888786858483828180"
				+ "7f7e7d7c7b7a79787776757473727170"
				+ "6f6e6d6c6b6a69686766656463626160"
				+ "5f5e5d5c5b5a59585756555453525150"
				+ "4f4e4d4c4b4a49484746454443424140"
				+ "3f3e3d3c3b3a39383736353433323130"
				+ "2f2e2d2c2b2a29282726252423222120"
				+ "1f1e1d1c1b1a19181716151413121110"
				+ "0f0e0d0c0b0a09080706050403020100";

			string hex = Hex.ToHex(bytes);
			Assert.AreEqual(expectedHex, hex);

			byte[] actualBytes = Hex.ToByteArray(hex);
			Assert.AreEqual(string.Join(",", bytes), string.Join(",", actualBytes));
		}
		[TestMethod]
		public void TestPrefixedHexToLong()
		{
			string input1 = "0x185b8ae584";
			long actual1 = Hex.PrefixedHexToLong(input1);
			long expected1 = 104615044484;
			Assert.AreEqual(expected1, actual1);

			string input2 = "0x0";
			long actual2 = Hex.PrefixedHexToLong(input2);
			long expected2 = 0;
			Assert.AreEqual(expected2, actual2);
		}
	}
}
