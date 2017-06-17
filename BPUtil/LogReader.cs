using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// A class which can read a log file and keep a rolling view of the last 50000 lines of text, presenting them upon request by a client.
	/// </summary>
	public class StreamingLogReader
	{
		private StreamReader inputFile;
		private FileInfo fiInputFile;
		private volatile LogLine[] logLines = new LogLine[50000];
		private const int MaxLinesToReadAtOnce = 1000;
		private volatile int logLineCount = 0;
		private Thread thrFileRead;
		public readonly long readerId;
		private int StreamingSleepLength;

		public StreamingLogReader(string path = null, int StreamingSleepLength = 500)
		{
			this.StreamingSleepLength = StreamingSleepLength;
			readerId = DateTime.Now.ToBinary();
			if (path == null)
				path = Globals.ErrorFilePath;
			fiInputFile = new FileInfo(path);
		}

		public void Start()
		{
			Stop();
			// Do initial file reading
			OpenFile();
			// Start continuous reading thread
			thrFileRead = new Thread(threadLoop);
			thrFileRead.Name = "Log Reader";
			thrFileRead.IsBackground = true;
			thrFileRead.Start();
		}

		private void OpenFile()
		{
			if (inputFile != null)
				inputFile.Close();
			inputFile = new StreamReader(new FileStream(fiInputFile.FullName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete), Encoding.UTF8);
			string line;
			while ((line = inputFile.ReadLine()) != null)
				AddLogLine(line);
		}
		public void Stop()
		{
			thrFileRead?.Abort();
		}
		private void AddLogLine(string line)
		{
			logLines[logLineCount % logLines.Length] = new LogLine(line, logLineCount);
			logLineCount++;
		}
		private void threadLoop()
		{
			try
			{
				while (true)
				{
					try
					{
						Thread.Sleep(StreamingSleepLength);
						string line;
						while ((line = inputFile.ReadLine()) != null)
							AddLogLine(line);
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception ex)
					{
						Logger.Debug(ex);
						Thread.Sleep(1000);
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			finally
			{
				inputFile.Close();
			}
		}
		private LogLine GetLogLine(int index)
		{
			if (index < 0)
				return null;
			index = index % logLines.Length;
			return logLines[index];
		}
		private List<string> GetLog(int startingLine, int patience, ref int lastLineLoaded)
		{
			if (startingLine <= 0)
			{
				startingLine = logLineCount - MaxLinesToReadAtOnce;
				if (startingLine < 0)
					startingLine = 0;
			}
			List<string> retVal = new List<string>();
			while (patience > -1 && retVal.Count == 0)
			{
				if (patience == 0)
					patience = -1;
				int endingLine = Math.Min(startingLine + MaxLinesToReadAtOnce, logLineCount);
				for (int i = startingLine; i < endingLine; i++)
				{
					LogLine line = GetLogLine(i);
					retVal.Add(line.text);
					lastLineLoaded = i;
				}
				if (retVal.Count == 0)
				{
					int timeToWait = Math.Min(patience, 500);
					if (timeToWait > 0)
					{
						patience -= timeToWait;
						Thread.Sleep(timeToWait);
					}
				}
			}
			return retVal;
		}
		public List<string> GetLogUpdate(long userReaderId, ref int lastLineLoaded)
		{
			if (userReaderId != readerId)
				return new List<string>(new string[] { "refresh" });
			return GetLog(lastLineLoaded + 1, 30000, ref lastLineLoaded);
		}
	}
	public class LogLine
	{
		public string text;
		public int lineIndex;

		public LogLine(string text, int lineIndex)
		{
			this.text = text;
			this.lineIndex = lineIndex;
		}
	}
}
