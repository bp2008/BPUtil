using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil
{
	public static class SetTimeout
	{
		/// <summary>
		/// Invokes on the GUI thread the specified action after the specified timeout.
		/// </summary>
		/// <param name="TheAction">The action to run.</param>
		/// <param name="Timeout">Values less than 0 are treated as 0.
		/// If the Timeout value is 0 for an OnGui operation, the invoke process will start before this method returns, making it impossible to cancel the operation.</param>
		/// <param name="formForInvoking">A System.Windows.Forms form, required to invoke your method on the GUI thread.</param>
		/// <returns></returns>
		public static TimeoutHandle OnGui(Action TheAction, int Timeout, Form formForInvoking)
		{
			return _internal_setTimeout(TheAction, Timeout, true, formForInvoking);
		}
		/// <summary>
		/// Invokes on a background thread the specified action after the specified timeout.
		/// </summary>
		/// <param name="TheAction">The action to run.</param>
		/// <param name="Timeout">Values less than 0 are treated as 0.</param>
		/// <returns></returns>
		public static TimeoutHandle OnBackground(Action TheAction, int Timeout)
		{
			return _internal_setTimeout(TheAction, Timeout, false, null);
		}
		/// <summary>
		/// Invokes a call to SetTimeout.OnBackground on the Gui Thread, so that other UI events have a chance to finish first.
		/// This timeout will not be cancelable.
		/// </summary>
		/// <param name="TheAction">The action to run.</param>
		/// <param name="Timeout">Values less than 0 are treated as 0.</param>
		/// <param name="formForInvoking">A System.Windows.Forms form, required to invoke your method on the GUI thread.</param>
		/// <returns></returns>
		public static void AfterGuiResumesThenOnBackground(Action TheAction, int Timeout, Form formForInvoking)
		{
			OnGui((Action)(() =>
			{
				OnBackground(TheAction, Timeout - 1);
			}), 1, formForInvoking);
		}
		private static TimeoutHandle _internal_setTimeout(Action TheAction, int Timeout, bool invokeOnGuiThread, Form formForInvoking)
		{
			if (Timeout < 0)
				Timeout = 0;
			TimeoutHandle cancelHandle = new TimeoutHandle();
			if (invokeOnGuiThread && Timeout == 0)
				formForInvoking.Invoke(TheAction);
			else
			{
				Thread t = new Thread(
					() =>
					{
						try
						{
							if (cancelHandle.Wait(Timeout))
								return;
							if (invokeOnGuiThread)
								formForInvoking.Invoke(TheAction);
							else
								TheAction();
						}
						catch (ThreadAbortException) { throw; }
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
					}
				);
				t.Name = "Timeout";
				t.IsBackground = true;
				t.Start();
			}
			return cancelHandle;
		}
		/// <summary>
		/// Allows cancellation of a scheduled operation up until the point that it begins. There is no confirmation if the cancelation was successful.
		/// </summary>
		public class TimeoutHandle
		{
			private EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
			/// <summary>
			/// Cancels this timeout, if it is still waiting.
			/// </summary>
			public void Cancel()
			{
				ewh.Set();
			}
			/// <summary>
			/// Waits up to the specified number of milliseconds and returns early with a value of true if the Cancel method was called during this time. Returns false at the end of the waiting period if not canceled.
			/// </summary>
			/// <param name="ms">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
			/// <returns></returns>
			internal bool Wait(int ms)
			{
				return ewh.WaitOne(ms);
			}
		}
	}
}
