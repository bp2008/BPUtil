using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPUtil;
using System.IO;

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
		[TestMethod]
		public void TestByteArraysMatch()
		{
			byte[] array1 = new byte[] { 0b00000001, 0b00000010, 0b00000100, 0b00001000, 0b00010000, 0b00100000, 0b01000000, 0b10000000, 0b11110000, 0b10101010 };
			byte[] array2 = new byte[] { 0b11111110, 0b11111101, 0b11111011, 0b11110111, 0b11101111, 0b11011111, 0b10111111, 0b01111111, 0b00001111, 0b01010101 };
			byte[] array3 = new byte[] { 0b11111110 };

			byte[] array1_copy = new byte[] { 0b00000001, 0b00000010, 0b00000100, 0b00001000, 0b00010000, 0b00100000, 0b01000000, 0b10000000, 0b11110000, 0b10101010 };
			byte[] array2_copy = new byte[] { 0b11111110, 0b11111101, 0b11111011, 0b11110111, 0b11101111, 0b11011111, 0b10111111, 0b01111111, 0b00001111, 0b01010101 };

			Assert.IsTrue(ByteUtil.ByteArraysMatch(array1, array1));
			Assert.IsTrue(ByteUtil.ByteArraysMatch(array1, array1_copy));
			Assert.IsTrue(ByteUtil.ByteArraysMatch(array1_copy, array1));
			Assert.IsTrue(ByteUtil.ByteArraysMatch(array2, array2_copy));

			Assert.IsFalse(ByteUtil.ByteArraysMatch(array1, array2));
			Assert.IsFalse(ByteUtil.ByteArraysMatch(array2, array1));
			Assert.IsFalse(ByteUtil.ByteArraysMatch(array2, array3));
			Assert.IsFalse(ByteUtil.ByteArraysMatch(array3, array2));

			Assert.IsFalse(ByteUtil.ByteArraysMatch(null, array1));
			Assert.IsFalse(ByteUtil.ByteArraysMatch(array1, null));

			Assert.IsTrue(ByteUtil.ByteArraysMatch(null, null));
		}
		[TestMethod]
		public void TestGetInverse()
		{
			byte[] array1 = new byte[] { 0b00000001, 0b00000010, 0b00000100, 0b00001000, 0b00010000, 0b00100000, 0b01000000, 0b10000000, 0b11110000, 0b10101010 };
			byte[] expected = new byte[] { 0b11111110, 0b11111101, 0b11111011, 0b11110111, 0b11101111, 0b11011111, 0b10111111, 0b01111111, 0b00001111, 0b01010101 };
			byte[] actual = ByteUtil.GetInverse(array1);
			Assert.IsTrue(ByteUtil.ByteArraysMatch(expected, actual));
			Assert.IsNull(ByteUtil.GetInverse(null));
		}
		[TestMethod]
		public void TestInvertBits()
		{
			byte[] array1 = new byte[] { 0b00000001, 0b00000010, 0b00000100, 0b00001000, 0b00010000, 0b00100000, 0b01000000, 0b10000000, 0b11110000, 0b10101010 };
			byte[] expected = new byte[] { 0b11111110, 0b11111101, 0b11111011, 0b11110111, 0b11101111, 0b11011111, 0b10111111, 0b01111111, 0b00001111, 0b01010101 };
			ByteUtil.InvertBits(array1);
			Assert.IsTrue(ByteUtil.ByteArraysMatch(expected, array1));
			// Passing null does not throw exception.
			ByteUtil.InvertBits(null);
		}
		[TestMethod]
		public void TestReadToEnd()
		{
			int len = 2 * 1024 * 1024;
			byte[] input = new byte[len];
			MemoryStream ms = new MemoryStream(len);
			for (int i = 0; i < len; i++)
			{
				input[i] = (byte)len;
				ms.WriteByte(input[i]);
			}

			Assert.AreEqual(len, ms.Position, "Position was not " + len);
			Assert.AreEqual(0, ByteUtil.ReadToEndAsync(ms).GetAwaiter().GetResult().Length, "MemoryStream length was not " + 0);
			Assert.AreEqual(0, ByteUtil.ReadToEndAsync(ms.Substream(ms.Length)).GetAwaiter().GetResult().Length, "Substream length was not " + 0);

			ms.Position = 0;
			byte[] output1 = ByteUtil.ReadToEnd(ms);
			CollectionAssert.AreEqual(input, output1);

			ms.Position = 0;
			byte[] output2 = ByteUtil.ReadToEnd(ms.Substream(ms.Length));
			CollectionAssert.AreEqual(input, output2);
		}
		[TestMethod]
		public void TestReadToEndAsync()
		{
			int len = 2 * 1024 * 1024;
			byte[] input = new byte[len];
			MemoryStream ms = new MemoryStream(len);
			for (int i = 0; i < len; i++)
			{
				input[i] = (byte)len;
				ms.WriteByte(input[i]);
			}

			Assert.AreEqual(len, ms.Position);
			Assert.AreEqual(0, ByteUtil.ReadToEnd(ms).Length);
			Assert.AreEqual(0, ByteUtil.ReadToEnd(ms.Substream(ms.Length)).Length);

			ms.Position = 0;
			byte[] output1 = ByteUtil.ReadToEndAsync(ms).GetAwaiter().GetResult();
			CollectionAssert.AreEqual(input, output1);

			ms.Position = 0;
			byte[] output2 = ByteUtil.ReadToEndAsync(ms.Substream(ms.Length)).GetAwaiter().GetResult();
			CollectionAssert.AreEqual(input, output2);
		}
		[TestMethod]
		public void TestReadToEndWithMaxLength()
		{
			int len = 2 * 1024 * 1024;
			byte[] input = new byte[len];
			MemoryStream ms = new MemoryStream(len);
			for (int i = 0; i < len; i++)
			{
				input[i] = (byte)len;
				ms.WriteByte(input[i]);
			}

			byte[] output;
			ms.Position = 0;
			Assert.IsTrue(ByteUtil.ReadToEndWithMaxLength(ms, len * 2, out output));
			CollectionAssert.AreEqual(input, output);

			ms.Position = 0;
			Assert.IsTrue(ByteUtil.ReadToEndWithMaxLength(ms.Substream(ms.Length), len * 2, out output));
			CollectionAssert.AreEqual(input, output);

			ms.Position = 0;
			Assert.IsTrue(ByteUtil.ReadToEndWithMaxLength(ms, len, out output));
			CollectionAssert.AreEqual(input, output);

			ms.Position = 0;
			Assert.IsTrue(ByteUtil.ReadToEndWithMaxLength(ms.Substream(ms.Length), len, out output));
			CollectionAssert.AreEqual(input, output);

			ms.Position = 0;
			Assert.IsFalse(ByteUtil.ReadToEndWithMaxLength(ms, len - 1, out output));
			Assert.IsNull(output);

			ms.Position = 0;
			Assert.IsFalse(ByteUtil.ReadToEndWithMaxLength(ms.Substream(ms.Length), len - 1, out output));
			Assert.IsNull(output);

			ms.Position = 0;
			Assert.IsFalse(ByteUtil.ReadToEndWithMaxLength(ms, 1, out output));
			Assert.IsNull(output);

			ms.Position = 0;
			Assert.IsFalse(ByteUtil.ReadToEndWithMaxLength(ms.Substream(ms.Length), 1, out output));
			Assert.IsNull(output);

			ms.Position = 0;
			Assert.IsFalse(ByteUtil.ReadToEndWithMaxLength(ms, 0, out output));
			Assert.IsNull(output);

			ms.Position = 0;
			Assert.IsFalse(ByteUtil.ReadToEndWithMaxLength(ms.Substream(ms.Length), 0, out output));
			Assert.IsNull(output);
		}
		[TestMethod]
		public void TestDiscardUntilEndOfStream()
		{
			int len = 2 * 1024 * 1024;
			byte[] input = new byte[len];
			MemoryStream ms = new MemoryStream(len);
			for (int i = 0; i < len; i++)
			{
				input[i] = (byte)len;
				ms.WriteByte(input[i]);
			}
			ms.Position = 0;
			ByteUtil.DiscardUntilEndOfStream(ms);
			Assert.AreEqual(len, ms.Position);

			ms.Position = 0;
			ByteUtil.DiscardUntilEndOfStream(ms.Substream(ms.Length));
			Assert.AreEqual(len, ms.Position);
		}
	}
}
