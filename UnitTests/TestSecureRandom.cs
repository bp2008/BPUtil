using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestSecureRandom
	{
		[TestMethod]
		public void TestNextBytesFillsCallerBuffer()
		{
			// Sizes below, at, and above the internal 512-byte pool size.
			int[] sizes = new int[] { 16, 32, 64, 256, 511, 512, 513, 1024, 4096, 100000 };
			foreach (int size in sizes)
			{
				byte[] buffer = new byte[size];
				SecureRandom.NextBytes(buffer);
				// Probability of 16+ random bytes all being zero is at most 2^-128.
				Assert.IsTrue(buffer.Any(b => b != 0), "NextBytes left a " + size + "-byte buffer filled with zeros.");
			}
		}

		[TestMethod]
		public void TestNextBytesFillsTinyBuffers()
		{
			for (int size = 1; size < 16; size++)
			{
				bool sawNonZero = false;
				for (int attempt = 0; attempt < 64 && !sawNonZero; attempt++)
				{
					byte[] buffer = new byte[size];
					SecureRandom.NextBytes(buffer);
					sawNonZero = buffer.Any(b => b != 0);
				}
				Assert.IsTrue(sawNonZero, "NextBytes left a " + size + "-byte buffer filled with zeros on 64 consecutive calls.");
			}
		}

		[TestMethod]
		public void TestNextBytesEmptyBuffer()
		{
			SecureRandom.NextBytes(new byte[0]);
		}

		[TestMethod]
		public void TestNextBytesNullBuffer()
		{
			Expect.Exception<ArgumentNullException>(() => SecureRandom.NextBytes(null));
		}

		[TestMethod]
		public void TestNextBytesDoesNotRepeat()
		{
			// 1000 calls of 100 bytes crosses the 512-byte pool boundary many times.
			HashSet<string> seen = new HashSet<string>();
			for (int i = 0; i < 1000; i++)
			{
				byte[] buffer = new byte[100];
				SecureRandom.NextBytes(buffer);
				Assert.IsTrue(seen.Add(Hex.ToHex(buffer)), "NextBytes produced a duplicate 100-byte output on call " + i + ".");
			}
		}

		[TestMethod]
		public void TestNextBytesConsecutiveCallsDiffer()
		{
			// Sizes on both sides of the internal 512-byte pool, which selects between the pooled and direct code paths.
			int[] sizes = new int[] { 1, 4, 16, 32, 511, 512, 513, 4096 };
			foreach (int size in sizes)
			{
				int zeroCount = 0;
				int repeatCount = 0;
				byte[] previous = null;
				for (int i = 0; i < 256; i++)
				{
					byte[] buffer = new byte[size];
					SecureRandom.NextBytes(buffer);
					if (buffer.All(b => b == 0))
						zeroCount++;
					if (previous != null && ByteUtil.ByteArraysMatch(previous, buffer))
						repeatCount++;
					previous = buffer;
				}
				// Buffers of 8+ bytes come back all-zero or equal to the previous output with probability <= 2^-64 per call, so none are allowed.
				// Tiny buffers legitimately do so (1 in 256 calls for 1 byte); a 1-byte count exceeds 10 of 256 with probability ~1e-8.
				int allowed = size >= 8 ? 0 : 10;
				Assert.IsTrue(zeroCount <= allowed, "NextBytes returned an all-zero " + size + "-byte buffer on " + zeroCount + " of 256 calls.");
				Assert.IsTrue(repeatCount <= allowed, "NextBytes returned the same " + size + "-byte output as the previous call " + repeatCount + " times in 256 calls.");
			}
		}

		[TestMethod]
		public void TestNextBytesUniformDistribution()
		{
			const int totalBytes = 1 << 20;

			// One large call is drawn directly from the CSPRNG.
			byte[] direct = new byte[totalBytes];
			SecureRandom.NextBytes(direct);
			AssertUniformByteDistribution(direct, "a single " + totalBytes + "-byte call");

			// Small calls are served from the internal pool.
			byte[] pooled = new byte[totalBytes];
			byte[] chunk = new byte[32];
			for (int offset = 0; offset < totalBytes; offset += chunk.Length)
			{
				SecureRandom.NextBytes(chunk);
				Buffer.BlockCopy(chunk, 0, pooled, offset, chunk.Length);
			}
			AssertUniformByteDistribution(pooled, "32-byte calls");
		}

		private static void AssertUniformByteDistribution(byte[] data, string description)
		{
			int[] counts = new int[256];
			foreach (byte b in data)
				counts[b]++;
			double expected = data.Length / 256.0;
			double chiSquare = 0;
			foreach (int count in counts)
				chiSquare += (count - expected) * (count - expected) / expected;
			// Chi-square with 255 degrees of freedom has mean 255 and standard deviation ~22.6; exceeding 420 by chance has probability below 1e-9.
			Assert.IsTrue(chiSquare < 420, "Byte distribution from " + description + " is not uniform: chi-square " + chiSquare.ToString("0.0") + ", byte counts ranged from " + counts.Min() + " to " + counts.Max() + " (expected ~" + expected + " each).");
		}

		[TestMethod]
		public void TestNextFullIntRange()
		{
			// Before the fix, (maxValue - minValue) overflowed and this always returned int.MinValue.
			bool sawNegative = false;
			bool sawNonNegative = false;
			for (int i = 0; i < 128; i++)
			{
				int n = SecureRandom.Next(int.MinValue, int.MaxValue);
				Assert.IsTrue(n >= int.MinValue && n < int.MaxValue);
				if (n < 0)
					sawNegative = true;
				else
					sawNonNegative = true;
			}
			Assert.IsTrue(sawNegative, "Next(int.MinValue, int.MaxValue) never returned a negative number.");
			Assert.IsTrue(sawNonNegative, "Next(int.MinValue, int.MaxValue) never returned a non-negative number.");
		}

		[TestMethod]
		public void TestNextWideRange()
		{
			// Before the fix, the range overflowed and results were confined to roughly [-2000000000, -1705032704).
			const int min = -2000000000;
			const int max = 2000000000;
			bool sawNonNegative = false;
			for (int i = 0; i < 128; i++)
			{
				int n = SecureRandom.Next(min, max);
				Assert.IsTrue(n >= min && n < max, "Next(" + min + ", " + max + ") returned out-of-range value " + n);
				if (n >= 0)
					sawNonNegative = true;
			}
			Assert.IsTrue(sawNonNegative, "Next(" + min + ", " + max + ") never returned a non-negative number.");
		}

		[TestMethod]
		public void TestNextSmallRange()
		{
			int[] counts = new int[10];
			for (int i = 0; i < 10000; i++)
			{
				int n = SecureRandom.Next(10);
				Assert.IsTrue(n >= 0 && n < 10);
				counts[n]++;
			}
			for (int i = 0; i < counts.Length; i++)
				Assert.IsTrue(counts[i] > 0, "Next(10) never returned " + i + " in 10000 calls.");
			Assert.AreEqual(5, SecureRandom.Next(5, 5));
		}
	}
}
