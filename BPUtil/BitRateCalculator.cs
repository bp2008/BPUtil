using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BPUtil
{
	/// <summary>
	/// <para>Class to calculate the bit rate of a stream by adding data points and querying.</para>
	/// <para>NOT THREAD SAFE.  To use on multiple threads, you must provide your own locking.</para>
	/// </summary>
	public class BitRateCalculator
	{
		private Queue<BitRateDataPoint> queue = new Queue<BitRateDataPoint>();
		private double sum = 0;
		private Stopwatch stopwatch = Stopwatch.StartNew();

		/// <summary>
		/// The time period in milliseconds over which to average the bit rate.
		/// </summary>
		public double AverageOverMs { get; set; }
		private double AverageOverSec => AverageOverMs / 1000.0;


		/// <summary>
		/// Constructor for BitRateCalculator.
		/// </summary>
		/// <param name="averageOverMs">The time period in milliseconds over which to average the bit rate.</param>
		public BitRateCalculator(double averageOverMs = 1000)
		{
			this.AverageOverMs = averageOverMs;
		}

		/// <summary>
		/// Add a data point to the bit rate calculation.
		/// </summary>
		/// <param name="bytes">The number of bytes in the data point.</param>
		public void AddDataPoint(double bytes)
		{
			Cleanup();
			sum += bytes;
			queue.Enqueue(new BitRateDataPoint(bytes, stopwatch.ElapsedMilliseconds));
		}

		/// <summary>
		/// Get the current bit rate in bytes per second.
		/// </summary>
		/// <returns>The current bit rate.</returns>
		public double GetBPS()
		{
			Cleanup();
			return sum / AverageOverSec;
		}

		/// <summary>
		/// Deletes the data points that are older than the average time period.
		/// </summary>
		private void Cleanup()
		{
			long now = stopwatch.ElapsedMilliseconds;
			while (queue.Count > 0 && now - queue.Peek().Time > this.AverageOverMs)
				sum -= queue.Dequeue().Bytes;
		}
	}

	/// <summary>
	/// Class to represent a data point in the bit rate calculation.
	/// </summary>
	class BitRateDataPoint
	{
		/// <summary>
		/// The number of bytes in the data point.
		/// </summary>
		public double Bytes { get; set; }

		/// <summary>
		/// The time at which the data point was created.
		/// </summary>
		public long Time { get; set; }

		/// <summary>
		/// Constructor for BitRateDataPoint.
		/// </summary>
		/// <param name="bytes">The number of bytes in the data point.</param>
		/// <param name="time">The time at which the data point was created.</param>
		public BitRateDataPoint(double bytes, long time)
		{
			this.Bytes = bytes;
			this.Time = time;
		}
	}
}