using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	public static class Expect
	{
		/// <summary>
		/// Runs the specified action within a try{} block, and then runs <see cref="Assert.Fail(string)"/> if the action does not throw an exception.
		/// </summary>
		/// <param name="action">Action which is expected to throw an exception.</param>
		/// <param name="failMessage">Exception message</param>
		public static void Exception(Action action, string failMessage = "Expected Exception")
		{
			bool threw = false;
			try
			{
				action();
			}
			catch (Exception)
			{
				threw = true;
			}
			if (!threw)
				Assert.Fail(failMessage);
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
