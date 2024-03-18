using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A class which makes it easy to time different parts of a procedure individually and later report the time in seconds taken for each part.  This class is NOT thread-safe.
	/// 
	/// Example:
	/// 
	/// BasicEventTimer timer = new BasicEventTimer();
	/// 
	/// timer.Start("Event 1");
	/// timed_procedure_1();
	///
	/// timer.Start("Event 2");
	/// timed_procedure_2();
	/// timer.Stop();
	/// 
	/// untimed_procedure();
	/// 
	/// timer.Start("Event 3");
	/// timed_procedure_3();
	/// timer.Stop();
	/// 
	/// Console.WriteLine(timer.ToString(Environment.NewLine));
	/// 
	/// Example output:
	/// 0.01 - Procedure 1
	/// 50.15 - Procedure 2
	/// 1.51 - Procedure 3
	/// </summary>
	public class BasicEventTimer
	{
		private TimedEvent currentEvent;
		private Dictionary<string, TimedEvent> dict_events;
		private List<TimedEvent> events;
		private string numberFormatString;
		private bool mergeSameEventTimes = false;
		/// <summary>
		/// Constructs a new BasicEventTimer which helps measure execution time of code.
		/// </summary>
		/// <param name="numberFormatString">A format string for the (double) number of seconds an event took.</param>
		/// <param name="mergeSameEventTimes">If true, multiple events with the same name will have their times added together into one record.  Useful when repeating a sequence of operations in a loop.</param>
		public BasicEventTimer(string numberFormatString = "0.00", bool mergeSameEventTimes = false)
		{
			this.numberFormatString = numberFormatString;
			this.mergeSameEventTimes = mergeSameEventTimes;
			Reset();
		}
		/// <summary>
		/// Stops the timer and clears the list of timed events, restoring this instance to its original state.
		/// </summary>
		public void Reset()
		{
			currentEvent = null;
			events = new List<TimedEvent>();
			if (mergeSameEventTimes)
				dict_events = new Dictionary<string, TimedEvent>();
		}
		/// <summary>
		/// Starts timing a new event, automatically stopping and logging the time for the previous event, if there was one.
		/// </summary>
		/// <param name="eventName"></param>
		public void Start(string eventName)
		{
			if (eventName == null)
				eventName = "null";
			Stop();
			if (mergeSameEventTimes)
			{
				if (dict_events.TryGetValue(eventName, out TimedEvent existing))
				{
					currentEvent = existing;
					existing.Resume();
				}
				else
				{
					currentEvent = new TimedEvent(eventName, numberFormatString);
					dict_events[eventName] = currentEvent;
					events.Add(currentEvent);
				}
			}
			else
			{
				currentEvent = new TimedEvent(eventName, numberFormatString);
				events.Add(currentEvent);
			}
		}
		/// <summary>
		/// Stops and logs the time for the previously started event, if there was one.
		/// </summary>
		public void Stop()
		{
			if (currentEvent != null)
			{
				currentEvent.Stop();
				currentEvent = null;
			}
		}
		/// <summary>
		/// Returns the time (in seconds) elapsed for the named event. If the event is not found, returns 0.
		/// </summary>
		public double Duration(string name)
		{
			return DurationTimeSpan(name).TotalSeconds;
		}
		/// <summary>
		/// Returns the time elapsed for the named event. If the event is not found, returns TimeSpan.Zero.
		/// </summary>
		public TimeSpan DurationTimeSpan(string name)
		{
			foreach (TimedEvent ev in events)
			{
				if (ev.name == name)
					return ev.time;
			}

			return TimeSpan.Zero;
		}
		/// <summary>
		/// Returns an string containing the time in seconds measured for each event.  Events are separated by HTML "br" tags.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ToString("<br/>");
		}
		/// <summary>
		/// Returns a string containing the time in seconds measured for each event.  Events are separated by the specified string.
		/// </summary>
		/// <returns></returns>
		public string ToString(string separator)
		{
			return string.Join(separator, events);
		}
		/// <summary>
		/// Produces a string that can be set in HTTP response header "Server-Timing" to allow browser developer tools to show these results among other request timing info.
		/// </summary>
		/// <returns></returns>
		public string ToServerTimingHeader()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < events.Count; i++)
			{
				bool isFirst = i == 0;
				if (!isFirst)
					sb.Append(",");
				double durationMs = Math.Round(events[i].time.TotalMilliseconds * 10) / 10.0; // Duration in milliseconds with precision of 0.1 ms
				sb.Append("e" + i + ";desc=\"" + events[i].name.Replace('"', '\'') + "\";dur=" + durationMs);
			}
			return sb.ToString();
		}
		/// <summary>
		/// Gets the number of events currently saved in the instance.
		/// </summary>
		public int EventCount { get { return events.Count; } }
		/// <summary>
		/// Gets the amount of time that passed for the event at the given index. Throws an exception if the given event index is not valid. See <see cref="EventCount"/>.
		/// </summary>
		/// <param name="eventIndex">0-based index of the event to get the elapsed time for.</param>
		/// <returns></returns>
		public TimeSpan GetEventTime(int eventIndex)
		{
			return events[eventIndex].time;
		}
		/// <summary>
		/// Gets the amount of time that passed for the event with the given name. Returns null if the given event name is not found.
		/// </summary>
		/// <param name="eventName">Name of the event to get the elapsed time for.</param>
		/// <returns></returns>
		public TimeSpan? GetEventTime(string eventName)
		{
			return events.FirstOrDefault(e => e.name == eventName)?.time;
		}
		private class TimedEvent
		{
			public readonly string name;
			/// <summary>
			/// The time measured for this TimedEvent.
			/// </summary>
			public TimeSpan time => stopwatch.Elapsed;
			public readonly string numberFormatString;
			private Stopwatch stopwatch;
			public TimedEvent(string name, string numberFormatString)
			{
				this.name = name;
				this.numberFormatString = numberFormatString;
				stopwatch = Stopwatch.StartNew();
			}
			/// <summary>
			/// Stops the Stopwatch such that this TimedEvent's [time] property stops increasing.
			/// </summary>
			public void Stop()
			{
				stopwatch.Stop();
			}
			/// <summary>
			/// Resumes the Stopwatch such that this TimedEvent's [time] property begins increasing.
			/// </summary>
			/// <exception cref="NotImplementedException"></exception>
			public void Resume()
			{
				stopwatch.Start();
			}
			public override string ToString()
			{
				return time.TotalSeconds.ToString(numberFormatString) + " - " + name + (stopwatch.IsRunning ? " (ongoing)" : "");
			}
		}
	}
}
