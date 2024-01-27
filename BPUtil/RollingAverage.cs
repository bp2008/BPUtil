using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>The RollingAverage class calculates rolling averages.</para>
	/// <para>NOT THREAD SAFE.  To use on multiple threads, you must provide your own locking.</para>
	/// </summary>
	public class RollingAverage
	{
		private int MAXSAMPLES;
		private int tickindex = 0;
		private double ticksum = 0;
		private int numTicks = 0;
		private double[] ticklist;

		/// <summary>
		/// Initializes a new instance of the RollingAverage class.
		/// </summary>
		/// <param name="MAXSAMPLES">The maximum number of samples to consider for the rolling average. Defaults to 10 if not provided.</param>
		public RollingAverage(int MAXSAMPLES = 10)
		{
			this.MAXSAMPLES = MAXSAMPLES;
			this.ticklist = new double[MAXSAMPLES];
		}

		/// <summary>
		/// Adds a new value to the rolling average calculation.
		/// </summary>
		/// <param name="newValue">The new value to add.</param>
		public void Add(double newValue)
		{
			ticksum -= ticklist[tickindex];  // subtract value falling off
			ticksum += newValue;             // add new value
			ticklist[tickindex] = newValue;  // save new value so it can be subtracted later
			if (++tickindex == MAXSAMPLES)   // inc buffer index
				tickindex = 0;
			if (numTicks < MAXSAMPLES)
				numTicks++;
		}

		/// <summary>
		/// Gets the current rolling average.
		/// </summary>
		/// <returns>The current rolling average. Returns 0 if no values have been added yet.</returns>
		public double Get()
		{
			if (numTicks == 0)
				return 0;
			return (ticksum / numTicks);
		}
	}
}
