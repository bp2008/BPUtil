using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

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
		public void TestGenerateMaskFromPrefixSize()
		{
			byte[] expected = IPUtil.GenerateMaskBytesFromPrefixSize(true, 23);
			byte[] actual = IPUtil.GenerateMaskFromPrefixSize(true, 23).GetAddressBytes();
			CollectionAssert.AreEqual(expected, actual);

			expected = IPUtil.GenerateMaskBytesFromPrefixSize(false, 47);
			actual = IPUtil.GenerateMaskFromPrefixSize(false, 47).GetAddressBytes();
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

		[TestMethod]
		public void TestGetPrefixSizeOfMask()
		{
			Assert.AreEqual(0, IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("0.0.0.0")));
			Assert.AreEqual(24, IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("255.255.255.0")));
			Assert.AreEqual(22, IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("255.255.252.0")));
			Assert.AreEqual(32, IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("255.255.255.255")));

			Assert.AreEqual(0, IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("::")));
			Assert.AreEqual(64, IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("ffff:ffff:ffff:ffff::")));
			Assert.AreEqual(128, IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void TestGetPrefixSizeOfMaskThrowsOnInvalidInput()
		{
			IPUtil.GetPrefixSizeOfMask(IPAddress.Parse("192.168.0.1"));
		}

		[TestMethod]
		public void TestSubnetCompare()
		{
			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("192.168.0.1"), IPAddress.Parse("192.168.0.255"), 0, 64));
			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("192.168.0.1"), IPAddress.Parse("192.168.0.255"), 24, 64));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("192.168.0.1"), IPAddress.Parse("192.168.0.255"), 25, 64));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("192.168.0.1"), IPAddress.Parse("192.168.0.255"), 32, 64));

			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("192.168.0.1"), IPAddress.Parse("192.168.0.1"), 32, 64));

			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("2001:4860:4860::8888"), IPAddress.Parse("2001:4860:4860::8844"), 32, 0));
			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("2001:4860:4860::8888"), IPAddress.Parse("2001:4860:4860::8844"), 32, 64));
			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("2001:4860:4860::8888"), IPAddress.Parse("2001:4860:4860::8844"), 32, 120));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("2001:4860:4860::8888"), IPAddress.Parse("2001:4860:4860::8844"), 32, 121));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("2001:4860:4860::8888"), IPAddress.Parse("2001:4860:4860::8844"), 32, 128));

			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("2001:4860:4860::8888"), IPAddress.Parse("2001:4860:4860::8888"), 32, 128));

			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:ffff:ffff:ffff:ffff"), 32, 0));
			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:ffff:ffff:ffff:ffff"), 32, 64));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:ffff:ffff:ffff:ffff"), 32, 65));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:ffff:ffff:ffff:ffff"), 32, 128));

			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:1111:1111:1111:1111"), 32, 0));
			Assert.IsTrue(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:1111:1111:1111:1111"), 32, 67));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:1111:1111:1111:1111"), 32, 68));
			Assert.IsFalse(IPUtil.SubnetCompare(IPAddress.Parse("1234:5678:1234:5678:0000:0000:0000:0000"), IPAddress.Parse("1234:5678:1234:5678:1111:1111:1111:1111"), 32, 128));
		}
	}
}
