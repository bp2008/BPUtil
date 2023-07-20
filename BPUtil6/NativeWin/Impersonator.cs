using System;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace BPUtil.NativeWin
{
	/// <summary>
	/// <para>
	/// ORIGINALLY FROM https://www.codeproject.com/Articles/10090/A-small-C-Class-for-impersonating-a-User
	/// </para>
	/// Impersonation of a user. Allows to execute code under another
	/// user context.
	/// Please note that the account that instantiates the Impersonator class
	/// needs to have the 'Act as part of operating system' privilege set.
	/// </summary>
	public static class Impersonator
	{
		#region P/Invoke.

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern int LogonUser(string lpszUserName, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int DuplicateToken(
			IntPtr hToken,
			int impersonationLevel,
			ref IntPtr hNewToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool RevertToSelf();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern bool CloseHandle(IntPtr handle);

		private const int LOGON32_LOGON_INTERACTIVE = 2;
		private const int LOGON32_PROVIDER_DEFAULT = 0;

		#endregion

		/// <summary>
		/// <para>Untested when ported to .NET 6.0</para>
		/// Runs the action as the user with the given credentials.
		/// Please note that the account that instantiates the Impersonator class
		/// needs to have the 'Act as part of operating system' privilege set.
		/// </summary>
		/// <param name="userName">The name of the user to act as.</param>
		/// <param name="domain">The domain name of the user to act as.</param>
		/// <param name="password">The password of the user to act as.</param>
		public static void ImpersonateValidUser(string userName, string domain, string password, Action action)
		{
			IntPtr token = IntPtr.Zero;
			IntPtr tokenDuplicate = IntPtr.Zero;

			try
			{
				if (RevertToSelf())
				{
					if (LogonUser(
						userName,
						domain,
						password,
						LOGON32_LOGON_INTERACTIVE,
						LOGON32_PROVIDER_DEFAULT,
						ref token) != 0)
					{
						if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
						{
							using (Microsoft.Win32.SafeHandles.SafeAccessTokenHandle handle = new Microsoft.Win32.SafeHandles.SafeAccessTokenHandle(tokenDuplicate))
							{
								WindowsIdentity.RunImpersonated(handle, action);
							}
						}
						else
						{
							throw new Win32Exception(Marshal.GetLastWin32Error());
						}
					}
					else
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
				else
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}
			}
			finally
			{
				if (token != IntPtr.Zero)
				{
					CloseHandle(token);
				}
				if (tokenDuplicate != IntPtr.Zero)
				{
					CloseHandle(tokenDuplicate);
				}
			}
		}
	}
}
