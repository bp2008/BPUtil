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
	/// Manages automatic caching of a read-only object that is expensive to create.
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
			/// Stopwatch time when this instance was created.
			/// </summary>
			public readonly long createdAt;
			public CachedInstance(T instance, Stopwatch updateTimer)
			{
				this.instance = instance;
				this.createdAt = updateTimer.ElapsedMilliseconds;
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
				CachedInstance ci = current = new CachedInstance(createNewObjectFunc(), updateTimer);
				return ci.instance;
			}
			finally
			{
				Interlocked.Decrement(ref updateCounter);
			}
		}

		private bool NeedsUpdate(long ageLimitMs)
		{
			CachedInstance ci = current;
			return ci == null || updateTimer.ElapsedMilliseconds - ci.createdAt >= ageLimitMs;
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
					SetTimeout.OnBackground(() =>
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
					}, 0, ReportException);
				}
			}
		}
	}
}
