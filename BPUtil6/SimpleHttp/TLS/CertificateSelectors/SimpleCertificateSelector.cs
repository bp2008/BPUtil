using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	public class SimpleCertificateSelector : ICertificateSelector
	{
		public X509Certificate cert;
		public SimpleCertificateSelector(X509Certificate cert)
		{
			this.cert = cert;
		}
		/// <summary>
		/// Returns an X509Certificate or null.
		/// </summary>
		/// <param name="p">ignored</param>
		/// <param name="serverName">ignored</param>
		/// <returns>an X509Certificate or null</returns>
		public Task<X509Certificate> GetCertificate(HttpProcessor p, string serverName)
		{
			return Task.FromResult(cert);
		}

		/// <summary>
		/// Returns a SimpleCertificateSelector or null, depending on the certificate provided.
		/// </summary>
		/// <param name="cert">A certificate or null.</param>
		/// <returns></returns>
		public static ICertificateSelector FromCertificate(X509Certificate2 cert)
		{
			if (cert == null)
				return null;
			else
				return new SimpleCertificateSelector(cert);
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