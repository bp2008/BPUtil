using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides utility methods for running processes with less code than using the Process class directly.
	/// </summary>
	public static class ProcessRunner
	{
		/// <summary>
		/// Synchronously run the specified process with the specified arguments, and return the exit code that the process returned.
		/// The contents of the std and err output streams are provided via out parameters.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">(out) standard output stream text</param>
		/// <param name="err">(out) standard error stream text</param>
		/// <returns>The exit code of the process that ran.</returns>
		public static int RunProcessAndWait(string fileName, string arguments, out string std, out string err)
		{
			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			StringBuilder sbOutput = new StringBuilder();
			StringBuilder sbError = new StringBuilder();

			Process p = Process.Start(psi);

			p.OutputDataReceived += (sender, e) =>
			{
				sbOutput.AppendLine(e.Data);
			};
			p.ErrorDataReceived += (sender, e) =>
			{
				sbError.AppendLine(e.Data);
			};
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();

			p.WaitForExit();

			std = sbOutput.ToString();
			err = sbError.ToString();

			return p.ExitCode;
		}
	}
}
