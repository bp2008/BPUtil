using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestStringCache
	{
		const string key0 = "This is SAMPLE key text for a unit test.";
		const string value0 = "This is SAMPLE value text for a unit test.";
		const string key1 = "This is sample key text for a unit test.";
		const string value1 = "This is sample value text for a unit test.";
		const string key2 = "This is a second sample key text for a unit test.";
		const string value2 = "This is a second sample value text for a unit test.";
		const string key3 = "This is a third sample key text for a unit test.";
		const string value3 = "This is a third sample value text for a unit test.";

		[TestMethod]
		public void StringCacheRemembersItemsUniquely()
		{
			StringCache cache = new StringCache(2000000, 10);
			cache.Add(key0, value0);
			cache.Add(key1, value1);
			cache.Add(key2, value2);
			cache.Add(key3, value3);
			Assert.AreEqual(value0, cache.Get(key0));
			Assert.AreEqual(value1, cache.Get(key1));
			Assert.AreEqual(value2, cache.Get(key2));
			Assert.AreEqual(value3, cache.Get(key3));

			// Test unmapped inputs
			Assert.IsNull(cache.Get(null));
			Assert.IsNull(cache.Get(""));
			Assert.IsNull(cache.Get("I Do Not Exist as a Key"));

			// Test that null can be used as a key or as a value
			cache.Add(null, "I'm with null");
			cache.Add("I have a null value", null);
			Assert.AreEqual("I'm with null", cache.Get(null));
			Assert.IsNull(cache.Get("I have a null value"));

			// Test that null can be used as a key and as a value
			cache.Add(null, null);
			Assert.IsNull(cache.Get(null));

			// Test that adding a key after failing to retrieve it causes correct behavior
			cache.Add("", "I am a new value");
			Assert.AreEqual("I am a new value", cache.Get(""));
		}
		[TestMethod]
		public void StringCacheEnforcesSizeLimit()
		{
			// Test correct normal behavior with large limit
			StringCache cache = new StringCache(2000000, 10);
			cache.Add(key2, value2);
			Assert.AreEqual(value2, cache.Get(key2));

			// Edge case: cache size limit is exactly the calculated size of the cached item (requires knowledge of the implementation details of the cache).
			// In this case, we are using a reference type for key and value, which means 3 references will be counted.
			// We also have one reference for the CacheItem used to wrap around the Value, and 8 bytes for the DateTime used to calculate its age.
			int overhead = (ObjectSize.ReferenceSize * 4) + 8;
			int requiredSize = overhead + (int)ObjectSize.SizeOf(key2) + (int)ObjectSize.SizeOf(value2);
			cache = new StringCache(requiredSize, 10);
			cache.Add(key2, value2);
			Assert.AreEqual(value2, cache.Get(key2));

			// Edge case: cache size limit one byte smaller than required
			cache = new StringCache(requiredSize - 1, 10);
			cache.Add(key2, value2);
			Assert.IsNull(cache.Get(key2)); // Value should fail to load from the cache, because it should have been removed for being over the limit.

			cache.Add(key0, value0); // Adding a shorter value should work
			Assert.AreEqual(value0, cache.Get(key0));

			cache.Add(key0, value0); // Adding the same key again with the same value should change nothing
			Assert.AreEqual(value0, cache.Get(key0));

			cache.Add(key0, value1); // Adding the same key again with a DIFFERENT but equal length value should work
			Assert.AreEqual(value1, cache.Get(key0));

			cache.Add(key2, value2); // Adding the oversized value again should cause the cache to be empty.
			Assert.IsNull(cache.Get(key0)); // Value should fail to load from the cache, because it should have been removed for being over the limit.
			Assert.IsNull(cache.Get(key2)); // Value should fail to load from the cache, because it should have been removed for being over the limit.
		}
		[TestMethod]
		public void StringCacheEnforcesAgeLimit()
		{
			// Test correct normal behavior with large limit
			StringCache cache = new StringCache(2000000, 10);
			cache.Add(key0, value0);
			Assert.AreEqual(value0, cache.Get(key0));

			// Test that an expiration time of 0 minutes immediately expires everything.
			cache = new StringCache(2000000, 0);
			cache.Add(key0, value0);
			Assert.IsNull(cache.Get(key0));
			cache.Add(key1, value1);
			Assert.IsNull(cache.Get(key1));
			cache.Add(key2, value2);
			cache.Add(key3, value3);
			Assert.IsNull(cache.Get(key2));
			Assert.IsNull(cache.Get(key3));
		}
	}
}
