﻿using BPUtil.Forms;
using BPUtil.SimpleHttp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
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
			SimpleHttpLogger.RegisterLogger(Logger.httpLogger, false);

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
				settingsObj.Load();
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
				if (Debugger.IsAttached || options.RunForDebugging)
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
				InstallLinuxSystemdService(serviceName);
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
		public static void InstallLinuxSystemdService(string serviceName = null)
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

					int exitCode = RunSystemctl("enable", serviceName);

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

					int exitCode = RunSystemctl("disable", serviceName);

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

			int exitCode = RunSystemctl("status", serviceName);

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

			int exitCode = RunSystemctl("start", serviceName);

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

			int exitCode = RunSystemctl("stop", serviceName);

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

			int exitCode = RunSystemctl("restart", serviceName);

			if (exitCode != 0)
				c.RedLine("systemctl exited with code " + exitCode);
		}
		private static int RunSystemctl(string command, string serviceName)
		{
			c.YellowLine("Running systemctl " + command + " \"" + serviceName + "\"");
			bool bThreadAbort = false;
			int exitCode = ProcessRunner.RunProcessAndWait("systemctl", command + " \"" + serviceName + "\"", out string std, out string err, ref bThreadAbort);

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
		#endregion
	}
	/// <summary>
	/// Options for <see cref="AppInit.WindowsService{ServiceType}(WindowsServiceInitOptions)"/>.
	/// </summary>
	public class WindowsServiceInitOptions
	{
		/// <summary>
		/// If true, the service's OnStart() method will be called even if <see cref="Debugger.IsAttached"/> is false.  Useful in some development circumstances, such as when launching the application with a performance profiler.
		/// </summary>
		public bool RunForDebugging = false;
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
	}
}
