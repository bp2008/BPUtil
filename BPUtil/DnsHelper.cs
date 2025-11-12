using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides cache-backed async DNS querying capability.  IPv4 addresses are cached for 30 seconds, while IPv6 addresses are cached for 120 seconds.  Cache times are subject to change in future versions of this class.
	/// </summary>
	public static class DnsHelper
	{
		private static ConcurrentDictionary<string, DnsCacheEntry> cache = new ConcurrentDictionary<string, DnsCacheEntry>();
		private static Cooldown cacheCleanupCooldown = new Cooldown(TimeSpan.FromSeconds(60));

		/// <summary>
		/// Asynchronously retrieves an IP address associated with a host.
		/// </summary>
		/// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <param name="preferredAddressFamily">(Optional) This specifies a preference for a particular address family (e.g. <see cref="AddressFamily.InterNetwork"/> to prefer IPv4). If an address of the preferred family is not found, an address of an unpreferred family may be returned.</param>
		/// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains an IPAddress for the host that is specified by the hostNameOrAddress parameter.</returns>
		public static async Task<IPAddress> GetHostAddressAsync(string hostNameOrAddress, CancellationToken cancellationToken = default, AddressFamily preferredAddressFamily = AddressFamily.Unspecified)
		{
			try
			{
				// Cleanup expired cache entries at most once per minute
				DateTime now = DateTime.UtcNow;
				if (cacheCleanupCooldown.Consume())
				{
					foreach (KeyValuePair<string, DnsCacheEntry> kvp in cache)
					{
						if (kvp.Value.Expired)
							cache.TryRemove(kvp.Key, out _);
					}
				}

				IPAddress ip;
				if (IPAddress.TryParse(hostNameOrAddress, out ip))
					return ip;

				string cacheKey = CacheKey(hostNameOrAddress, preferredAddressFamily);
				if (cache.TryGetValue(cacheKey, out DnsCacheEntry cacheEntry) && !cacheEntry.Expired)
					return cacheEntry.Address;

#if NET6_0_OR_GREATER
				IPAddress[] addresses = await Dns.GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
#else
				IPAddress[] addresses = await Dns.GetHostAddressesAsync(hostNameOrAddress).ConfigureAwait(false);
#endif
				if (preferredAddressFamily != AddressFamily.Unknown && preferredAddressFamily != AddressFamily.Unspecified)
					ip = addresses.FirstOrDefault(a => a.AddressFamily == preferredAddressFamily);
				if (ip == null)
					ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6);
				if (ip == null)
				{
					cache.TryRemove(cacheKey, out DnsCacheEntry ignored);
					throw new ApplicationException("Unable to resolve host \"" + hostNameOrAddress + "\" to an IPv4 or IPv6 address.");
				}

				int ttl = ip.AddressFamily == AddressFamily.InterNetworkV6 ? 120 : 30;
				cache[cacheKey] = new DnsCacheEntry(ip, ttl);
				return ip;
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Failed to resolve \"" + hostNameOrAddress + "\".", ex);
			}
		}
		private static string CacheKey(string host, AddressFamily addressFamily)
		{
			return ((int)addressFamily) + "|" + host;
		}

		class DnsCacheEntry
		{
			/// <summary>
			/// The IP Address which was resolved earlier.
			/// </summary>
			public IPAddress Address;
			private CountdownStopwatch Expiration;

			/// <summary>
			/// Gets a value indicating if this cache entry has expired.
			/// </summary>
			public bool Expired
			{
				get
				{
					return Expiration.Finished;
				}
			}

			/// <summary>
			/// Constructs a new DnsCacheEntry with the given IP Address and TTL.
			/// </summary>
			/// <param name="address">IP Address</param>
			/// <param name="ttl">Time to live, in seconds.</param>
			public DnsCacheEntry(IPAddress address, int ttl)
			{
				Address = address;
				Expiration = CountdownStopwatch.StartNew(TimeSpan.FromSeconds(ttl));
			}
		}
	}
}
