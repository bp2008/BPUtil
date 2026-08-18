using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides cache-backed async DNS querying capability.  Addresses are cached according to their TTL.
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
				CleanupExpiredCacheEntries();

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
		/// <summary>
		/// <para>Synchronously retrieves an IP address associated with a host by sending a DNS query directly to the specified DNS server (bypassing the operating system's resolver and its cache, but not this class's cache).</para>
		/// <para>The query is sent via UDP.  If the response is truncated, the query is retried via TCP.</para>
		/// <para>If the query cannot be resolved, an <c>ApplicationException</c> is thrown.</para>
		/// </summary>
		/// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
		/// <param name="dnsServer">The IP address of the DNS server which the query should be sent to.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <param name="preferredAddressFamily">(Optional) This specifies a preference for a particular address family (e.g. <see cref="AddressFamily.InterNetwork"/> to prefer IPv4). If an address of the preferred family is not found, an address of an unpreferred family may be returned.</param>
		/// <param name="dnsServerPort">(Optional) The port which the DNS server is listening on.</param>
		/// <param name="timeoutMilliseconds">(Optional) Number of milliseconds to wait for a response from the DNS server.  This timeout is applied separately to the UDP query and to the TCP retry which occurs only if the UDP response was truncated.</param>
		/// <returns>An IPAddress for the host that is specified by the hostNameOrAddress parameter.</returns>
		public static IPAddress GetHostAddress(string hostNameOrAddress, IPAddress dnsServer, int dnsServerPort = 53, int timeoutMilliseconds = 5000, CancellationToken cancellationToken = default, AddressFamily preferredAddressFamily = AddressFamily.Unspecified)
		{
			return TaskHelper.RunAsyncCodeSafely(() => GetHostAddressAsync(hostNameOrAddress, dnsServer, dnsServerPort, timeoutMilliseconds, cancellationToken, preferredAddressFamily));
		}
		/// <summary>
		/// <para>Asynchronously retrieves an IP address associated with a host by sending a DNS query directly to the specified DNS server (bypassing the operating system's resolver and its cache, but not this class's cache).</para>
		/// <para>The query is sent via UDP.  If the response is truncated, the query is retried via TCP.</para>
		/// <para>If the query cannot be resolved, an <c>ApplicationException</c> is thrown.</para>
		/// </summary>
		/// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
		/// <param name="dnsServer">The IP address of the DNS server which the query should be sent to.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <param name="preferredAddressFamily">(Optional) This specifies a preference for a particular address family (e.g. <see cref="AddressFamily.InterNetwork"/> to prefer IPv4). If an address of the preferred family is not found, an address of an unpreferred family may be returned.</param>
		/// <param name="dnsServerPort">(Optional) The port which the DNS server is listening on.</param>
		/// <param name="timeoutMilliseconds">(Optional) Number of milliseconds to wait for a response from the DNS server.  This timeout is applied separately to the UDP query and to the TCP retry which occurs only if the UDP response was truncated.</param>
		/// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains an IPAddress for the host that is specified by the hostNameOrAddress parameter.</returns>
		public static async Task<IPAddress> GetHostAddressAsync(string hostNameOrAddress, IPAddress dnsServer, int dnsServerPort = 53, int timeoutMilliseconds = 5000, CancellationToken cancellationToken = default, AddressFamily preferredAddressFamily = AddressFamily.Unspecified)
		{
			if (dnsServer == null)
				throw new ArgumentNullException(nameof(dnsServer));
			if (timeoutMilliseconds <= 0)
				throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), nameof(timeoutMilliseconds) + " must be > 0.");

			IPEndPoint dnsServerEndpoint = new IPEndPoint(dnsServer, dnsServerPort);
			try
			{
				CleanupExpiredCacheEntries();

				IPAddress ip;
				if (IPAddress.TryParse(hostNameOrAddress, out ip))
					return ip;

				string cacheKey = CacheKey(hostNameOrAddress, preferredAddressFamily, dnsServerEndpoint);
				if (cache.TryGetValue(cacheKey, out DnsCacheEntry cacheEntry) && !cacheEntry.Expired)
					return cacheEntry.Address;

				// Query the preferred record type first.  Only if that yields no address do we query the other record type.
				ushort[] recordTypes = preferredAddressFamily == AddressFamily.InterNetworkV6
					? new ushort[] { DNS_TYPE_AAAA, DNS_TYPE_A }
					: new ushort[] { DNS_TYPE_A, DNS_TYPE_AAAA };

				uint recordTtl = uint.MaxValue;
				foreach (ushort recordType in recordTypes)
				{
					DnsQueryResult result = await DnsQueryAsync(hostNameOrAddress, recordType, dnsServerEndpoint, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
					if (result.Addresses.Count > 0)
					{
						ip = result.Addresses[0];
						recordTtl = result.Ttl;
						break;
					}
				}
				if (ip == null)
				{
					cache.TryRemove(cacheKey, out DnsCacheEntry ignored);
					throw new ApplicationException("DNS server " + dnsServerEndpoint + " did not provide an IPv4 or IPv6 address for host \"" + hostNameOrAddress + "\".");
				}

				// Cache for the record's TTL, but no longer than this class's standard cache duration.
				int maxTtl = ip.AddressFamily == AddressFamily.InterNetworkV6 ? 120 : 30;
				int ttl = recordTtl > (uint)maxTtl ? maxTtl : (int)recordTtl;
				if (ttl < 1)
					ttl = 1;
				cache[cacheKey] = new DnsCacheEntry(ip, ttl);
				return ip;
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Failed to resolve \"" + hostNameOrAddress + "\" using DNS server " + dnsServerEndpoint + ".", ex);
			}
		}
		/// <summary>
		/// Removes expired entries from the cache, at most once per minute.
		/// </summary>
		private static void CleanupExpiredCacheEntries()
		{
			if (cacheCleanupCooldown.Consume())
			{
				foreach (KeyValuePair<string, DnsCacheEntry> kvp in cache)
				{
					if (kvp.Value.Expired)
						cache.TryRemove(kvp.Key, out _);
				}
			}
		}
		private static string CacheKey(string host, AddressFamily addressFamily, IPEndPoint dnsServer)
		{
			return ((int)addressFamily) + "|" + dnsServer + "|" + host;
		}

		#region DNS Protocol
		/// <summary>
		/// DNS resource record type "A" (IPv4 host address).
		/// </summary>
		private const ushort DNS_TYPE_A = 1;
		/// <summary>
		/// DNS resource record type "AAAA" (IPv6 host address).
		/// </summary>
		private const ushort DNS_TYPE_AAAA = 28;
		/// <summary>
		/// DNS resource record class "IN" (Internet).
		/// </summary>
		private const ushort DNS_CLASS_IN = 1;
		private static readonly Random dnsQueryIdRandom = new Random();

		/// <summary>
		/// Sends a DNS query to the given DNS server and returns the addresses which were provided in the answer section of the response.
		/// </summary>
		/// <param name="host">The host name to resolve.</param>
		/// <param name="recordType">The type of record to request (<see cref="DNS_TYPE_A"/> or <see cref="DNS_TYPE_AAAA"/>).</param>
		/// <param name="dnsServer">The DNS server to send the query to.</param>
		/// <param name="timeoutMilliseconds">Number of milliseconds to wait for a response.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>The addresses which were provided in the answer section of the response.  The collection is empty if the server did not answer with any address of the requested type.</returns>
		private static async Task<DnsQueryResult> DnsQueryAsync(string host, ushort recordType, IPEndPoint dnsServer, int timeoutMilliseconds, CancellationToken cancellationToken)
		{
			ushort queryId;
			lock (dnsQueryIdRandom)
				queryId = (ushort)dnsQueryIdRandom.Next(ushort.MaxValue + 1);

			byte[] query = BuildDnsQuery(queryId, host, recordType);

			byte[] response = await DnsQueryUdpAsync(query, queryId, dnsServer, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
			if ((ByteUtil.ReadUInt16(response, 2) & 0x0200) != 0) // TC (TrunCation) flag
				response = await DnsQueryTcpAsync(query, queryId, dnsServer, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);

			return ParseDnsResponse(response, recordType, dnsServer);
		}
		/// <summary>
		/// Sends the given DNS query message to the given DNS server via UDP and returns the raw response message.
		/// </summary>
		/// <param name="query">Raw DNS query message.</param>
		/// <param name="queryId">The ID which was assigned to the query.  Responses carrying a different ID are ignored.</param>
		/// <param name="dnsServer">The DNS server to send the query to.</param>
		/// <param name="timeoutMilliseconds">Number of milliseconds to wait for a response.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>The raw DNS response message.</returns>
		private static async Task<byte[]> DnsQueryUdpAsync(byte[] query, ushort queryId, IPEndPoint dnsServer, int timeoutMilliseconds, CancellationToken cancellationToken)
		{
			using (UdpClient udp = new UdpClient(dnsServer.AddressFamily))
			{
				CountdownStopwatch timer = CountdownStopwatch.StartNew(TimeSpan.FromMilliseconds(timeoutMilliseconds));
				udp.Connect(dnsServer);
				await udp.SendAsync(query, query.Length).ConfigureAwait(false);
				while (true)
				{
					Task<UdpReceiveResult> receiveTask = udp.ReceiveAsync();
					try
					{
						await TaskHelper.DoWithCancellation(receiveTask, RemainingMilliseconds(timer), cancellationToken).ConfigureAwait(false);
					}
					catch
					{
						ObserveException(receiveTask); // The socket is about to be closed, which will cause this task to fault.
						throw;
					}
					byte[] response = receiveTask.Result.Buffer;
					if (response.Length >= 12 && ByteUtil.ReadUInt16(response, 0) == queryId)
						return response;
					// This datagram is not a response to our query (it may be a late response to an earlier query, or a spoofing attempt).  Keep waiting.
				}
			}
		}
		/// <summary>
		/// Sends the given DNS query message to the given DNS server via TCP and returns the raw response message.
		/// </summary>
		/// <param name="query">Raw DNS query message.</param>
		/// <param name="queryId">The ID which was assigned to the query.</param>
		/// <param name="dnsServer">The DNS server to send the query to.</param>
		/// <param name="timeoutMilliseconds">Number of milliseconds to wait for the operation to complete.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>The raw DNS response message.</returns>
		private static async Task<byte[]> DnsQueryTcpAsync(byte[] query, ushort queryId, IPEndPoint dnsServer, int timeoutMilliseconds, CancellationToken cancellationToken)
		{
			using (TcpClient tcp = new TcpClient(dnsServer.AddressFamily))
			{
				CountdownStopwatch timer = CountdownStopwatch.StartNew(TimeSpan.FromMilliseconds(timeoutMilliseconds));
				Task connectTask = tcp.ConnectAsync(dnsServer.Address, dnsServer.Port);
				try
				{
					await TaskHelper.DoWithCancellation(connectTask, RemainingMilliseconds(timer), cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					ObserveException(connectTask); // The socket is about to be closed, which will cause this task to fault.
					throw;
				}
				using (NetworkStream stream = tcp.GetStream())
				{
					// DNS over TCP prefixes each message with its length as a 16-bit big-endian integer.
					byte[] framedQuery = new byte[2 + query.Length];
					ByteUtil.WriteUInt16((ushort)query.Length, framedQuery, 0);
					Array.Copy(query, 0, framedQuery, 2, query.Length);
					await stream.WriteAsync(framedQuery, 0, framedQuery.Length, cancellationToken).ConfigureAwait(false);

					byte[] lengthPrefix = await ReadExactlyAsync(stream, 2, timer, cancellationToken).ConfigureAwait(false);
					int responseLength = ByteUtil.ReadUInt16(lengthPrefix, 0);
					if (responseLength < 12)
						throw new ApplicationException("DNS server " + dnsServer + " announced a response of " + responseLength + " bytes via TCP, which is too short to be a DNS message.");

					byte[] response = await ReadExactlyAsync(stream, responseLength, timer, cancellationToken).ConfigureAwait(false);
					if (ByteUtil.ReadUInt16(response, 0) != queryId)
						throw new ApplicationException("DNS server " + dnsServer + " responded via TCP with a message ID that did not match the query.");
					return response;
				}
			}
		}
		/// <summary>
		/// Reads exactly the given number of bytes from the stream, throwing an exception if the stream ends first.
		/// </summary>
		/// <param name="stream">Stream to read from.</param>
		/// <param name="count">Number of bytes to read.</param>
		/// <param name="timer">Countdown which limits how long the reading may take.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>A buffer containing exactly <paramref name="count"/> bytes.</returns>
		private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CountdownStopwatch timer, CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[count];
			int read = 0;
			while (read < count)
			{
				int justRead = await ByteUtil.ReadAsyncWithTimeout(stream, buffer, read, count - read, RemainingMilliseconds(timer), cancellationToken).ConfigureAwait(false);
				if (justRead < 1)
					throw new EndOfStreamException("The DNS server closed the connection before sending a complete response.");
				read += justRead;
			}
			return buffer;
		}
		/// <summary>
		/// Returns the number of milliseconds remaining on the given countdown, clamped to the range [1, <see cref="int.MaxValue"/>].
		/// </summary>
		/// <param name="timer">Countdown to read.</param>
		/// <returns>The number of milliseconds remaining on the given countdown, clamped to the range [1, <see cref="int.MaxValue"/>].</returns>
		private static int RemainingMilliseconds(CountdownStopwatch timer)
		{
			long remaining = timer.RemainingMilliseconds;
			if (remaining < 1)
				return 1;
			if (remaining > int.MaxValue)
				return int.MaxValue;
			return (int)remaining;
		}
		/// <summary>
		/// Ensures that an exception thrown by the given task will not go unobserved.
		/// </summary>
		/// <param name="task">Task which may fault after we have stopped awaiting it.</param>
		private static void ObserveException(Task task)
		{
			task.ContinueWith(t => { Exception ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
		}
		/// <summary>
		/// Builds a raw DNS query message requesting a record of the given type for the given host.
		/// </summary>
		/// <param name="queryId">ID to assign to the query.</param>
		/// <param name="host">The host name to resolve.</param>
		/// <param name="recordType">The type of record to request.</param>
		/// <returns>A raw DNS query message.</returns>
		private static byte[] BuildDnsQuery(ushort queryId, string host, ushort recordType)
		{
			byte[] qname = EncodeDnsName(host);
			byte[] message = new byte[12 + qname.Length + 4];
			ByteUtil.WriteUInt16(queryId, message, 0);
			ByteUtil.WriteUInt16(0x0100, message, 2); // Flags: standard query with Recursion Desired
			ByteUtil.WriteUInt16(1, message, 4); // QDCOUNT: one question.  ANCOUNT, NSCOUNT and ARCOUNT remain zero.
			Array.Copy(qname, 0, message, 12, qname.Length);
			ByteUtil.WriteUInt16(recordType, message, 12 + qname.Length); // QTYPE
			ByteUtil.WriteUInt16(DNS_CLASS_IN, message, 14 + qname.Length); // QCLASS
			return message;
		}
		/// <summary>
		/// Encodes a host name in DNS wire format (a series of length-prefixed labels terminated by a zero-length label).
		/// </summary>
		/// <param name="host">The host name to encode.  Non-ASCII host names are converted to Punycode.</param>
		/// <returns>The host name in DNS wire format.</returns>
		private static byte[] EncodeDnsName(string host)
		{
			if (string.IsNullOrWhiteSpace(host))
				throw new ArgumentException("Host name is null or empty.", nameof(host));

			host = host.Trim();
			if (host.EndsWith("."))
				host = host.Substring(0, host.Length - 1); // The root label is added below.
			if (host.Any(c => c > 127))
				host = new IdnMapping().GetAscii(host);

			List<byte> encoded = new List<byte>(host.Length + 2);
			foreach (string label in host.Split('.'))
			{
				byte[] labelBytes = Encoding.ASCII.GetBytes(label);
				if (labelBytes.Length < 1 || labelBytes.Length > 63)
					throw new ArgumentException("Host name \"" + host + "\" contains a label which is not between 1 and 63 characters long.", nameof(host));
				encoded.Add((byte)labelBytes.Length);
				encoded.AddRange(labelBytes);
			}
			encoded.Add(0); // Root label

			if (encoded.Count > 255)
				throw new ArgumentException("Host name \"" + host + "\" is too long to be encoded in a DNS query.", nameof(host));
			return encoded.ToArray();
		}
		/// <summary>
		/// Parses a raw DNS response message, returning the addresses of the given record type which were found in the answer section.
		/// </summary>
		/// <param name="message">Raw DNS response message (at least 12 bytes long).</param>
		/// <param name="recordType">The type of record which was requested.</param>
		/// <param name="dnsServer">The DNS server which sent the response (used only for error messages).</param>
		/// <returns>The addresses of the given record type which were found in the answer section.</returns>
		private static DnsQueryResult ParseDnsResponse(byte[] message, ushort recordType, IPEndPoint dnsServer)
		{
			DnsQueryResult result = new DnsQueryResult();

			ushort flags = ByteUtil.ReadUInt16(message, 2);
			if ((flags & 0x8000) == 0)
				throw new ApplicationException("DNS server " + dnsServer + " sent a message which was not flagged as a response.");

			int rcode = flags & 0x000F;
			if (rcode == 3)
				return result; // NXDOMAIN: The name does not exist.  Return no addresses rather than throwing, so that the other record type can still be queried.
			if (rcode != 0)
				throw new ApplicationException("DNS server " + dnsServer + " responded with error code " + rcode + " (" + DescribeDnsResponseCode(rcode) + ").");

			int questionCount = ByteUtil.ReadUInt16(message, 4);
			int answerCount = ByteUtil.ReadUInt16(message, 6);

			int offset = 12;
			for (int i = 0; i < questionCount; i++)
			{
				SkipDnsName(message, ref offset);
				offset += 4; // QTYPE and QCLASS
			}

			for (int i = 0; i < answerCount; i++)
			{
				SkipDnsName(message, ref offset);
				if (offset + 10 > message.Length)
					throw new ApplicationException("DNS response from " + dnsServer + " ended unexpectedly while reading a resource record.");

				ushort type = ByteUtil.ReadUInt16(message, offset);
				ushort recordClass = ByteUtil.ReadUInt16(message, offset + 2);
				uint ttl = ByteUtil.ReadUInt32(message, offset + 4);
				ushort rdLength = ByteUtil.ReadUInt16(message, offset + 8);
				offset += 10;

				if (offset + rdLength > message.Length)
					throw new ApplicationException("DNS response from " + dnsServer + " ended unexpectedly while reading resource record data.");

				// Records of other types (such as the CNAME records which may precede the address records) are skipped.
				if (recordClass == DNS_CLASS_IN && type == recordType
					&& ((type == DNS_TYPE_A && rdLength == 4) || (type == DNS_TYPE_AAAA && rdLength == 16)))
				{
					result.Addresses.Add(new IPAddress(ByteUtil.SubArray(message, offset, rdLength)));
					if (ttl < result.Ttl)
						result.Ttl = ttl;
				}

				offset += rdLength;
			}

			return result;
		}
		/// <summary>
		/// Advances the given offset past the DNS name which begins at that offset.
		/// </summary>
		/// <param name="message">Raw DNS message.</param>
		/// <param name="offset">Offset of the start of the name.  Upon return, this is the offset of the first byte after the name.</param>
		private static void SkipDnsName(byte[] message, ref int offset)
		{
			while (true)
			{
				if (offset >= message.Length)
					throw new ApplicationException("DNS response ended unexpectedly while reading a domain name.");
				byte length = message[offset];
				if (length == 0)
				{
					offset++; // Root label; the name ends here.
					return;
				}
				if ((length & 0xC0) == 0xC0)
				{
					// This is a compression pointer, which is always 2 bytes long and always ends the name.
					offset += 2;
					if (offset > message.Length)
						throw new ApplicationException("DNS response ended unexpectedly while reading a compressed domain name.");
					return;
				}
				if ((length & 0xC0) != 0)
					throw new ApplicationException("DNS response contained a domain name label with an unsupported length format.");
				offset += 1 + length;
			}
		}
		/// <summary>
		/// Returns a short description of a DNS response code (RCODE).
		/// </summary>
		/// <param name="rcode">DNS response code.</param>
		/// <returns>A short description of the DNS response code.</returns>
		private static string DescribeDnsResponseCode(int rcode)
		{
			switch (rcode)
			{
				case 0: return "No Error";
				case 1: return "Format Error";
				case 2: return "Server Failure";
				case 3: return "Non-Existent Domain";
				case 4: return "Not Implemented";
				case 5: return "Query Refused";
				default: return "Unknown";
			}
		}
		/// <summary>
		/// The addresses and TTL which were learned from a DNS response.
		/// </summary>
		private class DnsQueryResult
		{
			/// <summary>
			/// The addresses which were found in the answer section of the DNS response.
			/// </summary>
			public List<IPAddress> Addresses = new List<IPAddress>();
			/// <summary>
			/// The lowest Time To Live (in seconds) of the records which provided <see cref="Addresses"/>.
			/// </summary>
			public uint Ttl = uint.MaxValue;
		}
		#endregion
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
