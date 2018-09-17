using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BPUtil.NativeWin;

namespace BPUtil
{
	/// <summary>
	/// Flags which guide ProcessHelper.ExecuteInteractive in selecting a process to impersonate.
	/// The interaction of various flags is not well-documented, and study of the source code may be required to understand the resulting behavior.
	/// </summary>
	[Flags]
	public enum ProcessExecuteFlags
	{
		None = 0b0000_0000,
		Default = 0b0000_1110,
		/// <summary>
		/// If set, ProcessHelper will prefer to impersonate explorer.exe.  Since explorer.exe is not usually run as an administrator, it may be impossible to start a process that requires administrator.
		/// </summary>
		ImpersonateExplorer = 0b0000_0001,
		/// <summary>
		/// If set, ProcessHelper will prefer to impersonate winlogon.exe.  Rumor has it that a process impersonating winlogin is killed by the system after about 10 minutes.
		/// </summary>
		ImpersonateWinlogon = 0b0000_0010,
		/// <summary>
		/// If set, ProcessHelper will fall back to impersonating any other process which does not have its own flag, but meets the other selection critera.
		/// </summary>
		ImpersonateAnyProcess = 0b0000_0100,
		/// <summary>
		/// If set, ProcessHelper will treat LocalSystem as a suitable user.  When allowed by this flag, LocalSystem is the first priority.
		/// </summary>
		ImpersonateLocalSystem = 0b0000_1000,
		/// <summary>
		/// If set, ProcessHelper will treat a specific user (by name) as a suitable user.
		/// </summary>
		ImpersonateSpecificUser = 0b0001_0000,
		/// <summary>
		/// If set, ProcessHelper will not care which user it impersonates.
		/// </summary>
		ImpersonateAnyUser = 0b0010_0000

	}
	public class ProcessExecuteArgs
	{
		public ProcessExecuteFlags flags;
		/// <summary>
		/// To be set when using ProcessExecuteFlags.ImpersonateSpecificUser.  This specifies the user name of the suitable user.
		/// </summary>
		public string userName = null;
		/// <summary>
		/// (Default: false) If true, case-insensitive comparison is used when matching the [userName] field.
		/// </summary>
		public bool userNameIgnoreCase = false;
		/// <summary>
		/// To be used internally by the ProcessHelper.ExecuteInteractive method.
		/// </summary>
		internal int requiredSessionID = -1;

		public ProcessExecuteArgs() { }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="flags">Flags to guide ProcessHelper.ExecuteInteractive.</param>
		/// <param name="userName">To be set when using ProcessExecuteFlags.ImpersonateSpecificUser.  This specifies the user name of the suitable user.</param>
		public ProcessExecuteArgs(ProcessExecuteFlags flags, string userName = null)
		{
			this.flags = flags;
			this.userName = userName;
		}
		public static ProcessExecuteArgs Default()
		{
			return new ProcessExecuteArgs(ProcessExecuteFlags.Default);
		}

	}
	/// <summary>
	/// Provides Windows-only process utility methods.
	/// </summary>
	public class ProcessHelper
	{
		/// <summary>
		/// Attempts to start the specified interactive process in the current console session (a.k.a. local desktop visible on the computer's monitor) by impersonating the security context of another process that is running in that session.  This method can be called from a background service to start an interactive process.  Returns the process ID of the executed program, or -1 if there was a problem. May also throw an exception.
		/// </summary>
		/// <param name="executablePath">This can be null if the first token in the [commandLine] argument is the executable path.</param>
		/// <param name="commandLine">Sometimes, for unknown reasons, the first argument in this command line can be dropped.  This may be avoided by passing null for [executablePath] and including the executable path as the first token of [commandLine].</param>
		/// <param name="workingDirectory"></param>
		/// <param name="execArgs">Optional arguments to control process selection.</param>
		/// <returns></returns>
		public static int ExecuteInteractive(string executablePath, string commandLine, string workingDirectory, ProcessExecuteArgs execArgs = null)
		{
			if (execArgs == null)
				execArgs = ProcessExecuteArgs.Default();
			execArgs.requiredSessionID = GetConsoleSessionId();
			if (execArgs.requiredSessionID == -1)
				return -1; // No session currently attached to the console.

			// In order to be able to open a process when no user is logged in, we will use an
			// existing process from the active console session as a sort of template.

			int templateProcessId = -1;
			if (templateProcessId == -1 && (execArgs.flags & ProcessExecuteFlags.ImpersonateExplorer) > 0)
			{
				// First, we will try explorer.exe
				// GetProcessesByName uses case-sensitive matching.
				templateProcessId = Process.GetProcessesByName("explorer").FirstOrDefault(p => IsSuitableProcess(p, execArgs))?.Id ?? -1;
				if (templateProcessId != -1)
					Logger.Info("Will impersonate explorer.exe for session " + execArgs.requiredSessionID);
			}

			if (templateProcessId == -1 && (execArgs.flags & ProcessExecuteFlags.ImpersonateWinlogon) > 0)
			{
				// Next, we will try winlogon.exe, though it is said a process impersonating winlogon is killed by the system after about 10 minutes.
				templateProcessId = Process.GetProcessesByName("winlogon").FirstOrDefault(p => IsSuitableProcess(p, execArgs))?.Id ?? -1;
				if (templateProcessId != -1)
					Logger.Info("Will impersonate winlogon.exe for session " + execArgs.requiredSessionID);
			}

			if (templateProcessId == -1 && (execArgs.flags & ProcessExecuteFlags.ImpersonateAnyProcess) > 0)
			{
				// If the above scans failed, try any process that meets the other criteria.
				templateProcessId = Process.GetProcesses().FirstOrDefault(p => IsSuitableProcess(p, execArgs))?.Id ?? -1;
				if (templateProcessId != -1)
					Logger.Info("Will impersonate an arbitrary local system process for session " + execArgs.requiredSessionID); // Don't log the process name, as that could potentially reveal private information if a log is shared.
			}

			if (templateProcessId == -1)
				return -1; // No process could be found to use as a template.

			// Open Process
			using (AutoDisposeHandle cloneableProcHandle = OpenProcess(templateProcessId))
			{
				// Get token from process
				using (AutoDisposeHandle originalToken = OpenProcessToken(cloneableProcHandle))
				{
					if (originalToken == null)
						return -1;

					// Clone the token
					using (AutoDisposeHandle duplicatedToken = DuplicateTokenEx(originalToken))
					{
						if (duplicatedToken == null)
							return -1;

						// Try to start process
						return CreateProcessAsUser(executablePath, commandLine, workingDirectory, duplicatedToken);
					}
				}
			}
		}

		/// <summary>
		/// Returns the current active console session Id, or -1 if there is no session currently attached to the physical console.
		/// </summary>
		/// <returns></returns>
		public static int GetConsoleSessionId()
		{
			return NativeMethods.WTSGetActiveConsoleSessionId();
		}

		public static Process GetProcByID(int id)
		{
			if (id == -1)
				return null;
			// Process.GetProcessById(id) throws an exception if the process can't be found.
			Process[] processlist = Process.GetProcesses();
			return processlist.FirstOrDefault(pr => pr.Id == id);
		}
		/// <summary>
		/// Returns true if the specified process is suitable for impersonation based on all the rules specified in the ProcessExecuteArgs instance.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="execArgs"></param>
		/// <returns></returns>
		private static bool IsSuitableProcess(Process p, ProcessExecuteArgs execArgs)
		{
			// p.ProcessName does not include the file extension. GetProcessesByName uses case-sensitive matching, so we'll use case-insensitive matching on process names here too.
			if (p.SessionId != execArgs.requiredSessionID)
				return false;
			else if ((execArgs.flags & ProcessExecuteFlags.ImpersonateExplorer) == 0 && string.Compare(p.ProcessName, "explorer", true) == 0)
				return false;
			else if ((execArgs.flags & ProcessExecuteFlags.ImpersonateWinlogon) == 0 && string.Compare(p.ProcessName, "winlogon", true) == 0)
				return false;
			else if (!ProcessOwnedBySuitableUser(p.Id, execArgs))
				return false;
			return true;
		}
		private static bool ProcessOwnedBySuitableUser(int pid, ProcessExecuteArgs execArgs)
		{
			if ((execArgs.flags & ProcessExecuteFlags.ImpersonateAnyUser) > 0)
				return true;
			if ((execArgs.flags & ProcessExecuteFlags.ImpersonateLocalSystem) > 0 && UserIsMatch(pid, WellKnownSidType.LocalSystemSid))
				return true;
			if ((execArgs.flags & ProcessExecuteFlags.ImpersonateSpecificUser) > 0 && ProcessOwnedByUser(pid, execArgs.userName, execArgs.userNameIgnoreCase))
				return true;
			return false;
		}
		/// <summary>
		/// Determines if the specified process is owned by the specified user.
		/// </summary>
		/// <param name="pid">The ID of the process.</param>
		/// <param name="userName">A user name.  If the provided user name contains a domain name, the domain name of the processes's owner must match in order for this method to return true.</param>
		/// <param name="ignoreCase">If true, the user name comparison is case-insensitive.</param>
		/// <returns></returns>
		public static bool ProcessOwnedByUser(int pid, string userName, bool ignoreCase = false)
		{
			if (userName == null)
				return false;
			string owner = GetUserWhichOwnsProcess(pid);
			int userNameIdxSlash = userName.IndexOf('\\');
			if (userNameIdxSlash == -1) // Specified user name did not contain a domain name, so we should remove it from the [owner] string.
				owner = owner.Substring(owner.IndexOf('\\') + 1);
			return string.Compare(owner, userName, ignoreCase) == 0;
		}
		/// <summary>
		/// Returns the name of the user which owns the specified process, including domain name. e.g. "NT AUTHORITY\SYSTEM"
		/// </summary>
		/// <param name="pid">The ID of the process.</param>
		/// <returns></returns>
		public static string GetUserWhichOwnsProcess(int pid)
		{
			try
			{
				using (AutoDisposeHandle processHandle = OpenProcess(pid))
				using (AutoDisposeHandle processToken = OpenProcessToken(processHandle))
				using (WindowsIdentity identity = new WindowsIdentity(processToken))
					return identity.Name;
			}
			catch
			{
				return null;
			}
		}
		private static bool UserIsMatch(int pid, WellKnownSidType type)
		{
			try
			{
				using (AutoDisposeHandle processHandle = OpenProcess(pid))
				using (AutoDisposeHandle processToken = OpenProcessToken(processHandle))
				using (WindowsIdentity identity = new WindowsIdentity(processToken))
					return identity.User.IsWellKnown(type);
			}
			catch
			{
				return false;
			}
		}

		private static AutoDisposeHandle OpenProcess(int templateProcessId)
		{
			return AutoDisposeHandle.Create(NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, templateProcessId), h => NativeMethods.CloseHandle(h));
		}
		private static AutoDisposeHandle OpenProcessToken(IntPtr processHandle)
		{
			IntPtr handle;
			if (NativeMethods.OpenProcessToken(processHandle, (uint)TokenAccessLevels.MaximumAllowed, out handle))
				return AutoDisposeHandle.Create(handle, h => NativeMethods.CloseHandle(h));
			return null;
		}
		private static AutoDisposeHandle DuplicateTokenEx(IntPtr originalToken)
		{
			IntPtr handle;
			if (NativeMethods.DuplicateTokenEx(originalToken, (uint)TokenAccessLevels.MaximumAllowed, IntPtr.Zero, NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, NativeMethods.TOKEN_TYPE.TokenPrimary, out handle))
				return AutoDisposeHandle.Create(handle, h => NativeMethods.CloseHandle(h));
			return null;
		}
		private static AutoDisposeHandle CreateEnvironmentBlock(IntPtr userToken)
		{
			IntPtr handle;
			if (NativeMethods.CreateEnvironmentBlock(out handle, userToken, false))
				return AutoDisposeHandle.Create(handle, h => NativeMethods.DestroyEnvironmentBlock(h));
			return null;
		}
		private static int CreateProcessAsUser(string executablePath, string commandLine, string workingDirectory, IntPtr userToken)
		{
			using (AutoDisposeHandle environmentVariables = CreateEnvironmentBlock(userToken))
			{
				if (environmentVariables == null)
					return -1;

				NativeMethods.STARTUPINFO startupInformation = new NativeMethods.STARTUPINFO();
				startupInformation.length = Marshal.SizeOf(typeof(NativeMethods.STARTUPINFO));
				startupInformation.desktop = "Winsta0\\Default";
				startupInformation.showWindow = (short)NativeMethods.WindowShowStyle.ShowNoActivate;
				NativeMethods.PROCESS_INFORMATION processInformation = new NativeMethods.PROCESS_INFORMATION();
				try
				{
					bool result = NativeMethods.CreateProcessAsUser
					(
						userToken,
						executablePath,
						commandLine,
						IntPtr.Zero,
						IntPtr.Zero,
						false,
						(uint)(NativeMethods.CreateProcessFlags.DETACHED_PROCESS | NativeMethods.CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT),
						environmentVariables,
						workingDirectory,
						ref startupInformation,
						ref processInformation
					);
					if (!result)
						Win32Helper.ThrowLastWin32Error("Unable to start process \"" + executablePath + "\"");
					return processInformation.processID;
				}
				finally
				{
					if (processInformation.processHandle != IntPtr.Zero)
					{
						try
						{
							NativeMethods.CloseHandle(processInformation.processHandle);
						}
						catch { }
					}
					if (processInformation.threadHandle != IntPtr.Zero)
					{
						try
						{
							NativeMethods.CloseHandle(processInformation.threadHandle);
						}
						catch { }
					}
				}
			}
		}
	}
}
