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
	public partial class InputDialog : Form
	{
		/// <summary>
		/// Gets the text which was input by the user.
		/// </summary>
		public string InputText { get { return txtInput.Text; } }

		/// <summary>
		/// Gets a value indicating if the the OK button was clicked.
		/// </summary>
		public bool OK { get; protected set; }

		/// <summary>
		/// Creates a new InputDialog containing a small single-line text input field with OK and Cancel buttons.
		/// </summary>
		/// <param name="title">Title for the form's title bar.</param>
		/// <param name="label">Label to display above the text input.</param>
		/// <param name="defaultValue">The default value of the input text field.</param>
		public InputDialog(string title, string label, string defaultValue = "")
		{
			InitializeComponent();
			this.Text = title;
			this.label1.Text = label;
			this.txtInput.Text = defaultValue;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			this.OK = true;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
	}
}
