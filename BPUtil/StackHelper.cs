using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class StackHelper
	{
		/// <summary>
		/// A ThreadStatic static field which can be used to save a reference to a StackTrace.
		/// </summary>
		[ThreadStatic]
		public static StackTrace currentThreadSavedStack;

		/// <summary>
		/// Gets <see cref="currentThreadSavedStack"/> as a string.
		/// </summary>
		public static string StackTrace { get { return currentThreadSavedStack == null ? "Saved stack trace is unavailable." : currentThreadSavedStack.ToString(); } }
	}
}
