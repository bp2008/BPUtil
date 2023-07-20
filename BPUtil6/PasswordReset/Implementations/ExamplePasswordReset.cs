using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;

namespace BPUtil.PasswordReset
{
	/// <summary>
	/// Example implementation allowing the stateless password reset system to access accounts from your database.
	/// </summary>
	public class ExamplePasswordReset : StatelessPasswordResetBase
	{
		public ExamplePasswordReset() : base("BPUtil_Example_Account_Type") // Make up an account type that is unique for the type of account you are using.  Most apps only deal with one account type, so this string won't matter.
		{
		}

		protected override AccountInfo GetCurrentAccountInfo(string accountIdentifier)
		{
			// Here is where you would look up the account in your database and return null if it doesn't exist, isn't allowed to change password, etc.
			AccountInfo accountInfo = new AccountInfo();
			accountInfo.Identifier = "ExampleAcctID";
			accountInfo.Email = "user@example.com";
			accountInfo.DisplayName = "Example User";
			accountInfo.Password = "Current Password"; // This should be whatever you store for the user's password; it doesn't need to be the actual password. It should be a hash.
			return accountInfo;
		}

		protected override bool CommitPasswordChange(string accountIdentifier, string newPassword)
		{
			// Here is where you would commit the password change in your database, then return true if successful. Return false if the user doesn't exist, isn't allowed to change password, or if the new password isn't complex enough, etc.
			return true;
		}
	}
}
