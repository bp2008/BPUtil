using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	public static class WinConsole
	{
		/// <summary>
		/// Un-redirects the console output of a program and allocates a console if necessary. Available only on Windows.
		/// </summary>
		/// <param name="alwaysCreateNewConsole">If false, we attempt to attach to a pre-existing console, but fall back to allocating a new one.  If true, we simply allocate the new console.</param>
		public static void Initialize(bool alwaysCreateNewConsole = true)
		{
			if (Platform.IsUnix())
				return;

			bool consoleAttached = true;
			if (alwaysCreateNewConsole
				|| (AttachConsole(ATTACH_PARENT) == 0
				&& Marshal.GetLastWin32Error() != ERROR_ACCESS_DENIED))
			{
				consoleAttached = AllocConsole() != 0;
			}

			if (consoleAttached)
			{
				InitializeOutAndErrorStreams();
				InitializeInStream();
			}
		}

		private static void InitializeOutAndErrorStreams()
		{
			IntPtr defaultStdoutHandle = CreateFileW("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
			IntPtr currentStdoutHandle = GetStdHandle(STD_OUTPUT);
			if (currentStdoutHandle != defaultStdoutHandle)
			{
				SetStdHandle(STD_OUTPUT, defaultStdoutHandle);

				//StreamWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
				//Console.SetOut(writer);
			}
			IntPtr currentStderrHandle = GetStdHandle(STD_ERROR);
			if (currentStderrHandle != defaultStdoutHandle)
			{
				// There is only one window for standard out and error, so ...
				SetStdHandle(STD_ERROR, defaultStdoutHandle);

				//StreamWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
				//Console.SetError(writer);
			}
		}

		private static void InitializeInStream()
		{

			IntPtr defaultStdinHandle = CreateFileW("CONIN$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
			IntPtr currentStdinHandle = GetStdHandle(STD_INPUT);
			if (currentStdinHandle != defaultStdinHandle)
			{
				SetStdHandle(STD_INPUT, defaultStdinHandle);

				//StreamReader reader = new StreamReader(Console.OpenStandardInput());
				//Console.SetIn(reader);
			}
		}

		#region Win API Functions and Constants
		[DllImport("kernel32.dll",
			EntryPoint = "AllocConsole",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern int AllocConsole();

		[DllImport("kernel32.dll",
			EntryPoint = "AttachConsole",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern UInt32 AttachConsole(UInt32 dwProcessId);

		[DllImport("kernel32.dll",
			EntryPoint = "CreateFileW",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr CreateFileW(
			  string lpFileName,
			  UInt32 dwDesiredAccess,
			  UInt32 dwShareMode,
			  IntPtr lpSecurityAttributes,
			  UInt32 dwCreationDisposition,
			  UInt32 dwFlagsAndAttributes,
			  IntPtr hTemplateFile
			);

		private const UInt32 GENERIC_WRITE = 0x40000000;
		private const UInt32 GENERIC_READ = 0x80000000;
		private const UInt32 FILE_SHARE_READ = 0x00000001;
		private const UInt32 FILE_SHARE_WRITE = 0x00000002;
		private const UInt32 OPEN_EXISTING = 0x00000003;
		private const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
		private const UInt32 ERROR_ACCESS_DENIED = 5;

		private const UInt32 ATTACH_PARENT = 0xFFFFFFFF;

		/// <summary>
		/// Gets the handle for the specified standard device (standard input, standard output, or standard error).
		/// </summary>
		/// <param name="nStdHandle">The standard device. This parameter can be one of the following values: <see cref="STD_INPUT"/>, <see cref="STD_OUTPUT"/>, <see cref="STD_ERROR"/>.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll")]
		private static extern IntPtr GetStdHandle(UInt32 nStdHandle);
		/// <summary>
		/// Sets the handle for the specified standard device (standard input, standard output, or standard error).
		/// </summary>
		/// <param name="nStdHandle">The standard device for which the handle is to be set. This parameter can be one of the following values: <see cref="STD_INPUT"/>, <see cref="STD_OUTPUT"/>, <see cref="STD_ERROR"/>.</param>
		/// <param name="handle">The handle for the standard device.</param>
		[DllImport("kernel32.dll")]
		private static extern void SetStdHandle(UInt32 nStdHandle, IntPtr handle);

		/// <summary>
		/// Represents the Standard Input Stream.
		/// </summary>
		private const UInt32 STD_INPUT = uint.MaxValue - 10 + 1;
		/// <summary>
		/// Represents the Standard Output Stream.
		/// </summary>
		private const UInt32 STD_OUTPUT = uint.MaxValue - 11 + 1;
		/// <summary>
		/// Represents the Standard Error Stream.
		/// </summary>
		private const UInt32 STD_ERROR = uint.MaxValue - 12 + 1;
		#endregion
	}
}
