using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public class NetworkInterfaceInfo
	{
		public readonly NetworkInterface Interface;
		public readonly IPAddress Address;
		public readonly IPAddress Mask;
		public NetworkInterfaceInfo(NetworkInterface iface, IPAddress address, IPAddress mask)
		{
			Interface = iface;
			Address = address;
			Mask = mask;
		}
		public IPAddress GetBroadcastAddress()
		{
			return Address.GetBroadcastAddress(Mask);
		}
		public IPAddress GetLowestInRange()
		{
			return IPUtil.GetLowestInRange(Address, Mask);
		}
		public IPAddress GetHighestInRange()
		{
			return IPUtil.GetHighestInRange(Address, Mask);
		}
	}
	public static class IPUtil
	{
		public static List<NetworkInterfaceInfo> GetOperationalInterfaces()
		{
			List<NetworkInterfaceInfo> ranges = new List<NetworkInterfaceInfo>();

			foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (netInterface.OperationalStatus != OperationalStatus.Up)
					continue;
				IPInterfaceProperties ipProps = netInterface.GetIPProperties();
				foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
				{
					if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
						ranges.Add(new NetworkInterfaceInfo(netInterface, addr.Address, addr.IPv4Mask));
				}
			}
			return ranges;
		}
		public static List<Tuple<IPAddress, IPAddress>> GetOperationalIPRanges()
		{
			return GetOperationalInterfaces()
				.Select(i => new Tuple<IPAddress, IPAddress>(GetLowestInRange(i.Address, i.Mask), GetHighestInRange(i.Address, i.Mask)))
				.ToList();
		}
		public static List<IPAddress> GetOperationalBroadcastIPs()
		{
			return GetOperationalInterfaces()
				.Select(i => i.Address.GetBroadcastAddress(i.Mask))
				.ToList();
		}
		public static IPAddress GetLowestInRange(IPAddress address, IPAddress mask)
		{
			byte[] addressBytes = address.GetAddressBytes();
			byte[] maskBytes = mask.GetAddressBytes();
			if (addressBytes.Length != maskBytes.Length || (addressBytes.Length != 4 && addressBytes.Length != 16))
				return IPAddress.None;
			byte[] lowest = new byte[addressBytes.Length];
			for (var i = 0; i < addressBytes.Length; i++)
				lowest[i] = (byte)(addressBytes[i] & maskBytes[i]);
			return new IPAddress(lowest);
		}
		public static IPAddress GetHighestInRange(IPAddress address, IPAddress mask)
		{
			byte[] addressBytes = address.GetAddressBytes();
			byte[] maskBytes = mask.GetAddressBytes();
			if (addressBytes.Length != maskBytes.Length || (addressBytes.Length != 4 && addressBytes.Length != 16))
				return IPAddress.None;
			byte[] highest = new byte[addressBytes.Length];
			for (var i = 0; i < addressBytes.Length; i++)
				highest[i] = (byte)((addressBytes[i] & maskBytes[i]) | ~maskBytes[i]);
			return new IPAddress(highest);
		}
		/// <summary>
		/// Returns a byte array containing the subnet mask with the given prefixSize.
		/// </summary>
		/// <param name="ipv4">If true, generate a subnet mask for IPv4.  If false, generate a subnet mask for IPv6.</param>
		/// <param name="prefixSize">Prefix size in bits (0-32 for IPv4, 0-128 for IPv6).</param>
		/// <returns></returns>
		public static byte[] GenerateMaskBytesFromPrefixSize(bool ipv4, int prefixSize)
		{
			int maxPrefixSize = ipv4 ? 32 : 128;
			if (prefixSize < 0 || prefixSize > maxPrefixSize)
				throw new ArgumentException("Prefix size for " + (ipv4 ? "IPv4" : "IPv6") + " must be between 0 and " + maxPrefixSize + ".", nameof(prefixSize));

			byte[] maskBytes = new byte[ipv4 ? 4 : 16];
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
			return maskBytes;
		}
	}
	public static class IPAddressExtensions
	{
		public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
		{
			byte[] ipAdressBytes = address.GetAddressBytes();
			byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

			byte[] broadcastAddress = new byte[ipAdressBytes.Length];
			for (int i = 0; i < broadcastAddress.Length; i++)
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
			}
			return new IPAddress(broadcastAddress);
		}

		public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
		{
			byte[] ipAdressBytes = address.GetAddressBytes();
			byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

			byte[] broadcastAddress = new byte[ipAdressBytes.Length];
			for (int i = 0; i < broadcastAddress.Length; i++)
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
			}
			return new IPAddress(broadcastAddress);
		}

		public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
		{
			IPAddress network1 = address.GetNetworkAddress(subnetMask);
			IPAddress network2 = address2.GetNetworkAddress(subnetMask);

			return network1.Equals(network2);
		}
	}
}
