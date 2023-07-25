using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

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
				Assembly assembly = Assembly.GetEntryAssembly();
				InitializeProgram(assembly.Location, assembly.GetName().Name);
			}
			catch { }
		}
		/// <summary>
		/// This method does nothing, but allows initialization to occur via the static constructor, where [exePath] is determined by <see cref="Assembly.GetEntryAssembly"/>, and WritableDirectoryBase is a subdirectory of <see cref="Environment.SpecialFolder.CommonApplicationData"/>.  The directory pointed at by WritableDirectoryBase will not be created automatically, and the current working directory will not be changed.
		/// </summary>
		public static void Initialize()
		{
		}
		/// <summary>
		/// Call this to initialize global static variables where the "WritableDirectoryBase" property is the parent folder of the exe.
		/// </summary>
		/// <param name="exePath">
		/// <para>Pass in the path to the exe in the root directory of the application. (if null/whitespace, then System.Windows.Forms.Application.ExecutablePath is used).</para>
		/// <para>The directory must exist, but the exe name can just be a descriptive exe file name like "My Application.exe" and does not need to exist.</para>
		/// <para>The exe name is used to create the CommonApplicationDataBase string.</para>
		/// </param>
		/// <param name="writablePath">A string to be appended to ApplicationDirectoryBase to form WritableDirectoryBase.  Example: "" or "writable/" or "somedir/writable/"</param>
		public static void Initialize(string exePath, string writablePath = "")
		{
			FileInfo fiExe = null;
			if (!string.IsNullOrWhiteSpace(exePath))
			{
				try
				{
					fiExe = new FileInfo(exePath.Replace('\\', '/'));
				}
				catch { }
			}
			if (fiExe == null)
				fiExe = new FileInfo(Assembly.GetEntryAssembly().Location);
			executableNameWithExtension = fiExe.Name.Replace('\\', '/');
			executableNameWithoutExtension = executableNameWithExtension.Substring(0, executableNameWithExtension.Length - fiExe.Extension.Length);
			applicationRoot = fiExe.Directory.FullName.TrimEnd('\\', '/').Replace('\\', '/');
			applicationDirectoryBase = applicationRoot + "/";
			writableDirectoryBase = applicationDirectoryBase + writablePath.Trim('\\', '/').Replace('\\', '/') + '/';
			configFilePath = writableDirectoryBase + "Config.cfg";
			errorFilePath = writableDirectoryBase + executableNameWithoutExtension + "Errors.txt";
		}
		/// <summary>
		/// Call this to initialize global static variables where the "WritableDirectoryBase" path is a subfolder of <see cref="Environment.SpecialFolder.CommonApplicationData"/>.
		/// </summary>
		/// <param name="exePath">
		/// <para>Optionally pass in the path to the exe in the root directory of the application. (if null/whitespace, then System.Windows.Forms.Application.ExecutablePath is used).</para>
		/// <para>The directory must exist, but the exe name can just be a descriptive exe file name like "My Application.exe" and does not need to exist.</para>
		/// <para>The exe name is used in the error file name and exposed in <see cref="ExecutableNameWithExtension"/> and <see cref="ExecutableNameWithoutExtension"/> properties.</para>
		/// </param>
		/// <param name="programName">
		/// <para>A globally unique program name that does not change and is unlikely to collide with other programs on the user's system.</para>
		/// <para>This defines the subfolder(s) of CommonApplicationData where this app's WritableDirectoryBase will be located.</para>
		/// <para>So you could pass in "MyApp" or to be even safer, "MyCompany/MyApp".</para>
		/// </param>
		/// <param name="CreateWritableDir">If true, the directory defined by WritableDirectoryBase will be created if needed.</param>
		public static void InitializeProgram(string exePath, string programName, bool CreateWritableDir = false)
		{
			Initialize(exePath);

			writableDirectoryBase = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			writableDirectoryBase = writableDirectoryBase.TrimEnd('\\', '/').Replace('\\', '/') + '/' + programName + '/';
			if (CreateWritableDir)
				Directory.CreateDirectory(writableDirectoryBase);
		}

		/// <summary>
		/// Call this after Globals initialization to change the writable directory path. The specified folder will be created if it does not already exist.
		/// </summary>
		/// <param name="writableDirectoryAbsolutePath">Absolute path for the writable directory. E.g. @"C:\MyApp\Data" or "/home/user/MyApp/Data"</param>
		public static void SetWritableDirectory(string writableDirectoryAbsolutePath)
		{
			DirectoryInfo diWritable = new DirectoryInfo(writableDirectoryAbsolutePath);
			if (!diWritable.Exists)
				diWritable = Directory.CreateDirectory(diWritable.FullName);
			writableDirectoryBase = diWritable.FullName.TrimEnd('\\', '/').Replace('\\', '/') + '/';
		}

		private static string executableNameWithExtension;
		/// <summary>
		/// Gets the name of the executable file, including the extension.  e.g. "MyProgram.exe"
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
		/// Gets the full path to the error log file.
		/// </summary>
		public static string ErrorFilePath
		{
			get
			{
				if (GetErrorFilePath != null)
					return errorFilePath = GetErrorFilePath();
				return errorFilePath;
			}
		}
		/// <summary>
		/// If specified, this function overrides <see cref="errorFilePath"/>.
		/// </summary>
		private static Func<string> GetErrorFilePath = null;
		/// <summary>
		/// Sets a function that will be called when getting <see cref="ErrorFilePath"/>.
		/// </summary>
		/// <param name="newPathFn">A function which returns the path to the log file. E.g. () => { return "C:/MyApp/MyErrorFile.txt"; }</param>
		public static void OverrideErrorFilePath(Func<string> newPathFn)
		{
			GetErrorFilePath = newPathFn;
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
		/// The BPUtil version number, not to be confused with the version number of the application this is included in.  This version number is often neglected.
		/// </summary>
		public static string Version = "0.8";

		/// <summary>
		/// Gets the GUID of the entry assembly.
		/// </summary>
		public static string AssemblyGuid
		{
			get
			{
				GuidAttribute attr = Assembly.GetEntryAssembly().GetCustomAttributes<GuidAttribute>().FirstOrDefault();
				if (attr != null)
					return attr.Value;
				return "";
			}
		}

		/// <summary>
		/// Gets the title of the entry assembly.
		/// </summary>
		public static string AssemblyTitle
		{
			get
			{
				AssemblyTitleAttribute attr = Assembly.GetEntryAssembly().GetCustomAttributes<AssemblyTitleAttribute>().FirstOrDefault();
				if (attr != null && !string.IsNullOrWhiteSpace(attr.Title))
					return attr.Title;
				return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
			}
		}

		/// <summary>
		/// Gets the name of the entry assembly.
		/// </summary>
		public static string AssemblyName
		{
			get
			{
				return Assembly.GetEntryAssembly().GetName().Name;
			}
		}

		/// <summary>
		/// Gets the version of the entry assembly.
		/// </summary>
		public static string AssemblyVersion
		{
			get
			{
				return Assembly.GetEntryAssembly().GetName().Version.ToString();
			}
		}
	}
}
