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
		/// This method does nothing, but allows initialization to occur via the static constructor, which calls <see cref="InitializeCommonApplicationData"/>.  WritableDirectoryBase gets initialized as a subdirectory of <see cref="Environment.SpecialFolder.CommonApplicationData"/>.  The directory pointed at by WritableDirectoryBase will not be created automatically, and the current working directory will not be changed.
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
		[Obsolete("Globals.Initialize is obsolete. Use Globals.InitializeCommonApplicationData or Globals.InitializeApplicationData instead. It is no longer allowed or necessary to pass the absolute executable path into Globals. If you want to override the WritableDirectoryPath, call SetWritableDirectory after one of the initialize functions.", true)]
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
		/// <para>Gets the assembly that best identifies this application: the entry assembly, or the web application's own assembly (the Global.asax code-behind assembly) when hosted under ASP.NET, where <see cref="Assembly.GetEntryAssembly"/> returns null.</para>
		/// <para>May be null in hosted environments where the web application's assembly cannot be identified (e.g. the site has no Global.asax code-behind).</para>
		/// </summary>
		public static Assembly ApplicationAssembly
		{
			get
			{
				Assembly asm = Assembly.GetEntryAssembly();
#if !NET6_0_OR_GREATER
				if (asm == null)
					asm = TryGetHostedAppAssembly(); // ASP.NET under IIS
#endif
				return asm;
			}
		}

		/// <summary>
		/// Gets the GUID of the application's assembly (see <see cref="ApplicationAssembly"/>).  Returns an empty string if the assembly cannot be identified or has no GuidAttribute.
		/// </summary>
		public static string AssemblyGuid
		{
			get
			{
				// ApplicationAssembly is null in hosted environments where the application's assembly cannot be identified.
				// In that case the fallback executable identified by EntryAssemblyLocation is native (e.g. w3wp.exe) and can have no GuidAttribute, so there is no suitable substitute value.
				GuidAttribute attr = ApplicationAssembly?.GetCustomAttributes<GuidAttribute>().FirstOrDefault();
				if (attr != null)
					return attr.Value;
				return "";
			}
		}

		/// <summary>
		/// Gets the title of the application's main assembly, if available, falling back to the executable file name without extension.
		/// </summary>
		public static string AssemblyTitle
		{
			get
			{
				Assembly asm = ApplicationAssembly;
				if (asm != null)
				{
					AssemblyTitleAttribute attr = asm.GetCustomAttributes<AssemblyTitleAttribute>().FirstOrDefault();
					if (attr != null && !string.IsNullOrWhiteSpace(attr.Title))
						return attr.Title;
				}
				return Path.GetFileNameWithoutExtension(EntryAssemblyLocation);
			}
		}

		/// <summary>
		/// Gets the name of the application's main assembly, if available, falling back to the executable file name without extension
		/// </summary>
		public static string AssemblyName
		{
			get
			{
				string name = ApplicationAssembly?.GetName().Name;
				if (!string.IsNullOrEmpty(name))
					return name;
				// Fall back to the file name of the executable identified by EntryAssemblyLocation (e.g. "w3wp"), consistent with AssemblyTitle.
				return Path.GetFileNameWithoutExtension(EntryAssemblyLocation);
			}
		}

		/// <summary>
		/// Gets the version of the application's main assembly, if available. Returns "0.0.0.0" if unknown.
		/// </summary>
		public static string AssemblyVersion
		{
			get
			{
				Version version = ApplicationAssembly?.GetName().Version;
				if (version != null)
					return version.ToString();
				// Fall back to the file version of the executable identified by EntryAssemblyLocation (e.g. w3wp.exe), so the version describes the same file as AssemblyTitle/AssemblyName.
				try
				{
					string fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(EntryAssemblyLocation).FileVersion;
					if (!string.IsNullOrWhiteSpace(fileVersion))
						return fileVersion;
				}
				catch { } // The file may not exist (EntryAssemblyLocation can be a guess) or may lack a version resource.
				return "0.0.0.0";
			}
		}

		/// <summary>
		/// Gets the last modified date and time of the application's main assembly (executable file or dll).
		/// </summary>
		public static DateTime AssemblyModifiedDate
		{
			get
			{
				return File.GetLastWriteTimeUtc(EntryAssemblyLocation);
			}
		}
		/// <summary>
		/// <para>Gets the absolute path to the application's own entry assembly file (e.g. the managed ".dll", or the ".exe"/apphost for a single-file publish).</para>
		/// <para>This is sourced from the framework rather than from Globals, which could have been initialized with a different path.</para>
		/// <para>Importantly, when the app is launched via the shared runtime host (e.g. <c>dotnet MyApp.dll</c>), this returns the path to <c>MyApp.dll</c>, NOT the path to the <c>dotnet</c> host executable.  This matters because callers use it to identify the application (e.g. to derive the writable data directory name or to build a service <c>ExecStart</c> command line).</para>
		/// </summary>
		public static string EntryAssemblyLocation
		{
			get
			{
				// Prefer the managed entry assembly's own path.  For a framework-dependent app
				// launched as "dotnet MyApp.dll", this is ".../MyApp.dll" — whereas
				// Environment.ProcessPath would report the shared "dotnet" host executable, which
				// would corrupt any value derived from the application's name (writable directory,
				// service ExecStart, error file name, etc.).
				string loc = Assembly.GetEntryAssembly()?.Location;
				if (!string.IsNullOrEmpty(loc))
					return loc;
#if NET6_0_OR_GREATER
				// Single-file publish: the entry assembly has no on-disk location, so the process
				// path IS the application's own executable (apphost), which is what we want here.
				string pp = Environment.ProcessPath;
				if (!string.IsNullOrEmpty(pp))
					return pp;
				// Real process name not available.  Make a guess.
				return Path.Combine(AppContext.BaseDirectory, System.Diagnostics.Process.GetCurrentProcess().ProcessName + (Platform.IsUnix() ? ".dll" : ".exe"));
#else
				// Hosted environments (e.g. ASP.NET under IIS) have no managed entry assembly.
				// Prefer the web application's own assembly (e.g. ".../bin/MyApp.dll") over the host process executable.
				Assembly hostedAppAsm = TryGetHostedAppAssembly();
				if (hostedAppAsm != null)
				{
					string path = GetOriginalAssemblyPath(hostedAppAsm);
					if (!string.IsNullOrEmpty(path))
						return path;
				}
				// The web application's assembly could not be identified.  Fall back to the host process executable (e.g. w3wp.exe).
				return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
#endif
			}
		}

#if !NET6_0_OR_GREATER
		/// <summary>
		/// Cache for <see cref="TryGetHostedAppAssembly"/>.  Only successful lookups are cached, because a lookup performed too early (before ASP.NET has compiled Global.asax) can fail while a later one would succeed.
		/// </summary>
		private static Assembly hostedAppAssembly = null;
		/// <summary>
		/// <para>When hosted under ASP.NET (e.g. IIS), returns the assembly that defines the web application (the Global.asax code-behind assembly).  Returns null when not hosted under ASP.NET, or when the web application's assembly cannot be identified (e.g. the site has no Global.asax code-behind, or ASP.NET has not compiled Global.asax yet).</para>
		/// <para>System.Web is accessed via reflection so that this file does not require a System.Web reference (BPUtil shares this code and does not reference System.Web).</para>
		/// </summary>
		private static Assembly TryGetHostedAppAssembly()
		{
			if (hostedAppAssembly != null)
				return hostedAppAssembly;
			try
			{
				const string systemWeb = ", System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
				Type hostingEnvironment = Type.GetType("System.Web.Hosting.HostingEnvironment" + systemWeb, false);
				if (hostingEnvironment == null || !(bool)hostingEnvironment.GetProperty("IsHosted").GetValue(null))
					return null;
				Type buildManager = Type.GetType("System.Web.Compilation.BuildManager" + systemWeb, false);
				if (buildManager == null)
					return null;
				Type globalAsaxType = (Type)buildManager.GetMethod("GetGlobalAsaxType").Invoke(null, null);
				if (globalAsaxType == null)
					return null;
				// ASP.NET compiles Global.asax into a generated assembly whose type derives from the
				// code-behind class (e.g. "MyApp.Global"), which derives from System.Web.HttpApplication.
				// The first base type defined outside the generated assembly belongs to the web
				// application's own assembly -- unless the site has no code-behind, in which case that
				// type is System.Web.HttpApplication itself and there is nothing suitable to return.
				Type t = globalAsaxType.BaseType;
				while (t != null && t.Assembly == globalAsaxType.Assembly)
					t = t.BaseType;
				if (t == null || t.Assembly == hostingEnvironment.Assembly)
					return null;
				return hostedAppAssembly = t.Assembly;
			}
			catch
			{
				return null;
			}
		}
		/// <summary>
		/// Gets the original on-disk path of the given assembly.  Under ASP.NET, <see cref="Assembly.Location"/> points at the shadow copy in "Temporary ASP.NET Files", whereas CodeBase preserves the original path in the application's bin folder.
		/// </summary>
		private static string GetOriginalAssemblyPath(Assembly asm)
		{
			try
			{
				string codeBase = asm.CodeBase;
				if (!string.IsNullOrEmpty(codeBase))
				{
					Uri uri = new Uri(codeBase);
					if (uri.IsFile)
						return uri.LocalPath;
				}
			}
			catch { } // CodeBase can be unavailable or malformed; fall back to Location.
			return asm.Location;
		}
#endif
	}
}
