using BPUtil.Forms;
using BPUtil.SimpleHttp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class AppInit
	{
		#region Windows Service
		/// <summary>
		/// <para>Call this from your Main() function, and it takes care of all initialization for a Windows Service app with the following features:</para>
		/// <list type="bullet">
		/// <item>Logs separated by month in <see cref="Globals.WritableDirectoryBase"/> + "Logs/"</item>
		/// <item><see cref="Environment.CurrentDirectory"/> set to <see cref="Globals.WritableDirectoryBase"/></item>
		/// <item>Unhandled exceptions logged.</item>
		/// <item>(Windows Only) Service Manager GUI with "Open Data Folder" button which opens <see cref="Globals.WritableDirectoryBase"/> in Explorer.</item>
		/// <item>(Windows Only) A temporary instance of the service is automatically started if the debugger is attached.</item>
		/// <item>If the service has a public static field named "settings" which inherits from SerializableObjectBase, that field will be instantiated if necessary, loaded, then saved if the settings file does not exist.</item>
		/// </list>
		/// <para>Notice that some assumptions are made about the architecture of the application.</para>
		/// <para>When running on linux, this class simply starts the service.</para>
		/// <para>You may initialize <see cref="Globals"/> with custom values, if desired, before calling this method.</para>
		/// </summary>
		/// <typeparam name="ServiceType">Type of Service class.</typeparam>
		/// <param name="options">Optional options for service initialization.</param>
		public static void WindowsService<ServiceType>(WindowsServiceInitOptions options = null)
#if NET6_0_LINUX
			where ServiceType : new()
#elif NETFRAMEWORK || NET6_0_WIN
			where ServiceType : ServiceBase, new()
#endif
		{
			if (options == null)
				options = new WindowsServiceInitOptions();
			Directory.CreateDirectory(Globals.WritableDirectoryBase);
			Directory.CreateDirectory(Globals.WritableDirectoryBase + "Logs/");
			Globals.OverrideErrorFilePath(() => Globals.WritableDirectoryBase + "Logs/" + Globals.AssemblyName + "_" + DateTime.Now.Year + "_" + DateTime.Now.Month.ToString().PadLeft(2, '0') + ".txt");
			Environment.CurrentDirectory = Globals.WritableDirectoryBase;

			if (options.LoggerCatchAll)
				Logger.CatchAll();
			HttpServer.EnableLoggingByDefault = true;

			ServiceType myService = new ServiceType();

			// Initialize the settings object, if the service has a public static field named "settings" that inherits from SerializableObjectBase.
			FieldInfo settingsField = myService.GetType().GetField("settings", BindingFlags.Static | BindingFlags.Public);
			SerializableObjectBase settingsObj = null;
			if (settingsField != null && settingsField.FieldType.IsSubclassOf(typeof(SerializableObjectBase)))
			{
				settingsObj = (SerializableObjectBase)settingsField.GetValue(null);
				if (settingsObj == null)
				{
					settingsObj = (SerializableObjectBase)Activator.CreateInstance(settingsField.FieldType);
					settingsField.SetValue(null, settingsObj);
				}
				if (settingsObj.FileExists() && !settingsObj.Load())
				{
					c.RedLine("Failed to load settings file.");
#if NETFRAMEWORK || NET6_0_WIN
					if (Environment.UserInteractive)
					{
						System.Windows.Forms.MessageBox.Show("Failed to load settings file.");
						Process.Start(settingsObj.GetDefaultFilePath());
					}
#endif
					throw new Exception("Failed to load settings file.");
				}
				settingsObj.SaveIfNoExist();
			}

#if NETFRAMEWORK || NET6_0_LINUX
			if (Platform.IsUnix() || Platform.IsRunningOnMono())
			{
				LinuxWindowsService(myService, options, settingsObj);
				return;
			}
#endif
#if NETFRAMEWORK || NET6_0_WIN
			string serviceName = !string.IsNullOrWhiteSpace(options.ServiceName) ? options.ServiceName : myService.ServiceName;
			if (Environment.UserInteractive)
			{
				string Title = serviceName + " " + Globals.AssemblyVersion + " Service Manager";

				List<ButtonDefinition> additionalButtons = new List<ButtonDefinition>();
				if (options.ServiceManagerButtons_OpenDataFolder)
					additionalButtons.Add(new ButtonDefinition("Open Data Folder", btnOpenDataFolder_Click));
				if (options.ServiceManagerButtons_UpdateSettingsFile && settingsObj != null)
					additionalButtons.Add(new ButtonDefinition("Update Settings File", (sender, ignored) =>
											{
												settingsObj.Save();
												ProcessRunner.Start(settingsObj.GetType().Name + ".cfg");
											}));
				if (options.ServiceManagerButtons != null)
				{
					foreach (ButtonDefinition btn in options.ServiceManagerButtons)
						additionalButtons.Add(btn);
				}

				bool didStart = false;
				if (options.RunForDebugging == true || (options.RunForDebugging == null && Debugger.IsAttached))
				{
					PrivateAccessor.CallMethod<ServiceType>(myService, "OnStart", new object[] { new string[0] });
					didStart = true;
				}

				try
				{
					System.Windows.Forms.Application.Run(new ServiceManager(Title, serviceName, additionalButtons.ToArray()));
				}
				finally
				{
					if (didStart)
						PrivateAccessor.CallMethod<ServiceType>(myService, "OnStop");
				}
			}
			else
			{
				ServiceBase.Run(myService);
			}
#endif
		}

		private static EConsole c = EConsole.I;

		/// <summary>
		/// Scriptable interface for service management on Linux.  Requires systemd for service install and uninstall.
		/// </summary>
		/// <typeparam name="ServiceType">Type of Service</typeparam>
		/// <param name="myService">Instance of Service</param>
		/// <param name="options">Options</param>
		/// <param name="settingsObj">Optional settings object.  If provided, a "savesettings" argument will be exposed.</param>
		private static void LinuxWindowsService<ServiceType>(ServiceType myService, WindowsServiceInitOptions options, SerializableObjectBase settingsObj)
#if NET6_0_LINUX
			where ServiceType : new()
#elif NETFRAMEWORK || NET6_0_WIN
			where ServiceType : ServiceBase, new()
#endif
		{
			string[] args = Environment.GetCommandLineArgs();
			string serviceName = !string.IsNullOrWhiteSpace(options.ServiceName) ? options.ServiceName : Globals.AssemblyName;

			string invocationId = Environment.GetEnvironmentVariable("INVOCATION_ID");
			if (invocationId != null && args.Length >= 1)
				args = new string[] { args[0], "svc" }; // This is running in systemd.  Force the "svc" argument.

			if (args.Length == 1)
			{
				Console.WriteLine();
				c.CyanLine(serviceName + " " + Globals.AssemblyVersion);

				Console.WriteLine();
				Console.WriteLine("Arguments:");
				ConsoleAppHelper.MaxCommandSize = 12;
				ConsoleAppHelper.WriteUsageCommand("svc", "Run the service interactively.");
				if (options.LinuxCommandLineInterface != null)
					ConsoleAppHelper.WriteUsageCommand("cmd", "Run the interactive command line interface.");
				ConsoleAppHelper.WriteUsageCommand("install", "Install as service using systemd.");
				ConsoleAppHelper.WriteUsageCommand("uninstall", "Uninstall as service using systemd.");
				ConsoleAppHelper.WriteUsageCommand("status", "Display the service status.");
				ConsoleAppHelper.WriteUsageCommand("start", "Start the service.");
				ConsoleAppHelper.WriteUsageCommand("stop", "Stop the service.");
				ConsoleAppHelper.WriteUsageCommand("restart", "Restart the service.");
				if (settingsObj != null)
					ConsoleAppHelper.WriteUsageCommand("savesettings", "Save a copy of the settings.");
				Console.WriteLine();
				c.Line("Data directory:").CyanLine("\t" + Globals.WritableDirectoryBase);
				Console.WriteLine();
			}
			else if (args.Length > 1 && args[1] == "svc")
			{
				PrivateAccessor.CallMethod<ServiceType>(myService, "OnStart", new object[] { new string[0] });
				try
				{
					if (invocationId != null)
					{
						c.CyanLine("Running " + serviceName + " " + Globals.AssemblyVersion + " in systemd service mode (INVOCATION_ID=" + invocationId + ")");
						EventWaitHandle ewhExitSignal = new EventWaitHandle(false, EventResetMode.ManualReset);
						AppDomain.CurrentDomain.ProcessExit += (sender, e) => { ewhExitSignal.Set(); };
						ewhExitSignal.WaitOne();
						c.CyanLine("Received SIGTERM. " + serviceName + " will now shut down.");
					}
					else
					{
						do
						{
							Console.WriteLine("Running " + serviceName + " " + Globals.AssemblyVersion + " in command-line service mode. Type \"exit\" to close.");
						}
						while (Console.ReadLine() != "exit");
					}
				}
				finally
				{
					PrivateAccessor.CallMethod<ServiceType>(myService, "OnStop");
				}
			}
			else if (args.Length > 1 && args[1] == "cmd")
			{
				if (options.LinuxCommandLineInterface != null)
					options.LinuxCommandLineInterface();
				else
					c.RedLine("This program does not implement an interactive command line interface.");
			}
			else if (args.Length > 1 && args[1] == "install")
			{
				InstallLinuxSystemdService(serviceName, options);
			}
			else if (args.Length > 1 && args[1] == "uninstall")
			{
				UninstallLinuxSystemdService(serviceName);
			}
			else if (args.Length > 1 && args[1] == "status")
			{
				StatusLinuxSystemdService(serviceName);
			}
			else if (args.Length > 1 && args[1] == "start")
			{
				StartLinuxSystemdService(serviceName);
			}
			else if (args.Length > 1 && args[1] == "stop")
			{
				StopLinuxSystemdService(serviceName);
			}
			else if (args.Length > 1 && args[1] == "restart")
			{
				RestartLinuxSystemdService(serviceName);
			}
			else if (args.Length > 1 && args[1] == "savesettings")
			{
				if (settingsObj != null)
				{
					settingsObj.Save();
					c.GreenLine("Saved settings file in " + Globals.WritableDirectoryBase);
				}
				else
					c.RedLine("This program was not configured to use a standard settings file.");
			}
			else
				c.RedLine("Unrecognized argument.");
		}

		/// <summary>
		/// Gets the name of the entry assembly in lower case.
		/// </summary>
		private static string DefaultLinuxSystemdServiceName
		{
			get
			{
				return Globals.AssemblyName.ToLower();
			}
		}
		/// <summary>
		/// Installs this application as a service in the systemd service manager.  Writes console output explaining the result.
		/// </summary>
		/// <param name="serviceName">Name of the service.  If not provided, the entry assembly's name will be converted to lower case and used as the name.</param>
		/// <param name="options">Optional options object so that an "LinuxOnInstall" callback can be called.</param>
		public static void InstallLinuxSystemdService(string serviceName = null, WindowsServiceInitOptions options = null)
		{
			GetServiceInfo(ref serviceName, out string servicePath);

			if (File.Exists(servicePath))
				c.CyanLine("Service was already installed. Service unit configuration file: " + servicePath);
			else
			{
				try
				{
#if NETFRAMEWORK
					// TODO: Implement ExecStart command via mono or mono-service. https://gist.github.com/bp2008/44ad5c81dca23010139fbdc2bc18f286#file-systemd-md
#endif
					string cfgFile = @"[Unit]
Description=" + serviceName + @" Service

[Service]
ExecStart=/usr/bin/dotnet """ + Assembly.GetEntryAssembly().Location + @""" svc
Restart=always
RestartSec=10
SyslogIdentifier=" + serviceName + @"
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target";
					cfgFile = StringUtil.LinuxLineBreaks(cfgFile);
					File.WriteAllText(servicePath, cfgFile, ByteUtil.Utf8NoBOM);

					int exitCode = RunSystemctlServiceCommand("enable", serviceName);

					if (exitCode == 0)
					{
						c.GreenLine("Install completed and service enabled for automatic startup. Service unit configuration file: " + servicePath);
					}
					else
					{
						c.RedLine("Install completed, but we were unable to enable the service. Service unit configuration file: " + servicePath);
					}

					c.Line("----------------------------");
					c.Line("Service management commands:");
					c.Yellow("sudo systemctl enable " + serviceName).Line("   # Enable service for automatic startup");
					c.Yellow("sudo systemctl disable " + serviceName).Line("  # Disable service for automatic startup");
					c.Yellow("sudo systemctl status " + serviceName).Line();
					c.Yellow("sudo systemctl start " + serviceName).Line();
					c.Yellow("sudo systemctl stop " + serviceName).Line();
					c.Yellow("sudo systemctl restart " + serviceName).Line();
					c.Line("----------------------------");

					options?.LinuxOnInstall?.Invoke();
				}
				catch (Exception ex)
				{
					c.RedLine("Failed to install. " + ex.ToHierarchicalString());
				}
			}
		}

		/// <summary>
		/// Uninstalls this application as a service in the systemd service manager.  Writes console output explaining the result.
		/// </summary>
		/// <param name="serviceName">Name of the service.  If not provided, the entry assembly's name will be converted to lower case and used as the name.</param>
		public static void UninstallLinuxSystemdService(string serviceName = null)
		{
			GetServiceInfo(ref serviceName, out string servicePath);
			if (File.Exists(servicePath))
			{
				try
				{
					File.Delete(servicePath);
					c.GreenLine("Uninstall completed. Service unit configuration file was removed: " + servicePath);

					int exitCode = RunSystemctlServiceCommand("disable", serviceName);

					if (exitCode == 0)
					{
						c.GreenLine("Service disabled.");
					}
					else
					{
						c.RedLine("Unable to disable the service.");
					}
				}
				catch (Exception ex)
				{
					c.RedLine("Failed to uninstall. " + ex.ToHierarchicalString());
				}
			}
			else
				c.YellowLine("Unable to uninstall. Service unit configuration file was not found: " + servicePath);
		}
		/// <summary>
		/// Writes to console the status of the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.  If not provided, the entry assembly's name will be converted to lower case and used as the name.</param>
		public static void StatusLinuxSystemdService(string serviceName = null)
		{
			GetServiceInfo(ref serviceName, out string servicePath);

			int exitCode = RunSystemctlServiceCommand("status", serviceName);

			if (exitCode != 0)
				c.RedLine("systemctl exited with code " + exitCode);
		}
		/// <summary>
		/// Starts the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.  If not provided, the entry assembly's name will be converted to lower case and used as the name.</param>
		public static void StartLinuxSystemdService(string serviceName = null)
		{
			GetServiceInfo(ref serviceName, out string servicePath);

			int exitCode = RunSystemctlServiceCommand("start", serviceName);

			if (exitCode != 0)
				c.RedLine("systemctl exited with code " + exitCode);
		}
		/// <summary>
		/// Stop the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.  If not provided, the entry assembly's name will be converted to lower case and used as the name.</param>
		public static void StopLinuxSystemdService(string serviceName = null)
		{
			GetServiceInfo(ref serviceName, out string servicePath);

			int exitCode = RunSystemctlServiceCommand("stop", serviceName);

			if (exitCode != 0)
				c.RedLine("systemctl exited with code " + exitCode);
		}
		/// <summary>
		/// Restarts the service in the systemd service manager.
		/// </summary>
		/// <param name="serviceName">Name of the service.  If not provided, the entry assembly's name will be converted to lower case and used as the name.</param>
		public static void RestartLinuxSystemdService(string serviceName = null)
		{
			GetServiceInfo(ref serviceName, out string servicePath);

			int exitCode = RunSystemctlServiceCommand("restart", serviceName);

			if (exitCode != 0)
				c.RedLine("systemctl exited with code " + exitCode);
		}
		/// <summary>
		/// Runs `systemctl daemon-reload`.  Call this after modifying an existing .service file to make systemd reload all .service files.  Afterward, you should probably restart the service that you modified.
		/// </summary>
		public static void RunSystemctlDaemonReload()
		{
			int exitCode = RunSystemctl("daemon-reload");

			if (exitCode != 0)
				c.RedLine("systemctl exited with code " + exitCode);
		}
		private static int RunSystemctlServiceCommand(string command, string serviceName)
		{
			return RunSystemctl(command + " \"" + serviceName + "\"");
		}
		private static int RunSystemctl(string command)
		{
			c.YellowLine("Running systemctl " + command);
			bool bThreadAbort = false;
			int exitCode = ProcessRunner.RunProcessAndWait("systemctl", command, out string std, out string err, ref bThreadAbort);

			if (!string.IsNullOrWhiteSpace(std))
				c.GreenLine(std);
			if (!string.IsNullOrWhiteSpace(err))
				c.RedLine(err);

			return exitCode;
		}
		private static void GetServiceInfo(ref string serviceName, out string servicePath)
		{
			if (string.IsNullOrWhiteSpace(serviceName))
				serviceName = DefaultLinuxSystemdServiceName;
			serviceName = StringUtil.MakeSafeForFileName(serviceName);
			servicePath = "/etc/systemd/system/" + serviceName + ".service";
		}

		private static void btnOpenDataFolder_Click(object sender, EventArgs e)
		{
			ProcessRunner.Start(Globals.WritableDirectoryBase);
		}
		/// <summary>
		/// Adds, updates, or deletes an environment variable for a Windows Service.
		/// </summary>
		/// <param name="serviceName">Windows service name</param>
		/// <param name="variableName">Environment variable name (case sensitive)</param>
		/// <param name="variableValue">Environment variable value.  Null to remove the environment variable.</param>
		public static void SetEnvironmentVariableInWindowsService(string serviceName, string variableName, string variableValue)
		{
#if NETFRAMEWORK || NET6_0_WIN
			if (string.IsNullOrWhiteSpace(serviceName))
				throw new ArgumentException(nameof(serviceName) + " is invalid", nameof(serviceName));
			if (string.IsNullOrWhiteSpace(variableName) || variableName.Contains('='))
				throw new ArgumentException(nameof(variableName) + " is invalid", nameof(variableName));

			string keyPath = @"SYSTEM\CurrentControlSet\Services\" + serviceName;

			// Open the registry key
			using (RegistryKey key = Registry.LocalMachine.CreateSubKey(keyPath))
			{
				string[] existingVars = (string[])key.GetValue("Environment");

				bool didFind = false;
				List<string> newVars = new List<string>();
				if (existingVars != null)
					foreach (string ev in existingVars)
					{
						int idxEquals = ev.IndexOf('=');
						if (idxEquals > -1)
						{
							string k = ev.Substring(0, idxEquals);
							string v;
							if (k == variableName)
							{
								didFind = true;
								if (variableValue == null)
									continue;
								v = variableValue;
								variableValue = null;
							}
							else
								v = ev.Substring(idxEquals + 1);
							newVars.Add(k + "=" + v);
						}
					}
				if (!didFind && variableValue != null)
					newVars.Add(variableName + "=" + variableValue);
				if (newVars.Count == 0)
					key.DeleteValue("Environment", false);
				else
					key.SetValue("Environment", newVars.ToArray());
			}
#else
			throw new PlatformNotSupportedException();
#endif
		}
		/// <summary>
		/// Adds, updates, or deletes an environment variable in a systemd .service file.
		/// </summary>
		/// <param name="serviceName">Systemd service name..</param>
		/// <param name="envVarName">Environment variable name (case sensitive)</param>
		/// <param name="envVarValue">Environment variable value.  Null to remove the environment variable.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">If the .service file is not found.</exception>
		public static Task SetEnvironmentVariableInSystemdServiceFileAsync(string serviceName, string envVarName, string envVarValue, CancellationToken cancellationToken = default)
		{
			return SetPropertySystemdServiceFileAsync(serviceName, "Service", "Environment=" + envVarName, envVarValue, cancellationToken);
		}
		/// <summary>
		/// Adds, updates, or deletes the MemoryMax property in a systemd .service file.
		/// </summary>
		/// <param name="serviceName">Systemd service name.</param>
		/// <param name="memoryMaxMiB">Max number of megabytes the process is allowed to have reserved.  If memory usage exceeds this limit, the service will be killed and restarted.  Set null to delete the memory limit.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">If the .service file is not found.</exception>
		public static Task SetMemoryMaxInSystemdServiceFileAsync(string serviceName, uint? memoryMaxMiB, CancellationToken cancellationToken = default)
		{
			return SetPropertySystemdServiceFileAsync(serviceName, "Service", "MemoryMax", memoryMaxMiB.HasValue ? (memoryMaxMiB.Value + "M") : null, cancellationToken);
		}
		/// <summary>
		/// Adds, updates, or deletes the specified property in a systemd .service file.
		/// </summary>
		/// <param name="serviceName">Systemd service name.</param>
		/// <param name="sectionName">Name of the section (case sensitive) in the .service file where the property should be added if it did not already exist, e.g. "Unit", "Service", "Install".  If the property is found in a different section, it will be updated or deleted in the section where it was found.</param>
		/// <param name="propertyName">Property name (case sensitive)</param>
		/// <param name="propertyValue">Property value.  Null to remove the property.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">If the .service file is not found.</exception>
		public static async Task SetPropertySystemdServiceFileAsync(string serviceName, string sectionName, string propertyName, string propertyValue, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(propertyName))
				throw new ArgumentException("propertyName cannot be null or whitespace", nameof(propertyName));

			GetServiceInfo(ref serviceName, out string servicePath);

			if (!File.Exists(servicePath))
				throw new FileNotFoundException("The service file '" + servicePath + "' does not exist.");

			string fileContent = await FileUtil.ReadAllTextAsync(servicePath, ByteUtil.Utf8NoBOM, cancellationToken).ConfigureAwait(false);

			Regex rx = new Regex(Regex.Escape(propertyName) + "=[^\\n]*\\n", RegexOptions.Singleline);
			Match m = rx.Match(fileContent);
			if (m.Success)
			{
				string newPropertyLine = propertyName + "=" + propertyValue + "\n";
				if (propertyValue == null)
					newPropertyLine = ""; // This is a delete operation, so replace the match with empty string.
				fileContent = fileContent.Substring(0, m.Index) + newPropertyLine + fileContent.Substring(m.Index + m.Length);
			}
			else
			{
				if (propertyValue == null)
					return; // This is a delete operation, and it already does not exist.
				string newPropertyLine = propertyName + "=" + propertyValue;
				string sectionHeader = "[" + sectionName + "]\n";
				int serviceIndex = fileContent.IndexOf(sectionHeader);
				if (serviceIndex != -1)
					fileContent = fileContent.Insert(serviceIndex + sectionHeader.Length, newPropertyLine + "\n");
				else
					fileContent += "\n" + sectionHeader + newPropertyLine + "\n";
			}

			await FileUtil.WriteAllTextAsync(servicePath, fileContent, ByteUtil.Utf8NoBOM, cancellationToken).ConfigureAwait(false);
		}
		/// <summary>
		/// Gets the value of the specified property in a systemd .service file. Returns null if the property is not found.
		/// </summary>
		/// <param name="serviceName">Systemd service name.</param>
		/// <param name="propertyName">Property name (case sensitive)</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>Gets the value of the specified property in a systemd .service file. Returns null if the property is not found.</returns>
		/// <exception cref="FileNotFoundException">If the .service file is not found.</exception>
		public static async Task<string> GetPropertySystemdServiceFileAsync(string serviceName, string propertyName, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(propertyName))
				throw new ArgumentException("propertyName cannot be null or whitespace", nameof(propertyName));

			GetServiceInfo(ref serviceName, out string servicePath);

			if (!File.Exists(servicePath))
				throw new FileNotFoundException("The service file '" + servicePath + "' does not exist.");

			string fileContent = await FileUtil.ReadAllTextAsync(servicePath, ByteUtil.Utf8NoBOM, cancellationToken).ConfigureAwait(false);

			Regex rx = new Regex(Regex.Escape(propertyName) + "=([^\\n]*)\\n", RegexOptions.Singleline);
			Match m = rx.Match(fileContent);
			if (m.Success)
				return m.Groups[1].Value;
			return null;
		}
		#endregion
	}
	/// <summary>
	/// Options for <see cref="AppInit.WindowsService{ServiceType}(WindowsServiceInitOptions)"/>.
	/// </summary>
	public class WindowsServiceInitOptions
	{
		/// <summary>
		/// Determines whether the service's OnStart() method will be called during GUI initialization (for debugging purposes). If null, this will be controlled by <see cref="Debugger.IsAttached"/>.  Useful in some development circumstances, such as when launching the application with a performance profiler.
		/// </summary>
		public bool? RunForDebugging = null;
		/// <summary>
		/// If true, the "Open Data Folder" button will be added to the service manager. True by default.
		/// </summary>
		public bool ServiceManagerButtons_OpenDataFolder = true;
		/// <summary>
		/// If true, the "Update Settings File" button may be added to the service manager if other conditions are met. True by default.
		/// </summary>
		public bool ServiceManagerButtons_UpdateSettingsFile = true;
		/// <summary>
		/// Buttons to add to the service manager.
		/// </summary>
		public IEnumerable<ButtonDefinition> ServiceManagerButtons = null;
		/// <summary>
		/// If not null or whitespace, this service name will override what was defined in the service's designer file.
		/// </summary>
		public string ServiceName = null;
		/// <summary>
		/// If true, <see cref="Logger.CatchAll()"/> will be called.
		/// </summary>
		public bool LoggerCatchAll = true;
		/// <summary>
		/// If provided, this action is called by AppInit and is expected to operate a command-line interface to the service.  The action is responsible for all console input and output and will be called when it is time for the service to run in command-line mode.  Once the action returns, the service is stopped and the program will exit.
		/// </summary>
		public Action LinuxCommandLineInterface;
		/// <summary>
		/// Callback to call when the service is installed on Linux via the "install" command.
		/// </summary>
		public Action LinuxOnInstall;
	}
}
