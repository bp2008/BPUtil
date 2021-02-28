using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides static methods to run a process and retrieve its output.
	/// </summary>
	public static class ProcessRunner
	{
		/// <summary>
		/// Synchronously run the specified process in the background with the specified arguments, and return the exit code that the process returned.
		/// The contents of the std and err output streams are provided via out parameters.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">(out) standard output stream text</param>
		/// <param name="err">(out) standard error stream text</param>
		/// <returns>The exit code of the process that ran.</returns>
		public static int RunProcessAndWait(string fileName, string arguments, out string std, out string err)
		{
			bool bThreadAbort = false;
			return RunProcessAndWait(fileName, arguments, out std, out err, ref bThreadAbort);
		}

		/// <summary>
		/// Synchronously run the specified process in the background with the specified arguments, and return the exit code that the process returned.
		/// The contents of the std and err output streams are provided via out parameters.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">(out) standard output stream text</param>
		/// <param name="err">(out) standard error stream text</param>
		/// <param name="bThreadAbort">Set to true from another thread to abort.</param>
		/// <returns>The exit code of the process that ran.</returns>
		public static int RunProcessAndWait(string fileName, string arguments, out string std, out string err, ref bool bThreadAbort)
		{
			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			StringBuilder sbOutput = new StringBuilder();
			StringBuilder sbError = new StringBuilder();

			int exitCode;
			using (Process p = Process.Start(psi))
			{
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

				while (!bThreadAbort && !p.HasExited)
				{
					p.WaitForExit(500);
				}
				if (bThreadAbort && !p.HasExited)
					p.Kill();

				if (!bThreadAbort)
				{
					// Must call WaitForExit without a timeout in order to force output buffers to be flushed.
					p.WaitForExit();
				}

				exitCode = p.ExitCode;
			}

			std = sbOutput.ToString();
			err = sbError.ToString();

			return exitCode;
		}

		/// <summary>
		/// Synchronously run the specified process in the background with the specified arguments, and return the exit code that the process returned.
		/// The contents of the std and err output streams are provided to callback functions.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">Lines read from standard output stream are sent to this callback.</param>
		/// <param name="err">Lines read from standard error stream are sent to this callback.</param>
		/// <returns>The exit code of the process that ran.</returns>
		public static int RunProcessAndWait(string fileName, string arguments, Action<ProcessRunnerOutputEventArgs> std, Action<ProcessRunnerOutputEventArgs> err)
		{
			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			using (Process p = Process.Start(psi))
			{
				Action abortCallback = () =>
				{
					p.CloseMainWindow();
					if (!p.WaitForExit(500))
						p.Kill();
				};
				p.OutputDataReceived += (sender, e) =>
				{
					std(new ProcessRunnerOutputEventArgs(e.Data, abortCallback));
				};
				p.ErrorDataReceived += (sender, e) =>
				{
					err(new ProcessRunnerOutputEventArgs(e.Data, abortCallback));
				};
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();

				while (!p.HasExited)
					p.WaitForExit(500);
				p.WaitForExit();
				return p.ExitCode;
			}
		}

		/// <summary>
		/// Asynchronously run the specified process in the background with the specified arguments.
		/// The contents of the std and err output streams are provided to callback functions.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">Lines read from standard output stream are sent to this callback.</param>
		/// <param name="err">Lines read from standard error stream are sent to this callback.</param>
		/// <returns>An object containing the Process instance and helper functions.</returns>
		public static ProcessRunnerHandle RunProcess(string fileName, string arguments, Action<string> std, Action<string> err)
		{
			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			Process p = Process.Start(psi);

			p.OutputDataReceived += (sender, e) =>
			{
				std(e.Data);
			};
			p.ErrorDataReceived += (sender, e) =>
			{
				err(e.Data);
			};
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();

			return new ProcessRunnerHandle(p);
		}
	}

	/// <summary>
	/// Provides access to the Process instance and a method to wait for exit.
	/// </summary>
	public class ProcessRunnerHandle
	{
		/// <summary>
		/// Reference to the process that was started.
		/// </summary>
		public Process process;

		/// <summary>
		/// Constructs a new ProcessRunnerHandle.
		/// </summary>
		/// <param name="process"></param>
		internal ProcessRunnerHandle(Process process)
		{
			this.process = process;
		}

		/// <summary>
		/// Waits for the process to exit, then returns the exit code.
		/// </summary>
		/// <returns></returns>
		public int WaitForExit()
		{
			while (!process.HasExited)
				process.WaitForExit(250);
			process.WaitForExit();
			return process.ExitCode;
		}
	}
	/// <summary>
	/// Event arguments for a Standard Output or Standard Error write event.
	/// </summary>
	public class ProcessRunnerOutputEventArgs : EventArgs
	{
		/// <summary>
		/// The line which was output by the process.
		/// </summary>
		public string Line;
		/// <summary>
		/// Call this function to abort the process early.
		/// </summary>
		public Action AbortCallback;
		/// <summary>
		/// Constructs an empty ProcessRunnerOutputEventArgs
		/// </summary>
		public ProcessRunnerOutputEventArgs() { }
		/// <summary>
		/// Constructs a ProcessRunnerOutputEventArgs
		/// </summary>
		/// <param name="line">Line of text that was output.</param>
		/// <param name="abortCallback">Action to call if you wish to exit the process.</param>
		public ProcessRunnerOutputEventArgs(string line, Action abortCallback)
		{
			this.Line = line;
			this.AbortCallback = abortCallback;
		}
	}
}
