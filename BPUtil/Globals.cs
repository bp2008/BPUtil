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
	/// <summary>
	/// Offers app-global strings.
	/// </summary>
	public static class Globals
	{
		[Obsolete("These static strings do not belong in BPUtil and should be copied into whatever app uses them.", true)]
		public static string jQueryPath = "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js";
		[Obsolete("These static strings do not belong in BPUtil and should be copied into whatever app uses them.", true)]
		public static string jQueryUIJsPath = "//ajax.googleapis.com/ajax/libs/jqueryui/1.10.4/jquery-ui.min.js";
		[Obsolete("These static strings do not belong in BPUtil and should be copied into whatever app uses them.", true)]
		public static string jQueryUICssPath = "//ajax.googleapis.com/ajax/libs/jqueryui/1.10.4/themes/smoothness/jquery-ui.css";
		static Globals()
		{
#if !NET10_0_OR_GREATER
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
			ServicePointManager.Expect100Continue = false;
			ServicePointManager.DefaultConnectionLimit = int.MaxValue;
#endif
			try
			{
				InitializeCommonApplicationData();
			}
			catch { }
		}
		/// <summary>
		/// <para>Initializes Globals with WritableDirectoryBase assigned to a folder in <see cref="Environment.SpecialFolder.CommonApplicationData"/> (e.g. <c>C:/ProgramData/</c>.</para>
		/// <para>This is the default globals initialization method which is run by the static constructor.  Call it again yourself if you want to use non-default arguments.</para>
		/// </summary>
		/// <param name="programName">(Optional) The name of the folder to create inside <c>CommonApplicationData</c>. If null, the executable name without its extension is used.</param>
		/// <param name="CreateWritableDir">True to automatically create the folder at <see cref="WritableDirectoryBase"/> if it does not already exist during this initialization.</param>
		public static void InitializeCommonApplicationData(string programName = null, bool CreateWritableDir = false)
		{
			InitializeShared(programName, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), CreateWritableDir);
		}
		/// <summary>
		/// <para>Initializes Globals with WritableDirectoryBase assigned to a folder in <see cref="Environment.SpecialFolder.ApplicationData"/> (e.g. <c>C:/Users/Username/AppData/Roaming/</c>.</para>
		/// </summary>
		/// <param name="programName">(Optional) The name of the folder to create inside <c>CommonApplicationData</c>. If null, the executable name without its extension is used.</param>
		/// <param name="CreateWritableDir">True to automatically create the folder at <see cref="WritableDirectoryBase"/> if it does not already exist during this initialization.</param>
		public static void InitializeApplicationData(string programName = null, bool CreateWritableDir = false)
		{
			InitializeShared(programName, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CreateWritableDir);
		}
		private static void InitializeShared(string programName, string writeableDirPath, bool CreateWritableDir)
		{
			FileInfo fiExe = new FileInfo(EntryAssemblyLocation);
			ExecutableNameWithExtension = fiExe.Name.Replace('\\', '/');
			ExecutableNameWithoutExtension = ExecutableNameWithExtension.Substring(0, ExecutableNameWithExtension.Length - fiExe.Extension.Length);
			ApplicationRoot = fiExe.Directory.FullName.TrimEnd('\\', '/').Replace('\\', '/');
			ApplicationDirectoryBase = ApplicationRoot + "/";

			if (!string.IsNullOrWhiteSpace(programName))
				programName = StringUtil.MakeSafeForFileName(programName);
			if (string.IsNullOrWhiteSpace(programName))
				programName = fiExe.NameWithoutExtension();
			WritableDirectoryBase = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			WritableDirectoryBase = WritableDirectoryBase.TrimEnd('\\', '/').Replace('\\', '/') + '/' + programName + '/';
			if (CreateWritableDir)
				Directory.CreateDirectory(WritableDirectoryBase);
		}
		/// <summary>
		/// This method does nothing, but allows initialization to occur via the static constructor, where [exePath] is determined by <see cref="Assembly.GetEntryAssembly"/>, and WritableDirectoryBase is a subdirectory of <see cref="Environment.SpecialFolder.CommonApplicationData"/>.  The directory pointed at by WritableDirectoryBase will not be created automatically, and the current working directory will not be changed.
		/// </summary>
		[Obsolete("Globals.Initialize is obsolete. Use Globals.InitializeCommonApplicationData or Globals.InitializeApplicationData instead.", true)]
		public static void Initialize()
		{
		}
		/// <summary>
		/// Call this to initialize global static variables where the "WritableDirectoryBase" property is the parent folder of the exe.
		/// </summary>
		/// <param name="exePath">
		/// <para>Pass in the path to the exe in the root directory of the application. (if null/whitespace, then <see cref="EntryAssemblyLocation"/> is used).</para>
		/// <para>The directory must exist, but the exe name can just be a descriptive exe file name like "My Application.exe" and does not need to exist.</para>
		/// <para>The exe name is used to create the CommonApplicationDataBase string.</para>
		/// </param>
		/// <param name="writablePath">A string to be appended to ApplicationDirectoryBase to form WritableDirectoryBase.  Example: "" or "writable/" or "somedir/writable/"</param>
		[Obsolete("Globals.Initialize is obsolete. Use Globals.InitializeCommonApplicationData or Globals.InitializeApplicationData instead.", true)]
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
				fiExe = new FileInfo(EntryAssemblyLocation);
			ExecutableNameWithExtension = fiExe.Name.Replace('\\', '/');
			ExecutableNameWithoutExtension = ExecutableNameWithExtension.Substring(0, ExecutableNameWithExtension.Length - fiExe.Extension.Length);
			ApplicationRoot = fiExe.Directory.FullName.TrimEnd('\\', '/').Replace('\\', '/');
			ApplicationDirectoryBase = ApplicationRoot + "/";
			WritableDirectoryBase = ApplicationDirectoryBase + writablePath.Trim('\\', '/').Replace('\\', '/') + '/';
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
		[Obsolete("Globals.InitializeProgram is obsolete. Use Globals.InitializeCommonApplicationData or Globals.InitializeApplicationData instead.", true)]
		public static void InitializeProgram(string exePath, string programName, bool CreateWritableDir = false)
		{
			Initialize(exePath);

			WritableDirectoryBase = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			WritableDirectoryBase = WritableDirectoryBase.TrimEnd('\\', '/').Replace('\\', '/') + '/' + programName + '/';
			if (CreateWritableDir)
				Directory.CreateDirectory(WritableDirectoryBase);
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
			WritableDirectoryBase = diWritable.FullName.TrimEnd('\\', '/').Replace('\\', '/') + '/';
		}

		/// <summary>
		/// Gets the name of the executable file, including the extension.  e.g. "MyProgram.exe"
		/// </summary>
		public static string ExecutableNameWithExtension { get; private set; }
		/// <summary>
		/// Gets the name of the executable file, NOT including the extension.  e.g. "MyProgram.exe" => "MyProgram"
		/// </summary>
		public static string ExecutableNameWithoutExtension { get; private set; }
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Does not have trailing '/'.
		/// </summary>
		public static string ApplicationRoot { get; private set; }
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Includes trailing '/'.
		/// </summary>
		public static string ApplicationDirectoryBase { get; private set; }
		/// <summary>
		/// Gets the full path to a persistent directory where the application can write to.  Includes trailing '/'.
		/// </summary>
		public static string WritableDirectoryBase { get; private set; }
		/// <summary>
		/// Gets the full path to the error log file.
		/// </summary>
		public static string ErrorFilePath
		{
			get
			{
				if (GetErrorFilePath != null)
					return GetErrorFilePath();
				return WritableDirectoryBase + ExecutableNameWithoutExtension + "Errors.txt";
			}
		}
		/// <summary>
		/// If specified, this function is called upon each <see cref="ErrorFilePath"/> property get.
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
		/// <summary>
		/// The BPUtil version number, not to be confused with the version number of the application this is included in.  This version number is often neglected.
		/// </summary>
		public static string Version = "0.9";

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
		/// Gets the title of the entry assembly (executable file name without extension).
		/// </summary>
		public static string AssemblyTitle
		{
			get
			{
				AssemblyTitleAttribute attr = Assembly.GetEntryAssembly().GetCustomAttributes<AssemblyTitleAttribute>().FirstOrDefault();
				if (attr != null && !string.IsNullOrWhiteSpace(attr.Title))
					return attr.Title;
				return Path.GetFileNameWithoutExtension(Globals.EntryAssemblyLocation);
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

		/// <summary>
		/// Gets the last modified date and time of the entry assembly (executable file).
		/// </summary>
		public static DateTime AssemblyModifiedDate
		{
			get
			{
				return File.GetLastWriteTimeUtc(EntryAssemblyLocation);
			}
		}
		/// <summary>
		/// Gets the absolute path to the executable that is running, sourced from the framework instead of from Globals, which could have been initialized with a different path.
		/// </summary>
		public static string EntryAssemblyLocation
		{
			get
			{
#if NET6_0_OR_GREATER
				string eal = Environment.ProcessPath;
				if (eal == null) // Real process name not available.  Make a guess.
					eal = Path.Combine(AppContext.BaseDirectory, System.Diagnostics.Process.GetCurrentProcess().ProcessName + (Platform.IsUnix() ? ".dll" : ".exe"));
				return eal;
#else
				return Assembly.GetEntryAssembly().Location;
#endif
			}
		}
	}
}
