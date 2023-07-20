using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.Forms
{
	/// <summary>
	/// <para>
	/// An application context you can use when you want to develop a Windows Forms application that "runs in the system tray" and normally has no form open.
	/// </para>
	/// <para>
	/// Example: Application.Run(new TrayIconApplicationContext(...));
	/// </para>
	/// </summary>
	public class TrayIconApplicationContext : ApplicationContext
	{
		private Func<TrayIconApplicationContext, bool> onCreateContextMenu;
		private Action onDoubleClick;

		/// <summary>
		/// If true, the context menu will be emptied and the CreateContextMenu event will be raised every time the context menu is opened.
		/// </summary>
		public bool CreateContextMenuAtEveryOpen = false;

		/// <summary>
		/// Keeps track of whether the menu has been created or not.
		/// </summary>
		private bool contextMenuCreated = false;

		/// <summary>
		/// This class should be created and passed into Application.Run( ... )
		/// </summary>
		/// <param name="trayIcon">The icon to show in the system tray.  You could load this from a Form by copying code from that form's Designer file.</param>
		/// <param name="tooltipText">(optional) Text to show upon mouseover of the tray icon.</param>
		/// <param name="onCreateContextMenu">(optional) A callback which is called when it is time to create the context menu. Return true if you have added an Exit button.  If this callback does not exist or returns false, an Exit item is added to the menu automatically.</param>
		/// <param name="onDoubleClick">A callback which is called when the tray icon is double-clicked. Enabling this disables single-left-click opening of the context menu.</param>
		public TrayIconApplicationContext(Icon trayIcon, string tooltipText, Func<TrayIconApplicationContext, bool> onCreateContextMenu, Action onDoubleClick)
		{
			if (trayIcon == null)
				throw new ArgumentException("An icon is required in order to use this class!");

			this.onCreateContextMenu = onCreateContextMenu;
			this.onDoubleClick = onDoubleClick;

			components = new Container();

			notifyIcon = new NotifyIcon(components);
			notifyIcon.ContextMenuStrip = new ContextMenuStrip();
			notifyIcon.Icon = trayIcon;
			if (!string.IsNullOrWhiteSpace(tooltipText))
				notifyIcon.Text = tooltipText;
			notifyIcon.Visible = true;

			notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
			if (onDoubleClick != null)
				notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
			else
				notifyIcon.MouseUp += notifyIcon_MouseUp;

			DoShowContextMenu();
		}

		private void NotifyIcon_DoubleClick(object sender, EventArgs e)
		{
			onDoubleClick();
		}

		/// <summary>
		/// Shows the context menu upon left mouse click too. http://stackoverflow.com/questions/2208690/invoke-notifyicons-context-menu
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void notifyIcon_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				DoShowContextMenu();
		}

		private void DoShowContextMenu()
		{
			MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
			mi.Invoke(notifyIcon, null);
		}

		private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			if (CreateContextMenuAtEveryOpen || !contextMenuCreated)
			{
				notifyIcon.ContextMenuStrip.Items.Clear();
				if (onCreateContextMenu == null || !onCreateContextMenu(this))
					AddToolStripMenuItem("E&xit", exitItem_Click);
			}
			if (!contextMenuCreated)
			{
				// The first time the menu opens, it is empty and e.Cancel is set to true by default.
				// Some advice says to set e.Cancel = false, but this advise ignores the issue where the framework already measured the size of the menu before raising the Opening event, so the position of the menu will be wrong if we un-cancel the open.
				// My approach is to instead allow the first open to be canceled, but to trigger the first open programmatically the moment the NotifyIcon is created!

				e.Cancel = true;
				contextMenuCreated = true;
			}
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
			ToolStripMenuItem item = new ToolStripMenuItem(text);
			if (eventHandler != null)
				item.Click += eventHandler;
			item.Image = icon;
			item.ToolTipText = tooltipText == null ? "" : tooltipText;
			notifyIcon.ContextMenuStrip.Items.Add(item);
		}
		/// <summary>
		/// Adds a <see cref="ToolStripSeparator"/> to the context menu.
		/// </summary>
		public void AddToolStripSeparator()
		{
			notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
		}

		/// <summary>
		/// A list of components to dispose when the context is disposed.
		/// </summary>
		private IContainer components;

		/// <summary>
		/// The icon that sits in the system tray.
		/// </summary>
		private NotifyIcon notifyIcon;
		/// <summary>
		/// When the application context is disposed, dispose things like the notify icon.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null) { components.Dispose(); }
		}
		/// <summary>
		/// When the exit menu item is clicked, make a call to terminate the ApplicationContext.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void exitItem_Click(object sender, EventArgs e)
		{
			ExitThread();
		}

		/// <summary>
		/// If we are presently showing a form, clean it up.
		/// </summary>
		protected override void ExitThreadCore()
		{
			// before we exit, let forms clean themselves up.
			//if (introForm != null) { introForm.Close(); }
			//if (detailsForm != null) { detailsForm.Close(); }

			notifyIcon.Visible = false; // should remove lingering tray icon
			base.ExitThreadCore();
		}
	}
}
