using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPUtil;
using System.Diagnostics;

namespace UnitTests
{
	internal class TokenBucketForUnitTest : TokenBucket
	{
		public TokenBucketForUnitTest(double capacity, double refillRate) : base(capacity, refillRate) { }
		long _ticks = 0;
		public void SetTime(long ticks)
		{
			_ticks = ticks;
		}
		protected override long GetTime()
		{
			return _ticks;
		}
	}
	[TestClass]
	public class TestTokenBucket
	{
		[TestMethod]
		public void TestTokenBucketBasic()
		{
			TokenBucketForUnitTest bucket = new TokenBucketForUnitTest(10, 1);

			// Consume all tokens, 5 at a time.
			Assert.IsTrue(bucket.TryConsume(5));
			Assert.IsTrue(bucket.TryConsume(5));

			// No tokens should remain
			Assert.IsFalse(bucket.TryConsume(1));
			Assert.IsFalse(bucket.TryConsume(5));

			// Test correct token allocation through the first hundred ticks.
			for (int i = 1; i <= 100; i++)
			{
				// One tick before a new token is available, 1 token should not be available.
				bucket.SetTime((Stopwatch.Frequency * i) - 1);
				Assert.IsFalse(bucket.TryConsume(1), "1 token should not be available 1 tick before it is available. (index " + i + ")");

				// Due to rounding error, a full token may not be available on exactly the expected tick, but it should be very close.
				bucket.SetTime((Stopwatch.Frequency * i));
				Assert.IsTrue(Math.Abs(1 - bucket.Peek()) < 0.0001);

				// On the next tick, it should be safe to consume a full token.
				bucket.SetTime((Stopwatch.Frequency * i) + 1);
				Assert.IsTrue(bucket.TryConsume(1), "1 token should be available after the correct tick. (index " + i + ")");
			}
		}

		[TestMethod]
		public void TestTokenBucketMultiThreaded()
		{
			TokenBucketForUnitTest bucket = new TokenBucketForUnitTest(10, 1);
			int successCount = 0;
			int failureCount = 0;
			Thread[] threads = new Thread[15];
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(() =>
				{
					if (bucket.TryConsume(1))
						Interlocked.Increment(ref successCount);
					else
						Interlocked.Increment(ref failureCount);
				});
				threads[i].Start();
			}
			foreach (Thread thread in threads)
				thread.Join();

			Assert.AreEqual(10, successCount);
			Assert.AreEqual(5, failureCount);
		}
		[TestMethod]
		public void TestTokenBucketWithInvalidCapacity()
		{
			TokenBucketForUnitTest bucket = new TokenBucketForUnitTest(0.00001, 1); // Valid
			try
			{
				bucket = new TokenBucketForUnitTest(0, 1);
				Assert.Fail("0 capacity should have thrown ArgumentOutOfRangeException");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			try
			{
				bucket = new TokenBucketForUnitTest(-1, 1);
				Assert.Fail("-1 capacity should have thrown ArgumentOutOfRangeException");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
		}
		[TestMethod]
		public void TestTokenBucketWithInvalidRefillRate()
		{
			TokenBucketForUnitTest bucket = new TokenBucketForUnitTest(10, 0.00001); // Valid
			try
			{
				bucket = new TokenBucketForUnitTest(10, 0);
				Assert.Fail("0 refill rate should have thrown ArgumentOutOfRangeException");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			try
			{
				bucket = new TokenBucketForUnitTest(10, -1);
				Assert.Fail("-1 refill rate should have thrown ArgumentOutOfRangeException");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
		}
		[TestMethod]
		public void TestTokenBucketWithInvalidTokenConsumeSize()
		{
			TokenBucketForUnitTest bucket = new TokenBucketForUnitTest(10, 1); // Valid
			Assert.IsTrue(bucket.TryConsume(0.00001));
			try
			{
				bucket.TryConsume(0);
				Assert.Fail("0 consume size should have thrown ArgumentOutOfRangeException");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			try
			{
				bucket.TryConsume(-1);
				Assert.Fail("-1 consume size should have thrown ArgumentOutOfRangeException");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
		}
	}
}
