using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BPUtil
{
	/// <summary>
	/// <para>Provides temporary storage of key/value pairs, with configurable limits for cache size (in items) and item age.</para>
	/// <para>All public methods of this class use locking to achieve thread-safety, which could have negative performance implications if used very heavily.</para>
	/// <para>For a version which measures the size of cached objects to provide a cache size limit in bytes, see <see cref="ObjectCache{TKey, TValue}"/>.</para>
	/// </summary>
	public class ObjectCacheSimple<TKey, TValue>
	{
		private object syncLock = new object();
		private Dictionary<TKey, CacheItem> cache = new Dictionary<TKey, CacheItem>();
		private Queue<TKey> ageQueue = new Queue<TKey>();
		private long maxCacheSizeItems;
		private TimeSpan maxCacheAge;
		/// <summary>
		/// Creates a new ObjectCacheSimple for caching string key/value pairs.  The maximum size and age of the cache may be specified.
		/// </summary>
		/// <param name="maxCacheSizeItems">(Optional) Maximum size of the cache, in items. Default: 10000.</param>
		/// <param name="maxCacheAgeMinutes">(Optional) Maximum age in minutes that an item can become before it expires. Default: 10 minutes.</param>
		public ObjectCacheSimple(long maxCacheSizeItems = 10000, double maxCacheAgeMinutes = 10)
		{
			this.maxCacheSizeItems = maxCacheSizeItems;
			this.maxCacheAge = TimeSpan.FromMinutes(maxCacheAgeMinutes);
		}
		/// <summary>
		/// Adds the specified key/value pair if the key does not already exist in the cache. Otherwise, updates the existing item.  If an existing item is updated, its "Created" date is not updated.
		/// </summary>
		/// <param name="key">The object to use as dictionary key. <c>null</c> is not a valid key.</param>
		/// <param name="value">The value to add to the cache.</param>
		public void Add(TKey key, TValue value)
		{
			lock (syncLock)
			{
				try
				{
					if (cache.TryGetValue(key, out CacheItem existing))
					{
						existing.Value = value;
					}
					else
					{
						cache.Add(key, new CacheItem(value));
						ageQueue.Enqueue(key);
					}
					while (CacheNeedsTrimmed())
						TrimOldestItem();
				}
				catch (Exception)
				{
					Rebuild();
					throw;
				}
			}
		}
		/// <summary>
		/// Returns the value that is cached for the given key, or default(<typeparamref name="TValue"/>).
		/// </summary>
		/// <param name="key">The object to use as dictionary key. <c>null</c> is not a valid key.</param>
		/// <returns></returns>
		public TValue Get(TKey key)
		{
			return Get(key, out long cacheAgeMs);
		}
		/// <summary>
		/// Returns the value that is cached for the given key, or default(<typeparamref name="TValue"/>).
		/// </summary>
		/// <param name="key">The object to use as dictionary key. <c>null</c> is not a valid key.</param>
		/// <param name="cacheAgeMs">Milliseconds age of the cached value.  Will be 0 if no item was cached.</param>
		/// <returns></returns>
		public TValue Get(TKey key, out long cacheAgeMs)
		{
			cacheAgeMs = 0;
			lock (syncLock)
			{
				try
				{
					while (CacheNeedsTrimmed())
						TrimOldestItem();
					if (cache.TryGetValue(key, out CacheItem item))
					{
						cacheAgeMs = (long)item.Age.TotalMilliseconds;
						return item.Value;
					}
					else
						return default(TValue);
				}
				catch (Exception)
				{
					Rebuild();
					throw;
				}
			}
		}
		/// <summary>
		/// Returns true if the value that is cached for the given key was found.  The out parameter <c>value</c> will contain the cached value, or <c>default(<typeparamref name="TValue"/>)</c> if not found.
		/// </summary>
		/// <param name="key">The object to use as dictionary key. <c>null</c> is not a valid key.</param>
		/// <param name="value">The value that is cached for the given key, or default(<typeparamref name="TValue"/>)</param>
		/// <returns></returns>
		public bool TryGet(TKey key, out TValue value)
		{
			return TryGet(key, out value, out _);
		}
		/// <summary>
		/// Returns true if the value that is cached for the given key was found.  The out parameter <c>value</c> will contain the cached value, or <c>default(<typeparamref name="TValue"/>)</c> if not found.
		/// </summary>
		/// <param name="key">The object to use as dictionary key. <c>null</c> is not a valid key.</param>
		/// <param name="value">The value that is cached for the given key, or default(<typeparamref name="TValue"/>)</param>
		/// <param name="cacheAgeMs">Milliseconds age of the cached value.  Will be 0 if no item was cached.</param>
		/// <returns></returns>
		public bool TryGet(TKey key, out TValue value, out long cacheAgeMs)
		{
			cacheAgeMs = 0;
			lock (syncLock)
			{
				try
				{
					while (CacheNeedsTrimmed())
						TrimOldestItem();
					if (cache.TryGetValue(key, out CacheItem item))
					{
						cacheAgeMs = (long)item.Age.TotalMilliseconds;
						value = item.Value;
						return true;
					}
					else
					{
						value = default(TValue);
						return false;
					}
				}
				catch (Exception)
				{
					Rebuild();
					throw;
				}
			}
		}

		private bool CacheNeedsTrimmed()
		{
			if (cache.Count > maxCacheSizeItems)
				return true;
			return ageQueue.Count > 0 && cache[ageQueue.Peek()].Expired(maxCacheAge);
		}

		private void TrimOldestItem()
		{
			if (ageQueue.Count > 0)
				RemoveItem(ageQueue.Dequeue());
		}

		private void RemoveItem(TKey key)
		{
			cache.Remove(key);
		}
		private void Rebuild()
		{
			KeyValuePair<TKey, CacheItem>[] allItems = cache.OrderBy(kvp => kvp.Value.Created).ToArray();
			ageQueue = new Queue<TKey>(allItems.Select(kvp => kvp.Key));
		}

		class CacheItem
		{
			private static Stopwatch stopwatch = Stopwatch.StartNew();
			public TValue Value;
			public TimeSpan Created;
			public CacheItem(TValue Value)
			{
				this.Value = Value;
				this.Created = stopwatch.Elapsed;
			}
			public bool Expired(TimeSpan maxAge)
			{
				return Age >= maxAge;
			}
			public TimeSpan Age
			{
				get
				{
					return stopwatch.Elapsed - Created;
				}
			}
		}
	}
}
