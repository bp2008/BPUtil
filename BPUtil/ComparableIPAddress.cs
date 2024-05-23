using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A class inheriting from IPAddress which also implements IComparable and can be compared with other IPAddresses.
	/// </summary>
	public class ComparableIPAddress : IPAddress, IComparable, IComparable<IPAddress>, IComparable<string>, IComparable<ComparableIPAddress>
	{
		/// <summary>
		/// Length of this IP address, in bytes.
		/// </summary>
		public int byteLength { get; private set; }
		protected ulong mostSignificantBytes = 0;
		protected ulong leastSignificantBytes = 0;
		/// <summary>
		/// Constructs a ComparableIPAddress from an IPAddress.
		/// </summary>
		/// <param name="address">IPAddress</param>
		public ComparableIPAddress(IPAddress address) : this(address.GetAddressBytes())
		{
			if (address?.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
				this.ScopeId = address.ScopeId;
		}
		/// <summary>
		/// Constructs a ComparableIPAddress from a byte array containing the address bytes.
		/// </summary>
		/// <param name="address">The byte array value of the IP address.</param>
		public ComparableIPAddress(byte[] address) : base(address)
		{
			Initialize(address);
		}
		/// <summary>
		/// Initializes a new instance of the ComparableIPAddress class with the address specified as an <see cref="long">.
		/// </summary>
		/// <param name="address">The <see cref="long"> value of the IP address. For example, the value 0x2414188f in big-endian format would be the IP address "143.24.20.36".</param>
		public ComparableIPAddress(long address) : base(address)
		{
			Initialize(null);
		}
		/// <summary>
		/// (for IPv6) Initializes a new instance of the ComparableIPAddress class with the address specified as a <see cref="byte"/> array and the specified scope identifier.
		/// </summary>
		/// <param name="address">The byte array value of the IP address.</param>
		/// <param name="scopeid">The long value of the IPv6 scope identifier.</param>
		public ComparableIPAddress(byte[] address, Int64 scopeid) : base(address, scopeid)
		{
			Initialize(address);
		}
		/// <summary>
		/// Initializes the two <see cref="ulong"/> fields <see cref="mostSignificantBytes"/> and <see cref="leastSignificantBytes"/> 
		/// which make this ComparableIPAddress able to compare with other ComparableIPAddress very slightly faster.
		/// </summary>
		/// <param name="address"></param>
		private void Initialize(byte[] address)
		{
			if (address == null)
				address = this.GetAddressBytes();
			byteLength = address.Length;
			for (int i = 0; i < address.Length && i < 16; i++)
			{
				if (i < 8)
					mostSignificantBytes |= ((ulong)address[i] << (56 - (i * 8)));
				else
					leastSignificantBytes |= ((ulong)address[i] << (120 - (i * 8)));
			}
			//string binary1 = Convert.ToString((long)mostSignificantBytes, 2).PadLeft(64, '0') + Convert.ToString((long)leastSignificantBytes, 2).PadLeft(64, '0');
			//string binary2 = string.Join("", address.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))).PadRight(128, '0');
			//if (binary1 != binary2)
			//	throw new Exception("Binary loading failed: \r\n" + binary1 + "\r\n" + binary2);
		}

		/// <summary>
		/// Parses the specified string into a ComparableIPAddress, returning null if the string does not represent a valid IP address.
		/// </summary>
		/// <param name="str">String that represents an IP address.</param>
		/// <returns></returns>
		public new static ComparableIPAddress Parse(string str)
		{
			if (!string.IsNullOrWhiteSpace(str) && TryParse(str, out IPAddress ip))
				return new ComparableIPAddress(ip.GetAddressBytes());
			return null;
		}

		/// <summary>
		/// Compares this address with another address.
		/// </summary>
		/// <param name="other">Other address.</param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (obj is ComparableIPAddress ci)
				return CompareTo(ci);
			if (obj is IPAddress ip)
				return CompareTo(ip);
			if (obj is string str)
				return CompareTo(str);
			return -1;
		}
		/// <summary>
		/// Compares this address with another address.
		/// </summary>
		/// <param name="other">Other address.</param>
		/// <returns></returns>
		public int CompareTo(IPAddress other)
		{
			if (other == null)
				return -1;
			byte[] b1 = this.GetAddressBytes();
			byte[] b2 = other.GetAddressBytes();
			if (b1.Length < b2.Length)
				return -1;
			else if (b1.Length > b2.Length)
				return 1;
			for (int i = 0; i < b1.Length; i++)
			{
				if (b1[i] < b2[i])
					return -1;
				else if (b1[i] > b2[i])
					return 1;
			}
			return 0;
		}
		/// <summary>
		/// Compares this address with another address.
		/// </summary>
		/// <param name="other">Other address.</param>
		/// <returns></returns>
		public int CompareTo(string other)
		{
			return CompareTo(Parse(other));
		}
		/// <summary>
		/// Compares this address with another address.
		/// </summary>
		/// <param name="other">Other address.</param>
		/// <returns></returns>
		public int CompareTo(ComparableIPAddress other)
		{
			// This specialized comparison method is barely any faster than the generic IPAddress comparer above.  It is not really significant.
			if (byteLength < other.byteLength)
				return -1;
			else if (byteLength > other.byteLength)
				return 1;
			if (byteLength > 16)
				return CompareTo((IPAddress)other);
			int diff = mostSignificantBytes.CompareTo(other.mostSignificantBytes);
			if (diff == 0)
				diff = leastSignificantBytes.CompareTo(other.leastSignificantBytes);
			return diff;
		}
	}
}
