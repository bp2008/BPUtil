using System;
using System.Diagnostics;
using System.Threading;

namespace BPUtil
{
	public class WaitProgressivelyLonger
	{
		/// <summary>
		/// sleepTime will not increase if it is greater than this value.
		/// </summary>
		private double sleepTimeCutoff;
		/// <summary>
		/// sleepTime increases by this much each time Wait is called.
		/// </summary>
		private double sleepTimeModifierMs;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="maxSleepTimeMs">The waiting time will not increase past this value</param>
		/// <param name="sleepTimeModifierMs">The waiting time will increase by this much each time Wait() is called.</param>
		public WaitProgressivelyLonger(int maxSleepTimeMs = 250, double sleepTimeModifierMs = 1)
		{
			this.sleepTimeCutoff = maxSleepTimeMs - sleepTimeModifierMs;
			this.sleepTimeModifierMs = Math.Max(0.001, sleepTimeModifierMs);
		}
		//private bool doReset = true;
		//private Stopwatch sw = new Stopwatch();
		private double sleepTime = 1;

		public void Wait()
		{
			//if (doReset)
			//{
			//	//sw.Restart();
			//	sleepTime = 0;
			//	doReset = false;
			//}
			//long elapsed = sw.ElapsedMilliseconds;

			//if (elapsed < 5)
			//	Thread.Yield();
			//else
			//{
			Thread.Sleep((int)sleepTime);
			if (sleepTime <= sleepTimeCutoff)
				sleepTime += sleepTimeModifierMs;
			//Thread.Sleep(Math.Min(250, timesSlept/50));
			//}
		}
		public void Reset()
		{
			sleepTime = 1;
			//doReset = true;
		}
	}
}
