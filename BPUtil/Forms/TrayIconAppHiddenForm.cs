using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.Forms
{
	internal partial class TrayIconAppHiddenForm : Form
	{
		private bool firstContextMenuOpen = true;
		/// <summary>
		/// Constructs a TrayIconAppHiddenForm but does not initialize the NotifyIcon.
		/// </summary>
		public TrayIconAppHiddenForm()
		{
			InitializeComponent();
			this.Text = "This form belongs to " + Globals.AssemblyName + " should not be visible.";
		}

		/// <summary>
		/// Constructs a TrayIconAppHiddenForm and initializes the NotifyIcon.
		/// </summary>
		/// <param name="trayIcon">The icon to show in the system tray.  You could load this from a Form by copying code from that form's Designer file.</param>
		/// <param name="tooltipText">(optional) Text to show upon mouseover of the tray icon.</param>
		/// <param name="onDoubleClick">A callback which is called when the tray icon is double-clicked. Enabling this disables single-left-click opening of the context menu.</param>
		/// <exception cref="ArgumentException">If an icon is not provided.</exception>
		public TrayIconAppHiddenForm(Icon trayIcon, string tooltipText, Action onDoubleClick) : this()
		{
			if (trayIcon == null)
				throw new ArgumentException("An icon is required in order to use this class!");

			_ = this.Handle;
			notifyIcon.ContextMenuStrip = new ContextMenuStrip();
			notifyIcon.Icon = trayIcon;
			if (!string.IsNullOrWhiteSpace(tooltipText))
				notifyIcon.Text = tooltipText;
			notifyIcon.Visible = true;

			if (onDoubleClick != null)
				notifyIcon.DoubleClick += (sender, e) => onDoubleClick();
			else
				notifyIcon.MouseUp += (sender, e) =>
				{
					if (e.Button == MouseButtons.Left)
						DoShowContextMenu();
				};

			notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
		}
		private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			if (firstContextMenuOpen)
			{
				// Based on guidance from the original TrayApplicationContext class, the first context menu strip opening will be triggered automatically by the TrayIconApplication2, then the opening will be canceled by this code.
				firstContextMenuOpen = false;
				e.Cancel = true;
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
		/// Call just before running Application.Run(), and the context menu strip will be initialized if needed.
		/// </summary>
		public void AboutToStart()
		{
			if (notifyIcon.ContextMenuStrip.Items.Count == 0)
			{
				AddToolStripMenuItem("E&xit", (sender, e) =>
				{
					this.Close();
				});
			}
			// This is the automatic first opening of the tray icon's context menu.  It will be canceled.  This happens to work around a layout bug.
			if (firstContextMenuOpen)
				DoShowContextMenu();
		}
		/// <summary>
		/// Shows the context menu.
		/// </summary>
		public void DoShowContextMenu()
		{
			MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
			mi.Invoke(notifyIcon, null);
		}

		private void TrayIconAppHiddenForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			notifyIcon.Visible = false;
		}
	}
}
