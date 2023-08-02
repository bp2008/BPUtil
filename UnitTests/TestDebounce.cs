using System;
using System.Threading;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestDebounce
	{
		[TestMethod]
		public void DebounceWorksProperly()
		{
			// If tests fail with short time periods, they may yet succeed when more time is granted for threads to be scheduled.
			try
			{
				TestDebounceInternal(5, 10);
			}
			catch
			{
				try
				{
					TestDebounceInternal(50, 100);
				}
				catch
				{
					TestDebounceInternal(250, 1000);
				}
			}
		}

		private static void TestDebounceInternal(int delayMs, int sleepTimeMs)
		{
			bool errored = false;
			long numCalls = 0;
			Action debounced = Debounce.Create(() =>
			{
				Interlocked.Increment(ref numCalls);
			}, delayMs, ex => { errored = true; });

			for (int i = 0; i < 20; i++)
				debounced();

			Thread.Sleep(delayMs * 2);

			Thread.Sleep(sleepTimeMs);

			Assert.AreEqual(1, Interlocked.Read(ref numCalls));
			Assert.IsFalse(errored);
		}
	}
}
