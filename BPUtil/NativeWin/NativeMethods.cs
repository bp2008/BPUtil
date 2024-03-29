﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	public static partial class NativeMethods
	{
		#region NativeMethods for dealing with images and desktop capture
		[DllImport("gdi32", EntryPoint = "GetObject", SetLastError = true)]
		public static extern int GetObjectBitmap(IntPtr hObject, int nCount, ref BITMAP lpObject);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

		/// <summary>
		/// Creates a bitmap compatible with the device that is associated with the specified device context.
		/// </summary>
		/// <param name="hdc">A handle to a device context.</param>
		/// <param name="nWidth">The bitmap width, in pixels.</param>
		/// <param name="nHeight">The bitmap height, in pixels.</param>
		/// <returns>If the function succeeds, the return value is a handle to the compatible bitmap (DDB). If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.</returns>
		[DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap", SetLastError = true)]
		public static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern IntPtr DeleteObject(IntPtr hDc);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern IntPtr DeleteDC(IntPtr hDc);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);

		/// <summary>
		///        Retrieves the bits of the specified compatible bitmap and copies them into a buffer as a DIB using the specified format.
		/// </summary>
		/// <param name="hdc">A handle to the device context.</param>
		/// <param name="hbmp">A handle to the bitmap. This must be a compatible bitmap (DDB).</param>
		/// <param name="uStartScan">The first scan line to retrieve.</param>
		/// <param name="cScanLines">The number of scan lines to retrieve.</param>
		/// <param name="lpvBits">A pointer to a buffer to receive the bitmap data. If this parameter is <see cref="IntPtr.Zero"/>, the function passes the dimensions and format of the bitmap to the <see cref="BITMAPINFO"/> structure pointed to by the <paramref name="lpbi"/> parameter.</param>
		/// <param name="lpbi">A pointer to a <see cref="BITMAPINFO"/> structure that specifies the desired format for the DIB data.</param>
		/// <param name="uUsage">The format of the bmiColors member of the <see cref="BITMAPINFO"/> structure. It must be one of the following values.</param>
		/// <returns>If the lpvBits parameter is non-NULL and the function succeeds, the return value is the number of scan lines copied from the bitmap.
		/// If the lpvBits parameter is NULL and GetDIBits successfully fills the <see cref="BITMAPINFO"/> structure, the return value is nonzero.
		/// If the function fails, the return value is zero.
		/// This function can return the following value: ERROR_INVALID_PARAMETER (87 (0×57))</returns>
		[DllImport("gdi32.dll", EntryPoint = "GetDIBits", SetLastError = true)]
		public static extern int GetDIBits([In] IntPtr hdc, [In] IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbi, DIB_Color_Mode uUsage);

		[DllImport("user32.dll")]
		public static extern bool GetCursorInfo(out CURSORINFO pci);

		[DllImport("user32.dll")]
		public static extern bool DrawIcon(IntPtr hdc, int x, int y, IntPtr hIcon);

		[DllImport("user32.dll")]
		public static extern IntPtr GetDesktopWindow();

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
		}
		public static readonly int SizeOfCursorInfo = Marshal.SizeOf(typeof(CURSORINFO));
		[StructLayout(LayoutKind.Sequential)]
		public struct CURSORINFO
		{
			public int cbSize;
			public int flags;
			public IntPtr hCursor;
			public POINT ptScreenPos;
		}

		/// <summary>
		/// The BITMAP structure defines the type, width, height, color format, and bit values of a bitmap.
		/// </summary>
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAP
		{
			/// <summary>
			/// The bitmap type. This member must be zero.
			/// </summary>
			public int bmType;

			/// <summary>
			/// The width, in pixels, of the bitmap. The width must be greater than zero.
			/// </summary>
			public int bmWidth;

			/// <summary>
			/// The height, in pixels, of the bitmap. The height must be greater than zero.
			/// </summary>
			public int bmHeight;

			/// <summary>
			/// The number of bytes in each scan line. This value must be divisible by 2, because the system assumes that the bit 
			/// values of a bitmap form an array that is word aligned.
			/// </summary>
			public int bmWidthBytes;

			/// <summary>
			/// The count of color planes.
			/// </summary>
			public int bmPlanes;

			/// <summary>
			/// The number of bits required to indicate the color of a pixel.
			/// </summary>
			public int bmBitsPixel;

			/// <summary>
			/// A pointer to the location of the bit values for the bitmap. The bmBits member must be a pointer to an array of 
			/// character (1-byte) values.
			/// </summary>
			public IntPtr bmBits;
		}
		public enum DIB_Color_Mode : uint
		{
			DIB_RGB_COLORS = 0,
			DIB_PAL_COLORS = 1
		}
		//[StructLayout(LayoutKind.Sequential)]
		//public struct BITMAPINFO
		//{
		//	/// <summary>
		//	/// A BITMAPINFOHEADER structure that contains information about the dimensions of color format.
		//	/// </summary>
		//	public BITMAPINFOHEADER bmiHeader;

		//	/// <summary>
		//	/// An array of RGBQUAD. The elements of the array that make up the color table.
		//	/// </summary>
		//	public unsafe fixed uint bmiColors[256];
		//}
		//[StructLayout(LayoutKind.Sequential)]
		//public struct RGBQUAD
		//{
		//	public byte B;
		//	public byte G;
		//	public byte R;
		//	public byte unused;
		//}
		//[StructLayout(LayoutKind.Sequential)]
		//public struct BITMAPINFOHEADER
		//{
		//	public uint biSize;
		//	public int biWidth;
		//	public int biHeight;
		//	public ushort biPlanes;
		//	public ushort biBitCount;
		//	public BitmapCompressionMode biCompression;
		//	public uint biSizeImage;
		//	public int biXPelsPerMeter;
		//	public int biYPelsPerMeter;
		//	public uint biClrUsed;
		//	public uint biClrImportant;

		//	/// <summary>
		//	/// Populates the biSize field.  MUST BE CALLED.
		//	/// </summary>
		//	public void Init()
		//	{
		//		biSize = (uint)Marshal.SizeOf(this);
		//	}
		//}
		//public enum BitmapCompressionMode : uint
		//{
		//	BI_RGB = 0,
		//	BI_RLE8 = 1,
		//	BI_RLE4 = 2,
		//	BI_BITFIELDS = 3,
		//	BI_JPEG = 4,
		//	BI_PNG = 5
		//}
		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAPINFO
		{
			public BITMAPINFOHEADER bmiHeader;
			public unsafe fixed uint bmiColors[256];
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAPINFOHEADER
		{
			public int biSize;
			public int biWidth;
			public int biHeight;
			public short biPlanes;
			public short biBitCount;
			public int biCompression;
			public int biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public int biClrUsed;
			public int biClrImportant;
		}
		#endregion
		#region NativeMethods for Window Management
		/// <summary>
		/// A Rectangle usable with certain Windows API methods.
		/// </summary>
		public struct Rect
		{
			public int Left { get; set; }
			public int Top { get; set; }
			public int Right { get; set; }
			public int Bottom { get; set; }
		}
		/// <summary>
		/// Returns the Handle of the topmost window at the given point.
		/// </summary>
		/// <param name="Point">Point at which to look for a window.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(POINT Point);
		/// <summary>
		/// Returns the Handle of the foreground window.
		/// </summary>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();
		/// <summary>
		/// Gets the bounds of the window with the given Handle.
		/// </summary>
		/// <param name="hwnd">Window Handle</param>
		/// <param name="rectangle">The bounds of the window with the given Handle are placed into this object.</param>
		/// <returns>Returns true if the window was found and the bounds rectangle has been populated.</returns>
		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(HandleRef hwnd, ref Rect rectangle);
		/// <summary>
		/// Gets the bounds of the window with the given Handle. If the window is not found, the returned Rectangle will have all 0 for all property values.
		/// </summary>
		/// <param name="windowHandle">Window Handle</param>
		/// <returns>A Rectangle representing the window's bounds.</returns>
		public static Rectangle GetWindowBounds(IntPtr windowHandle)
		{
			Rect rect = new Rect();
			NativeMethods.GetWindowRect(new HandleRef(null, windowHandle), ref rect);
			return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
		}
		/// <summary>
		/// Gets the bounds of the currently focused window. If the window is not found, the returned Rectangle will have all 0 for all property values.
		/// </summary>
		/// <returns>A Rectangle representing the window's bounds.</returns>
		public static Rectangle GetBoundsOfFocusedWindow()
		{
			return GetWindowBounds(GetForegroundWindow());
		}
		/// <summary>
		/// Gets the process ID that created the given Window Handle as an out parameter, and the thread ID that created the window as the return value.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <param name="lpdwProcessId">Process ID</param>
		/// <returns>Thread ID</returns>
		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
		/// <summary>
		/// Checks Extended Window Styles for the given window handle.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <param name="nIndex">Identifier of the style to query.</param>
		/// <returns></returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		/// <summary>
		/// Returns true if the window with the given Handle has the "TopMost" style.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <returns></returns>
		public static bool IsWindowTopMost(IntPtr hWnd)
		{
			const int GWL_EXSTYLE = -20;
			const int WS_EX_TOPMOST = 0x0008;

			int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
			return (exStyle & WS_EX_TOPMOST) == WS_EX_TOPMOST;
		}
		/// <summary>
		/// Sets a Window's position and flags.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <param name="hWndInsertAfter"></param>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <param name="cx"></param>
		/// <param name="cy"></param>
		/// <param name="uFlags"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		/// <summary>
		/// Sets a window to be above all other windows.
		/// </summary>
		/// <param name="hWnd"></param>
		public static void SetWindowTopmost(IntPtr hWnd)
		{
			IntPtr HWND_TOPMOST = new IntPtr(-1);
			const UInt32 SWP_NOSIZE = 0x0001;
			const UInt32 SWP_NOMOVE = 0x0002;
			const UInt32 SWP_SHOWWINDOW = 0x0040;
			SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
		}
		#endregion
		#region NativeMethods for Desktops/Threads

		/// <summary>
		/// Retrieves a handle to the desktop assigned to the specified thread.
		/// </summary>
		/// <param name="dwThread">[in] Handle to the thread for which to return the desktop handle.</param>
		/// <returns>If the function succeeds, the return value is a handle to the 
		/// desktop associated with the specified thread. 
		/// If the function fails, the return value is NULL.</returns>
		[DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr GetThreadDesktop(uint dwThread);

		/// <summary>
		/// Assigns the specified desktop to the calling thread. 
		/// All subsequent operations on the desktop use the access rights granted to the desktop.
		/// </summary>
		/// <param name="hDesktop">[in] Handle to the desktop to be assigned to the calling thread.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero. </returns>
		[DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool SetThreadDesktop(IntPtr hDesktop);

		/// <summary>
		/// Opens the desktop that receives user input.
		/// </summary>
		/// <param name="dwFlags">[in] This parameter can be zero or the following value.</param>
		/// <param name="fInherit">[in] If this value is TRUE, processes created by this process will inherit the handle.Otherwise, the processes do not inherit this handle.</param>
		/// <param name="dwDesiredAccess">[in] The access to the desktop.</param>
		/// <returns>If the function succeeds, the return value is a handle to the desktop that receives user input. When you are finished using the handle, call the CloseDesktop function to close it.
		/// If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);
		/// <summary>
		/// Closes an open handle to a desktop object.
		/// </summary>
		/// <param name="hDesktop">[in] Handle to the desktop to be closed.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero. </returns>
		[DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr CloseDesktop(IntPtr hDesktop);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern uint GetCurrentThreadId();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);

		public class UserObjectInformation
		{
			public const int FLAGS = 1;
			public const int NAME = 2;
			public const int TYPE = 3;
			public const int USER_SID = 4;
		}
		[Flags]
		public enum ACCESS_MASK : uint
		{
			DESKTOP_READOBJECTS = 0x00000001,
			DESKTOP_CREATEWINDOW = 0x00000002,
			DESKTOP_CREATEMENU = 0x00000004,
			DESKTOP_HOOKCONTROL = 0x00000008,
			DESKTOP_JOURNALRECORD = 0x00000010,
			DESKTOP_JOURNALPLAYBACK = 0x00000020,
			DESKTOP_ENUMERATE = 0x00000040,
			DESKTOP_WRITEOBJECTS = 0x00000080,
			DESKTOP_SWITCHDESKTOP = 0x00000100,
			DESKTOP_ALL_ACCESS = DESKTOP_SWITCHDESKTOP | DESKTOP_WRITEOBJECTS | DESKTOP_ENUMERATE | DESKTOP_JOURNALPLAYBACK | DESKTOP_JOURNALRECORD | DESKTOP_HOOKCONTROL | DESKTOP_CREATEMENU | DESKTOP_CREATEWINDOW | DESKTOP_READOBJECTS,

			WINSTA_ENUMDESKTOPS = 0x00000001,
			WINSTA_READATTRIBUTES = 0x00000002,
			WINSTA_ACCESSCLIPBOARD = 0x00000004,
			WINSTA_CREATEDESKTOP = 0x00000008,
			WINSTA_WRITEATTRIBUTES = 0x00000010,
			WINSTA_ACCESSGLOBALATOMS = 0x00000020,
			WINSTA_EXITWINDOWS = 0x00000040,
			WINSTA_ENUMERATE = 0x00000100,
			WINSTA_READSCREEN = 0x00000200,

			WINSTA_ALL_ACCESS = 0x0000037F
		}
		#endregion
		#region NativeMethods for process creation

		[DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern bool CloseHandle(IntPtr handle);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CreateProcessAsUser(IntPtr tokenHandle, string applicationName, string commandLine, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandle, uint creationFlags, IntPtr envrionment, string currentDirectory, ref STARTUPINFO startupInfo, ref PROCESS_INFORMATION processInformation);

		[DllImport("Kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
		public static extern int WTSGetActiveConsoleSessionId();

		[DllImport("WtsApi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WTSQueryUserToken(int SessionId, out IntPtr phToken);

		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public extern static bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out IntPtr phNewToken);

		[DllImport("userenv.dll", SetLastError = true)]
		public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

		[DllImport("userenv.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESS_INFORMATION
		{
			public IntPtr processHandle;
			public IntPtr threadHandle;
			public int processID;
			public int threadID;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct STARTUPINFO
		{
			public int length;
			public string reserved;
			public string desktop;
			public string title;
			public int x;
			public int y;
			public int width;
			public int height;
			public int consoleColumns;
			public int consoleRows;
			public int consoleFillAttribute;
			public int flags;
			public short showWindow;
			public short reserverd2;
			public IntPtr reserved3;
			public IntPtr stdInputHandle;
			public IntPtr stdOutputHandle;
			public IntPtr stdErrorHandle;
		}
		[Flags]
		public enum ProcessAccessFlags : uint
		{
			All = 0x001F0FFF,
			Terminate = 0x00000001,
			CreateThread = 0x00000002,
			VirtualMemoryOperation = 0x00000008,
			VirtualMemoryRead = 0x00000010,
			VirtualMemoryWrite = 0x00000020,
			DuplicateHandle = 0x00000040,
			CreateProcess = 0x000000080,
			SetQuota = 0x00000100,
			SetInformation = 0x00000200,
			QueryInformation = 0x00000400,
			QueryLimitedInformation = 0x00001000,
			Synchronize = 0x00100000
		}
		public enum SECURITY_IMPERSONATION_LEVEL
		{
			SecurityAnonymous,
			SecurityIdentification,
			SecurityImpersonation,
			SecurityDelegation
		}
		public enum TOKEN_TYPE
		{
			TokenPrimary = 1,
			TokenImpersonation
		}
		public enum WindowShowStyle : short
		{
			/// <summary>Hides the window and activates another window.</summary>
			/// <remarks>See SW_HIDE</remarks>
			Hide = 0,
			/// <summary>Activates and displays a window. If the window is minimized 
			/// or maximized, the system restores it to its original size and 
			/// position. An application should specify this flag when displaying 
			/// the window for the first time.</summary>
			/// <remarks>See SW_SHOWNORMAL</remarks>
			ShowNormal = 1,
			/// <summary>Activates the window and displays it as a minimized window.</summary>
			/// <remarks>See SW_SHOWMINIMIZED</remarks>
			ShowMinimized = 2,
			/// <summary>Activates the window and displays it as a maximized window.</summary>
			/// <remarks>See SW_SHOWMAXIMIZED</remarks>
			ShowMaximized = 3,
			/// <summary>Maximizes the specified window.</summary>
			/// <remarks>See SW_MAXIMIZE</remarks>
			Maximize = 3,
			/// <summary>Displays a window in its most recent size and position. 
			/// This value is similar to "ShowNormal", except the window is not 
			/// actived.</summary>
			/// <remarks>See SW_SHOWNOACTIVATE</remarks>
			ShowNormalNoActivate = 4,
			/// <summary>Activates the window and displays it in its current size 
			/// and position.</summary>
			/// <remarks>See SW_SHOW</remarks>
			Show = 5,
			/// <summary>Minimizes the specified window and activates the next 
			/// top-level window in the Z order.</summary>
			/// <remarks>See SW_MINIMIZE</remarks>
			Minimize = 6,
			/// <summary>Displays the window as a minimized window. This value is 
			/// similar to "ShowMinimized", except the window is not activated.</summary>
			/// <remarks>See SW_SHOWMINNOACTIVE</remarks>
			ShowMinNoActivate = 7,
			/// <summary>Displays the window in its current size and position. This 
			/// value is similar to "Show", except the window is not activated.</summary>
			/// <remarks>See SW_SHOWNA</remarks>
			ShowNoActivate = 8,
			/// <summary>Activates and displays the window. If the window is 
			/// minimized or maximized, the system restores it to its original size 
			/// and position. An application should specify this flag when restoring 
			/// a minimized window.</summary>
			/// <remarks>See SW_RESTORE</remarks>
			Restore = 9,
			/// <summary>Sets the show state based on the SW_ value specified in the 
			/// STARTUPINFO structure passed to the CreateProcess function by the 
			/// program that started the application.</summary>
			/// <remarks>See SW_SHOWDEFAULT</remarks>
			ShowDefault = 10,
			/// <summary>Windows 2000/XP: Minimizes a window, even if the thread 
			/// that owns the window is hung. This flag should only be used when 
			/// minimizing windows from a different thread.</summary>
			/// <remarks>See SW_FORCEMINIMIZE</remarks>
			ForceMinimized = 11
		}
		[Flags]
		public enum CreateProcessFlags : uint
		{
			CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
			CREATE_DEFAULT_ERROR_MODE = 0x04000000,
			CREATE_NEW_CONSOLE = 0x00000010,
			CREATE_NEW_PROCESS_GROUP = 0x00000200,
			CREATE_NO_WINDOW = 0x08000000,
			CREATE_PROTECTED_PROCESS = 0x00040000,
			CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
			CREATE_SEPARATE_WOW_VDM = 0x00000800,
			CREATE_SHARED_WOW_VDM = 0x00001000,
			CREATE_SUSPENDED = 0x00000004,
			CREATE_UNICODE_ENVIRONMENT = 0x00000400,
			DEBUG_ONLY_THIS_PROCESS = 0x00000002,
			DEBUG_PROCESS = 0x00000001,
			DETACHED_PROCESS = 0x00000008,
			EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
			INHERIT_PARENT_AFFINITY = 0x00010000
		}
		#endregion
		#region NativeMethods for Thread Manipulation
		/// <summary>
		/// Sets the execution state of the current thread and returns the previous state.
		/// </summary>
		/// <param name="esFlags">New executation state.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

		[Flags]
		public enum ExecutionState : uint
		{
			/// <summary>
			/// Prevent idle-to-sleep
			/// </summary>
			ES_AWAYMODE_REQUIRED = 0x00000040,
			/// <summary>
			/// Allow monitor power down
			/// </summary>
			ES_CONTINUOUS = 0x80000000,
			/// <summary>
			/// Prevent monitor power down
			/// </summary>
			ES_DISPLAY_REQUIRED = 0x00000002,
			/// <summary>
			/// Keep system awake
			/// </summary>
			ES_SYSTEM_REQUIRED = 0x00000001
		}
		#endregion
		/// <summary>
		///     Retrieves a handle to the Shell's desktop window.
		///     <para>
		///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633512%28v=vs.85%29.aspx for more
		///     information
		///     </para>
		/// </summary>
		/// <returns>
		///     C++ ( Type: HWND )<br />The return value is the handle of the Shell's desktop window. If no Shell process is
		///     present, the return value is NULL.
		/// </returns>
		[DllImport("user32.dll")]
		public static extern IntPtr GetShellWindow();

		/// <summary>
		///     Brings the thread that created the specified window into the foreground and activates the window. Keyboard input is
		///     directed to the window, and various visual cues are changed for the user. The system assigns a slightly higher
		///     priority to the thread that created the foreground window than it does to other threads.
		///     <para>See for https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539%28v=vs.85%29.aspx more information.</para>
		/// </summary>
		/// <param name="hWnd">
		///     C++ ( hWnd [in]. Type: HWND )<br />A handle to the window that should be activated and brought to the foreground.
		/// </param>
		/// <returns>
		///     <c>true</c> or nonzero if the window was brought to the foreground, <c>false</c> or zero If the window was not
		///     brought to the foreground.
		/// </returns>
		/// <remarks>
		///     The system restricts which processes can set the foreground window. A process can set the foreground window only if
		///     one of the following conditions is true:
		///     <list type="bullet">
		///     <listheader>
		///         <term>Conditions</term><description></description>
		///     </listheader>
		///     <item>The process is the foreground process.</item>
		///     <item>The process was started by the foreground process.</item>
		///     <item>The process received the last input event.</item>
		///     <item>There is no foreground process.</item>
		///     <item>The process is being debugged.</item>
		///     <item>The foreground process is not a Modern Application or the Start Screen.</item>
		///     <item>The foreground is not locked (see LockSetForegroundWindow).</item>
		///     <item>The foreground lock time-out has expired (see SPI_GETFOREGROUNDLOCKTIMEOUT in SystemParametersInfo).</item>
		///     <item>No menus are active.</item>
		///     </list>
		///     <para>
		///     An application cannot force a window to the foreground while the user is working with another window.
		///     Instead, Windows flashes the taskbar button of the window to notify the user.
		///     </para>
		///     <para>
		///     A process that can set the foreground window can enable another process to set the foreground window by
		///     calling the AllowSetForegroundWindow function. The process specified by dwProcessId loses the ability to set
		///     the foreground window the next time the user generates input, unless the input is directed at that process, or
		///     the next time a process calls AllowSetForegroundWindow, unless that process is specified.
		///     </para>
		///     <para>
		///     The foreground process can disable calls to SetForegroundWindow by calling the LockSetForegroundWindow
		///     function.
		///     </para>
		/// </remarks>
		// For Windows Mobile, replace user32.dll with coredll.dll
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);
		/// <summary>
		/// Switches focus to the window specified by the handle.  Optionally switches as though Alt + Tab were used.  One way causes the window to move to the foreground.  The other does not.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="fAltTab"></param>
		[DllImport("user32.dll", SetLastError = true)]
		static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
	}
}
