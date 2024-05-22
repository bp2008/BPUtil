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
	/// A thread-safe implementation of the Token Bucket algorithm using a lock to ensure thread safety.  Token generation has precision of 100 nanoseconds due to using <see cref="Stopwatch"/> ticks for internal timing, but due to less than perfect floating point precision, rounding error can mean that tokens are not available at exactly the expected tick.
	/// </summary>
	public class TokenBucket
	{
		private double _capacity;
		private double _tokensPerTick;
		private double _tokens;
		private double _refillRate;
		private long _lastRefillTime;
		private readonly object _lock = new object();

		/// <summary>
		/// Initializes a new instance of the TokenBucket class with the specified capacity and refill rate.
		/// </summary>
		/// <param name="capacity">The maximum number of tokens that the bucket can hold.  The bucket starts full.</param>
		/// <param name="refillRate">The rate at which tokens are added to the bucket (tokens per second).</param>
		public TokenBucket(double capacity, double refillRate)
		{
			if (capacity <= 0)
				throw new ArgumentOutOfRangeException("capacity", capacity, "TokenBucket capacity must be a positive number");
			if (refillRate <= 0)
				throw new ArgumentOutOfRangeException("refillRate", refillRate, "TokenBucket refill rate must be a positive number");

			_capacity = capacity;
			_refillRate = refillRate;
			_tokensPerTick = refillRate / Stopwatch.Frequency;
			_tokens = capacity;
			_lastRefillTime = GetTime();
		}

		/// <summary>
		/// Attempts to consume the specified number of tokens from the bucket.
		/// </summary>
		/// <param name="tokensToConsume">The number of tokens to consume.</param>
		/// <returns>True if the specified number of tokens were successfully consumed; otherwise, false.</returns>
		public bool TryConsume(double tokensToConsume)
		{
			if (tokensToConsume <= 0)
				throw new ArgumentOutOfRangeException("tokensToConsume", tokensToConsume, "must be a positive number");
			lock (_lock)
			{
				Refill();
				if (_tokens < tokensToConsume)
					return false;

				_tokens -= tokensToConsume;
				return true;
			}
		}
		/// <summary>
		/// Returns the current number of full and partial tokens in the bucket without consuming any.
		/// </summary>
		/// <returns></returns>
		public double Peek()
		{
			lock (_lock)
			{
				Refill();
				return _tokens;
			}
		}
		/// <summary>
		/// Refills the token bucket with as many full and partial tokens as were available since the last refill.  Any tokens beyond the bucket's capacity are lost.
		/// </summary>
		private void Refill()
		{
			long now = GetTime();
			long elapsedTicks = now - _lastRefillTime;
			double newTokens = _tokensPerTick * elapsedTicks;
			if (newTokens > 0)
			{
				_tokens = Math.Min(_capacity, _tokens + newTokens);
				_lastRefillTime = now;
			}
		}
		/// <summary>
		/// Gets or sets the capacity of this TokenBucket, in tokens.  If reducing the capacity, excess tokens will be lost.  Must be a positive number.
		/// </summary>
		public double Capacity
		{
			get
			{
				return _capacity;
			}
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException("value", value, "TokenBucket capacity must be a positive number");
				lock (_lock)
				{
					Refill();
					_capacity = value;
					_tokens = Math.Min(_capacity, _tokens);
				}
			}
		}
		/// <summary>
		/// Gets or sets the refill rate of this TokenBucket, in tokens per second.  Must be a positive number.
		/// </summary>
		public double RefillRate
		{
			get
			{
				return _refillRate;
			}
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException("value", value, "TokenBucket refill rate must be a positive number");
				lock (_lock)
				{
					Refill();
					_refillRate = value;
					_tokensPerTick = _refillRate / Stopwatch.Frequency;
				}
			}
		}
		/// <summary>
		/// Changes the quantity of tokens currently in this bucket.
		/// </summary>
		/// <param name="tokens">The amount of tokens you want in the bucket. Can be 0 or <see cref="Capacity"/> or any value between.  Out-of-range values are clamped to be between 0 and Capacity.</param>
		public void SetAvailableTokens(double tokens)
		{
			lock (_lock)
			{
				Refill();
				_tokens = tokens.Clamp(0, _capacity);
			}
		}
		#region Timekeeping
		private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
		/// <summary>
		/// Gets the current time in ticks since bucket creation. Method can be overridden (for unit testing). There are <see cref="Stopwatch.Frequency"/> ticks (10 million) in one second.
		/// </summary>
		/// <returns></returns>
		protected virtual long GetTime()
		{
			return _stopwatch.ElapsedTicks;
		}
		#endregion
	}
}
