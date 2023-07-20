using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// <para>Offers advanced SSL certificate selection capabilities.</para>
	/// <para>* ServerName-based certificate selection with access to the HttpProcessor in its early state of request processing.</para>
	/// <para>* A method to retrieve a special certificate in order to complete "acme-tls/1" or "TLS-ALPN-01" validation.</para>
	/// </summary>
	public interface ICertificateSelector
	{
		/// <summary>
		/// Returns an X509Certificate or null if no certificate is available (the connection will be closed).
		/// </summary>
		/// <param name="p">The HttpProcessor instance in its current early state of processing.  Many fields have not been initialized yet.</param>
		/// <param name="serverName">The server name as indicated by ServerNameIndication. May be null or empty. May be omitted if the configured certificate resolver is known to ignore this value.</param>
		/// <returns>an X509Certificate or null</returns>
		Task<X509Certificate> GetCertificate(HttpProcessor p, string serverName);
		/// <summary>
		/// Returns a special X509Certificate (or null) in order to complete "acme-tls/1" or "TLS-ALPN-01" validation.
		/// </summary>
		/// <param name="p">The HttpProcessor instance in its current early state of processing.  Many fields have not been initialized yet.</param>
		/// <param name="serverName">The server name as indicated by ServerNameIndication.  This is a required parameter and should not be null or empty.</param>
		/// <returns>an X509Certificate or null</returns>
		Task<X509Certificate> GetAcmeTls1Certificate(HttpProcessor p, string serverName);
	}
}