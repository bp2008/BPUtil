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
		private volatile bool abort = false;
		private Thread thrFileRead;
		public readonly long readerId;
		private int StreamingSleepLength;

		public StreamingLogReader(string path = null, int StreamingSleepLength = 500)
		{
			this.StreamingSleepLength = StreamingSleepLength;
			readerId = TimeUtil.GetTimeInMsSinceEpoch();
			if (path == null)
				path = Globals.ErrorFilePath;
			fiInputFile = new FileInfo(path);
		}

		public void Start()
		{
			Stop();
			abort = false;
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
			inputFile = new StreamReader(new FileStream(fiInputFile.FullName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete), ByteUtil.Utf8NoBOM);
			string line;
			while ((line = inputFile.ReadLine()) != null)
				AddLogLine(line);
		}
		public void Stop()
		{
			if (!abort)
			{
				abort = true;
				thrFileRead?.Join();
			}
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
				while (!abort)
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
	/// <summary>
	/// <para>A log reader similar to StreamingLogReader that offers a simpler API and doesn't run its own thread, but also doesn't share resources with multiple consumers.</para>
	/// <para>This reader is only capable of reading the application's regular log (see <see cref="Logger"/>) but is capable of transitioning seamlessly between log files as they roll over.</para>
	/// <para>Not thread safe.</para>
	/// </summary>
	public class StreamingLogReader2 : IDisposable
	{
		private string lastFilePath = null;
		private bool disposedValue;
		private StreamReader streamReader;
		/// <summary>
		/// Opens the current log file (see <see cref="Logger"/>) and seeks to the position that is the given number of lines away from the end.
		/// </summary>
		/// <param name="initialLineCount">Number of lines of text that should be passed while seeking backwards from the end of the file.</param>
		public StreamingLogReader2(uint initialLineCount = 100)
		{
			string filePathNow = Globals.ErrorFilePath;
			// Open stream, seek backwards until [initialLineCount] lines are found.
			FileStream fs = new FileStream(filePathNow, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			try
			{
				byte[] buffer = new byte[256];
				long offset = fs.Seek(0, SeekOrigin.End);
				uint lineEndingsSeen = 0;
				while (fs.Position > 0 && lineEndingsSeen < initialLineCount)
				{
					long toRead = Math.Min(buffer.Length, fs.Position);
					fs.Seek(-toRead, SeekOrigin.Current);
					fs.Read(buffer, 0, (int)toRead);

					for (int i = (int)toRead - 1; i >= 0; i--)
					{
						if (buffer[i] == '\n')
						{
							lineEndingsSeen++;
							if (lineEndingsSeen >= initialLineCount)
								break;
						}
						offset--;
					}
					fs.Seek(offset, SeekOrigin.Begin);
				}
				// Create StreamReader at current position
				streamReader = new StreamReader(fs, ByteUtil.Utf8NoBOM);
				lastFilePath = filePathNow;
			}
			catch
			{
				fs.Close();
				throw;
			}
		}
		/// <summary>
		/// <para>Reads from the current position to the end of the file, appending all text to the given <see cref="StringBuilder"/>.</para>
		/// <para>If we're already (still) at the end of the file, no text will be inserted into the StringBuilder.</para>
		/// <para>This method knows how to transition to the next log file when the current one is finished or deleted.</para>
		/// </summary>
		/// <param name="sb">StringBuilder to read text into.</param>
		public void ReadInto(StringBuilder sb)
		{
			int chances = 3;
			string line;
			do
			{
				try
				{
					line = streamReader.ReadLine();
					if (line != null)
						sb.AppendLine(line);
				}
				catch
				{
					if (--chances > 0)
					{
						ReopenLatestLogFile();
						line = "";
					}
					else
						throw;
				}
			}
			while (line != null);

			string filePathNow = Globals.ErrorFilePath;
			if (lastFilePath != filePathNow)
			{
				ReopenLatestLogFile();
				ReadInto(sb);
			}
		}

		private void ReopenLatestLogFile()
		{
			string filePathNow = Globals.ErrorFilePath;
			streamReader.Close();
			FileStream fs = new FileStream(filePathNow, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			streamReader = new StreamReader(fs, ByteUtil.Utf8NoBOM);
			lastFilePath = filePathNow;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					streamReader.Close();
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				disposedValue = true;
			}
		}

		// //  override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~StreamingLogReader2()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
