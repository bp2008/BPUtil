using BPUtil.SimpleHttp.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Assists in web service development using Vite.
	/// </summary>
	public class ViteProxy
	{
		int vitePort;
		string workingDirectory;
		SemaphoreSlim viteStartLock = new SemaphoreSlim(1);
		bool hasTriedRecovery = false;
		string viteHost = IPAddress.IPv6Loopback.ToString();

		public ViteProxy(int vitePort, string workingDirectory = null)
		{
			this.vitePort = vitePort;
			this.workingDirectory = workingDirectory;
		}
		/// <summary>
		/// Attempts to proxy the connection to Vite, starting Vite dev server if necessary.
		/// </summary>
		/// <param name="p">HttpProcessor</param>
		public void Proxy(HttpProcessor p)
		{
			ProxyAsync(p).Wait();
		}
		/// <summary>
		/// Attempts to proxy the connection to Vite, starting Vite dev server if necessary.
		/// </summary>
		/// <param name="p">HttpProcessor</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		public async Task ProxyAsync(HttpProcessor p, CancellationToken cancellationToken = default)
		{
			Exception ex = await TryProxy(p).ConfigureAwait(false);
			if (ex == null)
				return;
			if (!hasTriedRecovery)
			{
				await viteStartLock.WaitAsync(cancellationToken).ConfigureAwait(false);
				try
				{
					if (!hasTriedRecovery)
					{
						if (TestTcpPortBind(vitePort))
						{
							Console.Error.WriteLine("Vite dev server's TCP port " + vitePort + " is closed. Trying to start Vite dev server.");
							if (TryStartVite())
							{
								Stopwatch sw = new Stopwatch();
								sw.Start();
								while (sw.ElapsedMilliseconds < 10000)
								{
									if (TestTcpPortOpen(vitePort))
									{
										// At the time of this writing, it seems that early requests proxied to Vite's web server can fail because Vite says to use Connection: keep-alive, but does not provide a recognized way to know when the response is finished.
										// As a workaround, we sleep a little after starting the Vite web server.
										await Task.Delay(500, cancellationToken).ConfigureAwait(false);
										break;
									}
									else
										await Task.Delay(100, cancellationToken).ConfigureAwait(false);
								}
							}
						}
						else
							Console.Error.WriteLine("Vite dev server's TCP port " + vitePort + " is open, suggesting that it may be running.");
						hasTriedRecovery = true;
						Console.WriteLine(TestTcpPortOpen(vitePort) ? "Vite dev server is now running" : "Vite dev server could not be started");
					}
				}
				finally
				{
					viteStartLock.Release();
				}
			}
			if (!p.responseWritten)
				ex = await TryProxy(p).ConfigureAwait(false);
			if (ex == null)
				return;
			if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
				Logger.Debug(ex, "Failed to proxy \"" + p.request_url.PathAndQuery + "\" to Vite dev server.");
			if (!p.responseWritten)
				await p.writeFailureAsync("504 Gateway Timeout", cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		private async Task<Exception> TryProxy(HttpProcessor p, CancellationToken cancellationToken = default)
		{
			try
			{
				UriBuilder builder = new UriBuilder(p.request_url);
				builder.Scheme = "http";
				builder.Host = IPAddress.IPv6Loopback.ToString();
				builder.Port = vitePort;
				await p.ProxyToAsync(builder.Uri.ToString(), new ProxyOptions()
				{
					allowGatewayTimeoutResponse = false,
					allowConnectionKeepalive = true,
					networkTimeoutMs = 5000,
					cancellationToken = cancellationToken
				}).ConfigureAwait(false);
				return null;
			}
			catch (Exception ex) { return ex; }
		}
		/// <summary>
		/// Returns true if the Vite dev server was started successfully.
		/// </summary>
		/// <returns></returns>
		private bool TryStartVite()
		{
#if NETFRAMEWORK || NET6_0_WIN
			try
			{
				string npmPath = NativeWin.PathCheck.GetFullPath("npm.cmd");
				if (npmPath == null)
				{
					Logger.Debug("node.js does not seem to be installed.");
					return false;
				}
				ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/C \"npm run-script dev\"");
				psi.UseShellExecute = true;
				psi.WorkingDirectory = new DirectoryInfo(workingDirectory).FullName;
				Process npm = Process.Start(psi);
				return true;
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "Failed to start Vite dev server (cmd.exe /C \"npm run-script dev\")");
				return false;
			}
#else
			throw new Exception("Unsupported Platform");
#endif
		}
		/// <summary>
		/// Attempts to briefly bind the specified TCP port, returning true if successful or false otherwise.
		/// </summary>
		/// <param name="port">The TCP port to test.</param>
		/// <returns></returns>
		private bool TestTcpPortBind(int port)
		{
			TcpListener tcpListener = null;
			try
			{
				tcpListener = new TcpListener(IPAddress.IPv6Loopback, port);
				tcpListener.Start();
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				if (tcpListener != null)
					Try.Swallow(tcpListener.Stop);
			}
		}
		/// <summary>
		/// Attempts to briefly connect to the specified port number on the loopback adapter, returning true if successful or false otherwise.
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		private bool TestTcpPortOpen(int port)
		{
			TcpClient client = null;
			try
			{
				client = new TcpClient(AddressFamily.InterNetworkV6);
				client.SendTimeout = client.ReceiveTimeout = 1000;
				client.Connect(IPAddress.IPv6Loopback, port);
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				if (client != null)
					Try.Swallow(client.Close);
			}
		}
	}
}
