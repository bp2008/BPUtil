using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A packet which has been received by UDP.
	/// </summary>
	public class UdpPacket
	{
		public IPEndPoint Sender;
		public byte[] Data;
		public UdpPacket(IPEndPoint sender, byte[] data)
		{
			Sender = sender;
			Data = data;
		}
	}
	/// <summary>
	/// A class which sends and optionally receives UDP broadcast packets on a particular port.
	/// </summary>
	public class UdpBroadcaster
	{
		bool running = true;
		UdpClient udp;
		Thread receiver;
		Thread sender;
		IPEndPoint sendEP;

		/// <summary>
		/// Raised when a UDP packet is received.
		/// </summary>
		public event EventHandler<UdpPacket> PacketReceived = delegate { };

		ConcurrentQueue<byte[]> outgoingPacketQueue = new ConcurrentQueue<byte[]>();
		EventWaitHandle sendWaiter = new EventWaitHandle(false, EventResetMode.AutoReset);

		/// <summary>
		/// Constructs a UdpBroadcaster instance that uses the default network interface (ideal for systems that have a single network adapter).
		/// </summary>
		/// <param name="port">The port number to send and receive on. This class will attempt to share the port with other processes instead of claiming exclusive use.</param>
		/// <param name="listen">If true, the UdpBroadcaster will listen for incoming packets on the port.  There is no way to distinguish between a broadcast packet and a packet targeted for this machine specifically, so both kinds of packets will be received.  The PacketReceived event will be raised with each received packet.</param>
		public UdpBroadcaster(int port, bool listen)
		{
			Initialize(IPAddress.Broadcast, IPAddress.Any, port, listen);
		}

		/// <summary>
		/// Constructs a UdpBroadcaster instance to send and listen on specific addresses.
		/// </summary>
		/// <param name="broadcastAddress">The address to broadcast to.</param>
		/// <param name="listenAddress">The address to listen on.</param>
		/// <param name="port">The port number to send and receive on. This class will attempt to share the port with other processes instead of claiming exclusive use.</param>
		/// <param name="listen">If true, the UdpBroadcaster will listen for incoming packets on the port.  There is no way to distinguish between a broadcast packet and a packet targeted for this machine specifically, so both kinds of packets will be received.  The PacketReceived event will be raised with each received packet.</param>
		public UdpBroadcaster(IPAddress broadcastAddress, IPAddress listenAddress, int port, bool listen)
		{
			Initialize(broadcastAddress, listenAddress, port, listen);
		}

		private void Initialize(IPAddress broadcastAddress, IPAddress listenAddress, int port, bool listen)
		{
			sendEP = new IPEndPoint(broadcastAddress, port);
			udp = new UdpClient();
			if (listen)
			{
				udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				udp.ExclusiveAddressUse = false;
				udp.Client.Bind(new IPEndPoint(listenAddress, port));

				receiver = new Thread(bgReceiver);
				receiver.IsBackground = true;
				receiver.Name = "UdpBroadcastListener";
				receiver.Start();
			}
			sender = new Thread(bgSender);
			sender.IsBackground = true;
			sender.Name = "UdpBroadcastSender";
			sender.Start();
		}
		/// <summary>
		/// Ends the send and receive threads.
		/// </summary>
		public void Stop()
		{
			running = false;
			sendWaiter.Set();
			Try.Catch(() =>
			{
				if (receiver != null)
					receiver.Abort();
			});
			Try.Catch(() =>
			{
				if (sender != null)
					sender.Abort();
			});
		}
		/// <summary>
		/// Broadcasts the specified packet.
		/// </summary>
		/// <param name="packet"></param>
		public void Broadcast(byte[] packet)
		{
			outgoingPacketQueue.Enqueue(packet);
			sendWaiter.Set();
		}

		private void bgReceiver()
		{
			try
			{
				while (running)
				{
					try
					{
						IPEndPoint sender = null;
						byte[] data = udp.Receive(ref sender);

						try
						{
							PacketReceived(this, new UdpPacket(sender, data));
						}
						catch (ThreadAbortException) { throw; }
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception ex)
					{
						Logger.Debug(ex);
						Thread.Sleep(500);
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}
		private void bgSender()
		{
			try
			{
				while (running)
				{
					try
					{
						sendWaiter.WaitOne();
						byte[] data;
						while (outgoingPacketQueue.TryDequeue(out data))
						{
							udp.Send(data, data.Length, sendEP);
						}
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception ex)
					{
						Logger.Debug(ex);
						Thread.Sleep(500);
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}
	}
}
