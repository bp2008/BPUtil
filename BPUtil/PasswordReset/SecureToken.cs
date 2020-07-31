using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.PasswordReset
{
	/// <summary>
	/// Produces secure time-based tokens for "stateless" password reset request verification.
	/// </summary>
	internal static class SecureToken
	{
		/// <summary>
		/// This must be called prior to using the other class methods, or else they will throw a null reference exception.
		/// </summary>
		/// <param name="SignatureFactoryKey">A key you have previously generated and saved by calling `new SignatureFactory()` and retrieving its private key.</param>
		internal static void Initialize(string SignatureFactoryKey)
		{
			sigFactory = new SignatureFactory(SignatureFactoryKey);
		}
		/// <summary>
		/// Private key for a SignatureFactory to be used to create tokens.
		/// </summary>
		private static SignatureFactory sigFactory = null;
		/// <summary>
		/// Timestamps used in token creation will increment by one on this interval.  E.g. with an interval of 5 minutes, the timestamp will increment only once every 5 minutes.
		/// </summary>
		private static readonly TimeSpan expirationPrecision = TimeSpan.FromMinutes(5);
		/// <summary>
		/// This number is multiplied by <see cref="expirationPrecision"/> to determine the minimum lifespan of a token.  A token may remain valid for up to one additional <see cref="expirationPrecision"/> interval, depending on the time of token creation.
		/// </summary>
		private static readonly int expirationMultiplier = 3;
		/// <summary>
		/// Timestamps are divided by this number to yield a timestamp with reduced precision.
		/// </summary>
		private static readonly long timestampDivisor = (long)expirationPrecision.TotalMilliseconds;

		/// <summary>
		/// Returns a secure token for the specified accountType and accountIdentifier.  The token will expire in 15-20 minutes.
		/// </summary>
		/// <param name="account">An AccountInfo representing the account which the user wishes to reset the password for.</param>
		/// <returns></returns>
		internal static string GetToken(AccountInfo account)
		{
			string data = GetData(TimeUtil.GetTimeInMsSinceEpoch() / timestampDivisor, account.Type, account.Identifier, account.Password);
			return sigFactory.Sign(data);
		}
		/// <summary>
		/// Verifies a secure token against the account data. Returns true if the token is valid and un-expired.
		/// </summary>
		/// <param name="account">An AccountInfo representing the account which the user wishes to reset the password for.</param>
		/// <param name="token">The token which was created earlier.</param>
		/// <returns></returns>
		internal static bool VerifyToken(AccountInfo account, string token)
		{
			long timestamp = TimeUtil.GetTimeInMsSinceEpoch() / timestampDivisor;
			for (int i = 0; i <= expirationMultiplier; i++) // Try the current timestamp and up to [expirationMultiplier] previous timestamps.
			{
				string data = GetData(timestamp - i, account.Type, account.Identifier, account.Password);
				if (sigFactory.Verify(data, token))
					return true;
			}
			return false;
		}
		/// <summary>
		/// Generates a data string which is to be signed.
		/// </summary>
		/// <param name="timestamp">A low-precision timestamp, to facilitate token expiration.</param>
		/// <param name="accountType">The type of account this is.  Necessary to uniquely identify an account because account identifiers are not unique between account systems.</param>
		/// <param name="accountIdentifier">The account's unique identifier within its account system (could be a user name or an email address).</param>
		/// <param name="currentPassword">The account's current password.  Including this in the data string ensures that the signature can't be accidentally used to change the password more than once before the signature expires.</param>
		/// <returns></returns>
		private static string GetData(long timestamp, string accountType, string accountIdentifier, string currentPassword)
		{
			return timestamp + ":" + accountType + ":" + accountIdentifier + ":" + currentPassword;
		}
		/// <summary>
		/// Gets the lower bound of the lifespan of a token. e.g. 15 minutes.
		/// </summary>
		/// <returns></returns>
		internal static TimeSpan GetMinimumTokenLifespan()
		{
			return TimeSpan.FromTicks(expirationPrecision.Ticks * expirationMultiplier);
		}
		/// <summary>
		/// Gets the upper bound of the lifespan of a token. e.g. 20 minutes.
		/// </summary>
		/// <returns></returns>
		internal static TimeSpan GetMaximumTokenLifespan()
		{
			return TimeSpan.FromTicks(expirationPrecision.Ticks * (expirationMultiplier + 1));
		}
	}
}
