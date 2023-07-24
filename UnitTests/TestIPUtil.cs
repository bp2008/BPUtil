using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTests
{
	[TestClass]
	public class TestIPUtil
	{
		[TestMethod]
		public void TestGenerateMaskBytesFromPrefixSize_IPv4()
		{
			byte[] expected, actual;

			expected = new byte[] { 0, 0, 0, 0 };
			actual = IPUtil.GenerateMaskBytesFromPrefixSize(true, 0);
			CollectionAssert.AreEqual(expected, actual);

			expected = new byte[] { 128, 0, 0, 0 };
			actual = IPUtil.GenerateMaskBytesFromPrefixSize(true, 1);
			CollectionAssert.AreEqual(expected, actual);

			expected = new byte[] { 255, 255, 255, 0 };
			actual = IPUtil.GenerateMaskBytesFromPrefixSize(true, 24);
			CollectionAssert.AreEqual(expected, actual);

			expected = new byte[] { 255, 255, 255, 254 };
			actual = IPUtil.GenerateMaskBytesFromPrefixSize(true, 31);
			CollectionAssert.AreEqual(expected, actual);

			expected = new byte[] { 255, 255, 255, 255 };
			actual = IPUtil.GenerateMaskBytesFromPrefixSize(true, 32);
			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestGenerateMaskBytesFromPrefixSize_IPv6()
		{
			byte[] expected = new byte[16];
			byte[] actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 0);
			CollectionAssert.AreEqual(expected, actual);

			expected[0] = 255;
			expected[1] = 255;
			expected[2] = 255;
			expected[3] = 128;

			actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 25);
			CollectionAssert.AreEqual(expected, actual);

			for (int i = 0; i < 6; i++)
				expected[i] = 255;

			actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 48);
			CollectionAssert.AreEqual(expected, actual);

			for (int i = 0; i < 7; i++)
				expected[i] = 255;

			actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 56);
			CollectionAssert.AreEqual(expected, actual);

			for (int i = 0; i < 8; i++)
				expected[i] = 255;

			actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 64);
			CollectionAssert.AreEqual(expected, actual);

			for (int i = 0; i < 15; i++)
				expected[i] = 255;

			actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 120);
			CollectionAssert.AreEqual(expected, actual);

			expected[15] = 254;

			actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 127);
			CollectionAssert.AreEqual(expected, actual);

			expected[15] = 255;
			actual = IPUtil.GenerateMaskBytesFromPrefixSize(false, 128);
			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void TestGenerateMaskBytesFromPrefixSize_InvalidPrefixSize()
		{
			IPUtil.GenerateMaskBytesFromPrefixSize(true, -1);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void TestGenerateMaskBytesFromPrefixSize_InvalidPrefixSize_2()
		{
			IPUtil.GenerateMaskBytesFromPrefixSize(true, 33);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void TestGenerateMaskBytesFromPrefixSize_InvalidPrefixSize_3()
		{
			IPUtil.GenerateMaskBytesFromPrefixSize(false, 129);
		}
	}
}
