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
		public CachedObject(Func<T> createNewObjectFunc, TimeSpan minAge, TimeSpan maxAge)
		{
			updateTimer.Start();
			this.createNewObjectFunc = createNewObjectFunc;
			this.minAgeMs = (long)Math.Round(minAge.TotalMilliseconds);
			this.maxAgeMs = (long)Math.Round(maxAge.TotalMilliseconds);
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

		/// <summary>
		/// Returns the most recent copy of the object.  The first get may be slow, as the object will need to be created.  You should not work directly with this property, as it may change to a new instance at any time.  Make a local reference to the instance.
		/// </summary>
		public T Instance
		{
			get
			{
				RefreshIfNecessary();
				return current.instance;
			}
		}

		private void RefreshIfNecessary()
		{
			CachedInstance ci = current;
			if (ci == null || updateTimer.ElapsedMilliseconds - ci.createdAt >= maxAgeMs)
			{
				lock (myLock)
				{
					ci = current;
					if (ci == null || updateTimer.ElapsedMilliseconds - ci.createdAt >= maxAgeMs)
						current = new CachedInstance(createNewObjectFunc(), updateTimer);
				}
			}
			else if (updateTimer.ElapsedMilliseconds - ci.createdAt >= minAgeMs)
			{
				SetTimeout.OnBackground(() =>
				{
					ci = current;
					if (updateTimer.ElapsedMilliseconds - ci.createdAt >= minAgeMs)
					{
						lock (myLock)
						{
							ci = current;
							if (updateTimer.ElapsedMilliseconds - ci.createdAt >= minAgeMs)
							{
								current = new CachedInstance(createNewObjectFunc(), updateTimer);
							}
						}
					}
				}, 0);
			}
		}
	}
}
