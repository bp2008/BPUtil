using System;
using System.Runtime.InteropServices;

namespace BPUtil
{
	/// <summary>
	/// Provides methods for getting information about the system's RAM.
	/// </summary>
	public static class Ram
	{
		/// <summary>
		/// Gets the number of bytes of RAM in the system.
		/// </summary>
		/// <returns>The number of bytes of RAM in the system.</returns>
		/// <exception cref="System.Exception">Thrown when the method fails to get the RAM size.</exception>
		public static ulong GetRamSize()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				MEMORYSTATUSEX memoryStatus = new MEMORYSTATUSEX();
				if (GlobalMemoryStatusEx(memoryStatus))
				{
					return memoryStatus.ullTotalPhys;
				}
			}
			else if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				_sysinfo info = new _sysinfo();
				if (sysinfo(ref info) == 0)
				{
					return info.totalram * (ulong)info.mem_unit;
				}
			}
			else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
			{
				IntPtr size = IntPtr.Zero;
				int retVal = sysctlbyname("hw.memsize", size, out IntPtr oldLen, IntPtr.Zero, 0);
				if (retVal == 0)
				{
					return (ulong)size.ToInt64();
				}
			}

			throw new Exception("Failed to get RAM size");
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private class MEMORYSTATUSEX
		{
			public uint dwLength;
			public uint dwMemoryLoad;
			public ulong ullTotalPhys;
			public ulong ullAvailPhys;
			public ulong ullTotalPageFile;
			public ulong ullAvailPageFile;
			public ulong ullTotalVirtual;
			public ulong ullAvailVirtual;
			public ulong ullAvailExtendedVirtual;

			public MEMORYSTATUSEX()
			{
				this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
			}
		}

		[DllImport("libc.so.6")]
		private static extern int sysinfo(ref _sysinfo info);

		[StructLayout(LayoutKind.Sequential)]
		private struct _sysinfo
		{
			public long uptime;             /* Seconds since boot */
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public ulong[] loads;          /* 1, 5, and 15 minute load averages */
			public ulong totalram;          /* Total usable main memory size */
			public ulong freeram;           /* Available memory size */
			public ulong sharedram;         /* Amount of shared memory */
			public ulong bufferram;         /* Memory used by buffers */
			public ulong totalswap;         /* Total swap space size */
			public ulong freeswap;          /* Swap space still available */
			public ushort procs;             /* Number of current processes */
			public uint totalhigh;          /* Total high memory size */
			public uint freehigh;           /* Available high memory size */
			public uint mem_unit;           /* Memory unit size in bytes */
		}

		[DllImport("libc")]
		private static extern int sysctlbyname(string property, IntPtr output, out IntPtr oldLen, IntPtr newp, uint newlen);
	}
}
