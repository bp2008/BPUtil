using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	public class UdpInfoRow
	{
		public IPAddress LocalAddress;
		public int LocalPort;
		public int ProcessID;
	}
	public class UdpInfoTable
	{
		public List<UdpInfoRow> Rows = new List<UdpInfoRow>();

		public static UdpInfoTable Load()
		{
			int bufferSize = 0;
			IntPtr udpTablePtr = IntPtr.Zero;

			try
			{
				// Get the size of the UDP table
				uint result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, 2, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
				udpTablePtr = Marshal.AllocHGlobal(bufferSize);

				// Get the actual UDP table
				result = GetExtendedUdpTable(udpTablePtr, ref bufferSize, true, 2, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
				if (result != 0)
				{
					throw new Exception("Failed to get UDP table.");
				}

				MIB_UDPTABLE_OWNER_PID udpTable = (MIB_UDPTABLE_OWNER_PID)Marshal.PtrToStructure(udpTablePtr, typeof(MIB_UDPTABLE_OWNER_PID));
				udpTable.table = new MIB_UDPROW_OWNER_PID[udpTable.dwNumEntries];
				IntPtr rowPtr = new IntPtr(udpTablePtr.ToInt64() + Marshal.SizeOf(udpTable.dwNumEntries));

				for (int i = 0; i < udpTable.dwNumEntries; i++)
				{
					udpTable.table[i] = (MIB_UDPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_PID));
					rowPtr = new IntPtr(rowPtr.ToInt64() + Marshal.SizeOf(typeof(MIB_UDPROW_OWNER_PID)));
				}
				UdpInfoTable tbl = new UdpInfoTable();
				foreach (MIB_UDPROW_OWNER_PID row in udpTable.table)
				{
					UdpInfoRow newRow = new UdpInfoRow();
					newRow.LocalAddress = new IPAddress(row.dwLocalAddr);
					// Convert the port to an unsigned short first and then to an integer
					newRow.LocalPort = (int)((ushort)IPAddress.NetworkToHostOrder((short)row.dwLocalPort));
					newRow.ProcessID = (int)row.dwOwningPid;
					tbl.Rows.Add(newRow);
				}
				return tbl;
			}
			finally
			{
				if (udpTablePtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(udpTablePtr);
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MIB_UDPTABLE_OWNER_PID
		{
			public uint dwNumEntries;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public MIB_UDPROW_OWNER_PID[] table;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MIB_UDPROW_OWNER_PID
		{
			public uint dwLocalAddr;
			public uint dwLocalPort;
			public uint dwOwningPid;
		}

		[DllImport("iphlpapi.dll", SetLastError = true)]
		private static extern uint GetExtendedUdpTable(
			IntPtr pUdpTable,
			ref int pdwSize,
			bool bOrder,
			uint ulAf,
			UDP_TABLE_CLASS TableClass,
			uint Reserved);

		private enum UDP_TABLE_CLASS
		{
			UDP_TABLE_BASIC,
			UDP_TABLE_OWNER_PID
		}
	}
}
