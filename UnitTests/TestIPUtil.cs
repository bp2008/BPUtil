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
		[TestMethod]
		public void TestGetIPv4OrIPv6Slash64()
		{
			Assert.AreEqual("127.0.0.1", IPUtil.GetIPv4OrIPv6Slash64("127.0.0.1"));
			Assert.AreEqual("127.0.0.1", IPUtil.GetIPv4OrIPv6Slash64("127.0.0.1 ")); // Invalid input: Trailing space. Library pre-parses and makes it valid.
			Assert.AreEqual("127.0.0.1", IPUtil.GetIPv4OrIPv6Slash64(" 127.0.0.1")); // Invalid input: Leading space. Library pre-parses and makes it valid.
			Assert.AreEqual("127.0.0.1", IPUtil.GetIPv4OrIPv6Slash64(" 127.0.0.1 ")); // Invalid input: Leading/trailing spaces. Library pre-parses and makes it valid.
			Assert.AreEqual("", IPUtil.GetIPv4OrIPv6Slash64("127.256.0.1")); // Invalid input: One octet out of range

			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:0000:0000:8a2e:0370:7334"));
			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:0000::"));
			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:000::"));
			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:00::"));
			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:0::"));
			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3::"));
			Assert.AreEqual("2600:1234:5678:9abc::", IPUtil.GetIPv4OrIPv6Slash64("2600:1234:5678:9Abc:dEf0:1234:5678:9ABC")); // Normal test with mixed case A-F hex chars; output should be normalized to lower case.
			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]")); // IPAddress class can parse IPv6 wrapped with brackets
			Assert.AreEqual("", IPUtil.GetIPv4OrIPv6Slash64("[2001:0db8:85a3:0000:0000:8a2e:0370:7334")); // Invalid input: Missing closing bracket
			Assert.AreEqual("", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:0000:0000:8a2e:0370:7334]")); // Invalid input: Missing opening bracket
			Assert.AreEqual("2001:db8:85a3::", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:0000:0000:8a2e:0370:7334 ")); // Invalid input: Trailing space Library pre-parses and makes it valid.
			Assert.AreEqual("", IPUtil.GetIPv4OrIPv6Slash64("2001:0db8:85a3:0000:0000:8a2e:0370:7334:0")); // Invalid input: Too many bytes
		}
		[TestMethod]
		public void TestIpAddressTo16Bytes()
		{
			AssertIPBytes("0.0.0.0", "00000000000000000000FFFF00000000");
			AssertIPBytes("192.0.1.128", "00000000000000000000FFFFC0000180");
			AssertIPBytes("255.0.255.255", "00000000000000000000FFFFFF00FFFF");
			AssertIPBytes("255.255.255.255", "00000000000000000000FFFFFFFFFFFF");
			AssertIPBytes("::0", "00000000000000000000000000000000");
			AssertIPBytes("::1", "00000000000000000000000000000001");
			AssertIPBytes("::FFFF", "0000000000000000000000000000FFFF");
			AssertIPBytes("::FFFF:C000:0180", "00000000000000000000FFFFC0000180");
			AssertIPBytes("FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF", "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
		}
		private static void AssertIPBytes(string ipStr, string expectedHex)
		{
			expectedHex = expectedHex.ToUpper();

			byte[] ipBytes = IPUtil.IpAddressTo16Bytes(ipStr);
			string actualHex = Hex.ToHex(ipBytes, true);
			Assert.AreEqual(expectedHex, actualHex, "IPUtil.IpAddressTo16Bytes(\"" + ipStr + "\") failed.");

			IPAddress ipAddress = IPAddress.Parse(ipStr);
			ipBytes = ipAddress.To16Bytes();
			actualHex = Hex.ToHex(ipBytes, true);
			Assert.AreEqual(expectedHex, actualHex, "ipAddress.To16Bytes(\"" + ipStr + "\") extension method failed.");
		}
	}
}
