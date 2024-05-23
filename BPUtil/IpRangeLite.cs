using System;
using System.Net;

namespace BPUtil
{
	/// <summary>
	/// Provides basic IP range comparison capability.
	/// </summary>
	public class IpRangeLite
	{
		/// <summary>
		/// Lowest IP address in the range.
		/// </summary>
		public readonly ComparableIPAddress low;
		/// <summary>
		/// Highest IP address in the range.
		/// </summary>
		public readonly ComparableIPAddress high;
		/// <summary>
		/// Constructs an IpRangeLite from two IP addresses.
		/// </summary>
		/// <param name="ip1">IP Address</param>
		/// <param name="ip2">IP Address</param>
		public IpRangeLite(string ip1, string ip2)
			: this(ComparableIPAddress.Parse(ip1), ComparableIPAddress.Parse(ip2)) { }
		/// <summary>
		/// Constructs an IpRangeLite from two IP addresses.
		/// </summary>
		/// <param name="ip1">IP Address</param>
		/// <param name="ip2">IP Address</param>
		public IpRangeLite(IPAddress ip1, IPAddress ip2) : this(new ComparableIPAddress(ip1), new ComparableIPAddress(ip2)) { }
		/// <summary>
		/// Constructs an IpRangeLite from two IP addresses.
		/// </summary>
		/// <param name="ip1">IP Address</param>
		/// <param name="ip2">IP Address</param>
		public IpRangeLite(ComparableIPAddress ip1, ComparableIPAddress ip2)
		{
			low = ip1;
			high = ip2;
			if (low == null)
				throw new ArgumentNullException(nameof(low));
			if (high == null)
				throw new ArgumentNullException(nameof(high));
			if (low.byteLength != high.byteLength)
				throw new ArgumentException("The given IP addresses are not in the same address family.");
			if (low.CompareTo(high) > 0)
			{
				ComparableIPAddress tmp = low;
				low = high;
				high = tmp;
			}
		}
		/// <summary>
		/// Returns true if the specified IP address is contained within the IP range.
		/// </summary>
		/// <param name="address">IP Address</param>
		/// <returns>true if the specified IP address is contained within the IP range.</returns>
		public bool Contains(string address)
		{
			return Contains(ComparableIPAddress.Parse(address));
		}
		/// <summary>
		/// Returns true if the specified IP address is contained within the IP range.
		/// </summary>
		/// <param name="address">IP Address</param>
		/// <returns>true if the specified IP address is contained within the IP range.</returns>
		public bool Contains(IPAddress address)
		{
			if (address == null)
				return false;
			return low.CompareTo(address) <= 0 && high.CompareTo(address) >= 0;
		}
	}
}
