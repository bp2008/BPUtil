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
	/// Assists in web service development using webpack.
	/// </summary>
	public class WebpackProxy
	{
		int webpackPort;
		string workingDirectory;
		object webpackStartLock = new object();
		bool hasTriedRecovery = false;

		public WebpackProxy(int webpackPort, string workingDirectory = null)
		{
			this.webpackPort = webpackPort;
			this.workingDirectory = workingDirectory;
		}
		/// <summary>
		/// Attempts to proxy the connection to webpack, starting webpack dev server if necessary.
		/// </summary>
		/// <param name="p"></param>
		public void Proxy(HttpProcessor p)
		{
			Exception ex = TryProxy(p);
			if (ex == null)
				return;
			if (!hasTriedRecovery)
			{
				lock (webpackStartLock)
				{
					if (!hasTriedRecovery)
					{
						if (TestTcpPortBind(webpackPort))
						{
							Console.Error.WriteLine("Webpack dev server's TCP port " + webpackPort + " is closed. Trying to start webpack dev server.");
							if (TryStartWebpack())
							{
								Stopwatch sw = new Stopwatch();
								sw.Start();
								while (sw.ElapsedMilliseconds < 10000 && !TestTcpPortOpen(webpackPort))
									Thread.Sleep(100);
							}
						}
						else
							Console.Error.WriteLine("Webpack dev server's TCP port " + webpackPort + " is open, suggesting that it may be running.");
						hasTriedRecovery = true;
						Console.WriteLine(TestTcpPortOpen(webpackPort) ? "Webpack dev server is now running" : "Webpack dev server could not be started");
					}
				}
			}
			if (!p.Response.ResponseHeaderWritten)
				ex = TryProxy(p);
			if (ex == null)
				return;
			Logger.Debug(ex, "Failed to proxy \"" + p.Request.Url.PathAndQuery + "\" to webpack dev server.");
		}
		private Exception TryProxy(HttpProcessor p)
		{
			try
			{
				p.ProxyTo("http://" + IPAddress.Loopback.ToString() + ":" + webpackPort + p.Request.Url.AbsolutePath);
				return null;
			}
			catch (Exception ex) { return ex; }
		}
		/// <summary>
		/// Returns true if the webpack dev server was started successfully.
		/// </summary>
		/// <returns></returns>
		private bool TryStartWebpack()
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
				ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/C \"npm start\"");
				psi.UseShellExecute = true;
				psi.WorkingDirectory = new DirectoryInfo(workingDirectory).FullName;
				Process npm = Process.Start(psi);
				return true;
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "Failed to start webpack dev server (cmd.exe /C \"npm start\")");
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
				tcpListener = new TcpListener(IPAddress.Loopback, port);
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
				client = new TcpClient();
				client.SendTimeout = client.ReceiveTimeout = 1000;
				client.Connect(IPAddress.Loopback, port);
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
