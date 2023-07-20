using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.PasswordReset
{
	/// <summary>
	/// <para>A base class providing secure password reset functionality that requires no server-side state to be maintained between steps.</para>
	/// <para>Accounts without an email address on file will be unable to use this password reset algorithm.  Security is achieved by sending an email containing a link with a specially computed security token.  The user making the request must click this link, thereby verifying to us that they own the email address which was on file for the account.</para>
	/// <para>The security token is the cryptographic signature of the unique identifier for the account, combined with an imprecise timestamp (e.g. unix timestamp divided by 5 minutes).</para>
	/// <para>Any server with a matching private key will be able to validate a security token within a short timeframe from its creation, and therefore allow the password to be reset.</para>
	/// <para>After verifying account ownership, the user will be sent a new password.</para>
	/// </summary>
	public abstract class StatelessPasswordResetBase
	{
		/// <summary>
		/// This must be called prior to using StatelessPasswordResetBase, or else NullReferenceException will be thrown.
		/// </summary>
		/// <param name="SignatureFactoryKey">A key you have previously generated and saved by calling `new SignatureFactory()` and retrieving its private key.</param>
		public static void Initialize(string SignatureFactoryKey)
		{
			SecureToken.Initialize(SignatureFactoryKey);
		}
		public readonly string accountType;
		public StatelessPasswordResetBase(string accountType)
		{
			this.accountType = accountType;
		}
		#region Public API
		/// <summary>
		/// Gets a PasswordResetRequest object containing data necessary to send a password reset email to the user.  The email should contain a link which should result in the CompletePasswordReset method being called.
		/// Returns null if the account does not exist, or if it has no email address on file, or if the user is not allowed to change the password.
		/// </summary>
		/// <param name="accountIdentifier">The unique identifier for the account (user name or email address, depending on account type).</param>
		public PasswordResetRequest GetResetRequest(string accountIdentifier)
		{
			AccountInfo account = GetCurrentAccountInfo(accountIdentifier);
			if (account == null || string.IsNullOrWhiteSpace(account.Email))
				return null;

			account.Type = accountType; // In case the derived class forgets to set this.

			return CreateResetRequest(account);
		}

		/// <summary>
		/// Completes the password reset as requested and returns the new password.  If the request fails to validate (it may have been tampered with, expired, etc) returns null.
		/// </summary>
		/// <param name="type">Account type.</param>
		/// <param name="accountIdentifier">The unique identifier for the account (user name or email address, depending on account type).</param>
		/// <param name="token">The token from a reset request.</param>
		/// <param name="req">Upon success, this is set to a copy of the PasswordResetRequest so that some metadata such as the email address and user display name can be returned.</param>
		/// <returns></returns>
		public string CompletePasswordReset(string type, string accountIdentifier, string token, out PasswordResetRequest req)
		{
			req = null;
			if (type != accountType)
				throw new Exception(this.GetType().Name + " received PasswordResetRequest with type " + type + ". Expected type " + accountType + ".");

			AccountInfo account = GetCurrentAccountInfo(accountIdentifier);
			if (account == null || string.IsNullOrWhiteSpace(account.Email))
				return null; // Specified account is not eligible for password resets.

			account.Type = accountType; // In case the derived class forgets to set this.

			if (SecureToken.VerifyToken(account, token))
			{
				string newPassword = GenerateNewPassword();
				if (CommitPasswordChange(account.Identifier, newPassword))
				{
					req = CreateResetRequest(account, token);
					return newPassword;
				}
				else
					return null; // Password change failed
			}
			else
				return null; // Request validation failed
		}
		#endregion
		#region Helpers
		private static PasswordResetRequest CreateResetRequest(AccountInfo account, string token = null)
		{
			PasswordResetRequest req = new PasswordResetRequest();
			req.accountIdentifier = account.Identifier;
			req.accountType = account.Type;
			req.secureToken = token != null ? token : SecureToken.GetToken(account);
			req.email = account.Email;
			req.displayName = account.DisplayName;
			req.tokenExpiration = SecureToken.GetMinimumTokenLifespan();
			return req;
		}
		private string GenerateNewPassword()
		{
			return StringUtil.GetRandomAlphaNumericString(12);
		}
		#endregion
		#region Abstract Methods
		/// <summary>
		/// Returns true if the specified user identifier is eligible for password reset.
		/// </summary>
		/// <param name="accountIdentifier">Unique identifier for the account. User Name, Email Address, etc.</param>
		/// <returns></returns>
		protected abstract AccountInfo GetCurrentAccountInfo(string accountIdentifier);
		/// <summary>
		/// Assigns a new password to an account. Returns true if successful.  Derived classes should not return false without first sending an error email.
		/// </summary>
		/// <param name="accountIdentifier">Unique identifier for the account. User Name, Email Address, etc.</param>
		/// <param name="newPassword">New password for the account.</param>
		/// <returns></returns>
		protected abstract bool CommitPasswordChange(string accountIdentifier, string newPassword);
		#endregion
	}
}