using System;
using System.Diagnostics;
using System.Linq;

namespace BPUtil
{
	/// <summary>
	/// Extension methods for events.
	/// </summary>
	public static class EventExtensions
	{
		/// <summary>
		/// Invokes each subscriber and catches exceptions thrown by handlers so one failure doesn't stop the chain.
		/// </summary>
		/// <param name="handler">The event handler to invoke.</param>
		/// <param name="sender">The source of the event (usually <c>this</c> in the caller's context)</param>
		/// <param name="args">The event arguments to pass to the subscribers.</param>
		/// <param name="onError">(Optional) exception handling method.  If null, exceptions thrown by event handlers will be silently ignored.</param>
		public static void SafeInvoke<T>(this EventHandler<T> handler, object sender, T args, Action<Exception> onError = null)
		{
			if (handler == null)
				return;

			foreach (Delegate subscriber in handler.GetInvocationList())
			{
				try
				{
					// DynamicInvoke is used to work with the Delegate type returned by GetInvocationList
					subscriber.DynamicInvoke(sender, args);
				}
				catch (System.Reflection.TargetInvocationException ex)
				{
					if (onError != null)
					{
						// Exceptions thrown by DynamicInvoke are wrapped; we want the actual error
						Exception actualException = ex.InnerException ?? ex;
						onError(actualException);
					}
				}
			}
		}
	}
}