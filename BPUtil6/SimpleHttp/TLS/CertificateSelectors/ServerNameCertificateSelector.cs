using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	public class ServerNameCertificateSelector : ICertificateSelector
	{
		/// <summary>
		/// Dictionary which maps lower-case domain names to certificates. To specify a default certificate, use <see cref="string.Empty"/> as the key.
		/// </summary>
		public ConcurrentDictionary<string, X509Certificate> allCertsByDomain = new ConcurrentDictionary<string, X509Certificate>();

		/// <summary>
		/// Returns an X509Certificate or null if no certificate is available (the connection will be closed).
		/// </summary>
		/// <param name="p">The HttpProcessor instance in its current early state of processing.  Many fields have not been initialized yet.</param>
		/// <param name="serverName">The server name as indicated by ServerNameIndication. May be null or empty. May be omitted if the configured certificate resolver is known to ignore this value.</param>
		/// <returns>an X509Certificate or null</returns>
		public Task<X509Certificate> GetCertificate(HttpProcessor p, string serverName)
		{
			if (serverName == null)
				serverName = "";
			else
				serverName = serverName.ToUpper();

			X509Certificate cert;
			if (allCertsByDomain.TryGetValue(serverName, out cert))
				return Task.FromResult(cert);
			else if (allCertsByDomain.TryGetValue("", out cert)) // Fallback to default
				return Task.FromResult(cert);
			else
				return Task.FromResult<X509Certificate>(null); // Fallback to null
		}

		/// <summary>
		/// Sets the certificate for the specified server name.
		/// </summary>
		/// <param name="serverName">The domain name of the server.  To specify a default certificate, use <see cref="string.Empty"/> as the serverName.  Null is treated as empty string.  The upper-case form of this string be used as the dictionary key in order to avoid case-sensitivity.</param>
		/// <param name="cert">The certificate.</param>
		public void SetCertificate(string serverName, X509Certificate cert)
		{
			if (serverName == null)
				serverName = "";
			else
				serverName = serverName.ToUpper();

			allCertsByDomain[serverName] = cert;
		}
		/// <summary>
		/// Returns null.
		/// </summary>
		/// <param name="p">The HttpProcessor instance in its current early state of processing.  Many fields have not been initialized yet.</param>
		/// <param name="serverName">The server name as indicated by ServerNameIndication.  This is a required parameter and should not be null or empty.</param>
		/// <returns>null</returns>
		public Task<X509Certificate> GetAcmeTls1Certificate(HttpProcessor p, string serverName)
		{
			return Task.FromResult<X509Certificate>(null);
		}
	}
}