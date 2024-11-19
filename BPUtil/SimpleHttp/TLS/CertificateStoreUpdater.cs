using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS
{
	/// <summary>
	/// <para>A class which ensures intermediate certificates are in the OS's certificate store so that SslStream will serve them to clients.</para>
	/// <para>As of 2024-11-19, I've tested and it seems it is not actually necessary to add LetsEncrypt's intermediate servers to the certificate store for some reason, and SslStream somehow finds them anyway.  However if you use a private certificate authority, SslStream requires it to be found in the OS certificate store.</para>
	/// </summary>
	public static class CertificateStoreUpdater
	{
		private static Dictionary<string, bool> ThumbprintsAlreadyAdded = new Dictionary<string, bool>();
		/// <summary>
		/// Adds all intermediate certificates from the given pfx file to the local machine's "Intermediate Certificate Authorities" store (skips those that already exist in it).
		/// </summary>
		/// <param name="pfxFilePath">Path to a pfx file containing intermediate certificates.</param>
		/// <param name="pfxPassword">Password for the pfx file.  Null if the file is not password-protected.</param>
		public static void EnsureIntermediateCertificatesAreInStore(string pfxFilePath, string pfxPassword = null)
		{
			// Load the .pfx file into an X509Certificate2Collection
			X509Certificate2Collection certificateCollection = new X509Certificate2Collection();
			certificateCollection.Import(pfxFilePath, pfxPassword, X509KeyStorageFlags.DefaultKeySet);
			EnsureIntermediateCertificatesAreInStore(certificateCollection);
		}
		/// <summary>
		/// Adds all intermediate certificates from the given pfx file to the local machine's "Intermediate Certificate Authorities" store (skips those that already exist in it).
		/// </summary>
		/// <param name="pfxFileData">Binary content of a pfx file containing intermediate certificates.</param>
		/// <param name="pfxPassword">Password for the pfx file.  Null if the file is not password-protected.</param>
		public static void EnsureIntermediateCertificatesAreInStore(byte[] pfxFileData, string pfxPassword = null)
		{
			// Load the .pfx file into an X509Certificate2Collection
			X509Certificate2Collection certificateCollection = new X509Certificate2Collection();
			certificateCollection.Import(pfxFileData, pfxPassword, X509KeyStorageFlags.DefaultKeySet);
			EnsureIntermediateCertificatesAreInStore(certificateCollection);
		}
		/// <summary>
		/// <para>Adds all intermediate certificates from the given pfx file to the local machine's "Intermediate Certificate Authorities" store (skips those that already exist in it).</para>
		/// </summary>
		/// <param name="certificateCollection">An X509Certificate2Collection containing a leaf and zero or more intermediate (issuer) certificates.</param>
		public static void EnsureIntermediateCertificatesAreInStore(X509Certificate2Collection certificateCollection)
		{
			// There need to be at least 2 certificates in the pfx file in order for there to be any intermediates that we care about.
			// Otherwise a self-signed certificate would be its own issuer and would get added to the store for no purpose.
			if (certificateCollection.Count < 2)
				return;

			// Collect all the issuers' subjects
			HashSet<string> issuerSubjects = new HashSet<string>();
			foreach (X509Certificate2 certificate in certificateCollection)
				issuerSubjects.Add(certificate.Issuer);

			// We only want to add to the store any certificates that were the issuer of another certificate in the same collection.
			List<X509Certificate2> intermediates = new List<X509Certificate2>();
			foreach (X509Certificate2 certificate in certificateCollection)
				if (issuerSubjects.Contains(certificate.Subject))
					intermediates.Add(certificate);

			intermediates.RemoveAll(cert => ThumbprintsAlreadyAdded.ContainsKey(cert.Thumbprint));

			if (Platform.IsUnix())
			{
				// Attempting to add to LocalMachine yields:
				// System.PlatformNotSupportedException: Unix LocalMachine X509Stores are read-only for all users.
				EnsureCertificatesAreInStore(intermediates, StoreLocation.CurrentUser);
			}
			else
			{
				try
				{
					EnsureCertificatesAreInStore(intermediates, StoreLocation.LocalMachine);
				}
				catch (Exception)
				{
					EnsureCertificatesAreInStore(intermediates, StoreLocation.CurrentUser);
				}
			}
		}

		private static void EnsureCertificatesAreInStore(List<X509Certificate2> certificates, StoreLocation storeLocation)
		{
			if (certificates.Count == 0)
				return;

#if NETFRAMEWORK
			X509Store store = new X509Store(StoreName.CertificateAuthority, storeLocation);
			try
#else
			using (X509Store store = new X509Store(StoreName.CertificateAuthority, storeLocation))
#endif
			{
				store.Open(OpenFlags.ReadWrite);

				foreach (X509Certificate2 certificate in certificates)
				{
					// Check if the certificate is already in the store
					X509Certificate2Collection existingCertificates = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
					if (existingCertificates.Count == 0)
					{
						// Add the certificate to the store if it's not already there
						store.Add(certificate);
					}
					ThumbprintsAlreadyAdded[certificate.Thumbprint] = true;
				}
			}
#if NETFRAMEWORK
			finally
			{
				store.Close();
			}
#endif
		}
	}
}