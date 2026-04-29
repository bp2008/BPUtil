using System;
using System.Threading;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestConcurrentAccessLimiter
	{
		[TestMethod]
		public void Constructor_NonPositiveMaxConcurrent_Throws()
		{
			try
			{
				ConcurrentAccessLimiter limiter = new ConcurrentAccessLimiter(0);
				Assert.Fail("Expected ArgumentOutOfRangeException for 0.");
			}
			catch (ArgumentOutOfRangeException)
			{
			}

			try
			{
				ConcurrentAccessLimiter limiter = new ConcurrentAccessLimiter(-1);
				Assert.Fail("Expected ArgumentOutOfRangeException for -1.");
			}
			catch (ArgumentOutOfRangeException)
			{
			}
		}

		[TestMethod]
		public void GetAccess_NoAvailability_ThrowsTimeoutException()
		{
			ConcurrentAccessLimiter limiter = new ConcurrentAccessLimiter(1);
			using (limiter.GetDisposableAccess())
			{
				try
				{
					IDisposable secondAccess = limiter.GetDisposableAccess();
					Assert.Fail("Expected TimeoutException when access is unavailable.");
				}
				catch (TimeoutException)
				{
				}
			}
		}

		[TestMethod]
		public void GetAccess_WithTimeout_WaitsForReleaseThenSucceeds()
		{
			ConcurrentAccessLimiter limiter = new ConcurrentAccessLimiter(1);
			IDisposable firstAccess = limiter.GetDisposableAccess();

			Thread releaseThread = new Thread(() =>
			{
				Thread.Sleep(60);
				firstAccess.Dispose();
			});
			releaseThread.Start();

			using (limiter.GetDisposableAccess(TimeSpan.FromSeconds(1)))
			{
			}

			releaseThread.Join();
		}

		[TestMethod]
		public void GetAccess_AfterDispose_ThrowsObjectDisposedException()
		{
			ConcurrentAccessLimiter limiter = new ConcurrentAccessLimiter(1);
			limiter.Dispose();

			try
			{
				IDisposable access = limiter.GetDisposableAccess();
				Assert.Fail("Expected ObjectDisposedException after disposal.");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[TestMethod]
		public void RunWithAccess_NullAction_ThrowsArgumentNullException()
		{
			ConcurrentAccessLimiter limiter = new ConcurrentAccessLimiter(1);
			try
			{
				limiter.RunWithAccess(null);
				Assert.Fail("Expected ArgumentNullException for null action.");
			}
			catch (ArgumentNullException)
			{
			}
		}

		[TestMethod]
		public void RunWithAccess_ExecutesAction_AndReleasesAccess()
		{
			ConcurrentAccessLimiter limiter = new ConcurrentAccessLimiter(1);
			int count = 0;

			limiter.RunWithAccess(() =>
			{
				Interlocked.Increment(ref count);
			});

			using (limiter.GetDisposableAccess())
			{
			}

			Assert.AreEqual(1, count);
		}
	}
}
