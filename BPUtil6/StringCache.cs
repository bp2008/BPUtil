namespace BPUtil
{
	/// <summary>
	/// Provides temporary storage of string key/value pairs, with configurable limits for cache size and item age.
	/// All public methods of this class use locking to achieve thread-safety, which could have negative performance implications if used very heavily.
	/// </summary>
	public class StringCache
	{
		private ObjectCache<string, string> internalCache;
		/// <summary>
		/// Creates a new StringCache for caching string key/value pairs.  The maximum size and age of the cache may be specified.
		/// </summary>
		/// <param name="maxCacheSizeBytes">(Optional) Approximate maximum size in bytes of the cache. Default: 2000000 (2 MB).</param>
		/// <param name="maxCacheAgeMinutes">(Optional) Maximum age in minutes that an item can become before it expires. Default: 10 minutes.</param>
		public StringCache(long maxCacheSizeBytes = 2 * 1000000, double maxCacheAgeMinutes = 10)
		{
			internalCache = new ObjectCache<string, string>(maxCacheSizeBytes, maxCacheAgeMinutes);
		}
		/// <summary>
		/// Adds the specified key/value pair if the key does not already exist in the cache. Otherwise, updates the existing item.  If an existing item is updated, its "Created" date is not updated.
		/// </summary>
		/// <param name="key">Key string.  Null and empty string are treated as identical.</param>
		/// <param name="value"></param>
		public void Add(string key, string value)
		{
			if (key == null)
				key = "";
			internalCache.Add(key, value);
		}
		/// <summary>
		/// Returns the value that is cached for the given key, or null.
		/// </summary>
		/// <param name="key">Key string.  Null and empty string are treated as identical.</param>
		/// <returns></returns>
		public string Get(string key)
		{
			if (key == null)
				key = "";
			return internalCache.Get(key);
		}
		/// <summary>
		/// Returns the value that is cached for the given key, or null.
		/// </summary>
		/// <param name="key">Key string.  Null and empty string are treated as identical.</param>
		/// <param name="cacheAgeMs">Milliseconds age of the cached value.  Will be 0 if no item was cached.</param>
		/// <returns></returns>
		public string Get(string key, out long cacheAgeMs)
		{
			if (key == null)
				key = "";
			return internalCache.Get(key, out cacheAgeMs);
		}
	}
}
