#if LINUX
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.Linux
{
	/// <summary>
	/// Offers service management functions for systemd services on Linux systems.
	/// </summary>
	public static class SystemdHelper
	{
		/// <summary>
		/// Creates the service definition file for a systemd service, but does not enable or start it.  Returns true if an existing service definition file was overwritten by this method (in this case it is advisable to call <see cref="DaemonReload"/>).  If the file cannot be accessed, an exception is thrown.
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		/// <param name="WorkingDirectory">Working directory to assign to the service process.</param>
		/// <param name="ExecStartCommand">The command to run to start the service.</param>
		/// <param name="logOutputToSystemdJournal">True to instruct systemd to redirect standard output and error streams to its own log files.</param>
		/// <returns>Returns true if an existing service definition file was overwritten by this method (in this case it is advisable to call <see cref="DaemonReload"/>).</returns>
		public static bool Install(string serviceName, string WorkingDirectory, string ExecStartCommand, bool logOutputToSystemdJournal)
		{
			GetServiceInfo(serviceName, out string servicePath);

			string systemdLoggingLines = "";
			if (logOutputToSystemdJournal)
			{
				systemdLoggingLines = @"
StandardOutput=journal
StandardError=journal
SyslogIdentifier=" + serviceName;
			}

			string cfgFile = @"[Unit]
Description=" + serviceName + @" Service

[Service]
ExecStart=" + ExecStartCommand + @"
WorkingDirectory=" + WorkingDirectory + @"
Restart=always
RestartSec=10
SyslogIdentifier=" + serviceName + @"
TimeoutStopSec=30" + systemdLoggingLines + @"

[Install]
WantedBy=multi-user.target
";
			cfgFile = StringUtil.LinuxLineBreaks(cfgFile);

			if (File.Exists(servicePath))
			{
				string existing = File.ReadAllText(servicePath, ByteUtil.Utf8NoBOM);
				if (existing != cfgFile)
				{
					File.WriteAllText(servicePath, cfgFile, ByteUtil.Utf8NoBOM);
					return true;
				}
			}
			else
			{
				File.WriteAllText(servicePath, cfgFile, ByteUtil.Utf8NoBOM);
			}
			return false;
		}
		/// <summary>
		/// Deletes the service definition file for a systemd service, returning true if successful, false if the file did not exist.  If the file cannot be deleted, an exception is thrown.  This operation is independent of stopping or disabling the service.
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		public static bool Uninstall(string serviceName)
		{
			GetServiceInfo(serviceName, out string servicePath);
			if (File.Exists(servicePath))
			{
				File.Delete(servicePath);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Enables the systemd service for automatic startup (but does not start the service).
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		/// <returns>The systemd command output.</returns>
		public static string Enable(string serviceName)
		{
			return RunSystemctlServiceCommand("enable", serviceName);
		}
		/// <summary>
		/// Disables the systemd service for automatic startup.
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		/// <returns>The systemd command output.</returns>
		public static string Disable(string serviceName)
		{
			return RunSystemctlServiceCommand("disable", serviceName);
		}
		/// <summary>
		/// Gets the status of the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		/// <returns>The systemd command output.</returns>
		public static string Status(string serviceName)
		{
			return RunSystemctlServiceCommand("status", serviceName);
		}
		/// <summary>
		/// Starts the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		/// <returns>The systemd command output.</returns>
		public static string Start(string serviceName)
		{
			return RunSystemctlServiceCommand("start", serviceName);
		}
		/// <summary>
		/// Stop the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		/// <returns>The systemd command output.</returns>
		public static string Stop(string serviceName)
		{
			return RunSystemctlServiceCommand("stop", serviceName);
		}
		/// <summary>
		/// Restarts the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.</param>
		/// <returns>The systemd command output.</returns>
		public static string Restart(string serviceName)
		{
			return RunSystemctlServiceCommand("restart", serviceName);
		}
		/// <summary>
		/// Runs <c>systemctl daemon-reload</c>.  Call this after modifying an existing <c>.service</c> file to make systemd reload all <c>.service</c> files.  Afterward, you should probably restart the service that you modified.
		/// </summary>
		/// <returns>The systemd command output.</returns>
		public static string DaemonReload()
		{
			return RunSystemctl("daemon-reload");
		}

		private static string RunSystemctlServiceCommand(string command, string serviceName)
		{
			GetServiceInfo(serviceName, out string servicePath);
			return RunSystemctl(command + " \"" + serviceName + "\"");
		}
		private static string RunSystemctl(string command)
		{
			StringBuilder sbOutput = new StringBuilder();
			int exitCode = ProcessRunner.RunProcessAndWait("systemctl", command, e => sbOutput.AppendLine(e.Line), e => sbOutput.AppendLine(e.Line));

			string output = StringUtil.LinuxLineBreaks(sbOutput.ToString().Trim());
			if (exitCode != 0)
			{
				if (!string.IsNullOrWhiteSpace(output))
					throw new SystemdErrorException("systemctl exited with code " + exitCode + " and had output:" + Environment.NewLine + output);
				throw new SystemdErrorException("systemctl exited with code " + exitCode);
			}

			return output;
		}

		/// <summary>
		/// Validates the service name and constructs the service definition file path.
		/// </summary>
		/// <param name="serviceName">Service name to validate.</param>
		/// <param name="servicePath">(Output) path of the service definition file.</param>
		/// <exception cref="ArgumentException"></exception>
		private static void GetServiceInfo(string serviceName, out string servicePath)
		{
			if (!StringUtil.IsValidSystemdServiceName(serviceName))
				throw new ArgumentException("Service name is not a valid systemd service name. Name must be 1-247 valid characters.  Valid characters are ASCII letters, digits, \":\", \"-\", \"_\", \".\", and \"\\\"", nameof(serviceName));
			servicePath = "/etc/systemd/system/" + serviceName + ".service";
		}
		/// <summary>
		/// Prints systemd service management commands to the console.
		/// </summary>
		/// <param name="sudo">True to print the commands using "sudo".</param>
		/// <param name="serviceName">Name of the service.</param>
		public static void PrintServiceManagementCommands(bool sudo, string serviceName)
		{
			GetServiceInfo(serviceName, out string servicePath);

			string ss = sudo ? "sudo " : "";

			EConsole c = EConsole.I;
			c.Line("----------------------------");
			c.Line("Service management commands:");
			c.Yellow(ss + "systemctl enable " + serviceName).Line("   # Enable service for automatic startup");
			c.Yellow(ss + "systemctl disable " + serviceName).Line("  # Disable service for automatic startup");
			c.Yellow(ss + "systemctl status " + serviceName).Line();
			c.Yellow(ss + "systemctl start " + serviceName).Line();
			c.Yellow(ss + "systemctl stop " + serviceName).Line();
			c.Yellow(ss + "systemctl restart " + serviceName).Line();
			c.Line("----------------------------");
		}
	}
}
#endif