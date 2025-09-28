using BPUtil.NativeWin;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BPUtil.Forms
{
	/// <summary>
	/// Enumeration of display state options.
	/// </summary>
	public enum DisplayState
	{
		Unknown = -1,
		Off = 0,
		On = 1,
		Dimmed = 2
	}
	/// <summary>
	/// <para>A hidden window that listens for power broadcast messages about display state changes (on, off, dimmed).</para>
	/// <para>Be sure to call <see cref="Dispose()"/> when finished with this object.</para>
	/// </summary>
	public class DisplayStateListener : NativeWindow, IDisposable
	{
		private DisplayState _currentDisplayState = DisplayState.Unknown;
		/// <summary>
		/// Gets the current display state.  It will be the "Unknown" state until the first state change message is received from the OS.
		/// </summary>
		public DisplayState CurrentDisplayState => _currentDisplayState;
		/// <summary>
		/// An event raised when the display state is changed.
		/// </summary>
		public event EventHandler<DisplayState> DisplayStateChanged = delegate { };
		/// <summary>
		/// <para>Constructs a new DisplayStateListener.  Call <see cref="Start"/> to begin listening for display state changes.</para>
		/// <para>Be sure to call <see cref="Dispose()"/> when finished with this object.</para>
		/// </summary>
		public DisplayStateListener()
		{
		}

		private object myLock = new object();
		private bool handleCreated = false;

		/// <summary>
		/// <para>Starts listening for display state changes.  In my testing, this causes a display state change notification to be received soon afterward with the current state.</para>
		/// <para>Be sure to call <see cref="Dispose()"/> when finished with this object.</para>
		/// </summary>
		public void Start()
		{
			lock (myLock)
			{
				if (!handleCreated)
				{
					// Create hidden window:
					CreateHandle(new CreateParams());
					handleCreated = true;
				}
				if (_notificationHandle == IntPtr.Zero)
				{
					// Register for power setting notifications:
					Guid guid = GUID_CONSOLE_DISPLAY_STATE;
					_notificationHandle = RegisterPowerSettingNotification(Handle, ref guid, DEVICE_NOTIFY_WINDOW_HANDLE);
					if (_notificationHandle == IntPtr.Zero)
						Win32Helper.ThrowLastWin32Error("RegisterPowerSettingNotification failed");

				}
			}
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_POWERBROADCAST && m.WParam.ToInt32() == PBT_POWERSETTINGCHANGE)
			{
				POWERBROADCAST_SETTING setting = (POWERBROADCAST_SETTING)Marshal.PtrToStructure(m.LParam, typeof(POWERBROADCAST_SETTING));
				if (setting.PowerSetting == GUID_CONSOLE_DISPLAY_STATE)
				{
					if (!Enum.TryParse<DisplayState>(setting.Data.ToString(), out DisplayState state))
						state = DisplayState.Unknown;
					_currentDisplayState = state;
					DisplayStateChanged.Invoke(this, state);
				}
			}

			base.WndProc(ref m);
		}
		#region Native Stuff
		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr RegisterPowerSettingNotification(IntPtr hWnd, ref Guid powerSettingGuid, int flags);
		[DllImport("User32.dll", SetLastError = true)]
		private static extern bool UnregisterPowerSettingNotification(IntPtr handle);
		private IntPtr _notificationHandle = IntPtr.Zero;

		private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
		private const int WM_POWERBROADCAST = 0x0218;
		private const int PBT_POWERSETTINGCHANGE = 0x8013;
		private static readonly Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		struct POWERBROADCAST_SETTING
		{
			public Guid PowerSetting;
			public int DataLength;
			public int Data;
		}
		#endregion
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

				// Destroy hidden window:
				lock (myLock)
				{
					if (handleCreated)
						DestroyHandle();

					if (_notificationHandle != IntPtr.Zero)
					{
						if (!UnregisterPowerSettingNotification(_notificationHandle))
							Logger.Debug("UnregisterPowerSettingNotification failed");
						else
							_notificationHandle = IntPtr.Zero;
					}
				}

				disposedValue = true;
			}
		}

		// override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		~DisplayStateListener()
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
	}
}