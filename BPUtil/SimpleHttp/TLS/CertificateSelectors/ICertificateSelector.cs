using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Selects an SSL certificate.
	/// </summary>
	public interface ICertificateSelector
	{
		/// <summary>
		/// Returns an X509Certificate or null.
		/// </summary>
		/// <param name="serverName">The server name as indicated by ServerNameIndication. May be null or empty. May be omitted if the configured certificate resolver is known to ignore this value.</param>
		/// <returns>an X509Certificate or null</returns>
		X509Certificate GetCertificate(string serverName);
	}
}