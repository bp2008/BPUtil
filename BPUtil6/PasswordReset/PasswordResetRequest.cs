using System;

namespace BPUtil.PasswordReset
{
	/// <summary>
	/// An object containing information required to complete a password reset operation.
	/// </summary>
	public class PasswordResetRequest
	{
		/// <summary>
		/// The unique identifier of the account which needs its password to be changed.
		/// </summary>
		public string accountIdentifier;
		/// <summary>
		/// An integer representing the account type.
		/// </summary>
		public string accountType;
		/// <summary>
		/// A signature which can be used to validate the request.
		/// </summary>
		public string secureToken;
		/// <summary>
		/// Email address to send the reset link to.
		/// </summary>
		public string email;
		/// <summary>
		/// Display name for the user. ex: "Hello " + displayName + "."
		/// </summary>
		public string displayName;
		/// <summary>
		/// The amount of time after which this token may expire. (actual expiration may occur a little later than this)
		/// </summary>
		public TimeSpan tokenExpiration;
	}
}