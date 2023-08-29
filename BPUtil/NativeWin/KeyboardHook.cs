using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.NativeWin
{
	/// <summary>
	/// Event arguments for a key press event.
	/// </summary>
	public class AsyncKeyPressEventArgs : EventArgs
	{
		/// <summary>
		/// The key that was pressed.
		/// </summary>
		public Keys Key;
		/// <summary>
		/// State of the Ctrl modifier key.
		/// </summary>
		public bool Ctrl;
		/// <summary>
		/// State of the Alt modifier key.
		/// </summary>
		public bool Alt;
		/// <summary>
		/// State of the Shift modifier key.
		/// </summary>
		public bool Shift;
		/// <summary>
		/// Gets a string describing the key and modifiers being pressed.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return (Ctrl ? "CTRL + " : "") + (Alt ? "ALT + " : "") + (Shift ? "SHIFT + " : "") + Key;
		}
	}

	/// <summary>
	/// Event arguments for a key press event supporting cancellation
	/// </summary>
	public class CancelableKeyPressEventArgs : EventArgs
	{
		/// <summary>
		/// The key that was pressed.
		/// </summary>
		public Keys Key;
		/// <summary>
		/// State of the Ctrl modifier key.
		/// </summary>
		public bool Ctrl;
		/// <summary>
		/// State of the Alt modifier key.
		/// </summary>
		public bool Alt;
		/// <summary>
		/// State of the Shift modifier key.
		/// </summary>
		public bool Shift;
		/// <summary>
		/// Set = true to cancel the key press so that its default behavior does not occur.
		/// </summary>
		public bool Cancel;
	}

	/// <summary>
	/// Use within using() or use try{}finally{} to guarantee disposal of this object.  KeyboardHook uses a low-level keyboard hook to raise an event when keys are pressed.
	/// </summary>
	public class KeyboardHook : IDisposable
	{
		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_SYSKEYDOWN = 0x0104;
		private LowLevelKeyboardProc _proc;
		private IntPtr _hookID = IntPtr.Zero;

		/// <summary>
		/// This event is raised when keys are pressed.  The key press will not be able to take effect on the system until the event is finished, which increases input latency!
		/// </summary>
		public event EventHandler<CancelableKeyPressEventArgs> KeyPressedCancelable;
		/// <summary>
		/// This event is raised when keys are pressed, but the event is raised asynchronously and is not cancelable.
		/// </summary>
		public event EventHandler<AsyncKeyPressEventArgs> KeyPressedAsync;
		/// <summary>
		/// This event is raised asynchronously when an Exception is caught by the KeyboardHook.
		/// </summary>
		public event EventHandler<Exception> Error = delegate { };
		/// <summary>
		/// Use within using() or use try{}finally{} to guarantee disposal of this object.  KeyboardHook uses a low-level keyboard hook to raise an event when keys are pressed.
		/// </summary>
		public KeyboardHook()
		{
			_proc = HookCallback;
			_hookID = SetHook(_proc);
		}

		#region Dispose
		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				UnhookWindowsHookEx(_hookID);
				_proc = null;
				disposedValue = true;
			}
		}
		~KeyboardHook()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion

		private static IntPtr SetHook(LowLevelKeyboardProc proc)
		{
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule)
			{
				return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			bool cancel = false;
			try
			{
				if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
				{
					int vkCode = Marshal.ReadInt32(lParam);
					Keys key = (Keys)vkCode;
					bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) != 0;
					bool isAltPressed = (Control.ModifierKeys & Keys.Alt) != 0;
					bool isShiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;

					if (KeyPressedCancelable != null)
					{
						CancelableKeyPressEventArgs args = new CancelableKeyPressEventArgs
						{
							Key = key,
							Ctrl = isCtrlPressed,
							Alt = isAltPressed,
							Shift = isShiftPressed,
							Cancel = false
						};

						try
						{
							// Raise the event
							KeyPressedCancelable?.Invoke(null, args);
						}
						catch (Exception ex)
						{
							ThrowOnBackground(ex);
						}

						cancel = args.Cancel;
					}

					if (KeyPressedAsync != null)
					{
						_ = Task.Run(() =>
						{
							try
							{
								KeyPressedAsync.Invoke(this, new AsyncKeyPressEventArgs()
								{
									Key = key,
									Ctrl = isCtrlPressed,
									Alt = isAltPressed,
									Shift = isShiftPressed
								});
							}
							catch (Exception ex)
							{
								ThrowOnBackground(ex);
							}
						}).ConfigureAwait(false);
					}
				}
			}
			catch (Exception ex)
			{
				ThrowOnBackground(ex);
			}
			if (cancel)
				return (IntPtr)1;
			else
				return CallNextHookEx(_hookID, nCode, wParam, lParam);
		}

		private void ThrowOnBackground(Exception ex)
		{
			_ = Task.Run(() =>
			{
				try
				{
					Error.Invoke(this, ex);
				}
				catch { }
			}).ConfigureAwait(false);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}
