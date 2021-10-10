using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BPUtil.NativeWin
{
	/// <summary>
	/// Provides static methods for determining if the current process is elevated and for starting processes as administrator (elevated).
	/// </summary>
	public static class Admin
	{
		/// <summary>
		/// Returns true if the current process is elevated (running as administrator).
		/// </summary>
		/// <returns></returns>
		public static bool IsRunningAsAdmin()
		{
			WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			return pricipal.IsInRole(WindowsBuiltInRole.Administrator);
		}
		/// <summary>
		/// Attempts to start another instance of the current process as administrator, probably showing a UAC prompt. Returns true if the process starts successfully.
		/// </summary>
		/// <param name="args">Optional arguments to pass to the process.</param>
		/// <returns></returns>
		public static bool StartSelfAsAdmin(string args = null)
		{
			return StartAsAdmin(Application.ExecutablePath, args, System.IO.Directory.GetCurrentDirectory());
		}
		/// <summary>
		/// Attempts to start a process as administrator, probably showing a UAC prompt. Returns true if the process starts successfully.
		/// </summary>
		/// <param name="filePath">Path to the executable.</param>
		/// <param name="args">Optional arguments to pass to the process.</param>
		/// <returns></returns>
		public static bool StartAsAdmin(string filePath, string args = null, string workingDirectory = null)
		{
			ProcessStartInfo processInfo = new ProcessStartInfo();
			processInfo.Verb = "runas";
			processInfo.FileName = filePath;
			if (!string.IsNullOrWhiteSpace(workingDirectory))
				processInfo.WorkingDirectory = workingDirectory;
			if (!string.IsNullOrEmpty(args))
				processInfo.Arguments = args;
			try
			{
				Process.Start(processInfo);
				return true;
			}
			catch (Win32Exception)
			{
				// Do nothing. Probably the user canceled the UAC window
			}
			return false;
		}
	}
}
