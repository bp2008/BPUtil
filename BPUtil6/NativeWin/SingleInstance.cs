using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	/// <summary>
	/// Enforces a single instance.
	/// </summary>
	/// <remarks>
	/// This is where the magic happens.
	/// Start() tries to create a mutex.
	/// If it detects that another instance is already using the mutex, then it returns FALSE.
	/// Otherwise it returns TRUE.
	/// (Notice that a GUID is used for the mutex name, which is a little better than using the application name.)
	/// If another instance is detected, then you can use ShowFirstInstance() to show it
	/// (which will work as long as you override WndProc as shown above).
	/// ShowFirstInstance() broadcasts a message to all windows.
	/// The message is WM_SHOWFIRSTINSTANCE.
	/// (Notice that a GUID is used for WM_SHOWFIRSTINSTANCE.
	/// That allows you to reuse this code in multiple applications without getting
	/// strange results when you run them all at the same time.)
	/// 
	/// From http://www.codeproject.com/KB/cs/SingleInstanceAppMutex.aspx
	/// 
	/// Modified by bp2008
	/// </remarks>
	public static class SingleInstance
	{
		static readonly int WM_SHOWFIRSTINSTANCE = RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", Globals.AssemblyGuid);

		static Mutex mutex;

		/// <summary>
		/// If true, this class will allow one instance per desktop session.  If false, only one instance is allowed globally.
		/// </summary>
		public static bool Local = false;

		/// <summary>
		/// Attempts to own a mutex that enforces that this process is the only running instance of this application. Returns true if successful, or false if another process owns the mutex.
		/// If Start returns true, be sure to call Stop before the application exits.
		/// </summary>
		/// <returns></returns>
		public static bool Start()
		{
			string mutexName = (Local ? "Local" : "Global") + "\\" + Globals.AssemblyGuid;
			mutex = new Mutex(true, mutexName, out bool isOnlyInstance);
			if (!isOnlyInstance)
				mutex = null;
			return isOnlyInstance;
		}

		/// <summary>
		/// Shows the first running instance of the current application.
		/// </summary>
		public static void ShowFirstInstance()
		{
			PostMessage((IntPtr)HWND_BROADCAST, WM_SHOWFIRSTINSTANCE, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		/// Releases the mutex that enforces that this process is the only running instance of this application.
		/// </summary>
		public static void Stop()
		{
			mutex?.ReleaseMutex();
		}


		[DllImport("user32")]
		static extern int RegisterWindowMessage(string message);

		static int RegisterWindowMessage(string format, params object[] args)
		{
			string message = String.Format(format, args);
			return RegisterWindowMessage(message);
		}

		const int HWND_BROADCAST = 0xffff;
		const int SW_SHOWNORMAL = 1;

		[DllImport("user32")]
		static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

		[DllImportAttribute("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImportAttribute("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);


		static void ShowToFront(IntPtr window)
		{
			ShowWindow(window, SW_SHOWNORMAL);
			SetForegroundWindow(window);
		}
	}
}
