using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// Contains static methods to execute code with simple predefined exception handlers.
	/// </summary>
	public static class Try
	{
		/// <summary>
		/// Runs the specified Action inside a try block and swallows all exceptions.
		/// </summary>
		/// <param name="actionToTry"></param>
		public static void Swallow(Action actionToTry)
		{
			try
			{
				if (actionToTry != null)
					actionToTry();
			}
			catch { }
		}

		/// <summary>
		/// Runs the specified Func inside a try block and swallows all exceptions.  Returns default(T) in the event of an exception, otherwise whatever the Func returned.
		/// </summary>
		/// <param name="funcToTry"></param>
		public static T Swallow<T>(Func<T> funcToTry)
		{
			try
			{
				if (funcToTry != null)
					return funcToTry();
			}
			catch { }
			return default(T);
		}

		/// <summary>
		/// Runs the specified Func (returning bool) inside a try block and swallows all exceptions.  Returns false in the event of an exception, otherwise whatever the Func returned.
		/// </summary>
		/// <param name="funcBoolToTry"></param>
		public static bool SwallowBool(Func<bool> funcBoolToTry)
		{
			try
			{
				if (funcBoolToTry != null)
					return funcBoolToTry();
			}
			catch { }
			return false;
		}

		/// <summary>
		/// Runs the specified Action inside a try block and logs all exceptions.
		/// </summary>
		/// <param name="actionToTry"></param>
		public static void Catch(Action actionToTry)
		{
			try
			{
				if (actionToTry != null)
					actionToTry();
			}
			catch (Exception ex) { Logger.Debug(ex); }
		}
		/// <summary>
		/// Runs the specified Func inside a try block and logs all exceptions.  Returns default(T) in the event of an exception, otherwise whatever the Func returned.
		/// </summary>
		/// <param name="funcToTry"></param>
		public static T Catch<T>(Func<T> funcToTry)
		{
			try
			{
				if (funcToTry != null)
					return funcToTry();
			}
			catch (Exception ex) { Logger.Debug(ex); }
			return default(T);
		}

		/// <summary>
		/// Runs the specified Action inside a try block and logs all exceptions except ThreadAbortException, which is rethrown.
		/// </summary>
		/// <param name="actionToTry"></param>
		public static void Catch_RethrowThreadAbort(Action actionToTry)
		{
			try
			{
				if (actionToTry != null)
					actionToTry();
			}
			catch (ThreadAbortException) { throw; }
			catch (Exception ex) { Logger.Debug(ex); }
		}

		/// <summary>
		/// Runs the specified Func (returning bool) inside a try block and logs all exceptions.  Returns false in the event of an exception, otherwise whatever the Func returned.
		/// </summary>
		/// <param name="funcBoolToTry"></param>
		public static bool CatchBool(Func<bool> funcBoolToTry)
		{
			try
			{
				if (funcBoolToTry != null)
					return funcBoolToTry();
			}
			catch (Exception ex) { Logger.Debug(ex); }
			return false;
		}
	}
}
