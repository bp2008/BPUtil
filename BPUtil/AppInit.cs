using BPUtil.Forms;
using BPUtil.SimpleHttp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
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
		/// <item><see cref="Environment.CurrentDirectory"/> set to <see cref="Globals.WritableDirectoryBase"/> + "Logs/"</item>
		/// <item>Unhandled exceptions logged.</item>
		/// <item>(Windows Only) Service Manager GUI with "Open Data Folder" button which opens <see cref="Globals.WritableDirectoryBase"/> in Explorer.</item>
		/// <item>(Windows Only) A temporary instance of the service is automatically started if the debugger is attached.</item>
		/// <item>If the service has a public static field named "settings" which inherits from SerializableObjectBase, that field will be instantiated if necessary, loaded, then saved if the settings file does not exist.</item>
		/// </list>
		/// <para>Notice that some assumptions are made about the architecture of the application.</para>
		/// <para>When running on linux, this class simply starts the service.</para>
		/// </summary>
		/// <typeparam name="ServiceType">Type of Service class.</typeparam>
		/// <param name="options">Optional options for service initialization.</param>
		public static void WindowsService<ServiceType>(WindowsServiceInitOptions options = null) where ServiceType : ServiceBase, new()
		{
			if (options == null)
				options = new WindowsServiceInitOptions();
			Directory.CreateDirectory(Globals.WritableDirectoryBase);
			Directory.CreateDirectory(Globals.WritableDirectoryBase + "Logs/");
			Globals.OverrideErrorFilePath(() => Globals.WritableDirectoryBase + "Logs/" + Globals.AssemblyName + "_" + DateTime.Now.Year + "_" + DateTime.Now.Month.ToString().PadLeft(2, '0') + ".txt");
			Environment.CurrentDirectory = Globals.WritableDirectoryBase;

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

			if (Platform.IsUnix() || Platform.IsRunningOnMono())
			{
				LinuxWindowsService(myService, options, settingsObj);
				return;
			}
			if (Environment.UserInteractive)
			{
				string Title = myService.ServiceName + " " + Globals.AssemblyVersion + " Service Manager";

				List<ButtonDefinition> additionalButtons = new List<ButtonDefinition>();
				if (options.ServiceManagerButtons_OpenDataFolder)
					additionalButtons.Add(new ButtonDefinition("Open Data Folder", btnOpenDataFolder_Click));
				if (options.ServiceManagerButtons_UpdateSettingsFile && settingsObj != null)
					additionalButtons.Add(new ButtonDefinition("Update Settings File", (sender, ignored) =>
											{
												settingsObj.Save();
												Process.Start(settingsObj.GetType().Name + ".cfg");
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
					System.Windows.Forms.Application.Run(new ServiceManager(Title, myService.ServiceName, additionalButtons.ToArray()));

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
		}

		/// <summary>
		/// Takes over where <see cref="WindowsService{ServiceType}(WindowsServiceInitOptions)"/> left off.
		/// </summary>
		/// <typeparam name="ServiceType"></typeparam>
		/// <param name="myService"></param>
		/// <param name="options"></param>
		/// <param name="settingsObj"></param>
		private static void LinuxWindowsService<ServiceType>(ServiceType myService, WindowsServiceInitOptions options, SerializableObjectBase settingsObj) where ServiceType : ServiceBase, new()
		{
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length == 1)
			{
				Console.WriteLine(myService.ServiceName + " " + Globals.AssemblyVersion);
				Console.WriteLine("To run as a Windows Service: " + args[0] + " run");
				Console.WriteLine("To run as a command line app: " + args[0] + " cmd");
				if (settingsObj != null)
					Console.WriteLine("To update the settings file: " + args[0] + " savesettings");
				Console.WriteLine("Data directory: " + Globals.WritableDirectoryBase);
			}
			else if (args.Length > 1 && args[1] == "run")
			{
				ServiceBase.Run(myService);
			}
			else if (args.Length > 1 && args[1] == "cmd")
			{
				PrivateAccessor.CallMethod<ServiceType>(myService, "OnStart", new object[] { new string[0] });
				try
				{
					do
					{
						Console.WriteLine("Running " + myService.ServiceName + " " + Globals.AssemblyVersion + " in command-line mode. Type \"exit\" to close.");
					}
					while (Console.ReadLine() != "exit");
				}
				finally
				{
					PrivateAccessor.CallMethod<ServiceType>(myService, "OnStop");
				}
			}
			else if (args.Length > 1 && args[1] == "savesettings")
			{
				if (settingsObj != null)
				{
					settingsObj.Save();
					Console.WriteLine("Saved settings file in " + Globals.WritableDirectoryBase);
				}
				else
					Console.WriteLine("This program was not configured to use a standard settings file.");
			}
		}

		private static void btnOpenDataFolder_Click(object sender, EventArgs e)
		{
			Process.Start(Globals.WritableDirectoryBase);
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
		/// If true, the "Open Data Folder" button will be added to the service manager.
		/// </summary>
		public bool ServiceManagerButtons_OpenDataFolder = true;
		/// <summary>
		/// If true, the "Update Settings File" button may be added to the service manager if other conditions are met.
		/// </summary>
		public bool ServiceManagerButtons_UpdateSettingsFile = true;
		/// <summary>
		/// Buttons to add to the service manager.
		/// </summary>
		public IEnumerable<ButtonDefinition> ServiceManagerButtons = null;
	}
}
