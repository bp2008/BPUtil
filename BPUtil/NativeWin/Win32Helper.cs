using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	public static class Win32Helper
	{
		/// <summary>
		/// If the given result is a failure code (non-zero), throws a Win32Exception with the function name and error code and message.
		/// </summary>
		/// <param name="result">Return status code from a Win32 API function, where 0 indicates success.</param>
		/// <param name="functionName">The name of the function, to include in the exception message if the <paramref name="result"/> code indicated an error.</param>
		/// <exception cref="Win32Exception"></exception>
		public static void ThrowIfFailed(uint result, string functionName)
		{
			if (result != 0)
				ThrowWin32Error(result, functionName + " failed");
		}

		/// <summary>
		/// Retrieves the last Win32 error code and throws a Win32Exception with an optional custom message prepended.
		/// </summary>
		/// <param name="message"></param>
		/// <exception cref="Win32Exception"></exception>
		public static void ThrowLastWin32Error(string message = null)
		{
			int errorCode = Marshal.GetLastWin32Error();
			if (message != null)
				throw new Win32Exception(errorCode, message + " - Windows Error Code " + errorCode + ": " + GetErrorMessage(errorCode));
			else
				throw new Win32Exception(errorCode, "Windows Error Code " + errorCode + ": " + GetErrorMessage(errorCode));
		}
		/// <summary>
		/// Throws a Win32Exception for the specified Win32 error code with an optional custom message prepended.
		/// </summary>
		/// <param name="errorCode">Error code</param>
		/// <param name="message">Optional additional message to prepend to the system-generated error message.</param>
		/// <exception cref="Win32Exception"></exception>
		public static void ThrowWin32Error(int errorCode, string message = null)
		{
			if (message != null)
				throw new Win32Exception(errorCode, message + " - Windows Error Code " + errorCode + ": " + GetErrorMessage(errorCode));
			else
				throw new Win32Exception(errorCode, "Windows Error Code " + errorCode + ": " + GetErrorMessage(errorCode));
		}
		/// <summary>
		/// Throws a Win32Exception for the specified Win32 error code with an optional custom message prepended.
		/// </summary>
		/// <param name="errorCode">Error code</param>
		/// <param name="message">Optional additional message to prepend to the system-generated error message.</param>
		/// <exception cref="Win32Exception"></exception>
		public static void ThrowWin32Error(uint errorCode, string message = null)
		{
			if (message != null)
				throw new Win32Exception((int)errorCode, message + " - Windows Error Code " + errorCode + ": " + GetErrorMessage(errorCode));
			else
				throw new Win32Exception((int)errorCode, "Windows Error Code " + errorCode + ": " + GetErrorMessage(errorCode));
		}
		/// <summary>
		/// Gets the error message associated with a Win32 error code.
		/// </summary>
		/// <param name="errorCode">Win32 error code</param>
		/// <returns></returns>
		public static string GetErrorMessage(int errorCode)
		{
			return new Win32Exception(errorCode).Message;
		}
		/// <summary>
		/// Gets the error message associated with a Win32 error code.
		/// </summary>
		/// <param name="errorCode">Win32 error code</param>
		/// <returns></returns>
		public static string GetErrorMessage(uint errorCode)
		{
			return new Win32Exception((int)errorCode).Message;
		}
		/// <summary>
		/// Calls Marshal.GetLastWin32Error(). Returns the error code returned by the last unmanaged function that was called using platform invoke that has the <c>SetLastError</c> flag set.
		/// </summary>
		/// <returns>The last Win32 error code set by a call to the Win32 SetLastError function.</returns>
		public static int GetLastWin32Error()
		{
			return Marshal.GetLastWin32Error();
		}
	}
}
