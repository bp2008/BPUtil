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
	internal class TokenBucketDictionaryForUnitTest<TKey> : TokenBucketDictionary<TKey>
	{
		public TokenBucketDictionaryForUnitTest(double capacity, double refillRate, double maintenanceIntervalMilliseconds) : base(capacity, refillRate, maintenanceIntervalMilliseconds) { }
		/// <summary>
		/// Sets the time for all buckets in the dictionary.  This is only useful for unit testing.
		/// </summary>
		/// <param name="ticks">Ticks to assign</param>
		public void SetTime(long ticks)
		{
			lock (_lock)
			{
				foreach (TokenBucket bucket in _buckets.Values)
					((TokenBucketForUnitTest)bucket).SetTime(ticks);
			}
		}
		protected override TokenBucket CreateNewBucket(TKey key)
		{
			// Returns a token bucket where the timing method is overridden.
			return new TokenBucketForUnitTest(Capacity, RefillRate);
		}
		public void ForceMaintenance()
		{
			lock (_lock)
				RemoveFullBuckets();
		}
	}
	[TestClass]
	public class TestTokenBucketDictionary
	{
		[TestMethod]
		public void TestTokenBucketDictionaryBasic()
		{
			TokenBucketDictionaryForUnitTest<string> dict = new TokenBucketDictionaryForUnitTest<string>(10, 1, 60000);

			Assert.AreEqual(10, dict.Capacity);
			Assert.AreEqual(1, dict.RefillRate);
			Assert.AreEqual(60000, dict.MaintenanceIntervalMilliseconds);

			Assert.AreEqual(0, dict.NumberOfBuckets);

			Assert.AreEqual(10, dict.Peek("A"));
			Assert.AreEqual(10, dict.Peek("B"));
			Assert.AreEqual(10, dict.Peek("C"));

			Assert.IsTrue(dict.TryConsume("A", 5));

			Assert.AreEqual(1, dict.NumberOfBuckets);

			Assert.IsTrue(dict.TryConsume("A", 5));
			Assert.IsFalse(dict.TryConsume("A", 5));
			Assert.IsFalse(dict.TryConsume("A", 0.001));

			Assert.AreEqual(1, dict.NumberOfBuckets);

			Assert.IsFalse(dict.TryConsume("B", 10.001));
			Assert.IsTrue(dict.TryConsume("B", 10));

			Assert.AreEqual(2, dict.NumberOfBuckets);

			Assert.IsTrue(dict.TryConsume("C", 10));

			Assert.AreEqual(3, dict.NumberOfBuckets);

			dict.ForceMaintenance();

			// Set the time to one tick before the complete refill should occur.
			dict.SetTime((Stopwatch.Frequency * 10) - 1);

			dict.ForceMaintenance();

			Assert.AreEqual(3, dict.NumberOfBuckets);

			Assert.IsTrue(dict.Peek("A") > 0);
			Assert.IsTrue(dict.Peek("A") < 10);
			Assert.AreEqual(dict.Peek("A"), dict.Peek("B"));
			Assert.AreEqual(dict.Peek("A"), dict.Peek("C"));

			dict.ForceMaintenance();

			Assert.AreEqual(3, dict.NumberOfBuckets);

			dict.SetTime((Stopwatch.Frequency * 10) + 1); // Add one tick to ensure rounding errors don't cause a problem.

			// All buckets should be full, but maintenance won't happen because the interval hasn't elapsed yet.

			Assert.AreEqual(10.0, dict.Peek("A"));
			Assert.AreEqual(10.0, dict.Peek("B"));
			Assert.AreEqual(10.0, dict.Peek("C"));

			Assert.AreEqual(3, dict.NumberOfBuckets);

			// Forcing maintenance to run should delete all buckets because they are all full.
			dict.ForceMaintenance();

			Assert.AreEqual(10.0, dict.Peek("A"));
			Assert.AreEqual(10.0, dict.Peek("B"));
			Assert.AreEqual(10.0, dict.Peek("C"));

			Assert.AreEqual(0, dict.NumberOfBuckets);
		}
	}
}
