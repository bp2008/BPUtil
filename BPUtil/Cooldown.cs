using System;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// Provides an abstraction of a "cooldown".  A Cooldown instance can be used safely from multiple threads.
	/// </summary>
	public class Cooldown
	{
		private TimeSpan _timeToWait;
		/// <summary>
		/// The amount of time to wait.  This can be adjusted after constructing the Cooldown, but for most predictable results, changing it is not recommended.
		/// </summary>
		public TimeSpan TimeToWait
		{
			get
			{
				return _timeToWait;
			}
			set
			{
				_timeToWait = value;
				CountdownStopwatch cd = _cd;
				if (cd != null)
					cd.TimeToWait = value;
			}
		}
		private CountdownStopwatch _cd = null;
		/// <summary>
		/// Constructs a new Cooldown where <see cref="Available"/> is true and the first call to <see cref="Consume"/> will return true.
		/// </summary>
		/// <param name="timeToWait">Duration of the Cooldown.</param>
		public Cooldown(TimeSpan timeToWait)
		{
			this.TimeToWait = timeToWait;
		}
		/// <summary>
		/// Constructs a new Cooldown where <see cref="Available"/> is true and the first call to <see cref="Consume"/> will return true.
		/// </summary>
		/// <param name="timeToWaitMs">Millisecond duration of the Cooldown.</param>
		public Cooldown(int timeToWaitMs)
		{
			this.TimeToWait = TimeSpan.FromMilliseconds(timeToWaitMs);
		}
		/// <summary>
		/// Attempt to consume the limited resource.  Returns true if the resource was available and consumed.  Returns false if the resource was on cooldown.
		/// </summary>
		public bool Consume()
		{
			CountdownStopwatch cd = _cd;
			if (cd == null)
			{
				_cd = CountdownStopwatch.StartNew(TimeToWait);
				return true;
			}
			else if (cd.Finished)
			{
				cd.Restart();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Immediately resets the cooldown so that the next call to <see cref="Consume"/> will return true.
		/// </summary>
		/// <returns></returns>
		public void Reset()
		{
			_cd = null;
		}
		/// <summary>
		/// Gets or sets a value indicating if the countdown has reached zero.  If you set this value to true, it will instantly expire the cooldown.  If you set it to false, it will start a fresh cooldown period.
		/// </summary>
		public bool Available
		{
			get
			{
				CountdownStopwatch cd = _cd;
				return cd == null || cd.Finished;
			}
			set
			{
				if (value)
					_cd = null;
				else
				{
					if (_cd == null)
						_cd = new CountdownStopwatch(TimeToWait);
					_cd?.Restart();
				}
			}
		}
		/// <summary>
		/// Gets the amount of time remaining before the cooldown expires.
		/// </summary>
		public TimeSpan Remaining
		{
			get
			{
				CountdownStopwatch cd = _cd;
				if (cd == null)
					return TimeSpan.Zero;
				return cd.Remaining;
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
		/// Sleeps the current thread until the cooldown expires, waking to check status every 100ms in case another thread modifies this Cooldown.
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
