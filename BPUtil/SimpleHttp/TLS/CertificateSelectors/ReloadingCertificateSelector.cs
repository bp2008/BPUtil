using BPUtil.SimpleHttp.TLS;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// An object containing the path of a `.pfx` file and an optional password.
	/// </summary>
	public class CertificatePfxInfo
	{
		/// <summary>
		/// Path to a `.pfx` file.
		/// </summary>
		public readonly string FilePath;
		/// <summary>
		/// Password for the `.pfx` file, or null.
		/// </summary>
		public readonly string Password;
		/// <summary>
		/// Constructs a CertificatePfxInfo from arguments.
		/// </summary>
		/// <param name="filePath">Path to a `.pfx` file.</param>
		/// <param name="password">Password for the `.pfx` file, or null.</param>
		public CertificatePfxInfo(string filePath, string password)
		{
			FilePath = filePath;
			Password = password;
		}
		/// <summary>
		/// Determines if this CertificatePfxInfo is equivalent to another CertificatePfxInfo.
		/// </summary>
		/// <param name="obj">Another object to compare with.</param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			else if (obj == this)
				return true;
			else if (obj is CertificatePfxInfo other)
				return this.FilePath == other.FilePath && this.Password == other.Password;
			else
				return false;
		}
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return FilePath.GetHashCode() ^ Password.GetHashCode();
		}
		/// <inheritdoc/>
		public override string ToString()
		{
			return FilePath + (Password != null ? " (password protected)" : "");
		}
	}
	/// <summary>
	/// Provides simple SSL certificate selection capabilities with periodic reloading from disk.
	/// </summary>
	public class ReloadingCertificateSelector : ICertificateSelector
	{
		private readonly Func<CertificatePfxInfo> certificateInfoProvider;
		private CachedCertificate cachedCertificate;
		private CertificatePfxInfo lastCertificatePfxInfo = null;
		private object myLock = new object();
		private Cooldown checkLastWriteTimeCd = new Cooldown(10000);

		/// <summary>
		/// Initializes a new instance of the <see cref="ReloadingCertificateSelector"/> class.
		/// </summary>
		/// <param name="certificateInfoProvider">A function that returns a CertificatePfxInfo containing the path of the certificate and an optional password.  This will be called every time the web server needs to negotiate a TLS connection, so it should be very efficient.</param>
		public ReloadingCertificateSelector(Func<CertificatePfxInfo> certificateInfoProvider)
		{
			this.certificateInfoProvider = certificateInfoProvider ?? throw new ArgumentNullException(nameof(certificateInfoProvider));
		}

		/// <summary>
		/// Returns an X509Certificate or null if no certificate is available (the connection will be closed).
		/// </summary>
		/// <param name="p">The HttpProcessor instance in its current early state of processing. Many fields have not been initialized yet.</param>
		/// <param name="serverName">The server name as indicated by ServerNameIndication. May be null or empty. May be omitted if the configured certificate resolver is known to ignore this value.</param>
		/// <returns>An X509Certificate or null.</returns>
		public Task<X509Certificate> GetCertificate(HttpProcessor p, string serverName)
		{
			if (ShouldReloadCertificate(false))
			{
				lock (myLock)
				{
					if (ShouldReloadCertificate(true))
						ReloadCertificate();
				}
			}
			return Task.FromResult<X509Certificate>(cachedCertificate?.Certificate);
		}

		/// <summary>
		/// Returns a special X509Certificate (or null) in order to complete "acme-tls/1" or "TLS-ALPN-01" validation.
		/// </summary>
		/// <param name="p">The HttpProcessor instance in its current early state of processing. Many fields have not been initialized yet.</param>
		/// <param name="serverName">The server name as indicated by ServerNameIndication. This is a required parameter and should not be null or empty.</param>
		/// <returns>An X509Certificate or null.</returns>
		public Task<X509Certificate> GetAcmeTls1Certificate(HttpProcessor p, string serverName)
		{
			return Task.FromResult<X509Certificate>(null);
		}

		private bool ShouldReloadCertificate(bool isRecheck)
		{
			CachedCertificate cc = cachedCertificate;
			if (cc == null)
				return true; // The certificate hasn't been loaded yet.
			if (cc.IsExpiringSoon)
				return true; // The certificate is expired or expiring very soon.
			if (cc.Age > TimeSpan.FromDays(1))
				return true; // We haven't reloaded the certificate in over a day.

			CertificatePfxInfo cpi = certificateInfoProvider();
			if (!cpi.Equals(lastCertificatePfxInfo))
				return true;

			if (isRecheck || checkLastWriteTimeCd.Consume())
			{
				DateTime lastModifiedUtc = File.GetLastWriteTimeUtc(cpi.FilePath);
				if (cc.FileLastModifiedUTC != lastModifiedUtc)
					return true;
			}

			return false;
		}

		private void ReloadCertificate()
		{
			CertificatePfxInfo cpi = certificateInfoProvider();
			FileInfo fi = new FileInfo(cpi.FilePath);
			if (fi.Exists)
			{
				CachedCertificate cc = new CachedCertificate(fi, cpi.Password);
				try
				{
					CertificateStoreUpdater.EnsureIntermediateCertificatesAreInStore(fi.FullName, cpi.Password);
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "Unable to save intermediate certificates to \"Intermediate Certificate Authorites\" store.");
					return;
				}
				lastCertificatePfxInfo = cpi;
				cachedCertificate = cc;
			}
		}
	}
	/// <summary>
	/// An object storing a cached certificate.
	/// </summary>
	public class CachedCertificate
	{
		/// <summary>
		/// The cached certificate.
		/// </summary>
		public X509Certificate2 Certificate { get; private set; }
		/// <summary>
		/// A Stopwatch containing the age of the cached certificate.
		/// </summary>
		private Stopwatch age = Stopwatch.StartNew();
		/// <summary>
		/// The age of this cached certificate instance.
		/// </summary>
		public TimeSpan Age => age.Elapsed;
		/// <summary>
		/// The Last Modified timestamp of the certificate file, in UTC.
		/// </summary>
		public DateTime FileLastModifiedUTC { get; private set; }
		/// <summary>
		/// True if the certificate is expired or expiring within the next 1 hour.
		/// </summary>
		public bool IsExpiringSoon => Certificate.NotAfter < DateTime.Now.AddHours(1);
		/// <summary>
		/// Constructs a new CachedCertificate by reading the certificate from a file.
		/// </summary>
		/// <param name="fi">FileInfo pointing at a pfx file.</param>
		/// <param name="password">Pfx file password or null.</param>
		public CachedCertificate(FileInfo fi, string password)
		{
			Certificate = new X509Certificate2(fi.FullName, password);
			FileLastModifiedUTC = fi.LastWriteTimeUtc;
		}
	}
}
