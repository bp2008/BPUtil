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
	/// <para>Manages automatic caching of a read-only object that is expensive to create.</para>
	/// <para>Everything this class does that depends on the passage of time goes through <see cref="GetTimestampMs"/> and <see cref="RunInBackground"/>, both of which a derived class may override to take control of the clock and of the asynchronous reload.</para>
	/// </summary>
	/// <typeparam name="T">Type this CachedObject will manage.</typeparam>
	public class CachedObject<T>
	{
		/// <summary>
		/// Creates a new CachedObject.
		/// </summary>
		/// <param name="createNewObjectFunc">A function which returns a new instance of the managed object.  This is called whenever a new instance is needed.</param>
		/// <param name="minAge">Minimum age.  After the cached instance is this old, retrieving the cached instance will trigger a new instance to be created asynchronously.  The existing object will still be returned without delay.</param>
		/// <param name="maxAge">Maximum age.  After the cached instance is this old, it can no longer be returned.  Requests for a cached instance will block until an object newer than this is available.</param>
		/// <param name="ReportException">An action that will be called if an exception is thrown while reloading the cached object in a background thread.  If null, uses Logger.Debug.</param>
		public CachedObject(Func<T> createNewObjectFunc, TimeSpan minAge, TimeSpan maxAge, Action<Exception> ReportException = null)
		{
			updateTimer.Start();
			this.createNewObjectFunc = createNewObjectFunc;
			this.minAgeMs = (long)Math.Round(minAge.TotalMilliseconds);
			this.maxAgeMs = (long)Math.Round(maxAge.TotalMilliseconds);
			this.ReportException = ReportException ?? (ex => Logger.Debug(ex));
		}

		/// <summary>
		/// Contains an instance of the managed object along with its creation date.
		/// </summary>
		private class CachedInstance
		{
			/// <summary>
			/// An instance of the object.
			/// </summary>
			public readonly T instance;
			/// <summary>
			/// Value of <see cref="GetTimestampMs"/> when this instance was created.
			/// </summary>
			public readonly long createdAt;
			public CachedInstance(T instance, long createdAt)
			{
				this.instance = instance;
				this.createdAt = createdAt;
			}
		}

		private CachedInstance current;
		private Stopwatch updateTimer = new Stopwatch();
		private object myLock = new object();
		private long minAgeMs;
		private long maxAgeMs;
		private Func<T> createNewObjectFunc;
		private Action<Exception> ReportException;
		/// <summary>
		/// The number of [Reload] calls currently active, useful for when we want to avoid multiple concurrent reloads.
		/// </summary>
		private int updateCounter = 0;

		/// <summary>
		/// Returns the most recent copy of the object.  The first get may be slow, as the object will need to be created.  You should not expect repeated calls to this method to always return the same instance.  Make a local reference to the instance.
		/// </summary>
		public T GetInstance()
		{
			RefreshIfNecessary();
			return current.instance;
		}

		/// <summary>
		/// Reloads the cached object now without regard for the current age. Returns a reference to the object created by this method.
		/// </summary>
		public T Reload()
		{
			Interlocked.Increment(ref updateCounter);
			try
			{
				CachedInstance ci = current = new CachedInstance(createNewObjectFunc(), GetTimestampMs());
				return ci.instance;
			}
			finally
			{
				Interlocked.Decrement(ref updateCounter);
			}
		}

		/// <summary>
		/// <para>Reloads the cached object now if there is no cached instance yet or the cached instance is at least [age] old, then returns the current instance.  Unlike <see cref="GetInstance"/> this ignores minAge and maxAge, and unlike <see cref="Reload"/> it is throttled: callers that arrive while a reload is in progress wait for it and share its result instead of each reloading.</para>
		/// <para>Intended for consumers that can tell the cached object is stale (e.g. a lookup in it just failed) and want it refreshed immediately, but need a bound on how often that can happen.</para>
		/// </summary>
		/// <param name="age">Minimum age of the cached instance for a reload to occur.  Pass TimeSpan.Zero to always reload.</param>
		/// <returns>The current instance, freshly created if a reload occurred.</returns>
		public T ReloadIfOlderThan(TimeSpan age)
		{
			long ageMs = (long)Math.Round(age.TotalMilliseconds);
			if (NeedsUpdate(ageMs))
			{
				lock (myLock)
				{
					if (NeedsUpdate(ageMs))
						return Reload();
				}
			}
			return current.instance;
		}

		/// <summary>
		/// <para>Returns the current time in milliseconds, measured from an arbitrary origin which must not change during the lifetime of this object.  Only the difference between two of these values is used, to measure the age of the cached instance.</para>
		/// <para>Override this to supply a clock the caller controls, e.g. so that a test can age the cached instance without waiting.  This is called from any thread that uses this CachedObject, so an override must be thread-safe.</para>
		/// </summary>
		protected virtual long GetTimestampMs()
		{
			return updateTimer.ElapsedMilliseconds;
		}

		/// <summary>
		/// <para>Runs [action] on a background thread.  This is used for the reload that is triggered when the cached instance reaches minAge, which must not delay the caller that triggered it.</para>
		/// <para>Override this to control when that reload happens, e.g. so that a test can run it at a chosen moment instead of racing a thread.  An override must not run [action] on the calling thread before returning, or callers will be delayed by a reload they are not supposed to wait for, and it is responsible for reporting any exception [action] throws.</para>
		/// </summary>
		/// <param name="action">The action to run.</param>
		protected virtual void RunInBackground(Action action)
		{
			SetTimeout.OnBackground(action, 0, ReportException);
		}

		private bool NeedsUpdate(long ageLimitMs)
		{
			CachedInstance ci = current;
			return ci == null || GetTimestampMs() - ci.createdAt >= ageLimitMs;
		}

		private void RefreshIfNecessary()
		{
			if (NeedsUpdate(maxAgeMs))
			{
				lock (myLock)
				{
					if (NeedsUpdate(maxAgeMs))
						Reload();
				}
			}
			else if (NeedsUpdate(minAgeMs))
			{
				if (updateCounter == 0)
				{
					RunInBackground(() =>
					{
						if (NeedsUpdate(minAgeMs))
						{
							if (updateCounter == 0)
							{
								lock (myLock)
								{
									if (NeedsUpdate(minAgeMs))
										Reload();
								}
							}
						}
					});
				}
			}
		}
	}
}
