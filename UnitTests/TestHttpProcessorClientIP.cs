using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BPUtil;
using BPUtil.SimpleHttp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	/// <summary>
	/// Tests how <see cref="HttpProcessor.RemoteIPAddress"/> is learned from proxy headers ("X-Real-Ip", "X-Forwarded-For", "CF-Connecting-IP").
	/// </summary>
	[TestClass]
	public class TestHttpProcessorClientIP
	{
		private const int TestTimeoutMs = 20000;
		/// <summary>
		/// A web server that responds to every request with the client IP address it determined.
		/// </summary>
		private class EchoClientIPServer : HttpServer
		{
			public bool TrustLoopbackProxy = true;
			public override void handleGETRequest(HttpProcessor p)
			{
				p.Response.FullResponseUTF8(p.RemoteIPAddressStr, "text/plain; charset=utf-8");
			}
			public override void handlePOSTRequest(HttpProcessor p)
			{
				p.Response.Simple("405 Method Not Allowed");
			}
			public override bool IsTrustedProxyServer(HttpProcessor p, IPAddress remoteIpAddress)
			{
				return TrustLoopbackProxy && IPAddress.IsLoopback(remoteIpAddress);
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
		/// <summary>
		/// Sends a GET request with the given extra header lines and returns the response body (the client IP address determined by the server).
		/// </summary>
		private static string GetClientIP(int port, params string[] headerLines)
		{
			Exception lastError = null;
			// The listener starts asynchronously, so retry the connection briefly.
			for (int attempt = 0; attempt < 50; attempt++)
			{
				try
				{
					using (TcpClient client = new TcpClient())
					{
						client.ReceiveTimeout = client.SendTimeout = 5000;
						client.Connect(IPAddress.Loopback, port);
						NetworkStream s = client.GetStream();
						StringBuilder sb = new StringBuilder();
						sb.Append("GET / HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n");
						foreach (string line in headerLines)
							sb.Append(line + "\r\n");
						sb.Append("\r\n");
						byte[] request = ByteUtil.Utf8NoBOM.GetBytes(sb.ToString());
						s.Write(request, 0, request.Length);
						string response;
						using (StreamReader reader = new StreamReader(s, ByteUtil.Utf8NoBOM))
							response = reader.ReadToEnd();
						Assert.IsTrue(response.StartsWith("HTTP/1.1 200"), "Unexpected response: " + response);
						int bodyStart = response.IndexOf("\r\n\r\n");
						return response.Substring(bodyStart + 4);
					}
				}
				catch (SocketException ex)
				{
					lastError = ex;
					Thread.Sleep(100);
				}
			}
			throw new Exception("Unable to connect to test server.", lastError);
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestCFConnectingIPTakesPrecedence()
		{
			int port = GetUnusedPort();
			EchoClientIPServer server = new EchoClientIPServer();
			server.XRealIPHeader = true;
			server.XForwardedForHeader = true;
			server.CFConnectingIPHeader = true;
			try
			{
				server.SetBindings(new HttpServerBase.Binding(AllowedConnectionTypes.http, (ushort)port, IPAddress.Loopback));

				string[] allThree = new string[] { "X-Real-Ip: 10.0.0.1", "X-Forwarded-For: 10.0.0.2, 10.0.0.3", "CF-Connecting-IP: 203.0.113.9" };
				Assert.AreEqual("203.0.113.9", GetClientIP(port, allThree));

				// Header order in the request must not matter.
				Assert.AreEqual("2001:db8::1", GetClientIP(port, "CF-Connecting-IP: 2001:db8::1", "X-Forwarded-For: 10.0.0.2", "X-Real-Ip: 10.0.0.1"));

				// Without CF-Connecting-IP, the other headers still work as before (X-Forwarded-For is applied after X-Real-Ip).
				Assert.AreEqual("10.0.0.2", GetClientIP(port, "X-Real-Ip: 10.0.0.1", "X-Forwarded-For: 10.0.0.2, 10.0.0.3"));

				// An unparseable CF-Connecting-IP value is ignored.
				Assert.AreEqual("10.0.0.1", GetClientIP(port, "X-Real-Ip: 10.0.0.1", "CF-Connecting-IP: not-an-ip"));
			}
			finally
			{
				server.Stop();
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestCFConnectingIPRequiresFlag()
		{
			int port = GetUnusedPort();
			EchoClientIPServer server = new EchoClientIPServer();
			server.XRealIPHeader = true;
			try
			{
				server.SetBindings(new HttpServerBase.Binding(AllowedConnectionTypes.http, (ushort)port, IPAddress.Loopback));
				Assert.AreEqual("10.0.0.1", GetClientIP(port, "X-Real-Ip: 10.0.0.1", "CF-Connecting-IP: 203.0.113.9"));
			}
			finally
			{
				server.Stop();
			}
		}

		[TestMethod]
		[Timeout(TestTimeoutMs)]
		public void TestCFConnectingIPIgnoredFromUntrustedPeer()
		{
			int port = GetUnusedPort();
			EchoClientIPServer server = new EchoClientIPServer();
			server.TrustLoopbackProxy = false;
			server.XRealIPHeader = true;
			server.XForwardedForHeader = true;
			server.CFConnectingIPHeader = true;
			try
			{
				server.SetBindings(new HttpServerBase.Binding(AllowedConnectionTypes.http, (ushort)port, IPAddress.Loopback));
				Assert.AreEqual("127.0.0.1", GetClientIP(port, "X-Real-Ip: 10.0.0.1", "X-Forwarded-For: 10.0.0.2", "CF-Connecting-IP: 203.0.113.9"));
			}
			finally
			{
				server.Stop();
			}
		}
	}
}
