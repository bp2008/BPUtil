using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BPUtil
{
	/// <summary>
	/// Provides temporary storage of key/value pairs, with configurable limits for cache size and item age.
	/// All public methods of this class use locking to achieve thread-safety, which could have negative performance implications if used very heavily.
	/// </summary>
	public class ObjectCache<TKey, TValue>
	{
		private object syncLock = new object();
		private Dictionary<TKey, CacheItem> cache = new Dictionary<TKey, CacheItem>();
		private Queue<TKey> ageQueue = new Queue<TKey>();
		private long cacheSizeBytes = 0;
		private long maxCacheSizeBytes;
		private TimeSpan maxCacheAge;
		private Func<TKey, TValue, long> getSize;
		/// <summary>
		/// Creates a new ObjectCache for caching string key/value pairs.  The maximum size and age of the cache may be specified.
		/// </summary>
		/// <param name="maxCacheSizeBytes">(Optional) Approximate maximum size in bytes of the cache. Default: 2000000 (2 MB).</param>
		/// <param name="maxCacheAgeMinutes">(Optional) Maximum age in minutes that an item can become before it expires. Default: 10 minutes.</param>
		/// <param name="getSize">(Optional) A method that returns the size in bytes of the provided key and value objects. If null, the generic ObjectSize.SizeOf method will be used instead. The ObjectCache internally counts some extra bytes consumed by age-related objects and references to the keys and values you provide.</param>
		public ObjectCache(long maxCacheSizeBytes = 2 * 1000000, double maxCacheAgeMinutes = 10, Func<TKey, TValue, long> getSize = null)
		{
			this.maxCacheSizeBytes = maxCacheSizeBytes;
			this.maxCacheAge = TimeSpan.FromMinutes(maxCacheAgeMinutes);
			this.getSize = getSize;
		}
		/// <summary>
		/// Adds the specified key/value pair if the key does not already exist in the cache. Otherwise, updates the existing item.  If an existing item is updated, its "Created" date is not updated.
		/// </summary>
		/// <param name="key">The object to use as dictionary key. `null` is not a valid key.</param>
		/// <param name="value"></param>
		public void Add(TKey key, TValue value)
		{
			lock (syncLock)
			{
				try
				{
					if (cache.TryGetValue(key, out CacheItem existing))
					{
						// Modify the existing cacheItem and update the cacheSizeBytes field accordingly.
						// This efficiently updates the item both in the cache and in the ageQueue.
						cacheSizeBytes -= existing.Size;
						existing.Value = value;
						existing.Size = (int)CalculateItemSize(key, existing);
						// We must not update the Created field because doing so will screw up the age queue ordering which is relied upon for expiration logic.
						// This updated item will simply expire sooner than it would have.
						cacheSizeBytes += existing.Size;
					}
					else
					{
						CacheItem newItem = new CacheItem(value);
						newItem.Size = (int)CalculateItemSize(key, newItem);
						cache.Add(key, newItem);
						cacheSizeBytes += newItem.Size;
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
		/// Returns the value that is cached for the given key, or default(TValue).
		/// </summary>
		/// <param name="key">The object to use as dictionary key. `null` is not a valid key.</param>
		/// <returns></returns>
		public TValue Get(TKey key)
		{
			return Get(key, out long cacheAgeMs);
		}
		/// <summary>
		/// Returns the value that is cached for the given key, or default(TValue").
		/// </summary>
		/// <param name="key">The object to use as dictionary key. `null` is not a valid key.</param>
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

		private bool CacheNeedsTrimmed()
		{
			if (cacheSizeBytes > maxCacheSizeBytes)
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
			CacheItem item = cache[key];
			if (cache.Remove(key)) // Decrease size only if the item was actually removed.
				cacheSizeBytes -= item.Size;
		}
		private void Rebuild()
		{
			KeyValuePair<TKey, CacheItem>[] allItems = cache.OrderBy(kvp => kvp.Value.Created).ToArray();
			ageQueue = new Queue<TKey>(allItems.Select(kvp => kvp.Key));
			cacheSizeBytes = 0;
			foreach (KeyValuePair<TKey, CacheItem> kvp in allItems)
				cacheSizeBytes += kvp.Value.Size;
		}
		/// <summary>
		/// Calculates the amount of memory, in bytes, of storing this item in the ObjectCache.  Accuracy is not 100% guaranteed.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		private long CalculateItemSize(TKey key, CacheItem item)
		{
			long size = 0;

			if (getSize != null)
				size += getSize(key, item.Value);
			else
			{
				size += ObjectSize.SizeOf(key);
				size += ObjectSize.SizeOf(item.Value);
			}

			if (!typeof(TKey).IsValueType)
				size += ObjectSize.ReferenceSize * 2; // Reference in `cache` + Reference in `ageQueue`.
			if (!typeof(TValue).IsValueType)
				size += ObjectSize.ReferenceSize; // Reference in the `CacheItem` instance.

			size += ObjectSize.ReferenceSize; // One reference to the `CacheItem` in `cache`
			size += 8; // The `Created` date in the `CacheItem`

			return size;
		}

		class CacheItem
		{
			private static Stopwatch stopwatch = Stopwatch.StartNew();
			public TValue Value;
			public TimeSpan Created;
			public int Size;
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
