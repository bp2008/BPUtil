using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.Linux.InputListener
{
	/// <summary>
	/// Listens to all hardware keyboards connected to the system.
	/// </summary>
	public class AllKeyboardListener
	{
		private int msBetweenKeyboardScans;
		private Dictionary<string, LinuxInputListener> activeListeners = new Dictionary<string, LinuxInputListener>();
		public event EventHandler<LinuxInputEventArgs> KeyDownEvent = delegate { };
		public event EventHandler<LinuxInputEventArgs> KeyUpEvent = delegate { };
		public event EventHandler<LinuxInputEventArgs> KeyAutoRepeatEvent = delegate { };
		/// <summary>
		/// Begins listening to all hardware keyboards.
		/// </summary>
		/// <param name="msBetweenKeyboardScans">The rescan interval in milliseconds.  This interval shouldn't be extremely small, for efficiency's sake, as it creates new processes and whatnot to perform the scans.</param>
		public AllKeyboardListener(int msBetweenKeyboardScans = 5000)
		{
			this.msBetweenKeyboardScans = BPMath.Clamp(msBetweenKeyboardScans, 1000, (int)TimeSpan.FromDays(1).TotalSeconds);
			ScanForNewKeyboards();
		}
		/// <summary>
		/// Scans for new keyboards attached to the system, and begins listening to any keyboards that are not currently being listened to.
		/// </summary>
		public void ScanForNewKeyboards()
		{
			try
			{
				string[] keyboardNames = LinuxInputHelper.GetKeyboardInputNames();
				if (keyboardNames == null)
					return;
				foreach (string keyboardName in keyboardNames)
				{
					string devicePath = "/dev/input/" + keyboardName;
					lock (activeListeners)
					{
						if (!activeListeners.TryGetValue(devicePath, out LinuxInputListener listener) || !listener.IsAlive)
						{
							Logger.Info("Created Keyboard Listener for " + devicePath);
							activeListeners[devicePath] = listener = new LinuxInputListener(devicePath);
							listener.KeyDownEvent += Listener_KeyDownEvent;
							listener.KeyUpEvent += Listener_KeyUpEvent;
							listener.KeyAutoRepeatEvent += Listener_KeyAutoRepeatEvent;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			finally
			{
				SetTimeout.OnBackground(() =>
				{
					ScanForNewKeyboards();
				}, msBetweenKeyboardScans);
			}
		}

		private void Listener_KeyDownEvent(object sender, LinuxInputEventArgs e)
		{
			KeyDownEvent(sender, e);
		}
		private void Listener_KeyUpEvent(object sender, LinuxInputEventArgs e)
		{
			KeyUpEvent(sender, e);
		}
		private void Listener_KeyAutoRepeatEvent(object sender, LinuxInputEventArgs e)
		{
			KeyAutoRepeatEvent(sender, e);
		}
	}
}
