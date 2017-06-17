using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BPUtil
{
	public class Platform
	{
		/// <summary>
		/// Returns true if Mono.Runtime is defined, indicating that this application is running in the mono environment instead of the official .NET runtime.
		/// </summary>
		/// <returns></returns>
		public static bool IsRunningOnMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}
		/// <summary>
		/// Returns true if the current OS is Linux or MacOS X, etc (not Windows).
		/// </summary>
		/// <returns></returns>
		public static bool IsUnix()
		{
			int p = (int)Environment.OSVersion.Platform;
			return (p == 4) || (p == 6) || (p == 128);
		}
	}
}
