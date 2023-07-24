using System;
using System.Net;

namespace BPUtil
{
	/// <summary>
	/// Represents a range of IP addresses.
	/// </summary>
	public class IPAddressRange : IComparable<IPAddressRange>
	{
		/// <summary>
		/// The first address in the range.
		/// </summary>
		public readonly IPAddress StartAddress;
		/// <summary>
		/// The last address in the range.
		/// </summary>
		public readonly IPAddress EndAddress;

		/// <summary>
		/// Initializes a new instance of the <see cref="IPAddressRange"/> class with the specified range.
		/// </summary>
		/// <param name="range">The range of IP addresses, specified as a single IP address (e.g. "127.0.0.1"), an IP address range (e.g. "192.168.0.1 - 192.168.1.255"), or a subnet (e.g. "192.168.0.1/24").</param>
		/// <exception cref="ArgumentNullException"><paramref name="range"/> is <c>null</c>.</exception>
		public IPAddressRange(string range)
		{
			if (range == null)
				throw new ArgumentNullException(nameof(range));

			if (range.Contains("/"))
			{
				string[] parts = range.Split('/');
				if (parts.Length != 2)
					throw new ArgumentException("The given input string was invalid: \"" + range + "\"", nameof(range));

				IPAddress givenAddress = IPAddress.Parse(parts[0].Trim());
				int prefixSize = int.Parse(parts[1].Trim());
				GetAddressRange(givenAddress, prefixSize, out StartAddress, out EndAddress);
			}
			else if (range.Contains("-"))
			{
				string[] parts = range.Split('-');
				if (parts.Length != 2)
					throw new ArgumentException("The given input string was invalid: \"" + range + "\"", nameof(range));
				StartAddress = IPAddress.Parse(parts[0].Trim());
				EndAddress = IPAddress.Parse(parts[1].Trim());
				if (StartAddress.AddressFamily != EndAddress.AddressFamily)
					throw new ArgumentException("The given addresses are not in the same AddressFamily: \"" + range + "\"", nameof(range));
			}
			else
			{
				StartAddress = IPAddress.Parse(range);
				EndAddress = StartAddress;
			}
		}

		/// <summary>
		/// Determines whether the specified IP address is within the range represented by this instance.
		/// </summary>
		/// <param name="address">The IP address to check.</param>
		/// <returns><c>true</c> if the specified IP address is within the range represented by this instance; otherwise, <c>false</c>.</returns>
		public bool IsInRange(string address)
		{
			return IsInRange(IPAddress.Parse(address));
		}

		/// <summary>
		/// Determines whether the specified IP address is within the range represented by this instance.
		/// </summary>
		/// <param name="address">The IP address to check.</param>
		/// <returns><c>true</c> if the specified IP address is within the range represented by this instance; otherwise, <c>false</c>.</returns>
		public bool IsInRange(IPAddress address)
		{
			byte[] startBytes = StartAddress.GetAddressBytes();
			byte[] endBytes = EndAddress.GetAddressBytes();
			byte[] ipBytes = address.GetAddressBytes();

			for (int i = 0; i < startBytes.Length; i++)
			{
				if (ipBytes[i] < startBytes[i] || ipBytes[i] > endBytes[i])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the first and last IP addresses in the specified network.
		/// </summary>
		/// <param name="address">Address which is part of the network.</param>
		/// <param name="prefixSize">Network size in bits.  E.g. for the subnet "192.168.0.1/24", <c>prefixSize</c> is <c>24</c>.</param>
		/// <param name="first">(Output) First address in the range.</param>
		/// <param name="last">(Output) Last address in the range.</param>
		/// <returns></returns>
		private static void GetAddressRange(IPAddress address, int prefixSize, out IPAddress first, out IPAddress last)
		{
			byte[] addressBytes = address.GetAddressBytes();
			int maxPrefixSize = addressBytes.Length * 8;

			if (prefixSize < 0 || prefixSize > maxPrefixSize)
				throw new ArgumentException("Prefix size must be between 0 and maxPrefixSize" + prefixSize + ".");

			byte[] maskBytes = new byte[addressBytes.Length];
			int remainingBits = prefixSize;
			for (int i = 0; i < maskBytes.Length; i++)
			{
				if (remainingBits >= 8)
				{
					maskBytes[i] = 0xFF;
					remainingBits -= 8;
				}
				else
				{
					maskBytes[i] = (byte)(0xFF << (8 - remainingBits));
					break;
				}
			}

			byte[] firstIPAddressBytes = new byte[addressBytes.Length];
			byte[] lastIPAddressBytes = new byte[addressBytes.Length];
			for (int i = 0; i < addressBytes.Length; i++)
			{
				firstIPAddressBytes[i] = (byte)(addressBytes[i] & maskBytes[i]);
				lastIPAddressBytes[i] = (byte)(firstIPAddressBytes[i] | ~maskBytes[i]);
			}
			first = new IPAddress(firstIPAddressBytes);
			last = new IPAddress(lastIPAddressBytes);
		}

		/// <summary>
		/// Compares this instance to another instance of the same type and returns an integer that indicates whether this instance precedes, follows, or occurs in the same position in the sort order as the other instance.
		/// </summary>
		/// <param name="other">An object to compare with this instance.</param>
		/// <returns>A value that indicates the relative order of the objects being compared.</returns>
		public int CompareTo(IPAddressRange other)
		{
			if (other == null)
				return 1;

			byte[] startBytes = StartAddress.GetAddressBytes();
			byte[] otherStartBytes = other.StartAddress.GetAddressBytes();
			for (int i = 0; i < startBytes.Length; i++)
			{
				if (startBytes[i] < otherStartBytes[i])
					return -1;
				else if (startBytes[i] > otherStartBytes[i])
					return 1;
			}

			byte[] endBytes = EndAddress.GetAddressBytes();
			byte[] otherEndBytes = other.EndAddress.GetAddressBytes();
			for (int i = 0; i < endBytes.Length; i++)
			{
				if (endBytes[i] < otherEndBytes[i])
					return -1;
				else if (endBytes[i] > otherEndBytes[i])
					return 1;
			}

			return 0;
		}
		/// <summary>
		/// Returns true if the IPAddressRange is equal to another IPAddressRange.
		/// </summary>
		/// <param name="obj">Another IPAddressRange instance to compare with.</param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			var other = (IPAddressRange)obj;
			return StartAddress.Equals(other.StartAddress) && EndAddress.Equals(other.EndAddress);
		}
		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return StartAddress.GetHashCode() ^ EndAddress.GetHashCode();
		}
		/// <summary>
		/// Determines if two IPAddressRange instances are equal.
		/// </summary>
		/// <param name="left">IPAddressRange left of the operator.</param>
		/// <param name="right">IPAddressRange right of the operator.</param>
		/// <returns></returns>
		public static bool operator ==(IPAddressRange left, IPAddressRange right)
		{
			if (ReferenceEquals(left, null))
				return ReferenceEquals(right, null);

			return left.Equals(right);
		}
		/// <summary>
		/// Determines if two IPAddressRange instances are not equal.
		/// </summary>
		/// <param name="left">IPAddressRange left of the operator.</param>
		/// <param name="right">IPAddressRange right of the operator.</param>
		/// <returns></returns>
		public static bool operator !=(IPAddressRange left, IPAddressRange right)
		{
			return !(left == right);
		}
		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			if (StartAddress.Equals(EndAddress))
				return StartAddress.ToString();
			else
				return StartAddress + " - " + EndAddress;
		}
	}
}
