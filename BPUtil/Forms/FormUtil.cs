using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.Forms
{
	/// <summary>
	/// Utility methods for Windows Forms.
	/// </summary>
	public static class FormUtil
	{
		/// <summary>
		/// If the given form is offscreen or partially offscreen, move it as little as possible so that it is completely on screen.
		/// </summary>
		/// <param name="form">The form to move onto the screen if any part of it is offscreen.</param>
		public static void MoveOntoScreen(Form form)
		{
			// Get the screen that the form is on
			Screen screen = Screen.FromControl(form);

			// Check if the form is completely off all screens
			if (screen == null)
			{
				// Move the form to the primary screen's working area
				Rectangle primaryScreenWorkingArea = Screen.PrimaryScreen.WorkingArea;
				form.Left = primaryScreenWorkingArea.Left;
				form.Top = primaryScreenWorkingArea.Top;
			}
			else
			{
				// Get the working area of the screen (excluding taskbars, etc.)
				Rectangle workingArea = screen.WorkingArea;

				// Check if the form is offscreen and adjust its position if necessary
				if (form.Right > workingArea.Right)
				{
					form.Left = workingArea.Right - form.Width;
				}

				if (form.Bottom > workingArea.Bottom)
				{
					form.Top = workingArea.Bottom - form.Height;
				}

				if (form.Left < workingArea.Left)
				{
					form.Left = workingArea.Left;
				}

				if (form.Top < workingArea.Top)
				{
					form.Top = workingArea.Top;
				}
			}
		}
	}
}
