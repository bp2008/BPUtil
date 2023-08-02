using System;
using System.Threading;
using Timer = System.Threading.Timer;

namespace BPUtil
{
	/// <summary>
	/// Allows the creation of "debounced" actions.  A debounced action can be called many times, but the underlying action will be delayed until after the frequent calls stop.  See also <see cref="Throttle"/> for different behavior.
	/// </summary>
	public static class Debounce
	{
		/// <summary>
		/// Creates a thread-safe debounced action that delays invoking the underlying action until after [wait] milliseconds have elapsed since the last time the debounced function was invoked.
		/// </summary>
		/// <param name="action">The action to debounce.</param>
		/// <param name="wait">The number of milliseconds to delay.</param>
		/// <param name="errorHandler">If the action throws an exception, the exception will be passed to this handler.  If the handler is null, the exception will be swallowed.</param>
		/// <returns>A new action that is a debounced version of the original action.</returns>
		public static Action Create(Action action, int wait, Action<Exception> errorHandler)
		{
			object syncLock = new object();
			Timer timer = null;

			return () =>
			{
				lock (syncLock)
				{
					if (timer != null)
					{
						timer.Dispose();
						timer = null;
					}

					timer = new Timer(_ =>
					{
						action();
					}, null, wait, Timeout.Infinite);
				}
			};
		}
	}
}
