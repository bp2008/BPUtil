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
	/// <para>This extension of Form simply positions itself at the center of its owner, upon being shown.</para>
	/// <para>It will be offset by approximately 1 centimeter to down and to the right for each form the owner owns.</para>
	/// <para>The parent form should pass a reference to itself into the Show method of this form to declare itself to be this form's owner.</para>
	/// </summary>
	public class SelfCenteredForm : Form
	{
		protected override void OnShown(EventArgs e)
		{
			if (Owner == null)
				this.StartPosition = FormStartPosition.CenterParent;
			else
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
