using System;

namespace BPUtil.SimpleHttp.Client
{
	/// <summary>
	/// Contains information about whether an http proxy operation was successful.  May contain a detailed error message intended for the administrator/developer.
	/// </summary>
	public class ProxyResult
	{
		/// <summary>
		/// True if the requested proceeded normally.  False otherwise, in which case [ErrorMessage] explains what was wrong.  In either case, a response has been written to the HttpProcessor.
		/// </summary>
		public bool Success { get { return ErrorCode == ProxyResultErrorCode.Success; } }
		/// <summary>
		/// Error Code for programmatic handling of specific errors.
		/// </summary>
		public ProxyResultErrorCode ErrorCode;
		/// <summary>
		/// If true, the ProxyClient still has an open connection and can be used again.
		/// </summary>
		public bool IsProxyClientReusable = false;
		/// <summary>
		/// Error message, provided only if Success is false.
		/// </summary>
		public string ErrorMessage;
		/// <summary>
		/// May be true if [Success] is false, indicating that the request may be able to proceed normally if it is attempted with a different connection.
		/// </summary>
		public bool ShouldTryAgainWithAnotherConnection = false;

		/// <summary>
		/// Constructs a ProxyResult.
		/// </summary>
		/// <param name="errorCode">Error Code for programmatic handling of specific errors.</param>
		/// <param name="errorMessage">Error message, provided only if Success is false.</param>
		/// <param name="isProxyClientReusable">If true, the ProxyClient still has an open connection and can be used again.</param>
		/// <param name="shouldTryAgainWithAnotherConnection">May be true if [Success] is false, indicating that the request may be able to proceed normally if it is attempted with a different connection.</param>
		public ProxyResult(ProxyResultErrorCode errorCode, string errorMessage, bool isProxyClientReusable, bool shouldTryAgainWithAnotherConnection)
		{
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
			IsProxyClientReusable = isProxyClientReusable;
			ShouldTryAgainWithAnotherConnection = shouldTryAgainWithAnotherConnection;

			if (Success != string.IsNullOrEmpty(errorMessage))
				throw new ArgumentException("Ambiguous ProxyResult with code: " + errorCode + " and errorMessage: " + (errorMessage == null ? "null" : ("\"" + errorMessage + "\"")));
		}
	}
	public enum ProxyResultErrorCode
	{
		/// <summary>
		/// Request was proxied successfully.
		/// </summary>
		Success = 0,
		/// <summary>
		/// The remote server did not respond.
		/// </summary>
		GatewayTimeout = 1,
		/// <summary>
		/// Failed to perform TLS negotiation with the remote server.
		/// </summary>
		TLSNegotiationError = 2,
		/// <summary>
		/// We think the remote server did something wrong.
		/// </summary>
		BadGateway = 3,
		/// <summary>
		/// A generic error.
		/// </summary>
		Error = 4
	}
}