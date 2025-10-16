using System;
using System.Runtime.Serialization;

namespace BPUtil.MVC
{
	/// <summary>
	/// An error that can/should be shown to the client.  E.g. When an action method is called with invalid arguments.
	/// </summary>
	[Serializable]
	internal class ClientException : Exception
	{
		public ClientException()
		{
		}

		public ClientException(string message) : base(message)
		{
		}

		public ClientException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}