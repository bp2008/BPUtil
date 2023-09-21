using System;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// A class which handles error logging by the http server.  It allows you to (optionally) register an ILogger instance to use for logging.
	/// </summary>
	public static class SimpleHttpLogger
	{
		private static ILogger logger = null;
		/// <summary>
		/// Must be true for the <c>LogVerbose</c> methods to log anything.  If false, those methods are a no-op.
		/// </summary>
		public static bool logVerbose { get; private set; } = false;
		/// <summary>
		/// (OPTIONAL) Keeps a static reference to the specified ILogger and uses it for http server error logging.  Only one logger can be registered at a time; attempting to register a second logger simply replaces the first one.
		/// </summary>
		/// <param name="loggerToRegister">The logger that should be used when an error message needs logged.  If null, logging will be disabled.</param>
		/// <param name="logVerboseMessages">If true, additional error reporting will be enabled.  These errors include things that can occur frequently during normal operation, so it may be spammy.</param>
		public static void RegisterLogger(ILogger loggerToRegister, bool logVerboseMessages = false)
		{
			logger = loggerToRegister;
			logVerbose = logVerboseMessages;
		}
		/// <summary>
		/// Unregisters the currently registered logger (if any) by calling RegisterLogger(null);
		/// </summary>
		public static void UnregisterLogger()
		{
			RegisterLogger(null);
		}
		internal static void Log(Exception ex, string additionalInformation = "")
		{
			try
			{
				if (logger != null)
					logger.Log(ex, additionalInformation);
			}
			catch { }
		}
		internal static void Log(string str)
		{
			try
			{
				if (logger != null)
					logger.Log(str);
			}
			catch { }
		}

		internal static void LogVerbose(Exception ex, string additionalInformation = "")
		{
			if (logVerbose)
				Log(ex, additionalInformation);
		}

		internal static void LogVerbose(string str)
		{
			if (logVerbose)
				Log(str);
		}
		internal static void LogRequest(DateTime time, string remoteHost, string requestMethod, string requestedUrl, string hostName)
		{
			LogVerbose(remoteHost + "\t-> " + hostName + "\t" + requestMethod + "\t" + requestedUrl);
			if (logger != null)
				try
				{
					logger.LogRequest(time, time.ToString("yyyy-MM-dd hh:mm:ss tt") + ":\t" + remoteHost + "\t" + requestMethod + "\t" + requestedUrl);
				}
				catch { }
		}
	}
	/// <summary>
	/// An interface which handles logging of exceptions and strings.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Log an exception, possibly with additional information provided to assist with debugging.
		/// </summary>
		/// <param name="ex">An exception that was caught.</param>
		/// <param name="additionalInformation">Additional information about the exception.</param>
		void Log(Exception ex, string additionalInformation = "");
		/// <summary>
		/// Log a string.
		/// </summary>
		/// <param name="str">A string to log.</param>
		void Log(string str);
		/// <summary>
		/// Log a request that was made to the server.
		/// </summary>
		/// <param name="time">The time of the request, from which the log file name will be chosen.</param>
		/// <param name="line">The string to log, including a timestamp and all information desired. This string should not contain line breaks.</param>
		void LogRequest(DateTime time, string line);
	}
}