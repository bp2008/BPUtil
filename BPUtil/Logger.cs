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
		/// <summary>
		/// Writes an exception to the logger, optionally including an additional string of information. Debug messages are colored yellow and/or red.
		/// </summary>
		/// <param name="ex">The exception to write.</param>
		/// <param name="additionalInformation">(Optional) An additional string of information, omitted if null or empty.</param>
		public static void Debug(Exception ex, string additionalInformation = "")
		{
			if (additionalInformation == null)
				additionalInformation = "";
			bool extraMsg = !string.IsNullOrEmpty(additionalInformation);
			if (!extraMsg && ex == null)
				return;
			lock (lockObj)
			{
				if ((logType & LoggingMode.Console) > 0)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(DateTime.Now.ToString());
					Console.ResetColor();
					Console.Write(": ");
					if (extraMsg)
					{
						Console.ForegroundColor = ConsoleColor.DarkYellow;
						Console.WriteLine(additionalInformation);
					}
					if (ex != null)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(ex.ToString());
					}
					Console.ResetColor();
				}
				if ((logType & LoggingMode.File) > 0 && (ex != null || !string.IsNullOrEmpty(additionalInformation)))
				{
					StringBuilder debugMessage = new StringBuilder();
					debugMessage.Append(DateTime.Now.ToString()).Append('\t');
					if (extraMsg)
						debugMessage.AppendLine(additionalInformation);
					if (ex != null)
						debugMessage.AppendLine(ex.ToString());
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

		/// <summary>
		/// Writes a string to the logger using debug coloring for the console.
		/// </summary>
		/// <param name="message">A string to write to the logger using debug coloring for the console.</param>
		public static void Debug(string message)
		{
			Debug(null, message);
		}
		/// <summary>
		/// Writes a string to the logger using standard coloring for the console.
		/// </summary>
		/// <param name="message"></param>
		public static void Info(string message)
		{
			if (message == null)
				return;
			lock (lockObj)
			{
				if ((logType & LoggingMode.Console) > 0)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(DateTime.Now.ToString());
					Console.ResetColor();
					Console.WriteLine(": " + message);
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

		/// <summary>
		/// Currently does nothing.
		/// </summary>
		/// <param name="logType"></param>
		/// <param name="message"></param>
		public static void Special(LogType logType, string message)
		{
			if (logType == LogType.HttpServer)
			{
				//Info(message);
			}
		}

		public static HttpLogger httpLogger = new HttpLogger();

		/// <summary>
		/// Call this if using [httpLogger] with the BPUtil SimpleHttpServer.
		/// </summary>
		public static void StartLoggingThreads()
		{
			httpLogger.StartLoggingThreads();
		}
		/// <summary>
		/// Call this to stop logging threads if you called [StartLoggingThreads].
		/// </summary>
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
			loggingThread.IsBackground = true;
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
