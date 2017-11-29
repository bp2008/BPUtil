using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace BPUtil
{
	public static class Globals
	{
		public static string jQueryPath = "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js";
		public static string jQueryUIJsPath = "//ajax.googleapis.com/ajax/libs/jqueryui/1.10.4/jquery-ui.min.js";
		public static string jQueryUICssPath = "//ajax.googleapis.com/ajax/libs/jqueryui/1.10.4/themes/smoothness/jquery-ui.css";
		static Globals()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
			ServicePointManager.Expect100Continue = false;
			ServicePointManager.DefaultConnectionLimit = int.MaxValue;
			try
			{
				Initialize(System.Reflection.Assembly.GetEntryAssembly().Location);
			}
			catch { }
		}
		/// <summary>
		/// Call this to initialize global static variables.
		/// </summary>
		/// <param name="exePath">Pass in the path to the exe in the root directory of the application.  The directory must exist, but the exe name can just be a descriptive exe file name like "My Application.exe" and does not need to exist.</param>
		/// <param name="writablePath">A string to be appended to ApplicationDirectoryBase to create WritableDirectoryBase.  Example: "" or "writable/" or "somedir/writable/"</param>
		public static void Initialize(string exePath, string writablePath = "")
		{
			executablePath = exePath.Replace('\\', '/');
			FileInfo fiExe;
			try
			{
				fiExe = new FileInfo(executablePath);
			}
			catch
			{
				fiExe = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
			}
			executableNameWithExtension = fiExe.Name.Replace('\\', '/');
			executableNameWithoutExtension = executableNameWithExtension.Substring(0, executableNameWithExtension.Length - fiExe.Extension.Length);
			applicationRoot = fiExe.Directory.FullName.TrimEnd('\\', '/').Replace('\\', '/');
			applicationDirectoryBase = applicationRoot + "/";
			writableDirectoryBase = applicationDirectoryBase + writablePath.TrimStart('\\', '/').Replace('\\', '/');
			configFilePath = writableDirectoryBase + "Config.cfg";
			errorFilePath = writableDirectoryBase + executableNameWithoutExtension + "Errors.txt";
		}
		private static string executablePath;
		private static string executableNameWithExtension;
		/// <summary>
		/// Gets the name of the executable file, including the extension.  e.g. "MyProgram.exe" => "MyProgram.exe"
		/// </summary>
		public static string ExecutableNameWithExtension
		{
			get { return executableNameWithExtension; }
		}
		private static string executableNameWithoutExtension;
		/// <summary>
		/// Gets the name of the executable file, NOT including the extension.  e.g. "MyProgram.exe" => "MyProgram"
		/// </summary>
		public static string ExecutableNameWithoutExtension
		{
			get { return executableNameWithoutExtension; }
		}
		private static string applicationRoot;
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Does not have trailing '/'.
		/// </summary>
		public static string ApplicationRoot
		{
			get { return applicationRoot; }
		}
		private static string applicationDirectoryBase;
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Includes trailing '/'.
		/// </summary>
		public static string ApplicationDirectoryBase
		{
			get { return applicationDirectoryBase; }
		}
		private static string writableDirectoryBase;

		/// <summary>
		/// Gets the full path to a persistent directory where the application can write to.  Includes trailing '/'.
		/// </summary>
		public static string WritableDirectoryBase
		{
			get { return writableDirectoryBase; }
		}
		private static string errorFilePath;
		/// <summary>
		/// Gets the full path to the error log file.  Includes trailing '/'.
		/// </summary>
		public static string ErrorFilePath
		{
			get { return errorFilePath; }
		}
		private static string configFilePath;
		/// <summary>
		/// Gets the full path to the config file.
		/// </summary>
		public static string ConfigFilePath
		{
			get { return configFilePath; }
		}
		/// <summary>
		/// The BPUtil version number, not to be confused with the version number of the application this is included in.
		/// </summary>
		public static string Version = "0.4";
	}
}
