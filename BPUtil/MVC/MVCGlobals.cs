using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	public static class MVCGlobals
	{
		/// <summary>
		/// If false, details of exceptions thrown by Controller Action Methods will be suppressed for connections that originate from a remote host.
		/// </summary>
		public static bool RemoteClientsMaySeeExceptionDetails = false;
	}
}
