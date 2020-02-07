using BPUtil.Forms;
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
		/// <para>[Windows Apps Only] Call this from your Main() function, and it takes care of all initialization for a Windows Service app with the following features:</para>
		/// <list type="bullet">
		/// <item>Logs separated by month in <see cref="Globals.WritableDirectoryBase"/> + "Logs/"</item>
		/// <item><see cref="Environment.CurrentDirectory"/> set to <see cref="Globals.WritableDirectoryBase"/> + "Logs/"</item>
		/// <item>Unhandled exceptions logged.</item>
		/// <item>Service Manager GUI with "Open Data Folder" button which opens <see cref="Globals.WritableDirectoryBase"/> in Explorer.</item>
		/// <item>A temporary instance of the service is automatically started if the debugger is attached.</item>
		/// <item>If the service has a public static field named "settings" which inherits from SerializableObjectBase, that field will be instantiated if necessary, loaded, then saved if the settings file does not exist.</item>
		/// </list>
		/// <para>Notice that some assumptions are made about the architecture of the application.</para>
		/// </summary>
		/// <typeparam name="ServiceType">Type of Service class.</typeparam>
		/// <param name="options">Optional options for service initialization.</param>
		public static void WindowsService<ServiceType>(WindowsServiceInitOptions options = null) where ServiceType : ServiceBase, new()
		{
			Directory.CreateDirectory(Globals.WritableDirectoryBase);
			Directory.CreateDirectory(Globals.WritableDirectoryBase + "Logs/");
			Globals.OverrideErrorFilePath(() => Globals.WritableDirectoryBase + "Logs/" + Globals.AssemblyName + "_" + DateTime.Now.Year + "_" + DateTime.Now.Month + ".txt");
			Environment.CurrentDirectory = Globals.WritableDirectoryBase;

			Logger.CatchAll();

			ServiceType myService = new ServiceType();

			// Initialize the settings object, if the service has a public static field named "settings" that inherits from SerializableObjectBase.
			FieldInfo settingsField = myService.GetType().GetField("settings", BindingFlags.Static | BindingFlags.Public);
			if (settingsField != null && settingsField.FieldType.IsSubclassOf(typeof(SerializableObjectBase)))
			{
				SerializableObjectBase settingsObj = (SerializableObjectBase)settingsField.GetValue(null);
				if (settingsObj == null)
				{
					settingsObj = (SerializableObjectBase)Activator.CreateInstance(settingsField.FieldType);
					settingsField.SetValue(null, settingsObj);
				}
				settingsObj.Load();
				settingsObj.SaveIfNoExist();
			}

			if (Environment.UserInteractive)
			{
				string Title = myService.ServiceName + " " + Globals.AssemblyVersion + " Service Manager";

				ButtonDefinition[] additionalButtons = new ButtonDefinition[] {
					new ButtonDefinition("Open Data Folder", btnOpenDataFolder_Click)
				};

				bool didStart = false;
				if (Debugger.IsAttached)
				{
					PrivateAccessor.CallMethod<ServiceType>(myService, "OnStart", new object[] { new string[0] });
					didStart = true;
				}

				try
				{
					System.Windows.Forms.Application.Run(new ServiceManager(Title, myService.ServiceName, additionalButtons));

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

		private static void btnOpenDataFolder_Click(object sender, EventArgs e)
		{
			Process.Start(Globals.WritableDirectoryBase);
		}
		#endregion
	}
	/// <summary>
	/// Placeholder options for <see cref="AppInit.WindowsService{ServiceType}(WindowsServiceInitOptions)"/>.
	/// </summary>
	public class WindowsServiceInitOptions
	{
	}
}
