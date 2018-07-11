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
	/// This extension of Form simply positions itself at the center of its owner, upon being shown.
	/// </summary>
	public class SelfCenteredForm : Form
	{
		protected override void OnShown(EventArgs e)
		{
			this.StartPosition = FormStartPosition.Manual;
			base.OnShown(e);
			if (Owner != null && StartPosition == FormStartPosition.Manual)
			{
				int offset = Owner.OwnedForms.Length * 38;  // approx. 10mm
				Point p = new Point(Owner.Left + Owner.Width / 2 - Width / 2 + offset, Owner.Top + Owner.Height / 2 - Height / 2 + offset);
				this.Location = p;
			}
		}
	}
}
