using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace BPUtil
{
	public enum LogType
	{
		HttpServer
	}
	public enum LoggingMode
	{
		None = 0,
		Console = 1,
		File = 2,
		Email = 4
	}
	public class Logger
	{
		public static LoggingMode logType = LoggingMode.File;
		private static object lockObj = new object();
		public static void Debug(Exception ex, string additionalInformation = "")
		{
			if (additionalInformation == null)
				additionalInformation = "";
			lock (lockObj)
			{
				if ((logType & LoggingMode.Console) > 0)
				{
					if (ex != null)
						Console.Write("Exception thrown at ");
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(DateTime.Now.ToString());
					if (ex != null)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(ex.ToString());
					}
					if (!string.IsNullOrEmpty(additionalInformation))
					{
						Console.ForegroundColor = ConsoleColor.DarkYellow;
						if (ex != null)
							Console.Write("Additional information: ");
						Console.WriteLine(additionalInformation);
					}
					Console.ResetColor();
				}
				if ((logType & LoggingMode.File) > 0 && (ex != null || !string.IsNullOrEmpty(additionalInformation)))
				{
					StringBuilder debugMessage = new StringBuilder();
					debugMessage.Append("-------------").Append(Environment.NewLine);
					if (ex != null)
						debugMessage.Append("Exception thrown at ");
					debugMessage.Append(DateTime.Now.ToString()).Append(Environment.NewLine);
					if (!string.IsNullOrEmpty(additionalInformation))
					{
						if (ex != null)
							debugMessage.Append("Additional information: ");
						debugMessage.Append(additionalInformation).Append(Environment.NewLine);
					}
					if (ex != null)
						debugMessage.Append(ex.ToString()).Append(Environment.NewLine);
					debugMessage.Append("-------------").Append(Environment.NewLine);
					int attempts = 0;
					while (attempts < 5)
					{
						try
						{
							File.AppendAllText(Globals.ErrorFilePath, debugMessage.ToString(), Encoding.UTF8);
							attempts = 10;
						}
						catch (ThreadAbortException) { throw; }
						catch
						{
							attempts++;
						}
					}
				}
			}
		}

		public static void Debug(string message)
		{
			Debug(null, message);
		}
		public static void Info(string message)
		{
			if (message == null)
				return;
			lock (lockObj)
			{
				if ((logType & LoggingMode.Console) > 0)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(DateTime.Now.ToString());
					Console.ResetColor();
					Console.WriteLine(message);
				}
				if ((logType & LoggingMode.File) > 0)
				{
					int attempts = 0;
					while (attempts < 5)
					{
						try
						{
							File.AppendAllText(Globals.ErrorFilePath, DateTime.Now.ToString() + '\t' + message + Environment.NewLine, Encoding.UTF8);
							attempts = 10;
						}
						catch (ThreadAbortException) { throw; }
						catch
						{
							attempts++;
						}
					}
				}
			}
		}

		public static void Special(LogType logType, string message)
		{
			if (logType == LogType.HttpServer)
			{
				//Info(message);
			}
		}

		public static HttpLogger httpLogger = new HttpLogger();

		public static void StartLoggingThreads()
		{
			httpLogger.StartLoggingThreads();
		}
		public static void StopLoggingThreads()
		{
			httpLogger.StopLoggingThreads();
		}
	}
	public class HttpLogger : SimpleHttp.ILogger
	{
		void SimpleHttp.ILogger.Log(Exception ex, string additionalInformation)
		{
			Logger.Debug(ex, additionalInformation);
		}

		void SimpleHttp.ILogger.Log(string str)
		{
			Logger.Debug(str);
		}

		void SimpleHttp.ILogger.LogRequest(DateTime time, string line)
		{
			itemsToLog.Enqueue(new Tuple<DateTime, string>(time, line));
		}

		ConcurrentQueue<Tuple<DateTime, string>> itemsToLog = new ConcurrentQueue<Tuple<DateTime, string>>();
		Thread loggingThread = null;
		public HttpLogger()
		{
		}
		void loggingThreadLoop()
		{
			try
			{
				Tuple<DateTime, string> nextItemToLog;
				while (true)
				{
					try
					{
						while (itemsToLog.TryDequeue(out nextItemToLog))
						{
							string logFileName = GetWebServerLogFilePathForToday(nextItemToLog.Item1);
							FileInfo logFile = new FileInfo(logFileName);
							DirectoryInfo di = logFile.Directory;
							if (!di.Exists)
								Directory.CreateDirectory(di.FullName);
							File.AppendAllText(logFileName, nextItemToLog.Item2 + Environment.NewLine, Encoding.UTF8);
						}
						Thread.Sleep(100);
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}
		public void StopLoggingThreads()
		{
			try
			{
				if (loggingThread != null)
					loggingThread.Abort();
			}
			catch (ThreadAbortException) { throw; }
			catch { }
		}

		public void StartLoggingThreads()
		{
			StopLoggingThreads();
			loggingThread = new Thread(loggingThreadLoop);
			loggingThread.Name = "HttpLogger thread";
			loggingThread.Start();
		}
		/// <summary>
		/// Gets the full path to the web server log file that should be used for events logged at the specified time.
		/// </summary>
		/// <returns></returns>
		private string GetWebServerLogFilePathForToday(DateTime time)
		{
			return Globals.WritableDirectoryBase + "web_server_request_logs/" + time.Year + "/" + time.Month.ToString().PadLeft(2, '0') + "/" + time.Year + "-" + time.Month.ToString().PadLeft(2, '0') + "-" + time.Day.ToString().PadLeft(2, '0') + ".txt";
		}
	}
}
