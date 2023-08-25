using System;
using System.Diagnostics;

namespace BPUtil
{
	/// <summary>
	/// <para>A class for tracking the rate of change of a number over time.</para>
	/// </summary>
	/// <example>
	/// RateOfChange roc = new RateOfChange(0);
	/// 
	/// Thread.Sleep(1000);
	/// Console.WriteLine(roc.GetRate(50)); // Prints "50.00/sec"
	/// 
	/// Thread.Sleep(1000);
	/// Console.WriteLine(roc.GetRate(50)); // Prints "0.00/sec"
	/// 
	/// Thread.Sleep(500);
	/// Console.WriteLine(roc.GetRate(150, 0)); // Prints "200/sec" because the number went up by 100 in the last 500 milliseconds.
	/// 
	/// Thread.Sleep(1000);
	/// Console.WriteLine(roc.GetRate(200, 1)); // Prints "50.0/sec"
	/// </example>
	public class RateOfChange
	{
		private double _lastValue;
		private Stopwatch _stopwatch;

		/// <summary>
		/// Initializes a new instance of the RateOfChange class with an initial value.
		/// </summary>
		/// <param name="initialValue">The initial value to track.</param>
		public RateOfChange(double initialValue)
		{
			_lastValue = initialValue;
			_stopwatch = Stopwatch.StartNew();
		}

		/// <summary>
		/// Gets the rate of change (per second) of the tracked value since the last call to this method.
		/// </summary>
		/// <param name="currentValue">The current value to track.</param>
		/// <param name="decimals">(Default: 2) The number of decimal places of precision to use in the output.</param>
		/// <param name="label">(Default: "/sec") The string to append to the number as a label.</returns>
		public string GetRate(double currentValue, int decimals = 2, string label = "/sec")
		{
			double elapsedTime = _stopwatch.Elapsed.TotalSeconds;
			double rateOfChange = (currentValue - _lastValue) / elapsedTime;

			_lastValue = currentValue;
			_stopwatch.Restart();

			if (decimals <= 0)
				return Math.Round(rateOfChange) + "/sec";
			else
				return rateOfChange.ToString("0." + new string('#', decimals)) + "/sec";
		}
	}
}