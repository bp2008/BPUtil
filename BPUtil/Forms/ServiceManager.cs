using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace BPUtil.Forms
{
	/// <summary>
	/// A Windows Form which offers the ability to Install, Uninstall, Start, and Stop the current executable as a Windows Service.
	/// This form can be extended with additional functionality in the form of `additionalButtons` passed to the constructor.
	/// </summary>
	public partial class ServiceManager : Form
	{
		string ServiceName;
		ButtonDefinition[] additionalButtons;
		List<Button> customButtons = new List<Button>();
		Timer timer;
		BackgroundWorker bw = null;
		string statusStr = "";
		string servicePath = "";
		/// <summary>
		/// 
		/// </summary>
		/// <param name="Title">The title of the form (will appear in the title bar).</param>
		/// <param name="ServiceName">The name of the Windows Service.</param>
		/// <param name="additionalButtons">An array of buttons to add to the bottom of the form.  May be null.  You may specify the Text and Click handler for each button.  `null` buttons will still take up space, allowing more control over the layout.</param>
		public ServiceManager(string Title, string ServiceName, ButtonDefinition[] additionalButtons = null)
		{
			if (Platform.IsUnix() || Platform.IsRunningOnMono())
				throw new PlatformNotSupportedException("Service Manager only runs on Windows, using the official .NET runtime.");
			this.Text = Title;
			this.ServiceName = ServiceName;
			this.additionalButtons = additionalButtons;

			Application.EnableVisualStyles();
			InitializeComponent();
		}

		private void ServiceManager_Load(object sender, EventArgs e)
		{
			AddCustomButtons();
			UpdateStatus();
			timer = new Timer();
			timer.Tick += Timer_Tick;
			timer.Interval = 1000;
			timer.Start();
		}

		private void AddCustomButtons()
		{
			if (additionalButtons == null || additionalButtons.Length == 0)
				return;
			this.SuspendLayout();
			this.Height += 25 * ((additionalButtons.Length + 1) / 2);
			int addedCustomButtons = 0;
			int leftX = btnInstall.Left;
			int leftWidth = btnInstall.Width;
			int rightX = btnStart.Left;
			int rightWidth = btnStart.Width;
			int newX = leftX;
			int newY = txtStatus.Top + txtStatus.Height + 6;
			int newWidth = leftWidth;
			for (int i = 0; i < additionalButtons.Length; i++)
			{
				ButtonDefinition def = additionalButtons[i];
				if (addedCustomButtons > 0 && addedCustomButtons % 2 == 0)
				{
					newX = leftX;
					newWidth = leftWidth;
					newY += 25;
				}
				else if (addedCustomButtons % 2 == 1)
				{
					newX = rightX;
					newWidth = rightWidth;
				}

				if (def == null)
				{
					customButtons.Add(null);
				}
				else
				{
					Button btn = new Button();
					btn.Text = def.Text;
					btn.Click += def.OnClick;
					btn.Bounds = new Rectangle(newX, newY, newWidth, 23);

					customButtons.Add(btn);
					Controls.Add(btn);
				}

				addedCustomButtons++;
			}
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private void DetachCustomButtonClickEvents()
		{
			for (int i = 0; i < customButtons.Count; i++)
			{
				if (customButtons[i] != null)
					customButtons[i].Click -= additionalButtons[i].OnClick;
			}
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			UpdateStatus();
		}

		private void btnInstall_Click(object sender, EventArgs e)
		{
			if (btnInstall.Tag is string)
			{
				if ((string)btnInstall.Tag == "INSTALL")
				{
					DoInBackground((string)btnInstall.Tag);
				}
				else if ((string)btnInstall.Tag == "UNINSTALL")
				{
					DoInBackground((string)btnInstall.Tag);
				}
			}
		}

		private void btnStart_Click(object sender, EventArgs e)
		{
			if (btnStart.Tag is string)
			{
				if ((string)btnStart.Tag == "START")
				{
					DoInBackground((string)btnStart.Tag);
				}
				else if ((string)btnStart.Tag == "STOP")
				{
					DoInBackground((string)btnStart.Tag);
				}
			}
		}

		private void DoInBackground(string tag)
		{
			if (bw != null)
				return;
			statusStr = "";
			progressBar.Visible = true;
			bw = new BackgroundWorker();
			bw.DoWork += Bw_DoWork;
			bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
			bw.WorkerSupportsCancellation = true;
			bw.RunWorkerAsync(tag);
			UpdateStatus();
		}

		private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			bw = null;
			progressBar.Visible = false;
			UpdateStatus();
		}

		private void Bw_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				string command = (string)e.Argument;
				if (command == "INSTALL")
				{
					InstallService();
				}
				else if (command == "UNINSTALL")
				{
					UninstallService();
				}
				else if (command == "START")
				{
					StartService();
				}
				else if (command == "STOP")
				{
					StopService();
				}
			}
			catch (Exception ex)
			{
				statusStr = ex.ToString();
			}
		}

		private void UpdateStatus()
		{
			ServiceController service = ServiceController.GetServices().FirstOrDefault(sc => sc.ServiceName == ServiceName);
			if (service == null)
			{
				lblService.Text = "Service Status: Not installed";
				btnInstall.Text = "Install Service";
				btnInstall.Tag = "INSTALL";
				btnInstall.Enabled = (bw == null);
				btnStart.Text = "Start Service";
				btnStart.Tag = "";
				btnStart.Enabled = false;
			}
			else
			{
				btnInstall.Text = "Uninstall Service";
				btnInstall.Tag = "UNINSTALL";
				ServiceControllerStatus status = service.Status;
				lblService.Text = "Service Status: " + status.ToString();
				if (status == ServiceControllerStatus.Running)
				{
					btnStart.Text = "Stop Service";
					btnStart.Tag = "STOP";
					btnStart.Enabled = (bw == null);
					btnInstall.Enabled = false;
				}
				else if (status == ServiceControllerStatus.Stopped)
				{
					btnStart.Text = "Start Service";
					btnStart.Tag = "START";
					btnStart.Enabled = (bw == null);
					btnInstall.Enabled = (bw == null);
				}
				else
				{
					btnStart.Text = "Start Service";
					btnStart.Tag = "";
					btnStart.Enabled = false;
					btnInstall.Enabled = false;
				}
			}
			if (service != null && servicePath == "")
				servicePath = "Service path: " + GetServicePath(service.ServiceName);

			bool twoStatusStrs = (!string.IsNullOrWhiteSpace(statusStr) && !string.IsNullOrWhiteSpace(statusStr));
			string newStatus = statusStr + (twoStatusStrs ? Environment.NewLine : "") + servicePath;
			if (txtStatus.Text != newStatus)
				txtStatus.Text = newStatus;
		}
		private string GetServicePath(string serviceName)
		{
			using (ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + serviceName + "'"))
			{
				wmiService.Get();
				string currentserviceExePath = wmiService["PathName"].ToString();
				return currentserviceExePath;
			}
		}
		private void ServiceManager_FormClosing(object sender, FormClosingEventArgs e)
		{
			timer?.Stop();
			DetachCustomButtonClickEvents();
		}

		private void InstallService()
		{
			// sc create ServiceName binPath= "%~dp0Service.exe" start= auto
			// sc failure ServiceName reset= 0 actions= restart/60000/restart/60000/restart/60000
			string std, err;
			RunProcessAndWait("sc", "create \"" + ServiceName + "\" binPath= \"" + Application.ExecutablePath + "\" start= auto", out std, out err);
			statusStr = std + Environment.NewLine + err;
			RunProcessAndWait("sc", "failure \"" + ServiceName + "\" reset= 0 actions= restart/60000/restart/60000/restart/60000", out std, out err);
			statusStr = std + Environment.NewLine + err;
			servicePath = "";
		}

		private void UninstallService()
		{
			string std, err;
			RunProcessAndWait("sc", "delete \"" + ServiceName + "\"", out std, out err);
			statusStr = std + Environment.NewLine + err;
			servicePath = "";
		}

		private void StartService()
		{
			string std, err;
			RunProcessAndWait("NET", "START \"" + ServiceName + "\"", out std, out err);
			statusStr = std + Environment.NewLine + err;
		}

		private void StopService()
		{
			string std, err;
			RunProcessAndWait("NET", "STOP \"" + ServiceName + "\"", out std, out err);
			statusStr = std + Environment.NewLine + err;
		}

		private int RunProcessAndWait(string fileName, string arguments, out string output, out string errorOutput)
		{
			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			StringBuilder sbOutput = new StringBuilder();
			StringBuilder sbError = new StringBuilder();

			Process p = Process.Start(psi);

			p.OutputDataReceived += (sender, e) =>
			{
				sbOutput.AppendLine(e.Data);
			};
			p.ErrorDataReceived += (sender, e) =>
			{
				sbError.AppendLine(e.Data);
			};
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();

			p.WaitForExit();

			output = sbOutput.ToString();
			errorOutput = sbError.ToString();

			return p.ExitCode;
		}
	}
	public class ButtonDefinition
	{
		public string Text;
		public EventHandler OnClick;
		public ButtonDefinition(string Text, EventHandler OnClick)
		{
			this.Text = Text;
			this.OnClick = OnClick;
		}
	}
}
