using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.Forms
{
	public partial class PasswordPrompt : Form
	{

		/// <summary>
		/// Gets a value indicating if the the OK button was clicked.
		/// </summary>
		public bool OK { get; protected set; }
		/// <summary>
		/// Gets the password which was input by the user.
		/// </summary>
		public string InputPassword
		{
			get
			{
				return txtPassword.Text;
			}
		}
		/// <summary>
		/// Creates a new PasswordPrompt containing a password input field with OK and Cancel buttons.
		/// </summary>
		/// <param name="title">Title for the form's title bar.</param>
		/// <param name="labelText">Label to display above the password input.</param>
		public PasswordPrompt(string title = "Password Prompt", string labelText = "Enter the password:")
		{
			InitializeComponent();
			this.Text = title;
			lblPasswordPrompt.Text = labelText;
			cbMask_CheckedChanged(null, null);
		}

		private void cbMask_CheckedChanged(object sender, EventArgs e)
		{
			txtPassword.UseSystemPasswordChar = cbMask.Checked;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			OK = true;
			this.Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			txtPassword.Clear();
			this.Close();
		}
	}
}
