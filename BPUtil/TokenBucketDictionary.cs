using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>A thread-safe implementation of a dictionary of token buckets (see <see cref="TokenBucket"/>) that periodically deletes full buckets to reduce memory usage.  The dictionary key can be specified by the user of this class.</para>
	/// <para>This is useful when you need multiple rate limiters keyed on a variable number of keys (e.g. rate limiter per client IP address).</para>
	/// </summary>
	public class TokenBucketDictionary<TKey>
	{
		/// <summary>
		/// The maximum number of tokens that buckets can hold.  Buckets start full.
		/// </summary>
		public double Capacity { get; private set; }
		/// <summary>
		/// The rate at which tokens are added to buckets (tokens per second).
		/// </summary>
		public double RefillRate { get; private set; }
		/// <summary>
		/// The minimum number of seconds between maintenances.  Maintenance involves iterating over all buckets in the dictionary and deleting full buckets (they'll be recreated later when needed).  More frequent maintenance saves memory, but can be expensive if many non-full buckets are stored.  Maintenance only occurs during TokenBucketDictionary method calls.  If 0, maintenance occurs with every TokenBucketDictionary method call.
		/// </summary>
		public double MaintenanceIntervalMilliseconds { get; private set; }
		/// <summary>
		/// Gets the number of buckets currently stored in this instance.  This property getter does not perform maintenance on the dictionary so it will not cause full buckets to be evicted.
		/// </summary>
		public int NumberOfBuckets
		{
			get
			{
				lock (_lock)
				{
					return _buckets.Count;
				}
			}
		}
		/// <summary>
		/// Ditionary of <see cref="TokenBucket"/>s.
		/// </summary>
		protected Dictionary<TKey, TokenBucket> _buckets = new Dictionary<TKey, TokenBucket>();
		/// <summary>
		/// Hold this lock when performing maintenance or accessing the <see cref="_buckets"/> field.
		/// </summary>
		protected readonly object _lock = new object();
		/// <summary>
		/// A Stopwatch for handling the maintenance interval.
		/// </summary>
		private Stopwatch swMaintainanceTimer = Stopwatch.StartNew();

		/// <summary>
		/// Initializes a new instance of the TokenBucketDictionary class with the specified capacity and refill rate.
		/// </summary>
		/// <param name="capacity">The maximum number of tokens that buckets can hold.  Buckets start full.</param>
		/// <param name="refillRate">The rate at which tokens are added to buckets (tokens per second).</param>
		/// <param name="maintenanceIntervalMilliseconds">The minimum number of seconds between maintenances.  Maintenance involves iterating over all buckets in the dictionary and deleting full buckets (they'll be recreated later when needed).  More frequent maintenance saves memory, but can be expensive if many non-full buckets are stored.  Maintenance only occurs during TokenBucketDictionary method calls.  If 0, maintenance occurs with every TokenBucketDictionary method call.</param>
		public TokenBucketDictionary(double capacity, double refillRate, double maintenanceIntervalMilliseconds = 60000)
		{
			if (capacity <= 0)
				throw new ArgumentOutOfRangeException("capacity", capacity, "TokenBucket capacity must be a positive number");
			if (refillRate <= 0)
				throw new ArgumentOutOfRangeException("refillRate", refillRate, "TokenBucket refill rate must be a positive number");

			Capacity = capacity;
			RefillRate = refillRate;
			MaintenanceIntervalMilliseconds = maintenanceIntervalMilliseconds;
		}
		/// <summary>
		/// Attempts to consume the specified number of tokens from a bucket.
		/// </summary>
		/// <param name="key">The key of the token bucket.</param>
		/// <param name="tokensToConsume">The number of tokens to consume.</param>
		/// <returns>True if the specified number of tokens were successfully consumed; otherwise, false.</returns>
		public bool TryConsume(TKey key, double tokensToConsume)
		{
			if (tokensToConsume <= 0)
				throw new ArgumentOutOfRangeException("tokensToConsume", tokensToConsume, "must be a positive number");

			lock (_lock)
			{
				if (!_buckets.TryGetValue(key, out TokenBucket bucket))
					bucket = _buckets[key] = CreateNewBucket(key);
				bool retVal = bucket.TryConsume(tokensToConsume);
				Maintain();
				return retVal;
			}
		}
		/// <summary>
		/// Returns the current number of full and partial tokens in a bucket without consuming any.
		/// </summary>
		/// <param name="key">The key of the token bucket.</param>
		/// <returns>The number of full and partial tokens in the bucket.</returns>
		public double Peek(TKey key)
		{
			lock (_lock)
			{
				Maintain();
				if (_buckets.TryGetValue(key, out TokenBucket existingBucket))
					return existingBucket.Peek();
				else
					return Capacity;
			}
		}
		/// <summary>
		/// Runs maintenance if the maintenance interval has elapsed since the previous maintenance.  Hold "_lock" when calling this method.
		/// </summary>
		protected void Maintain()
		{
			if (swMaintainanceTimer.ElapsedMilliseconds >= MaintenanceIntervalMilliseconds)
			{
				swMaintainanceTimer.Restart();
				RemoveFullBuckets();
			}
		}
		/// <summary>
		/// Iterates over all buckets in the dictionary and deletes full buckets (they'll be recreated later when needed).  Hold "_lock" when calling this method.
		/// </summary>
		protected void RemoveFullBuckets()
		{
			List<TKey> keysToRemove = new List<TKey>();
			foreach (KeyValuePair<TKey, TokenBucket> kvp in _buckets)
			{
				if (kvp.Value.Peek() >= Capacity)
					keysToRemove.Add(kvp.Key);
			}
			foreach (TKey key in keysToRemove)
			{
				_buckets.Remove(key);
			}
		}
		/// <summary>
		/// Creates a new <see cref="TokenBucket"/>.  Can be overridden by a dervied class to provide a custom <see cref="TokenBucket"/> implementation.
		/// </summary>
		/// <param name="key">The key of the token bucket (ignored).</param>
		/// <returns>A new <see cref="TokenBucket"/>.</returns>
		protected virtual TokenBucket CreateNewBucket(TKey key)
		{
			return new TokenBucket(Capacity, RefillRate);
		}
	}
}
