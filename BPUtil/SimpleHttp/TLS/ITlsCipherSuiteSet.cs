#if NET6_0
using System.Net.Security;

namespace BPUtil.SimpleHttp.TLS
{
	/// <summary>
	/// <para>Derived classes provide a list of TLS cipher suites.</para>
	/// <para>Implementations built-in to BPUtil:</para>
	/// <para>
	/// <list type="bullet">
	/// <item><see cref="TlsCipherSuiteSet_DotNet5_Q3_2023"/></item>
	/// <item><see cref="TlsCipherSuiteSet_IANA_Q3_2023"/></item>
	/// </list>
	/// </para>
	/// </summary>
	public interface ITlsCipherSuiteSet
	{
		/// <summary>
		/// Returns a list of TLS cipher suites.
		/// </summary>
		/// <returns></returns>
		public TlsCipherSuite[] GetCipherSuites();
	}
	/// <summary>
	/// <para>Provides the list of cipher suites defined by Microsoft here for .NET 5+ on August 8th, 2023: https://learn.microsoft.com/en-us/dotnet/core/compatibility/cryptography/5.0/default-cipher-suites-for-tls-on-linux</para>
	/// <para>These cipher suites support TLS 1.2 and 1.3, but do not support TLS 1.1 or older.</para>
	/// </summary>
	public class TlsCipherSuiteSet_DotNet5_Q3_2023 : ITlsCipherSuiteSet
	{
		/// <inheritdoc/>
		public TlsCipherSuite[] GetCipherSuites()
		{
			return new TlsCipherSuite[]
			{
				// TLS 1.3:
				TlsCipherSuite.TLS_AES_128_GCM_SHA256,
				TlsCipherSuite.TLS_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
				TlsCipherSuite.TLS_AES_128_CCM_SHA256,
				TlsCipherSuite.TLS_AES_128_CCM_8_SHA256,
				// TLS 1.2:
				TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
				TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
				TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384,
				TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
				TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
				TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256
			};
		}
	}
	/// <summary>
	/// <para>Provides the list of cipher suites defined by IANA here on August 8th, 2023: https://www.iana.org/assignments/tls-parameters/tls-parameters.xhtml</para>
	/// <para>These cipher suites support TLS 1.2 and 1.3, but do not support TLS 1.1 or older.</para>
	/// </summary>
	public class TlsCipherSuiteSet_IANA_Q3_2023 : ITlsCipherSuiteSet
	{
		/// <inheritdoc/>
		public TlsCipherSuite[] GetCipherSuites()
		{
			return new TlsCipherSuite[]
			{
				TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256,
				TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256,
				TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
				TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
				TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
				TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
				TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
				TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM,
				TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM,
				TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM,
				TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM,
				TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
				TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
				TlsCipherSuite.TLS_AES_128_CCM_SHA256,       // TLS 1.3
				TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256, // TLS 1.3
				TlsCipherSuite.TLS_AES_256_GCM_SHA384,       // TLS 1.3
				TlsCipherSuite.TLS_AES_128_GCM_SHA256,       // TLS 1.3
				TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256,
				TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
				TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256
			};
		}
	}
}
#endif