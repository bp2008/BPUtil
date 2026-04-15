using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	/// <summary>
	/// Provides additional assertion methods for unit tests.
	/// </summary>
	public static class Expect
	{
		/// <summary>
		/// Runs the specified action within a try{} block, and then runs <see cref="Assert.Fail(string)"/> if the action does not throw an exception.  Returns the thrown exception only if the action throws an exception as expected.
		/// </summary>
		/// <param name="action">Action which is expected to throw an exception.</param>
		/// <param name="failMessage">Exception message</param>
		/// <returns>The thrown exception if the action throws an exception as expected.</returns>
		public static Exception Exception(Action action, string failMessage = "Expected Exception")
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				return ex;
			}
			Assert.Fail(failMessage);
			throw new Exception("This should never be reached.");
		}
		/// <summary>
		/// Runs the specified action within a try{} block, and then runs <see cref="Assert.Fail(string)"/> if the action does not throw the expected exception.  Returns the thrown exception only if the action throws an exception of the expected type.
		/// </summary>
		/// <param name="action">Action which is expected to throw an exception.</param>
		/// <param name="failMessage">Exception message</param>
		/// <returns>The thrown exception if the action throws an exception as expected.</returns>
		public static T Exception<T>(Action action, string failMessage = "Expected Exception")
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				if (ex is T tex)
					return tex;
			}
			Assert.Fail(failMessage);
			throw new Exception("This should never be reached.");
		}
		/// <summary>
		/// Throws AssertFailedException if the given collections are not equal.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="expected"></param>
		/// <param name="actual"></param>
		public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual)
		{
			if (expected == null && actual != null)
				Assert.Fail("Expected null. Got " + actual.GetType().ToString() + ".");
			if (expected != null && actual == null)
				Assert.Fail("Expected " + expected.GetType().ToString() + ". Got null.");
			if (expected.Count() != actual.Count())
				Assert.Fail("Expected " + expected.Count() + " items in collection. Got " + actual.Count() + " items.");
			int max = expected.Count();
			for (int i = 0; i < max; i++)
			{
				object e = expected.ElementAt(i);
				object a = actual.ElementAt(i);
				if (e == null && a != null)
					Assert.Fail("Expected null item at index " + i + ". Got \"" + a.ToString() + "\".");
				if (e != null && a == null)
					Assert.Fail("Expected \"" + e.ToString() + "\" at index " + i + ". Got null.");
				if (!e.Equals(a))
					Assert.Fail("Expected index " + i + " to be \"" + expected.ElementAt(i) + "\" but actual item was not equal: \"" + actual.ElementAt(i) + "\"");
			}
		}
		/// <summary>
		/// Throws AssertFailedException if the given collections are equal. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="expected"></param>
		/// <param name="actual"></param>
		public static void NotEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
		{
			bool failed = false;
			try
			{
				Equal(expected, actual);
			}
			catch { failed = true; }
			if (!failed)
				Assert.Fail("Arrays were equal, but expected to not be.");
		}
	}
}
