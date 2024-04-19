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
		/// True if the <see cref="low"/> and <see cref="high"/> addresses are valid IP addresses in the same address family.
		/// </summary>
		public readonly bool isValid = false;

		/// <summary>
		/// Constructs an IpRangeLite from any two IP addresses.
		/// </summary>
		/// <param name="ip1"></param>
		/// <param name="ip2"></param>
		public IpRangeLite(string ip1, string ip2)
		{
			low = ComparableIPAddress.Parse(ip1);
			high = ComparableIPAddress.Parse(ip2);
			if (low == null || high == null || low.byteLength != high.byteLength)
				return;
			if (low.CompareTo(high) > 0)
			{
				ComparableIPAddress tmp = low;
				low = high;
				high = tmp;
			}
			isValid = true;
		}

		/// <summary>
		/// Returns true if the specified IP address is contained within the IP range.
		/// </summary>
		/// <param name="address">IP Address</param>
		/// <returns></returns>
		public bool Contains(string address)
		{
			if (!isValid)
				return false;
			return Contains(ComparableIPAddress.Parse(address));
		}
		/// <summary>
		/// Returns true if the specified IP address is contained within the IP range.
		/// </summary>
		/// <param name="address">IP Address</param>
		/// <returns></returns>
		public bool Contains(IPAddress address)
		{
			if (!isValid)
				return false;
			if (address == null)
				return false;
			return low.CompareTo(address) <= 0 && high.CompareTo(address) >= 0;
		}
	}
}
