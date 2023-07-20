using System;
using System.Diagnostics;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// A class which assists with retry operations that should wait progressively longer with each attempt.
	/// </summary>
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
		/// Remembers the starting sleep time for later reset calls.
		/// </summary>
		private double startSleepTimeMs;
		/// <summary>
		/// The amount of time in milliseconds to sleep next.
		/// </summary>
		private double sleepTime = 1;
		/// <summary>
		/// If positive and nonzero, sleep time increases by multiplying by this value.
		/// </summary>
		private double sleepTimeModifierMultiplier = 0;

		private WaitProgressivelyLonger()
		{
		}

		/// <summary>
		/// This legacy constructor is the same as the static method WaitProgressivelyLonger.Linear(…).
		/// </summary>
		/// <param name="maxSleepTimeMs">The waiting time will not increase beyond this value.</param>
		/// <param name="sleepTimeModifierMs">The waiting time will increase by this much each time Wait() is called.</param>
		/// <param name="startSleepTimeMs">The time to sleep the first time Wait() is called.</param>
		public WaitProgressivelyLonger(int maxSleepTimeMs = 250, double sleepTimeModifierMs = 1, double startSleepTimeMs = 1)
		{
			this.sleepTimeCutoff = maxSleepTimeMs - sleepTimeModifierMs;
			this.sleepTimeModifierMs = Math.Max(0.001, sleepTimeModifierMs);
			this.startSleepTimeMs = startSleepTimeMs;
			this.sleepTime = startSleepTimeMs;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="maxSleepTimeMs">The waiting time will not increase beyond this value.</param>
		/// <param name="sleepTimeModifierMs">The waiting time will increase by this much each time Wait() is called.</param>
		/// <param name="startSleepTimeMs">The time to sleep the first time Wait() is called.</param>
		/// <returns></returns>
		public static WaitProgressivelyLonger Linear(int maxSleepTimeMs = 250, double sleepTimeModifierMs = 1, double startSleepTimeMs = 1)
		{
			return new WaitProgressivelyLonger(maxSleepTimeMs, sleepTimeModifierMs, startSleepTimeMs);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="maxSleepTimeMs">The waiting time will not increase beyond this value.</param>
		/// <param name="sleepTimeModifierMultiplier">The waiting time is multiplied by this value each time.</param>
		/// <param name="startSleepTimeMs">The time to sleep the first time Wait() is called.</param>
		/// <returns></returns>
		public static WaitProgressivelyLonger Exponential(int maxSleepTimeMs = 60000, double sleepTimeModifierMultiplier = 2, double startSleepTimeMs = 1000)
		{
			return new WaitProgressivelyLonger(maxSleepTimeMs, 0, startSleepTimeMs) { sleepTimeModifierMultiplier = sleepTimeModifierMultiplier };
		}

		public void Wait()
		{
			Thread.Sleep(GetNextTimeout());
		}

		/// <summary>
		/// Returns the timeout in milliseconds and increments the timeout for next time.  This should only be called externally if NOT using the built-in Wait() method.
		/// </summary>
		/// <returns></returns>
		public int GetNextTimeout()
		{
			double current = sleepTime;
			if (sleepTime < sleepTimeCutoff)
			{
				if (sleepTimeModifierMultiplier > 0)
					sleepTime *= sleepTimeModifierMultiplier;
				else
					sleepTime += sleepTimeModifierMs;
				if (sleepTime > sleepTimeCutoff)
					sleepTime = sleepTimeCutoff;
			}
			return (int)current;
		}

		/// <summary>
		/// Changes internal state back to what it was when this object was constructed.
		/// </summary>
		public void Reset()
		{
			sleepTime = startSleepTimeMs;
		}
	}
}
