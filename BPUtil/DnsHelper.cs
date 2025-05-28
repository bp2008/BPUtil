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
		/// Asynchronously retrieves an IP address associated with a host.  IPv4 will be preferred over IPv6.
		/// </summary>
		/// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains an IPAddress for the host that is specified by the hostNameOrAddress parameter.</returns>
		public static async Task<IPAddress> GetHostAddressAsync(string hostNameOrAddress, CancellationToken cancellationToken = default)
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

			if (cache.TryGetValue(hostNameOrAddress, out DnsCacheEntry cacheEntry) && !cacheEntry.Expired)
				return cacheEntry.Address;

#if NET6_0
			IPAddress[] addresses = await Dns.GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
#else
			IPAddress[] addresses = await Dns.GetHostAddressesAsync(hostNameOrAddress).ConfigureAwait(false);
#endif

			ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
			if (ip == null)
				ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6);
			if (ip == null)
			{
				cache.TryRemove(hostNameOrAddress, out DnsCacheEntry ignored);
				throw new ApplicationException("Unable to resolve IP address for hostname \"" + hostNameOrAddress + "\"");
			}

			int ttl = ip.AddressFamily == AddressFamily.InterNetworkV6 ? 120 : 30;
			cache[hostNameOrAddress] = new DnsCacheEntry(ip, ttl);
			return ip;
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
