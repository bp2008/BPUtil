using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{

	/// <summary>
	/// A class which makes it easy to time different parts of a procedure individually and later report the time in seconds taken for each part.
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
		private Stopwatch watch;
		private string currentEvent;
		private List<TimedEvent> events;
		private string numberFormatString;
		public BasicEventTimer(string numberFormatString = "0.00")
		{
			this.numberFormatString = numberFormatString;
			Reset();
		}
		/// <summary>
		/// Stops the timer and clears the list of timed events, restoring this instance to its original state.
		/// </summary>
		public void Reset()
		{
			if (watch != null)
				watch.Stop();
			watch = new Stopwatch();
			currentEvent = null;
			events = new List<TimedEvent>();
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
			currentEvent = eventName;
			watch.Start();
		}
		/// <summary>
		/// Stops and logs the time for the previously started event, if there was one.
		/// </summary>
		public void Stop()
		{
			if (currentEvent == null)
				return;
			watch.Stop();
			events.Add(new TimedEvent(currentEvent, watch.Elapsed, numberFormatString));
			watch.Reset();
			currentEvent = null;
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
			return string.Join(separator, events) + (currentEvent == null ? "" : separator + new TimedEvent(currentEvent + " (ongoing)", watch.Elapsed, numberFormatString).ToString());
		}
		private class TimedEvent
		{
			public string name;
			public TimeSpan time;
			public string numberFormatString;
			public TimedEvent(string name, TimeSpan time, string numberFormatString)
			{
				this.name = name;
				this.time = time;
				this.numberFormatString = numberFormatString;
			}
			public override string ToString()
			{
				return time.TotalSeconds.ToString(numberFormatString) + " - " + name;
			}
		}
	}
}
