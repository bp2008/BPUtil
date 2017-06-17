using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPUtil;

namespace UnitTests
{
	[TestClass]
	public class TestByteUtil
	{
		[TestMethod]
		public void TestCompareWithMask()
		{
			byte[] array1 = new byte[] { 0b01, 0b10, 0b11 };
			byte[] array2 = new byte[] { 0b01, 0b00, 0b11 };

			byte[] mask_1 = new byte[] { 0b11, 0b01, 0b11 };
			byte[] mask_2 = new byte[] { 0b11, 0b11, 0b11 };
			byte[] mask_3 = new byte[] { 0b11, 0b10, 0b11 };

			Assert.IsTrue(ByteUtil.CompareWithMask(array1, array2, mask_1));
			Assert.IsFalse(ByteUtil.CompareWithMask(array1, array2, mask_2));
			Assert.IsFalse(ByteUtil.CompareWithMask(array1, array2, mask_3));

			// Test with possible IP addresses and subnet masks
			byte[] array_192_168_000_100 = new byte[] { 0b11000000, 0b10101000, 0b00000000, 0b01100100 }; // 192.168.0.100
			byte[] array_192_168_000_150 = new byte[] { 0b11000000, 0b10101000, 0b00000000, 0b10010110 }; // 192.168.0.150
			byte[] array_192_168_001_100 = new byte[] { 0b11000000, 0b10101000, 0b00000001, 0b01100100 }; // 192.168.1.100
			byte[] array_008_008_004_004 = new byte[] { 0b00001000, 0b00001000, 0b00000100, 0b00000100 }; // 8.8.4.4
			byte[] mask__255_255_255_000 = new byte[] { 0b11111111, 0b11111111, 0b11111111, 0b00000000 }; // 255.255.255.0
			byte[] mask__255_255_254_000 = new byte[] { 0b11111111, 0b11111111, 0b11111110, 0b00000000 }; // 255.255.254.0

			Assert.IsTrue(ByteUtil.CompareWithMask(array_192_168_000_100, array_192_168_000_150, mask__255_255_255_000));
			Assert.IsTrue(ByteUtil.CompareWithMask(array_192_168_000_100, array_192_168_000_150, mask__255_255_254_000));

			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_000_100, array_192_168_001_100, mask__255_255_255_000));
			Assert.IsTrue(ByteUtil.CompareWithMask(array_192_168_000_100, array_192_168_001_100, mask__255_255_254_000));

			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_000_150, array_192_168_001_100, mask__255_255_255_000));
			Assert.IsTrue(ByteUtil.CompareWithMask(array_192_168_000_150, array_192_168_001_100, mask__255_255_254_000));

			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_000_100, array_008_008_004_004, mask__255_255_255_000));
			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_000_150, array_008_008_004_004, mask__255_255_255_000));
			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_001_100, array_008_008_004_004, mask__255_255_255_000));
			Assert.IsTrue(ByteUtil.CompareWithMask(array_008_008_004_004, array_008_008_004_004, mask__255_255_255_000));

			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_000_100, array_008_008_004_004, mask__255_255_254_000));
			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_000_150, array_008_008_004_004, mask__255_255_254_000));
			Assert.IsFalse(ByteUtil.CompareWithMask(array_192_168_001_100, array_008_008_004_004, mask__255_255_254_000));
			Assert.IsTrue(ByteUtil.CompareWithMask(array_008_008_004_004, array_008_008_004_004, mask__255_255_254_000));
		}
	}
}
