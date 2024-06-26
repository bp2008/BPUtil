﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
		/// <param name="options">(Optional) Additional options.</param>
		/// <returns>The exit code of the process that ran.</returns>
		public static int RunProcessAndWait(string fileName, string arguments, out string std, out string err, ProcessRunnerOptions options = null)
		{
			bool bThreadAbort = false;
			return RunProcessAndWait(fileName, arguments, out std, out err, ref bThreadAbort, options);
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
		/// <param name="options">(Optional) Additional options.</param>
		/// <returns>The exit code of the process that ran.</returns>
		public static int RunProcessAndWait(string fileName, string arguments, out string std, out string err, ref bool bThreadAbort, ProcessRunnerOptions options = null)
		{
			if (options == null)
				options = new ProcessRunnerOptions();

			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);

			StringBuilder sbOutput = new StringBuilder();
			StringBuilder sbError = new StringBuilder();

			options?.Apply(psi);
			int exitCode;
			using (Process p = Process.Start(psi))
			{
				options?.Apply(p);

				if (options == null || !options.RunAsAdministrator)
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
				}

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
		/// <param name="options">(Optional) Additional options.</param>
		/// <returns>The exit code of the process that ran.</returns>
		public static int RunProcessAndWait(string fileName, string arguments, Action<ProcessRunnerOutputEventArgs> std, Action<ProcessRunnerOutputEventArgs> err, ProcessRunnerOptions options = null)
		{
			if (options == null)
				options = new ProcessRunnerOptions();

			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);

			options?.Apply(psi);
			using (Process p = Process.Start(psi))
			{
				options?.Apply(p);

				if (options == null || !options.RunAsAdministrator)
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
				}

				while (!p.HasExited)
					p.WaitForExit(500);
				// Must call WaitForExit without a timeout in order to force output buffers to be flushed.
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
		/// <param name="options">(Optional) Additional options.</param>
		/// <returns>An object containing the Process instance and helper functions.</returns>
		public static ProcessRunnerHandle RunProcess(string fileName, string arguments, Action<string> std, Action<string> err, ProcessRunnerOptions options = null)
		{
			if (options == null)
				options = new ProcessRunnerOptions();

			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);

			options?.Apply(psi);
			Process p = Process.Start(psi);
			options?.Apply(p);

			if (options == null || !options.RunAsAdministrator)
			{
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
			}

			return new ProcessRunnerHandle(p);
		}

		/// <summary>
		/// Asynchronously run the specified process in the background with the specified arguments.
		/// The contents of the std (binary) and err (string) output streams are provided to callback functions.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">Binary data buffers read from standard output stream are sent to this callback.</param>
		/// <param name="err">Lines read from standard error stream are sent to this callback.</param>
		/// <param name="options">(Optional) Additional options.</param>
		/// <returns>An object containing the Process instance and helper functions.</returns>
		public static ProcessRunnerHandle RunProcess_StdBinary_ErrString(string fileName, string arguments, Action<byte[]> std, Action<string> err, ProcessRunnerOptions options = null)
		{
			if (options == null)
				options = new ProcessRunnerOptions();

			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);

			options?.Apply(psi);
			Process p = Process.Start(psi);
			options?.Apply(p);

			if (options == null || !options.RunAsAdministrator)
			{
				p.ErrorDataReceived += (sender, e) =>
				{
					err(e.Data);
				};
				p.BeginErrorReadLine();
			}

			return new ProcessRunnerHandle(p)
			{
				stdoutReader = new ProcessStreamBinaryReader(p.StandardOutput.BaseStream, std, options)
			};
		}

		/// <summary>
		/// Asynchronously run the specified process in the background with the specified arguments.
		/// The contents of the std (string) and err (binary) output streams are provided to callback functions.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">Lines read from standard output stream are sent to this callback.</param>
		/// <param name="err">Binary data buffers read from standard error stream are sent to this callback.</param>
		/// <param name="options">(Optional) Additional options.</param>
		/// <returns>An object containing the Process instance and helper functions.</returns>
		public static ProcessRunnerHandle RunProcess_StdString_ErrBinary(string fileName, string arguments, Action<string> std, Action<byte[]> err, ProcessRunnerOptions options = null)
		{
			if (options == null)
				options = new ProcessRunnerOptions();

			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);

			options?.Apply(psi);
			Process p = Process.Start(psi);
			options?.Apply(p);

			if (options == null || !options.RunAsAdministrator)
			{
				p.OutputDataReceived += (sender, e) =>
				{
					std(e.Data);
				};
				p.BeginOutputReadLine();
			}

			return new ProcessRunnerHandle(p)
			{
				stderrReader = new ProcessStreamBinaryReader(p.StandardError.BaseStream, err, options)
			};
		}

		/// <summary>
		/// Asynchronously run the specified process in the background with the specified arguments.
		/// The contents of the std (binary) and err (binary) output streams are provided to callback functions.
		/// </summary>
		/// <param name="fileName">The path to the process to start.</param>
		/// <param name="arguments">Arguments for the process.</param>
		/// <param name="std">Binary data buffers read from standard output stream are sent to this callback.</param>
		/// <param name="err">Binary data buffers from standard error stream are sent to this callback.</param>
		/// <param name="options">(Optional) Additional options.</param>
		/// <returns>An object containing the Process instance and helper functions.</returns>
		public static ProcessRunnerHandle RunProcess_StdBinary_ErrBinary(string fileName, string arguments, Action<byte[]> std, Action<byte[]> err, ProcessRunnerOptions options = null)
		{
			if (options == null)
				options = new ProcessRunnerOptions();

			ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);

			options?.Apply(psi);
			Process p = Process.Start(psi);
			options?.Apply(p);

			return new ProcessRunnerHandle(p)
			{
				stdoutReader = new ProcessStreamBinaryReader(p.StandardOutput.BaseStream, std, options),
				stderrReader = new ProcessStreamBinaryReader(p.StandardError.BaseStream, err, options)
			};
		}
		/// <summary>
		/// <para>Opens the specified path using <see cref="ProcessStartInfo.UseShellExecute"/> == true, and if that fails, tries with <see cref="ProcessStartInfo.UseShellExecute"/> == false.</para>
		/// <para>This method exists because <see cref="ProcessStartInfo.UseShellExecute"/> is true by default in .NET Framework apps, but false in .NET Core apps including .NET 5.0+.</para>
		/// </summary>
		/// <param name="path">Path to open.  Could be a URL, a path to an executable or a text file, etc.</param>
		public static Process Start(string path)
		{
			ProcessStartInfo psi = new ProcessStartInfo(path);
			psi.UseShellExecute = true;
			try
			{
				return Process.Start(psi);
			}
			catch
			{
				psi.UseShellExecute = false;
				return Process.Start(psi);
			}
		}
	}

	/// <summary>
	/// Options to customize the process runner.
	/// </summary>
	public class ProcessRunnerOptions
	{
		/// <summary>
		/// The priority to set for the process.  This may have no effect if we don't have permission to change the process priority.
		/// </summary>
		public ProcessPriorityClass? priority;
		/// <summary>
		/// Environment variables to set for the process.
		/// </summary>
		public Dictionary<string, string> environmentVariables = new Dictionary<string, string>();
		/// <summary>
		/// Working directory to set in the <see cref="ProcessStartInfo"/> (if not null).
		/// </summary>
		public string workingDirectory;
		/// <summary>
		/// Gets or sets a value indicating whether to use the operating system shell to start the process. (Default: false)
		/// </summary>
		public bool UseShellExecute = false;
		/// <summary>
		/// Gets or sets a value indicating if we should try to start the process in the background with no visible windows. (Default: true)
		/// </summary>
		public bool CreateNoWindow = true;
		/// <summary>
		/// If not null, the <see cref="ProcessStartInfo.StandardErrorEncoding"/> property will be set to this.
		/// </summary>
		public Encoding StandardOutputEncoding = null;
		/// <summary>
		/// If not null, the <see cref="ProcessStartInfo.StandardErrorEncoding"/> property will be set to this.
		/// </summary>
		public Encoding StandardErrorEncoding = null;
		/// <summary>
		/// <para>If true, the process will be run as administrator, which typically causes a UAC prompt.</para>
		/// <para>Running as administrator prevents accessing the Standard Input/Output/Error streams of the process.</para>
		/// <para>Running as administrator forces <see cref="UseShellExecute"/> to be set to true.</para>
		/// </summary>
		public bool RunAsAdministrator = false;

		/// <summary>
		/// Constructs an empty ProcessRunnerOptions.
		/// </summary>
		public ProcessRunnerOptions() { }
		/// <summary>
		/// Constructs a ProcessRunnerOptions.
		/// </summary>
		/// <param name="priority">Priority which the process should be set to just after it is started.</param>
		public ProcessRunnerOptions(ProcessPriorityClass? priority)
		{
			this.priority = priority;
		}
		/// <summary>
		/// Applies options from this class to the ProcessStartInfo object.
		/// </summary>
		/// <param name="psi">A ProcessStartInfo to apply the options to.</param>
		internal void Apply(ProcessStartInfo psi)
		{
			foreach (KeyValuePair<string, string> v in environmentVariables)
				psi.EnvironmentVariables[v.Key] = v.Value;
			if (workingDirectory != null)
				psi.WorkingDirectory = workingDirectory;
			psi.CreateNoWindow = CreateNoWindow;
			if (RunAsAdministrator)
			{
				psi.Verb = "runas";
				psi.RedirectStandardInput = false;
				psi.RedirectStandardOutput = false;
				psi.RedirectStandardError = false;
				psi.UseShellExecute = UseShellExecute = true;
			}
			else
			{
				psi.UseShellExecute = UseShellExecute;
				psi.RedirectStandardOutput = true;
				psi.RedirectStandardError = true;
				if (StandardOutputEncoding != null)
					psi.StandardErrorEncoding = StandardOutputEncoding;
				if (StandardErrorEncoding != null)
					psi.StandardErrorEncoding = StandardErrorEncoding;
			}
		}

		/// <summary>
		/// Applies options from this class to the Process object.
		/// </summary>
		/// <param name="p">A Process to apply the options to.</param>
		internal void Apply(Process p)
		{
			if (priority.HasValue)
			{
				try
				{
					p.PriorityClass = priority.Value;
				}
				catch { }
			}
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
		/// Can contain a reference to a ProcessStreamBinaryReader for stdout.
		/// </summary>
		internal ProcessStreamBinaryReader stdoutReader;

		/// <summary>
		/// Can contain a reference to a ProcessStreamBinaryReader for stderr.
		/// </summary>
		internal ProcessStreamBinaryReader stderrReader;

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
			process.WaitForExit(); // Must call WaitForExit without a timeout in order to force output buffers to be flushed.
			return process.ExitCode;
		}

		/// <summary>
		/// Waits for the process to exit, then returns true if the process exited in the allotted time.
		/// </summary>
		/// <param name="millisecondTimeout">Number of milliseconds to wait.  If this timeout expires, the method returns false.</param>
		/// <param name="exitCode">The exit code returned by the process (only if the proxess exited in the allotted time).</param>
		/// <returns></returns>
		public bool WaitForExit(int millisecondTimeout, out int exitCode)
		{
			if (millisecondTimeout <= 0)
				throw new ArgumentException("millisecondTimeout argument must be a positive number. " + millisecondTimeout + " is not accepted.", "millisecondTimeout");
			Stopwatch sw = Stopwatch.StartNew();
			while (!process.HasExited)
			{
				double remaining = millisecondTimeout - sw.ElapsedMilliseconds;
				if (remaining <= 0)
					break;
				process.WaitForExit(((int)remaining).Clamp(1, 250));
			}
			if (process.HasExited)
			{
				process.WaitForExit(); // Must call WaitForExit without a timeout in order to force output buffers to be flushed.
				exitCode = process.ExitCode;
				return true;
			}
			else
			{
				exitCode = 0;
				return false;
			}
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
	/// <summary>
	/// Reads binary data from a stream and sends each buffer to a callback method.
	/// </summary>
	public class ProcessStreamBinaryReader
	{
		private Stream stream;
		private Action<byte[]> callback;
		private Thread thrReadStream;
		public readonly bool Enabled;
		public ProcessStreamBinaryReader(Stream stream, Action<byte[]> callback, ProcessRunnerOptions options)
		{
			Enabled = options == null || !options.RunAsAdministrator;
			if (Enabled)
			{
				this.stream = stream;
				this.callback = callback;
				thrReadStream = new Thread(ReadFromStream);
				thrReadStream.Name = "ProcessStreamBinaryReader";
				thrReadStream.IsBackground = true;
				thrReadStream.Start();
			}
		}
		private void ReadFromStream()
		{
			try
			{
				byte[] buffer = new byte[32768];
				int read = stream.Read(buffer, 0, buffer.Length);
				while (read > 0)
				{
					callback(ByteUtil.SubArray(buffer, 0, read));
					read = stream.Read(buffer, 0, buffer.Length);
				}
			}
			catch (EndOfStreamException) { }
		}
	}
}
