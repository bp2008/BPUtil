using System;
using System.Collections.Generic;
using System.Threading;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestCachedObject
	{
		private class ObjectThatIsDifferentEachTime
		{
			private static long counter = 0;
			public readonly long id;
			public ObjectThatIsDifferentEachTime()
			{
				id = Interlocked.Increment(ref counter);
			}
			public static void Reset()
			{
				counter = 0;
			}
		}

		/// <summary>
		/// A CachedObject whose clock and background thread belong to the test, so that age-dependent behavior can be tested exactly and without waiting for real time to pass.
		/// </summary>
		private class TestableCachedObject<T> : CachedObject<T>
		{
			/// <summary>
			/// The current value of this object's clock, in milliseconds.  Only <see cref="Advance"/> moves it.
			/// </summary>
			private long nowMs = 0;
			/// <summary>
			/// Actions the CachedObject asked to have run on a background thread, waiting for <see cref="RunPendingBackgroundWork"/> to run them.
			/// </summary>
			private readonly List<Action> pendingBackgroundWork = new List<Action>();

			public TestableCachedObject(Func<T> createNewObjectFunc, TimeSpan minAge, TimeSpan maxAge, Action<Exception> ReportException)
				: base(createNewObjectFunc, minAge, maxAge, ReportException)
			{
			}

			protected override long GetTimestampMs()
			{
				return Interlocked.Read(ref nowMs);
			}

			protected override void RunInBackground(Action action)
			{
				lock (pendingBackgroundWork)
					pendingBackgroundWork.Add(action);
			}

			/// <summary>
			/// Moves this object's clock forward, aging the cached instance by [amount].
			/// </summary>
			/// <param name="amount">Amount of time to age the cached instance by.</param>
			public void Advance(TimeSpan amount)
			{
				Interlocked.Add(ref nowMs, (long)Math.Round(amount.TotalMilliseconds));
			}

			/// <summary>
			/// Runs on this thread, in the order they were scheduled, all the actions the CachedObject asked to have run in the background, and returns how many there were.
			/// </summary>
			public int RunPendingBackgroundWork()
			{
				List<Action> work;
				lock (pendingBackgroundWork)
				{
					work = new List<Action>(pendingBackgroundWork);
					pendingBackgroundWork.Clear();
				}
				foreach (Action action in work)
					action();
				return work.Count;
			}

			/// <summary>
			/// The number of actions waiting for <see cref="RunPendingBackgroundWork"/> to run them.
			/// </summary>
			public int PendingBackgroundWorkCount
			{
				get
				{
					lock (pendingBackgroundWork)
						return pendingBackgroundWork.Count;
				}
			}
		}

		/// <summary>
		/// Returns a CachedObject that creates a new <see cref="ObjectThatIsDifferentEachTime"/> on each reload, with ids counted from 1 again.
		/// </summary>
		/// <param name="minAge">Minimum age.</param>
		/// <param name="maxAge">Maximum age.</param>
		private TestableCachedObject<ObjectThatIsDifferentEachTime> NewCachedObject(TimeSpan minAge, TimeSpan maxAge)
		{
			ObjectThatIsDifferentEachTime.Reset();
			return new TestableCachedObject<ObjectThatIsDifferentEachTime>(() =>
			{
				return new ObjectThatIsDifferentEachTime();
			},
			minAge,
			maxAge,
			ReportException);
		}

		/// <summary>
		/// Tests zero maxAge -- the object should be recreated every time we get the instance.
		/// </summary>
		[TestMethod]
		public void TestZeroMaxAge()
		{
			TestableCachedObject<ObjectThatIsDifferentEachTime> co = NewCachedObject(TimeSpan.FromMinutes(1), TimeSpan.Zero);

			for (int n = 1; n <= 50; n++)
				Assert.AreEqual(n, co.GetInstance().id);

			Assert.AreEqual(0, co.PendingBackgroundWorkCount, "An instance that was reloaded because it reached maxAge should not also have been scheduled for a background reload.");
		}

		/// <summary>
		/// Tests that the object is created only once when expiration dates are long.
		/// </summary>
		[TestMethod]
		public void TestLongExpirationDates()
		{
			TestableCachedObject<ObjectThatIsDifferentEachTime> co = NewCachedObject(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);

			co.Advance(TimeSpan.FromSeconds(59));

			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(0, co.PendingBackgroundWorkCount, "An instance younger than minAge should not have been scheduled for a background reload.");
		}

		/// <summary>
		/// Tests short minAge -- reaching minAge should schedule a reload in the background without making the caller wait for it.
		/// </summary>
		[TestMethod]
		public void TestShortMinAge()
		{
			TestableCachedObject<ObjectThatIsDifferentEachTime> co = NewCachedObject(TimeSpan.FromMilliseconds(50), TimeSpan.FromMinutes(2));

			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);

			co.Advance(TimeSpan.FromMilliseconds(49));
			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(0, co.PendingBackgroundWorkCount, "minAge should not have been treated as reached one millisecond early.");

			co.Advance(TimeSpan.FromMilliseconds(1));
			Assert.AreEqual(1, co.GetInstance().id, "At minAge, the existing instance should still be returned without delay.");
			Assert.AreEqual(1, co.PendingBackgroundWorkCount, "At minAge, one reload should have been scheduled in the background.");
			Assert.AreEqual(1, co.RunPendingBackgroundWork());
			Assert.AreEqual(2, co.GetInstance().id);
			Assert.AreEqual(0, co.PendingBackgroundWorkCount, "The reloaded instance is new, so nothing further should have been scheduled.");

			// Callers that arrive before the scheduled reload has run each schedule one, but only one new instance is created.
			co.Advance(TimeSpan.FromMilliseconds(50));
			Assert.AreEqual(2, co.GetInstance().id);
			Assert.AreEqual(2, co.GetInstance().id);
			Assert.AreEqual(2, co.PendingBackgroundWorkCount);
			Assert.AreEqual(2, co.RunPendingBackgroundWork());
			Assert.AreEqual(3, co.GetInstance().id, "Two scheduled reloads that both ran after minAge was reached once should have created only one new instance.");

			// An instance that is well past minAge still costs only one reload.
			co.Advance(TimeSpan.FromMinutes(1));
			Assert.AreEqual(3, co.GetInstance().id);
			co.RunPendingBackgroundWork();
			Assert.AreEqual(4, co.GetInstance().id);
		}

		/// <summary>
		/// Tests short maxAge -- reaching maxAge should make the caller wait for a new instance rather than returning the expired one.
		/// </summary>
		[TestMethod]
		public void TestShortMaxAge()
		{
			TestableCachedObject<ObjectThatIsDifferentEachTime> co = NewCachedObject(TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(50));

			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);

			co.Advance(TimeSpan.FromMilliseconds(49));
			Assert.AreEqual(1, co.GetInstance().id, "maxAge should not have been treated as reached one millisecond early.");

			co.Advance(TimeSpan.FromMilliseconds(1));
			Assert.AreEqual(2, co.GetInstance().id);
			Assert.AreEqual(2, co.GetInstance().id);
			Assert.AreEqual(2, co.GetInstance().id);
			Assert.AreEqual(0, co.PendingBackgroundWorkCount, "The reload at maxAge happens on the calling thread, so nothing should have been scheduled in the background.");

			co.Advance(TimeSpan.FromMilliseconds(50));
			Assert.AreEqual(3, co.GetInstance().id);
			Assert.AreEqual(3, co.GetInstance().id);

			co.Advance(TimeSpan.FromMinutes(1));
			Assert.AreEqual(4, co.GetInstance().id, "An instance far past maxAge should cost one reload, the same as one that just reached maxAge.");
			Assert.AreEqual(4, co.GetInstance().id);
		}

		[TestMethod]
		public void TestReloadIfOlderThan()
		{
			TestableCachedObject<ObjectThatIsDifferentEachTime> co = NewCachedObject(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

			// With no instance yet, any threshold creates one.
			Assert.AreEqual(1, co.ReloadIfOlderThan(TimeSpan.FromMinutes(10)).id);
			// An instance younger than the threshold is returned as-is.
			Assert.AreEqual(1, co.ReloadIfOlderThan(TimeSpan.FromMinutes(10)).id);
			Assert.AreEqual(1, co.GetInstance().id);
			// A zero threshold always reloads, and GetInstance sees the result.
			Assert.AreEqual(2, co.ReloadIfOlderThan(TimeSpan.Zero).id);
			Assert.AreEqual(3, co.ReloadIfOlderThan(TimeSpan.Zero).id);
			Assert.AreEqual(3, co.GetInstance().id);
			// Reload() is unconditional, and the throttled variant then sees the new instance as young.
			Assert.AreEqual(4, co.Reload().id);
			Assert.AreEqual(4, co.ReloadIfOlderThan(TimeSpan.FromMinutes(10)).id);

			// Once the instance is older than the threshold, exactly one reload occurs; the next call sees a young instance again.
			co.Advance(TimeSpan.FromMilliseconds(100));
			Assert.AreEqual(5, co.ReloadIfOlderThan(TimeSpan.FromMilliseconds(50)).id);
			Assert.AreEqual(5, co.ReloadIfOlderThan(TimeSpan.FromMilliseconds(50)).id);
			Assert.AreEqual(5, co.GetInstance().id);
		}

		private void ReportException(Exception ex)
		{
			Assert.Fail("An exception was thrown: " + ex.ToString());
		}
	}
}
