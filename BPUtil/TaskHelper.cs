using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Offers helper methods for working with Tasks.
	/// </summary>
	public static class TaskHelper
	{
#if !NET6_0
		/// <summary>A task that has already completed successfully.</summary>
		private static Task s_completedTask;
#endif

		/// <summary>Gets a task that has already completed successfully.</summary>
		/// <remarks>May not always return the same instance.</remarks>        
		public static Task CompletedTask
		{
			get
			{
#if NET6_0
				return Task.CompletedTask;
#else
				Task completedTask = s_completedTask;
				if (completedTask == null)
					s_completedTask = completedTask = Task.Delay(0);
				return completedTask;
#endif
			}
		}
	}
}
