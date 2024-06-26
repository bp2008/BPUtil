﻿using System;
using System.Threading;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestCachedObject
	{
		private static bool allowSlowTest = true;
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
		/// This test is flaky because it relies on unpredictable timing.
		/// </summary>
		[TestMethod]
		public void TestCachedObjectCorrectness()
		{
			// Test zero maxAge -- object should be recreated every time we get the instance.
			CachedObject<ObjectThatIsDifferentEachTime> co = new CachedObject<ObjectThatIsDifferentEachTime>(() =>
			{
				return new ObjectThatIsDifferentEachTime();
			},
			TimeSpan.FromMinutes(1),
			TimeSpan.Zero);

			for (int n = 1; n <= 50; n++)
			{
				Assert.AreEqual(n, co.GetInstance().id);
			}

			// Test that the object is created only once when expiration dates are long.
			ObjectThatIsDifferentEachTime.Reset();
			co = new CachedObject<ObjectThatIsDifferentEachTime>(() =>
				{
					return new ObjectThatIsDifferentEachTime();
				},
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMinutes(2));

			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);
			Assert.AreEqual(1, co.GetInstance().id);

			if (allowSlowTest)
			{
				Thread.Sleep(100);
				Assert.AreEqual(1, co.GetInstance().id);
				Thread.Sleep(100);

				// Test short minAge
				ObjectThatIsDifferentEachTime.Reset();
				co = new CachedObject<ObjectThatIsDifferentEachTime>(() =>
				{
					return new ObjectThatIsDifferentEachTime();
				},
				TimeSpan.FromMilliseconds(50),
				TimeSpan.FromMinutes(2));

				Assert.AreEqual(1, co.GetInstance().id);
				Assert.AreEqual(1, co.GetInstance().id);
				Assert.AreEqual(1, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(1, co.GetInstance().id);
				Thread.Sleep(25);
				Assert.AreEqual(2, co.GetInstance().id);
				Assert.AreEqual(2, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(2, co.GetInstance().id);
				Thread.Sleep(25);
				Assert.AreEqual(3, co.GetInstance().id);
				Assert.AreEqual(3, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(3, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(4, co.GetInstance().id);
				Thread.Sleep(100);

				// Test short maxAge
				ObjectThatIsDifferentEachTime.Reset();
				co = new CachedObject<ObjectThatIsDifferentEachTime>(() =>
				{
					return new ObjectThatIsDifferentEachTime();
				},
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMilliseconds(50));

				Assert.AreEqual(1, co.GetInstance().id);
				Assert.AreEqual(1, co.GetInstance().id);
				Assert.AreEqual(1, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(2, co.GetInstance().id);
				Assert.AreEqual(2, co.GetInstance().id);
				Assert.AreEqual(2, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(3, co.GetInstance().id);
				Assert.AreEqual(3, co.GetInstance().id);
				Assert.AreEqual(3, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(4, co.GetInstance().id);
				Thread.Sleep(100);
				Assert.AreEqual(5, co.GetInstance().id);

			}
		}
	}
}
