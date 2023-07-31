using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Creates a SelfSignedCertificateSelector which accesses a shared (static) self-signed certificate that is automatically generated upon request.
	/// </summary>
	public class SelfSignedCertificateSelector : ICertificateSelector
	{
		private static object certCreationLock = new object();
		private static X509Certificate _cert;
		public X509Certificate cert
		{
			get
			{
				if (_cert == null)
				{
					lock (certCreationLock)
					{
						if (_cert == null)
							_cert = HttpServer.GetSelfSignedCertificate();
					}
				}
				return _cert;
			}
		}
		public SelfSignedCertificateSelector()
		{
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