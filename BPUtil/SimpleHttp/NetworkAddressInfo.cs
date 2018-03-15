using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BPUtil.SimpleHttp
{
	public class NetworkAddressInfo
	{
		/// <summary>
		/// A list of IPv4 addresses belonging to this server.  Each item in this list has a corresponding item in the `localIPv4Masks` list at the same index.
		/// </summary>
		public List<byte[]> localIPv4Addresses = new List<byte[]>();
		/// <summary>
		/// A list of IPv4 subnet masks belonging to this server.  Each item in this list has a corresponding item in the `localIPv4Addresses` list at the same index.
		/// </summary>
		public List<byte[]> localIPv4Masks = new List<byte[]>();
		/// <summary>
		/// A list of IPv6 addresses belonging to this server.
		/// </summary>
		public List<IPAddress> localIPv6Addresses = new List<IPAddress>();

		public NetworkAddressInfo()
		{
		}

		public NetworkAddressInfo(NetworkInterface[] networkInterfaces)
		{
			foreach (NetworkInterface adapter in networkInterfaces)
			{
				IPInterfaceProperties ipProp = adapter.GetIPProperties();
				if (ipProp == null)
					continue;
				foreach (UnicastIPAddressInformation addressInfo in ipProp.UnicastAddresses)
				{
					if (addressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
						AddV4Address(addressInfo.Address.GetAddressBytes(), addressInfo.IPv4Mask.GetAddressBytes());
					else if (addressInfo.Address.AddressFamily == AddressFamily.InterNetworkV6)
						localIPv6Addresses.Add(addressInfo.Address);
				}
			}
		}

		private void AddV4Address(byte[] address, byte[] mask)
		{
			if (address.Length != 4 || mask.Length != 4)
				return;
			localIPv4Addresses.Add(address);
			localIPv4Masks.Add(mask);
		}

		/// <summary>
		/// Returns true if the specified address is the same as any of this server's addresses.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public bool IsSameMachine(IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetwork)
			{
				byte[] addressBytes = address.GetAddressBytes();
				foreach (byte[] localAddress in localIPv4Addresses)
					if (ByteUtil.ByteArraysMatch(addressBytes, localAddress))
						return true;
			}
			else if (address.AddressFamily == AddressFamily.InterNetworkV6)
			{
				foreach (IPAddress localAddress in localIPv6Addresses)
					if (address.Equals(localAddress))
						return true;
			}
			return false;
		}

		/// <summary>
		/// Returns true if the specified address is in the same subnet as any of this server's addresses.  For IPv6, this simply returns [address].IsIPv6LinkLocal.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public bool IsSameLAN(IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetwork)
				return IsSameLAN(address.GetAddressBytes());
			else if (address.AddressFamily == AddressFamily.InterNetworkV6)
				return address.IsIPv6LinkLocal;
			else
				return false;
		}
		/// <summary>
		/// Returns true if the specified IPv4 address is in the same subnet as any of this server's IPv4 addresses.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public bool IsSameLAN(byte[] address)
		{
			if (address.Length != 4)
				return false;
			for (int i = 0; i < localIPv4Addresses.Count; i++)
			{
				byte[] localAddress = localIPv4Addresses[i];
				byte[] mask = localIPv4Masks[i];
				if (ByteUtil.CompareWithMask(localAddress, address, mask))
					return true;
			}
			return false;
		}
	}
}