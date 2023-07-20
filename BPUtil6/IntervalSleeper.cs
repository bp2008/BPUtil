using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Helper object for running an action on a predefined interval such as "every one minute".
	/// </summary>
	public class IntervalSleeper
	{
		Stopwatch sw = new Stopwatch();
		/// <summary>
		/// The maximum number of milliseconds to sleep between checking cancellation tokens.  Very small values may slightly increase CPU usage.
		/// </summary>
		public readonly long CancellationResponseTimeMilliseconds;
		/// <summary>
		/// Constructs a new IntervalSleeper and starts the internal stopwatch.
		/// </summary>
		/// <param name="CancellationResponseTimeMilliseconds">The maximum number of milliseconds to sleep between checking cancellation tokens.  Very small values may slightly increase CPU usage.</param>
		public IntervalSleeper(long CancellationResponseTimeMilliseconds = 100)
		{
			this.CancellationResponseTimeMilliseconds = CancellationResponseTimeMilliseconds;
			sw.Start();
		}
		/// <summary>
		/// Sleeps the current thread until the internal stopwatch reaches or exceeds the specified number of milliseconds. Restarts the internal stopwatch just before returning.
		/// </summary>
		/// <param name="wakeTimeMilliseconds">The millisecond count which the stopwatch should reach before this method returns.</param>
		public void SleepUntil(long wakeTimeMilliseconds)
		{
			long remaining = wakeTimeMilliseconds - sw.ElapsedMilliseconds;
			while (remaining > 0)
			{
				Thread.Sleep((int)Math.Min(int.MaxValue, remaining));
				remaining = wakeTimeMilliseconds - sw.ElapsedMilliseconds;
			}
			sw.Restart();
		}
		/// <summary>
		/// Sleeps the current thread until the internal stopwatch reaches or exceeds the specified number of milliseconds. Restarts the internal stopwatch just before returning.
		/// </summary>
		/// <param name="wakeTimeMilliseconds">The millisecond count which the stopwatch should reach before this method returns.</param>
		/// <param name="cancellationToken">A cancellationToken which can make the sleep end early.</param>
		public void SleepUntil(long wakeTimeMilliseconds, CancellationToken cancellationToken)
		{
			long remaining = wakeTimeMilliseconds - sw.ElapsedMilliseconds;
			while (remaining > 0 && !cancellationToken.IsCancellationRequested)
			{
				Thread.Sleep((int)Math.Min(CancellationResponseTimeMilliseconds, remaining));
				remaining = wakeTimeMilliseconds - sw.ElapsedMilliseconds;
			}
			sw.Restart();
		}
		/// <summary>
		/// Sleeps the current thread until the internal stopwatch reaches or exceeds the specified number of milliseconds. Restarts the internal stopwatch just before returning.
		/// </summary>
		/// <param name="wakeTimeMilliseconds">The millisecond count which the stopwatch should reach before this method returns.</param>
		/// <param name="returnTrueIfShouldCancel">A function which returns true if the sleep should be ended early. The thread will be woken every 100ms to call this function.</param>
		public void SleepUntil(long wakeTimeMilliseconds, Func<bool> returnTrueIfShouldCancel)
		{
			long remaining = wakeTimeMilliseconds - sw.ElapsedMilliseconds;
			while (remaining > 0 && !returnTrueIfShouldCancel())
			{
				Thread.Sleep((int)Math.Min(CancellationResponseTimeMilliseconds, remaining));
				remaining = wakeTimeMilliseconds - sw.ElapsedMilliseconds;
			}
			sw.Restart();
		}
	}
}
