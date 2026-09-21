using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BPUtil;
using BPUtil.SimpleHttp;
using BPUtil.SimpleHttp.WebSockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestWebSocketClient
	{
		/// <summary>
		/// Every test has a hard MSTest timeout so that a regression shows up as a failure rather than a hung test run.
		/// </summary>
		private const int TestTimeoutMs = 30000;

		#region Fake Servers
		/// <summary>
		/// A TCP server on 127.0.0.1 which accepts connections and passes each one to a handler on a background thread.  Each connection is closed when its handler returns or when the server is disposed.
		/// </summary>
		private class FakeServer : IDisposable
		{
			private readonly TcpListener listener;
			private readonly Action<TcpClient> handler;
			private readonly List<TcpClient> accepted = new List<TcpClient>();
			private volatile bool stopped = false;
			public int Port { get; private set; }
			public FakeServer(Action<TcpClient> handler)
			{
				this.handler = handler;
				listener = new TcpListener(IPAddress.Loopback, 0);
				listener.Start();
				Port = ((IPEndPoint)listener.LocalEndpoint).Port;
				Thread thr = new Thread(AcceptLoop);
				thr.IsBackground = true;
				thr.Start();
			}
			private void AcceptLoop()
			{
				try
				{
					while (!stopped)
					{
						TcpClient c = listener.AcceptTcpClient();
						lock (accepted)
							accepted.Add(c);
						Thread thr = new Thread(() =>
						{
							try { handler(c); } catch { }
							try { c.Close(); } catch { }
						});
						thr.IsBackground = true;
						thr.Start();
					}
				}
				catch { }
			}
			public void Dispose()
			{
				stopped = true;
				try { listener.Stop(); } catch { }
				lock (accepted)
				{
					foreach (TcpClient c in accepted)
						try { c.Close(); } catch { }
					accepted.Clear();
				}
			}
		}
		/// <summary>
		/// Accepts the connection and never sends anything.  The connection is held open until the client closes it.
		/// </summary>
		private static void SilentHandler(TcpClient c)
		{
			DrainUntilClientCloses(c.GetStream());
		}
		/// <summary>
		/// Reads the HTTP request headers, then never responds.  The connection is held open until the client closes it.
		/// </summary>
		private static void ReadRequestThenSilentHandler(TcpClient c)
		{
			NetworkStream s = c.GetStream();
			ReadRequestHeaders(s);
			DrainUntilClientCloses(s);
		}
		/// <summary>
		/// Reads the HTTP request headers and returns a non-101 response.  The connection is held open until the client closes it.
		/// </summary>
		private static void Respond500Handler(TcpClient c)
		{
			NetworkStream s = c.GetStream();
			ReadRequestHeaders(s);
			byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 500 Nope\r\nContent-Length: 0\r\n\r\n");
			s.Write(response, 0, response.Length);
			DrainUntilClientCloses(s);
		}
		private static void DrainUntilClientCloses(Stream s)
		{
			while (s.ReadByte() != -1)
			{
			}
		}
		private static void ReadRequestHeaders(Stream s)
		{
			string line;
			while (!string.IsNullOrEmpty(line = ByteUtil.ReadPrintableASCIILine(s)))
			{
			}
		}
		/// <summary>
		/// A BPUtil web server with a WebSocket echo endpoint.
		/// </summary>
		private class EchoWebSocketServer : HttpServer
		{
			/// <summary>
			/// Uses a self-signed certificate stored in the temp directory, because the default location (the test runner's directory) may not be writable.
			/// </summary>
			public EchoWebSocketServer() : base(new SelfSignedCertificateSelector(Path.GetTempPath(), "BPUtilUnitTests-SslCert.pfx")) { }
			public override void handleGETRequest(HttpProcessor p)
			{
				if (!WebSocket.IsWebSocketRequest(p))
				{
					p.Response.Simple("404 Not Found");
					return;
				}
				ManualResetEvent closed = new ManualResetEvent(false);
				WebSocket ws = null;
				ws = new WebSocket(p, frame =>
				{
					if (frame is WebSocketTextFrame textFrame)
					{
						if (textFrame.Text == "close-me")
							ws.Close();
						else
							ws.Send("echo:" + textFrame.Text);
					}
				}, closeFrame => closed.Set());
				ws.ReceiveTimeout = TestTimeoutMs;
				ws.SendTimeout = TestTimeoutMs;
				closed.WaitOne(TestTimeoutMs);
			}
			public override void handlePOSTRequest(HttpProcessor p)
			{
				p.Response.Simple("405 Method Not Allowed");
			}
			protected override void stopServer()
			{
			}
		}
		private static int GetUnusedPort()
		{
			TcpListener l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			int port = ((IPEndPoint)l.LocalEndpoint).Port;
			l.Stop();
			return port;
		}
		#endregion

		/// <summary>
		/// Runs the constructor, asserting that it throws <see cref="WebSocketClientTimeoutException"/> for the expected phase, within the expected time.
		/// </summary>
		private static WebSocketClientTimeoutException ExpectTimeout(Func<WebSocketClient> construct, WebSocketClientPhase expectedPhase, int timeoutMs)
		{
			Stopwatch sw = Stopwatch.StartNew();
			try
			{
				WebSocketClient client = construct();
				Assert.Fail("WebSocketClient constructor returned after " + sw.ElapsedMilliseconds + " ms instead of throwing.");
				return null;
			}
			catch (WebSocketClientTimeoutException ex)
			{
				sw.Stop();
				Assert.AreEqual(expectedPhase, ex.Phase, ex.ToString());
				Assert.AreEqual(timeoutMs, ex.TimeoutMs);
				StringAssert.Contains(ex.Message, WebSocketClientTimeoutException.GetPhaseDescription(expectedPhase));
				StringAssert.Contains(ex.Message, ex.HostAndPort);
				StringAssert.Contains(ex.Message, timeoutMs + " ms");
				Assert.IsTrue(sw.ElapsedMilliseconds >= timeoutMs - 250, "Threw too early: " + sw.ElapsedMilliseconds + " ms");
				Assert.IsTrue(sw.ElapsedMilliseconds < timeoutMs + 3000, "Threw too late: " + sw.ElapsedMilliseconds + " ms");
				return ex;
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestSilentTlsPeerTimesOut()
		{
			// The production case: the TCP connection is accepted, then the peer never sends a TLS ServerHello.
			using (FakeServer server = new FakeServer(SilentHandler))
			{
				ExpectTimeout(() => new WebSocketClient("https://127.0.0.1:" + server.Port + "/ws", true, 5000, 2000), WebSocketClientPhase.TlsHandshake, 2000);
				ExpectTimeout(() => new WebSocketClient("wss://127.0.0.1:" + server.Port + "/ws", true, 5000, 2000), WebSocketClientPhase.TlsHandshake, 2000);
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestSilentHttpPeerTimesOut()
		{
			using (FakeServer server = new FakeServer(ReadRequestThenSilentHandler))
			{
				ExpectTimeout(() => new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws", false, 5000, 2000), WebSocketClientPhase.HttpUpgradeResponse, 2000);
				ExpectTimeout(() => new WebSocketClient("ws://127.0.0.1:" + server.Port + "/ws", false, 5000, 2000), WebSocketClientPhase.HttpUpgradeResponse, 2000);
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestSilentPeerTimesOutWithLegacyConstructor()
		{
			// Code compiled against the original 2-argument constructor gets the default 15 second handshake timeout.
			using (FakeServer server = new FakeServer(ReadRequestThenSilentHandler))
			{
				ExpectTimeout(() => new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws"), WebSocketClientPhase.HttpUpgradeResponse, WebSocketClient.DefaultHandshakeTimeoutMs);
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestSlowDripHeadersHitOverallDeadline()
		{
			// Each header line arrives well within the per-read timeout, so only an overall deadline can stop this.
			using (FakeServer server = new FakeServer(c =>
			{
				NetworkStream s = c.GetStream();
				ReadRequestHeaders(s);
				byte[] first = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\n");
				s.Write(first, 0, first.Length);
				for (int i = 0; i < 1000; i++)
				{
					Thread.Sleep(250);
					byte[] header = Encoding.ASCII.GetBytes("X-Drip-" + i + ": " + i + "\r\n");
					s.Write(header, 0, header.Length);
				}
			}))
			{
				ExpectTimeout(() => new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws", false, 5000, 2000), WebSocketClientPhase.HttpUpgradeResponse, 2000);
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestTooManyResponseHeaderLinesRejected()
		{
			using (FakeServer server = new FakeServer(c =>
			{
				NetworkStream s = c.GetStream();
				ReadRequestHeaders(s);
				StringBuilder sb = new StringBuilder("HTTP/1.1 101 Switching Protocols\r\n");
				for (int i = 0; i < 101; i++)
					sb.Append("X-Header-" + i + ": " + i + "\r\n");
				byte[] response = Encoding.ASCII.GetBytes(sb.ToString());
				s.Write(response, 0, response.Length);
			}))
			{
				try
				{
					new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws", false, 5000, 10000);
					Assert.Fail("Expected exception.");
				}
				catch (Exception ex) when (!(ex is AssertFailedException))
				{
					Assert.IsNotInstanceOfType(ex, typeof(TimeoutException), ex.ToString());
					StringAssert.Contains(ex.Message, "header lines");
				}
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestServerClosesBeforeResponse()
		{
			// Formerly reported as WebSocketHttpResponseUnexpectedException: Invalid first HTTP response line: "".
			ExpectEndOfStream("", "status line");
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestServerClosesDuringStatusLine()
		{
			ExpectEndOfStream("HTTP/1.1 101 Swi", "status line");
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestServerClosesDuringHeaders()
		{
			// Formerly reported as a missing "Connection" header.
			ExpectEndOfStream("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\n", "headers");
		}
		/// <summary>
		/// Starts a server which reads the request, sends <paramref name="partialResponse"/>, then closes the connection.  Asserts that the client throws <see cref="EndOfStreamException"/> promptly.
		/// </summary>
		private static void ExpectEndOfStream(string partialResponse, string expectedMessagePart)
		{
			using (FakeServer server = new FakeServer(c =>
			{
				NetworkStream s = c.GetStream();
				ReadRequestHeaders(s);
				byte[] response = Encoding.ASCII.GetBytes(partialResponse);
				s.Write(response, 0, response.Length);
			}))
			{
				Stopwatch sw = Stopwatch.StartNew();
				try
				{
					new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws", false, 5000, 10000);
					Assert.Fail("Expected exception.");
				}
				catch (EndOfStreamException ex)
				{
					StringAssert.Contains(ex.Message, "127.0.0.1:" + server.Port);
					StringAssert.Contains(ex.Message, expectedMessagePart);
					Assert.IsTrue(sw.ElapsedMilliseconds < 5000, "took " + sw.ElapsedMilliseconds + " ms");
				}
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		[TestCategory("Network")]
		public void TestConnectTimeout()
		{
			// 192.0.2.1 is TEST-NET-1 (RFC 5737), which is not routed and normally drops SYNs silently.
			// If this network answers it immediately (RST or unreachable), the test cannot demonstrate anything and is inconclusive.
			try
			{
				ExpectTimeout(() => new WebSocketClient("https://192.0.2.1:443/ws", true, 1500, 2000), WebSocketClientPhase.Connect, 1500);
			}
			catch (SocketException ex)
			{
				Assert.Inconclusive("The network did not drop SYNs to TEST-NET-1: " + ex.Message);
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestConnectionRefusedStillThrowsSocketException()
		{
			int port = GetUnusedPort();
			try
			{
				new WebSocketClient("http://127.0.0.1:" + port + "/ws", false, 5000, 2000);
				Assert.Fail("Expected exception.");
			}
			catch (SocketException ex)
			{
				Assert.AreEqual(SocketError.ConnectionRefused, ex.SocketErrorCode);
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestUnexpectedResponseCodeStillThrowsOriginalException()
		{
			using (FakeServer server = new FakeServer(Respond500Handler))
			{
				try
				{
					new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws", false, 5000, 2000);
					Assert.Fail("Expected exception.");
				}
				catch (WebSocketHttpResponseCodeUnexpectedException ex)
				{
					Assert.AreEqual(500, ex.StatusCode);
				}
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestSuccessfulHandshake()
		{
			int port = GetUnusedPort();
			EchoWebSocketServer server = new EchoWebSocketServer();
			try
			{
				server.SetBindings(new HttpServerBase.Binding(AllowedConnectionTypes.http, (ushort)port, IPAddress.Loopback));
				string url = "ws://127.0.0.1:" + port + "/ws";

				// Legacy constructor: timeouts must be 0 after construction, exactly as before.
				WebSocketClient client = ConnectWithRetry(() => new WebSocketClient(url));
				Assert.AreEqual(0, client.tcpClient.ReceiveTimeout);
				Assert.AreEqual(0, client.tcpClient.SendTimeout);
				Assert.AreEqual(true, client.tcpClient.Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive) is int ka && ka != 0);
				AssertEcho(client);

				// New constructor with session timeouts.
				client = new WebSocketClient(url, false, 5000, 5000, 12345, 23456, false);
				Assert.AreEqual(12345, client.tcpClient.ReceiveTimeout);
				Assert.AreEqual(23456, client.tcpClient.SendTimeout);
				AssertEcho(client);

				// Host name resolution path, and a close handshake initiated by the server.
				client = new WebSocketClient("ws://localhost:" + port + "/ws", false, 5000, 5000);
				AssertEcho(client, true);
			}
			finally
			{
				server.Stop();
			}
		}
		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestTls13()
		{
			if (!IsTls13SupportedByOS())
				Assert.Inconclusive("TLS 1.3 is not supported by this OS.");
			int port = GetUnusedPort();
			EchoWebSocketServer server = new EchoWebSocketServer();
			try
			{
				server.SetBindings(new HttpServerBase.Binding(AllowedConnectionTypes.https, (ushort)port, IPAddress.Loopback));
				WebSocketClient client = ConnectWithRetry(() => new WebSocketClient("wss://127.0.0.1:" + port + "/ws", true, 5000, 10000));
				SslStream ssl = client.tcpStream as SslStream;
				Assert.IsNotNull(ssl, "tcpStream is not an SslStream");
				Assert.AreEqual(BPUtil.SimpleHttp.TLS.TlsNegotiate.Tls13, ssl.SslProtocol);
				AssertEcho(client);
			}
			finally
			{
				server.Stop();
			}
		}
		private static bool IsTls13SupportedByOS()
		{
			// Windows 11 / Server 2022 (build 20348+) are the first Windows versions with TLS 1.3 enabled in SChannel.
			// Environment.OSVersion is unreliable here because .NET Framework reports Windows 8 to apps without a compatibility manifest.
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				return true;
			using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
				return int.TryParse(key?.GetValue("CurrentBuildNumber") as string, out int build) && build >= 20348;
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestStateErroredOnConnectionLoss()
		{
			// The server completes the handshake, then drops the TCP connection without a close frame.
			using (FakeServer server = new FakeServer(c =>
			{
				NetworkStream s = c.GetStream();
				string key = null;
				string line;
				while (!string.IsNullOrEmpty(line = ByteUtil.ReadPrintableASCIILine(s)))
					if (line.StartsWith("Sec-WebSocket-Key: ", StringComparison.OrdinalIgnoreCase))
						key = line.Substring("Sec-WebSocket-Key: ".Length);
				byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + WebSocket.CreateSecWebSocketAcceptValue(key) + "\r\n\r\n");
				s.Write(response, 0, response.Length);
				Thread.Sleep(250);
			}))
			{
				WebSocketClient client = new WebSocketClient("ws://127.0.0.1:" + server.Port + "/ws", false, 5000, 5000);
				Assert.AreEqual(WebSocketState.Open, client.State);
				ManualResetEvent closed = new ManualResetEvent(false);
				client.StartReading(frame => { }, closeFrame => closed.Set());
				Assert.IsTrue(closed.WaitOne(10000), "onClose was not called.");
				Assert.AreEqual(WebSocketState.Errored, client.State);
			}
		}

		private static WebSocketClient ConnectWithRetry(Func<WebSocketClient> construct)
		{
			// The server starts its listener asynchronously.
			Stopwatch sw = Stopwatch.StartNew();
			while (true)
			{
				try
				{
					return construct();
				}
				catch (SocketException) when (sw.ElapsedMilliseconds < 5000)
				{
					Thread.Sleep(50);
				}
			}
		}
		/// <summary>
		/// Exchanges messages with the echo server, then closes gracefully and asserts the state transitions.
		/// </summary>
		/// <param name="client">A connected client which has not started reading yet.</param>
		/// <param name="serverInitiatesClose">If true, the server is asked to initiate the close handshake; otherwise the client initiates it.</param>
		private static void AssertEcho(WebSocketClient client, bool serverInitiatesClose = false)
		{
			Assert.AreEqual(WebSocketState.Open, client.State);
			BlockingQueue<string> received = new BlockingQueue<string>();
			ManualResetEvent closed = new ManualResetEvent(false);
			client.StartReading(frame =>
			{
				if (frame is WebSocketTextFrame textFrame)
					received.Enqueue(textFrame.Text);
			}, closeFrame => closed.Set());
			client.Send("hello");
			Assert.AreEqual("echo:hello", received.Dequeue(5000));
			client.Send("world");
			Assert.AreEqual("echo:world", received.Dequeue(5000));
			Assert.AreEqual(WebSocketState.Open, client.State);
			if (serverInitiatesClose)
				client.Send("close-me");
			else
			{
				client.Close();
				WebSocketState afterClose = client.State;
				Assert.IsTrue(afterClose == WebSocketState.CloseSent || afterClose == WebSocketState.Closed, "State after Close(): " + afterClose);
			}
			Assert.IsTrue(closed.WaitOne(5000), "onClose was not called.");
			Assert.AreEqual(WebSocketState.Closed, client.State);
		}
		private class BlockingQueue<T>
		{
			private readonly Queue<T> queue = new Queue<T>();
			private readonly SemaphoreSlim available = new SemaphoreSlim(0);
			public void Enqueue(T item)
			{
				lock (queue)
					queue.Enqueue(item);
				available.Release();
			}
			public T Dequeue(int timeoutMs)
			{
				if (!available.Wait(timeoutMs))
					throw new TimeoutException("No item was received within " + timeoutMs + " ms.");
				lock (queue)
					return queue.Dequeue();
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestNoSocketLeakOnHandshakeTimeout()
		{
			using (FakeServer server = new FakeServer(ReadRequestThenSilentHandler))
			{
				AssertNoLeak(server.Port, () =>
				{
					try
					{
						new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws", false, 5000, 100);
						Assert.Fail("Expected exception.");
					}
					catch (WebSocketClientTimeoutException) { }
				});
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestNoSocketLeakOnHandshakeFailure()
		{
			using (FakeServer server = new FakeServer(Respond500Handler))
			{
				AssertNoLeak(server.Port, () =>
				{
					try
					{
						new WebSocketClient("http://127.0.0.1:" + server.Port + "/ws", false, 5000, 5000);
						Assert.Fail("Expected exception.");
					}
					catch (WebSocketHttpResponseCodeUnexpectedException) { }
				});
			}
		}
		/// <summary>
		/// Runs the failing action 50 times against a server that holds each connection open until the client closes it, then asserts that no client-side connection to that server is still established (i.e. the client closed every socket itself rather than leaving it for the finalizer), and that the process handle count did not grow.
		/// </summary>
		private static void AssertNoLeak(int serverPort, Action failingAction)
		{
			failingAction(); // warm up
			GC.Collect();
			GC.WaitForPendingFinalizers();
			int handlesBefore = Process.GetCurrentProcess().HandleCount;
			for (int i = 0; i < 50; i++)
				failingAction();
			Thread.Sleep(500); // Let the fake server's handler threads see the client close and exit.

			int stillEstablished = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections()
				.Count(c => c.RemoteEndPoint.Port == serverPort && c.State == TcpState.Established);
			Assert.AreEqual(0, stillEstablished, "Client-side connections still established after failed constructor calls.");

			GC.Collect();
			GC.WaitForPendingFinalizers();
			int handlesAfter = Process.GetCurrentProcess().HandleCount;
			Assert.IsTrue(handlesAfter - handlesBefore < 25, "Handle count grew from " + handlesBefore + " to " + handlesAfter);
		}
	}
}
