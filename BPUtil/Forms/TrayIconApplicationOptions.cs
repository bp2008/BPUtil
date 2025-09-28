using System.Drawing;
using System;

namespace BPUtil.Forms
{
	/// <summary>
	/// Options for first-generation <see cref="TrayIconApplicationContext"/>.
	/// </summary>
	public class TrayIconApplicationOptions
	{
		/// <summary>
		/// The icon to show in the system tray.  You could load this from a Form by copying code from that form's Designer file.
		/// </summary>
		public Icon trayIcon;
		/// <summary>
		/// (optional) Text to show upon mouseover of the tray icon.
		/// </summary>
		public string tooltipText;
		/// <summary>
		/// (optional) A callback which is called when it is time to create the context menu. Return true if you have added an Exit button.  If this callback does not exist or returns false, an Exit item is added to the menu automatically.
		/// </summary>
		public Func<TrayIconApplicationContext, bool> onCreateContextMenu;
		/// <summary>
		/// A callback which is called when the tray icon is double-clicked. Enabling this disables single-left-click opening of the context menu.
		/// </summary>
		public Action onDoubleClick;
		/// <summary>
		/// If true, the tray icon application will set up a listener for display state changes (such as monitors being turned on or off by the OS) and raise the <see cref="DisplayStateChanged"/> event when such a change occurs.
		/// </summary>
		public bool ListenForDisplayStateChanges = false;
		/// <summary>
		/// An event raised when the display state changes, such as monitors being turned on or off by the OS.  This event is only raised if <see cref="ListenForDisplayStateChanges"/> is true.
		/// </summary>
		public event EventHandler<DisplayState> DisplayStateChanged = delegate { };
		/// <summary>
		/// Constructs a new TrayIconApplicationOptions with the specified tray icon and default values for all other options.
		/// </summary>
		/// <param name="trayIcon">The icon to show in the system tray.  You could load this from a Form by copying code from that form's Designer file.</param>
		public TrayIconApplicationOptions(Icon trayIcon)
		{
			this.trayIcon = trayIcon;
		}
		/// <summary>
		/// Raises the DisplayStateChanged event.
		/// </summary>
		/// <param name="newState"></param>
		internal void RaiseDisplayStateChangedEvent(DisplayState newState)
		{
			DisplayStateChanged?.Invoke(this, newState);
		}
	}
}