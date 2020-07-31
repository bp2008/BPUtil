namespace BPUtil.PasswordReset
{
	/// <summary>
	/// Contains a minimal representation of an account for the purposes of resetting its password.
	/// </summary>
	public class AccountInfo
	{
		/// <summary>
		/// The type of account represented by this object.
		/// </summary>
		public string Type;
		/// <summary>
		/// The unique identifier of this account.
		/// </summary>
		public string Identifier;
		/// <summary>
		/// The current password or hash of password.
		/// </summary>
		public string Password;
		/// <summary>
		/// The account's email address.
		/// </summary>
		public string Email;
		/// <summary>
		/// Display name for the user. ex: "Hello " + displayName + "."
		/// </summary>
		public string DisplayName;
	}
}