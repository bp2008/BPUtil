﻿namespace BPUtil.Forms
{
	partial class ServiceManager
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServiceManager));
			this.btnInstall = new System.Windows.Forms.Button();
			this.btnStart = new System.Windows.Forms.Button();
			this.txtStatus = new System.Windows.Forms.TextBox();
			this.lblService = new System.Windows.Forms.Label();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.SuspendLayout();
			// 
			// btnInstall
			// 
			this.btnInstall.Enabled = false;
			this.btnInstall.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnInstall.Location = new System.Drawing.Point(12, 12);
			this.btnInstall.Name = "btnInstall";
			this.btnInstall.Size = new System.Drawing.Size(193, 57);
			this.btnInstall.TabIndex = 0;
			this.btnInstall.Text = "Install Service";
			this.btnInstall.UseVisualStyleBackColor = true;
			this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
			// 
			// btnStart
			// 
			this.btnStart.Enabled = false;
			this.btnStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnStart.Location = new System.Drawing.Point(211, 12);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(193, 57);
			this.btnStart.TabIndex = 1;
			this.btnStart.Text = "Start Service";
			this.btnStart.UseVisualStyleBackColor = true;
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// txtStatus
			// 
			this.txtStatus.BackColor = System.Drawing.SystemColors.Window;
			this.txtStatus.Location = new System.Drawing.Point(12, 117);
			this.txtStatus.Multiline = true;
			this.txtStatus.Name = "txtStatus";
			this.txtStatus.ReadOnly = true;
			this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtStatus.Size = new System.Drawing.Size(392, 145);
			this.txtStatus.TabIndex = 4;
			// 
			// lblService
			// 
			this.lblService.AutoSize = true;
			this.lblService.Location = new System.Drawing.Point(12, 101);
			this.lblService.Name = "lblService";
			this.lblService.Size = new System.Drawing.Size(91, 13);
			this.lblService.TabIndex = 3;
			this.lblService.Text = "Service Status: ...";
			// 
			// progressBar
			// 
			this.progressBar.Location = new System.Drawing.Point(12, 75);
			this.progressBar.MarqueeAnimationSpeed = 9;
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(392, 23);
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.progressBar.TabIndex = 2;
			this.progressBar.Value = 100;
			this.progressBar.Visible = false;
			// 
			// ServiceManager
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(416, 274);
			this.Controls.Add(this.btnStart);
			this.Controls.Add(this.btnInstall);
			this.Controls.Add(this.txtStatus);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.lblService);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ServiceManager";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServiceManager_FormClosing);
			this.Load += new System.EventHandler(this.ServiceManager_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnInstall;
		private System.Windows.Forms.Button btnStart;
		private System.Windows.Forms.TextBox txtStatus;
		private System.Windows.Forms.Label lblService;
		private System.Windows.Forms.ProgressBar progressBar;
	}
}