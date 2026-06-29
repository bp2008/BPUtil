using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Creates a SelfSignedCertificateSelector which accesses a self-signed certificate that is automatically generated upon request.
	/// </summary>
	public class SelfSignedCertificateSelector : ICertificateSelector
	{
		private object certCreationLock = new object();
		private X509Certificate _cert;
		private string certDirectoryPath;
		private string certFileName;

		public X509Certificate cert
		{
			get
			{
				if (_cert == null)
				{
					lock (certCreationLock)
					{
						if (_cert == null)
							_cert = GetSelfSignedCertificate(certDirectoryPath, certFileName);
					}
				}
				return _cert;
			}
		}
		/// <summary>
		/// Constructs a SelfSignedCertificateSelector with the default directory path and file name (executable directory, "SimpleHttpServer-SslCert.pfx").
		/// </summary>
		public SelfSignedCertificateSelector() : this(GetDefaultCertificateDirectoryPath())
		{
		}
		/// <summary>
		/// Constructs a SelfSignedCertificateSelector with a configurable directory path and file name.
		/// </summary>
		/// <param name="certificateDirectoryPath">Path of the directory where the certificate should be saved to or loaded from.</param>
		/// <param name="certificateFileName">Name of the certificate file.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public SelfSignedCertificateSelector(string certificateDirectoryPath, string certificateFileName = "SimpleHttpServer-SslCert.pfx")
		{
			if (certificateDirectoryPath == null)
				throw new ArgumentNullException(nameof(certificateDirectoryPath));
			if (certificateFileName == null)
				throw new ArgumentNullException(nameof(certificateFileName));
			if (StringUtil.MakeSafeForFileName(certificateFileName) != certificateFileName)
				throw new ArgumentException("Certificate file name is not a valid file name.", nameof(certificateFileName));

			this.certDirectoryPath = certificateDirectoryPath;
			this.certFileName = certificateFileName;
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

		private static string GetDefaultCertificateDirectoryPath()
		{
			FileInfo fiExe;
			try
			{
				fiExe = new FileInfo(Globals.EntryAssemblyLocation);
			}
			catch
			{
#if NETFRAMEWORK || NET6_PLUS_WIN
				try
				{
					fiExe = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
				}
				catch
				{
					fiExe = new FileInfo(Globals.ApplicationDirectoryBase + Globals.ExecutableNameWithExtension);
				}
#else
					fiExe = new FileInfo(Globals.ApplicationDirectoryBase + Globals.ExecutableNameWithExtension);
#endif
			}
			return fiExe.Directory.FullName;
		}
		private static object certCreateLock = new object();
		private static X509Certificate2 GetSelfSignedCertificate(string certificateDirectoryPath, string certificateFileName)
		{
			lock (certCreateLock)
			{
				X509Certificate2 ssl_certificate;
				string autoCertPassword = "N0t_V3ry-S3cure#lol";
				FileInfo fiCert = new FileInfo(Path.Combine(certificateDirectoryPath, certificateFileName));
				if (fiCert.Exists)
				{
					// Load existing certificate
					try
					{
#if NET10_0_OR_GREATER
						ssl_certificate = X509CertificateLoader.LoadPkcs12FromFile(fiCert.FullName, autoCertPassword);
#else
						ssl_certificate = new X509Certificate2(fiCert.FullName, autoCertPassword);
#endif
					}
					catch (Exception ex1)
					{
						try
						{
#if NET10_0_OR_GREATER
							ssl_certificate = X509CertificateLoader.LoadPkcs12FromFile(fiCert.FullName, null);
#else
							ssl_certificate = new X509Certificate2(fiCert.FullName);
#endif
						}
						catch
						{
							throw ex1;
						}
					}
				}
				else
				{
					// Create new certificate
#if NETFRAMEWORK
					using (BPUtil.SimpleHttp.Crypto.CryptContext ctx = new BPUtil.SimpleHttp.Crypto.CryptContext())
					{
						ctx.Open();

						ssl_certificate = ctx.CreateSelfSignedCertificate(
							new BPUtil.SimpleHttp.Crypto.SelfSignedCertProperties
							{
								IsPrivateKeyExportable = true,
								KeyBitLength = 4096,
								Name = new X500DistinguishedName("cn=localhost"),
								ValidFrom = DateTime.Today.AddDays(-1),
								ValidTo = DateTime.Today.AddYears(100),
							});

						byte[] certData = ssl_certificate.Export(X509ContentType.Pfx, autoCertPassword);
						File.WriteAllBytes(fiCert.FullName, certData);
					}
#elif NET6_0_OR_GREATER
					// Native cert generator. .NET 4.7.2 required.
					using (System.Security.Cryptography.RSA key = System.Security.Cryptography.RSA.Create(2048))
					{
						CertificateRequest request = new CertificateRequest("cn=localhost", key, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);

						SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
						sanBuilder.AddDnsName("localhost");
						request.CertificateExtensions.Add(sanBuilder.Build());

						ssl_certificate = request.CreateSelfSigned(DateTime.Today.AddDays(-1), DateTime.Today.AddYears(100));

						byte[] certData = ssl_certificate.Export(X509ContentType.Pfx, autoCertPassword);
						File.WriteAllBytes(fiCert.FullName, certData);
					}
#endif
				}
				return ssl_certificate;
			}
		}
	}
}