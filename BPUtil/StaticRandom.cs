using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// This class keeps an internal thread-local Random instance, making it thread-safe. Each thread's Random instance uses a different seed.
	/// </summary>
	public static class StaticRandom
	{
		private static int seed = Environment.TickCount;
		private static readonly ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

		/// <summary>
		/// Returns a nonnegative random number.
		/// </summary>
		/// <returns></returns>
		public static int Next()
		{
			return random.Value.Next();
		}
		/// <summary>
		/// Returns a random number in the range [0, maxValue)
		/// </summary>
		/// <param name="maxValue">Exclusive maximum value.</param>
		/// <returns></returns>
		public static int Next(int maxValue)
		{
			return random.Value.Next(maxValue);
		}
		/// <summary>
		/// Returns a random number in the range [minvalue, maxValue)
		/// </summary>
		/// <param name="minValue">Inclusive minimum value.</param>
		/// <param name="maxValue">Exclusive maximum value.</param>
		/// <returns></returns>
		public static int Next(int minValue, int maxValue)
		{
			return random.Value.Next(minValue, maxValue);
		}
	}
}
