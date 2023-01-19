using System;
using System.Diagnostics;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// Measures elapsed time against a goal, providing an interface equivalent to a timer that is counting down.  Internally uses a <see cref="System.Diagnostics.Stopwatch"/> and provides a similar API.
	/// </summary>
	public class CountdownStopwatch
	{
		/// <summary>
		/// The amount of time to wait.  This can be adjusted after constructing the CountdownStopwatch, but for most predictable results, changing it is not recommended.
		/// </summary>
		public TimeSpan TimeToWait;
		private Stopwatch sw = new Stopwatch();
		/// <summary>
		/// Constructs a new CountdownStopwatch where Running is false.
		/// </summary>
		/// <param name="timeToWait">Time to wait before Finished is true.</param>
		public CountdownStopwatch(TimeSpan timeToWait)
		{
			this.TimeToWait = timeToWait;
		}
		/// <summary>
		/// Starts or resumes the countdown.
		/// </summary>
		public void Start()
		{
			sw.Start();
		}
		/// <summary>
		/// Stops the countdown, resets the elapsed time to zero, and starts the countdown.
		/// </summary>
		public void Restart()
		{
			sw.Restart();
		}
		/// <summary>
		/// Stops the countdown, leaving elapsed time frozen.
		/// </summary>
		public void Stop()
		{
			sw.Stop();
		}
		/// <summary>
		/// Stops the countdown and resets the elapsed time to zero.
		/// </summary>
		public void Reset()
		{
			sw.Reset();
		}
		/// <summary>
		/// Constructs a new CountdownStopwatch, starts it, and returns it.
		/// </summary>
		/// <param name="timeToWait">Time to wait before Finished is true.</param>
		/// <returns></returns>
		public static CountdownStopwatch StartNew(TimeSpan timeToWait)
		{
			CountdownStopwatch cs = new CountdownStopwatch(timeToWait);
			cs.Start();
			return cs;
		}
		/// <summary>
		/// Gets a value indicating if the countdown is running.  If the countdown is <see cref="Finished"/> or stopped via <see cref="Stop"/> or <see cref="Reset"/> or has not been started, then it is not running.
		/// </summary>
		public bool IsRunning
		{
			get
			{
				if (Finished)
					return false;
				else
					return sw.IsRunning;
			}
		}
		/// <summary>
		/// Gets a value indicating if the countdown has reached zero.
		/// </summary>
		public bool Finished
		{
			get
			{
				return sw.Elapsed >= TimeToWait;
			}
		}
		/// <summary>
		/// Gets the amount of time remaining before the countdown reaches zero.
		/// </summary>
		public TimeSpan Remaining
		{
			get
			{
				TimeSpan remaining = TimeToWait - sw.Elapsed;
				if (remaining >= TimeSpan.Zero)
					return remaining;
				else
					return TimeSpan.Zero;
			}
		}
		/// <summary>
		/// Gets the amount of time in milliseconds remaining before the countdown reaches zero, rounded up to the millisecond.
		/// </summary>
		public long RemainingMilliseconds
		{
			get
			{
				return (long)Math.Ceiling(Remaining.TotalMilliseconds);
			}
		}

		private static TimeSpan sleepingCheckInterval = new TimeSpan(0, 0, 0, 0, 100);
		/// <summary>
		/// Sleeps the current thread until <see cref="Remaining"/> is zero (<see cref="Finished"/> returns true), waking to check status every 100ms in case another thread modifies this CountdownStopwatch.
		/// </summary>
		public void SleepUntilZero()
		{
			TimeSpan remaining = Remaining;
			while (remaining > TimeSpan.Zero)
			{
				if (remaining < sleepingCheckInterval)
					Thread.Sleep(remaining);
				else
					Thread.Sleep(sleepingCheckInterval);
				remaining = Remaining;
			}
		}
	}
}
