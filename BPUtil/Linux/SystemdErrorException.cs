using System;
using System.Runtime.Serialization;

namespace BPUtil.Linux
{
	/// <summary>
	/// An exception thrown by SystemdHelper because of a systemd-related errorr.
	/// </summary>
	[Serializable]
	internal class SystemdErrorException : Exception
	{
		/// <summary>
		/// Constructs a SystemdErrorException.
		/// </summary>
		public SystemdErrorException()
		{
		}
		/// <summary>
		/// Constructs a SystemdErrorException with a message.
		/// </summary>
		/// <param name="message">Error message.</param>
		public SystemdErrorException(string message) : base(message)
		{
		}
		/// <summary>
		/// Constructs a SystemdErrorException with a message and inner exception
		/// </summary>
		/// <param name="message">Error message.</param>
		/// <param name="innerException">The exception which caused this exception.</param>
		public SystemdErrorException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}