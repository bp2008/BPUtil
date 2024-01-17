using BPUtil.MVC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.Forms
{
	/// <summary>
	/// <para>
	/// Second-generation of an application context you can use when you want to develop a Windows Forms application that "runs in the system tray" and normally has no visible form open.
	/// </para>
	/// <para>
	/// Example: TrayIconApplication2 app = new TrayIconApplication2(); app.Start();
	/// </para>
	/// </summary>
	public class TrayIconApplication2 : IDisposable
	{
		private TrayIconAppHiddenForm myForm;
		int myThreadId;

		/// <summary>
		/// Call <see cref="Start"/> to begin running the app.  The Start method will not return until the application exits.
		/// </summary>
		/// <param name="trayIcon">The icon to show in the system tray.  You could load this from a Form by copying code from that form's Designer file.</param>
		/// <param name="tooltipText">(optional) Text to show upon mouseover of the tray icon.</param>
		/// <param name="onDoubleClick">A callback which is called when the tray icon is double-clicked. Enabling this disables single-left-click opening of the context menu.</param>
		public TrayIconApplication2(Icon trayIcon, string tooltipText, Action onDoubleClick)
		{
			myForm = new TrayIconAppHiddenForm(trayIcon, tooltipText, onDoubleClick);
			myThreadId = Thread.CurrentThread.ManagedThreadId;
		}
		/// <summary>
		/// Gets a value indicating if the current thread is different from the UI thread.
		/// </summary>
		public bool InvokeRequired
		{
			get
			{
				return Thread.CurrentThread.ManagedThreadId != myThreadId || myForm.InvokeRequired;
			}
		}
		/// <summary>
		/// Runs an application message loop and does not return until the <see cref="Exit"/> method is called.
		/// </summary>
		public void Start()
		{
			myForm.AboutToStart();
			Application.Run(); // Don't pass in myForm, as that would make it show the form.
		}
		/// <summary>
		/// Exits the application message loop and allows the <see cref="Start"/> method to return.
		/// </summary>
		public void Exit()
		{
			myForm.Close();
			Application.ExitThread();
		}
		/// <summary>
		/// Adds an item to the context menu.
		/// </summary>
		/// <param name="text">Text to show for the menu item. E.g. "E&amp;xit".</param>
		/// <param name="eventHandler">(optional) An event handler to call when the item is clicked.</param>
		/// <param name="icon">(optional) icon to show for the menu item.</param>
		/// <param name="tooltipText">(optional) tooltip text to show for the item.</param>
		public void AddToolStripMenuItem(string text, EventHandler eventHandler = null, Image icon = null, string tooltipText = null)
		{
			myForm.AddToolStripMenuItem(text, eventHandler, icon, tooltipText);
		}
		/// <summary>
		/// Adds a <see cref="ToolStripSeparator"/> to the context menu.
		/// </summary>
		public void AddToolStripSeparator()
		{
			myForm.AddToolStripSeparator();
		}
		/// <summary>
		/// Runs the given action on the UI thread.
		/// </summary>
		/// <param name="action">Action to run on the UI thread.</param>
		public void RunOnUiThread(Action action)
		{
			if (InvokeRequired)
			{
				myForm.Invoke(action);
			}
			else
			{
				action();
			}
		}
		/// <summary>
		/// Runs the given function on the UI thread, returning its result.
		/// </summary>
		/// <typeparam name="T">Type of return value from the function.</typeparam>
		/// <param name="func">Function to run on the UI thread which returns a value.</param>
		/// <returns></returns>
		public T RunOnUiThread<T>(Func<T> func)
		{
			if (InvokeRequired)
			{
				return (T)myForm.Invoke(func);
			}
			else
			{
				return func();
			}
		}
		Action timerCallback = null;
		/// <summary>
		/// Registers a callback method to be called on the UI thread on an interval.
		/// </summary>
		/// <param name="action">Action to call on an interval (it should take less time to execute than the interval).</param>
		/// <param name="msInterval">Milliseconds between calls to the action.</param>
		public void RegisterTimerCallback(Action action, int msInterval)
		{
			if (timerCallback != null)
				throw new Exception("A timer callback was already registered.");
			timerCallback = action;
			myForm.timer.Tick += Timer_Tick;
			myForm.timer.Interval = msInterval;
			myForm.timer.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			timerCallback();
		}

		#region IDisposable
		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					myForm.Dispose();
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				disposedValue = true;
			}
		}

		// // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~TrayIconApplication2()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
