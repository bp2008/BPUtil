using System.Windows.Forms;

namespace BPUtil.Forms
{
	partial class MovableOverlay
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.panelDragMe = new System.Windows.Forms.Panel();
			this.btnLockPosition = new System.Windows.Forms.Button();
			this.lblDragMe = new System.Windows.Forms.Label();
			this.panelResizeMe = new System.Windows.Forms.Panel();
			this.lblResizeHandle = new System.Windows.Forms.Label();
			this.lblResizeMe = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.panelDragMe.SuspendLayout();
			this.panelResizeMe.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelDragMe
			// 
			this.panelDragMe.BackColor = System.Drawing.Color.White;
			this.panelDragMe.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelDragMe.Controls.Add(this.btnLockPosition);
			this.panelDragMe.Controls.Add(this.lblDragMe);
			this.panelDragMe.Cursor = System.Windows.Forms.Cursors.SizeAll;
			this.panelDragMe.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelDragMe.Location = new System.Drawing.Point(0, 0);
			this.panelDragMe.Name = "panelDragMe";
			this.panelDragMe.Size = new System.Drawing.Size(250, 33);
			this.panelDragMe.TabIndex = 1;
			this.panelDragMe.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Move_MouseDown);
			this.panelDragMe.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseMove);
			this.panelDragMe.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseUp);
			// 
			// btnLockPosition
			// 
			this.btnLockPosition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnLockPosition.Cursor = System.Windows.Forms.Cursors.Default;
			this.btnLockPosition.Location = new System.Drawing.Point(180, 3);
			this.btnLockPosition.Name = "btnLockPosition";
			this.btnLockPosition.Size = new System.Drawing.Size(57, 23);
			this.btnLockPosition.TabIndex = 1;
			this.btnLockPosition.Text = "LOCK";
			this.toolTip1.SetToolTip(this.btnLockPosition, "Locks the overlay\'s current position and makes it transparent to the mouse.\r\nTo u" +
        "nlock, close and reopen the overlay.");
			this.btnLockPosition.UseVisualStyleBackColor = true;
			this.btnLockPosition.Click += new System.EventHandler(this.btnLockPosition_Click);
			this.btnLockPosition.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseMove);
			this.btnLockPosition.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseUp);
			// 
			// lblDragMe
			// 
			this.lblDragMe.AutoSize = true;
			this.lblDragMe.ForeColor = System.Drawing.Color.Black;
			this.lblDragMe.Location = new System.Drawing.Point(11, 8);
			this.lblDragMe.Name = "lblDragMe";
			this.lblDragMe.Size = new System.Drawing.Size(47, 13);
			this.lblDragMe.TabIndex = 0;
			this.lblDragMe.Text = "Drag me";
			this.lblDragMe.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Move_MouseDown);
			this.lblDragMe.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseMove);
			this.lblDragMe.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseUp);
			// 
			// panelResizeMe
			// 
			this.panelResizeMe.BackColor = System.Drawing.Color.White;
			this.panelResizeMe.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelResizeMe.Controls.Add(this.lblResizeHandle);
			this.panelResizeMe.Controls.Add(this.lblResizeMe);
			this.panelResizeMe.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelResizeMe.Location = new System.Drawing.Point(0, 220);
			this.panelResizeMe.Name = "panelResizeMe";
			this.panelResizeMe.Size = new System.Drawing.Size(250, 30);
			this.panelResizeMe.TabIndex = 2;
			this.panelResizeMe.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseMove);
			this.panelResizeMe.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseUp);
			// 
			// lblResizeHandle
			// 
			this.lblResizeHandle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblResizeHandle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
			this.lblResizeHandle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblResizeHandle.Cursor = System.Windows.Forms.Cursors.SizeNWSE;
			this.lblResizeHandle.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblResizeHandle.ForeColor = System.Drawing.Color.Black;
			this.lblResizeHandle.Location = new System.Drawing.Point(219, -1);
			this.lblResizeHandle.Name = "lblResizeHandle";
			this.lblResizeHandle.Size = new System.Drawing.Size(30, 30);
			this.lblResizeHandle.TabIndex = 0;
			this.lblResizeHandle.Text = "⇲";
			this.lblResizeHandle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Resize_MouseDown);
			this.lblResizeHandle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseMove);
			this.lblResizeHandle.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseUp);
			// 
			// lblResizeMe
			// 
			this.lblResizeMe.AutoSize = true;
			this.lblResizeMe.Location = new System.Drawing.Point(12, 8);
			this.lblResizeMe.Name = "lblResizeMe";
			this.lblResizeMe.Size = new System.Drawing.Size(59, 13);
			this.lblResizeMe.TabIndex = 0;
			this.lblResizeMe.Text = "Resize me:";
			this.lblResizeMe.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseMove);
			this.lblResizeMe.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseUp);
			// 
			// MovableOverlay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Red;
			this.ClientSize = new System.Drawing.Size(250, 250);
			this.Controls.Add(this.panelResizeMe);
			this.Controls.Add(this.panelDragMe);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "MovableOverlay";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "MovableOverlay";
			this.TopMost = true;
			this.TransparencyKey = System.Drawing.Color.Red;
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MovableOverlay_FormClosed);
			this.Load += new System.EventHandler(this.MovableOverlay_Load);
			this.ResizeEnd += new System.EventHandler(this.MovableOverlay_ResizeEnd);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveResize_MouseUp);
			this.Move += new System.EventHandler(this.MovableOverlay_Move);
			this.panelDragMe.ResumeLayout(false);
			this.panelDragMe.PerformLayout();
			this.panelResizeMe.ResumeLayout(false);
			this.panelResizeMe.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panelDragMe;
		private System.Windows.Forms.Button btnLockPosition;
		private System.Windows.Forms.Label lblDragMe;
		private System.Windows.Forms.Panel panelResizeMe;
		private System.Windows.Forms.Label lblResizeMe;
		private System.Windows.Forms.Label lblResizeHandle;
		private ToolTip toolTip1;
	}
}