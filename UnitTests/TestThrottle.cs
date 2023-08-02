using System;
using System.Threading;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestThrottle
	{
		[TestMethod]
		public void ThrottleWorksProperly()
		{
			// If tests fail with short time periods, they may yet succeed when more time is granted for threads to be scheduled.
			try
			{
				TestThrottleInternal(5, 10);
			}
			catch
			{
				try
				{
					TestThrottleInternal(50, 100);
				}
				catch
				{
					TestThrottleInternal(250, 1000);
				}
			}
		}

		private static void TestThrottleInternal(int delayMs, int sleepTimeMs)
		{
			bool errored = false;
			long numCalls = 0;
			Action throttled = Throttle.Create(() =>
			{
				Interlocked.Increment(ref numCalls);
			}, delayMs, ex => { errored = true; });

			for (int i = 0; i < 20; i++)
				throttled();

			Thread.Sleep(sleepTimeMs);

			Assert.AreEqual(2, Interlocked.Read(ref numCalls));
			Assert.IsFalse(errored);
		}
	}
}
