using System.Security.Cryptography.X509Certificates;

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
		/// <param name="serverName">ignored</param>
		/// <returns>an X509Certificate or null</returns>
		public X509Certificate GetCertificate(string serverName)
		{
			return cert;
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
	}
}