using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPUtil;

namespace UnitTests
{
	[TestClass]
	public class TestEventExtensions
	{
		[TestMethod]
		public void SafeInvoke_NullHandler_DoesNotThrow()
		{
			EventHandler<EventArgs> handler = null;
			handler.SafeInvoke(this, EventArgs.Empty);
		}

		[TestMethod]
		public void SafeInvoke_SingleSubscriber_IsInvoked()
		{
			bool invoked = false;
			EventHandler<EventArgs> handler = (s, e) => { invoked = true; };
			handler.SafeInvoke(this, EventArgs.Empty);
			Assert.IsTrue(invoked);
		}

		[TestMethod]
		public void SafeInvoke_MultipleSubscribers_AllInvoked()
		{
			int count = 0;
			EventHandler<EventArgs> handler = null;
			handler += (s, e) => { count++; };
			handler += (s, e) => { count++; };
			handler += (s, e) => { count++; };
			handler.SafeInvoke(this, EventArgs.Empty);
			Assert.AreEqual(3, count);
		}

		[TestMethod]
		public void SafeInvoke_SubscriberThrows_OtherSubscribersStillInvoked()
		{
			List<int> results = new List<int>();
			EventHandler<EventArgs> handler = null;
			handler += (s, e) => { results.Add(1); };
			handler += (s, e) => { throw new InvalidOperationException("fail"); };
			handler += (s, e) => { results.Add(3); };
			handler.SafeInvoke(this, EventArgs.Empty);
			CollectionAssert.AreEqual(new[] { 1, 3 }, results);
		}

		[TestMethod]
		public void SafeInvoke_SubscriberThrows_OnErrorReceivesInnerException()
		{
			List<Exception> errors = new List<Exception>();
			EventHandler<EventArgs> handler = (s, e) => { throw new ArgumentException("test error"); };
			handler.SafeInvoke(this, EventArgs.Empty, ex => errors.Add(ex));
			Assert.AreEqual(1, errors.Count);
			Assert.IsInstanceOfType(errors[0], typeof(ArgumentException));
			Assert.AreEqual("test error", errors[0].Message);
		}

		[TestMethod]
		public void SafeInvoke_SubscriberThrows_NullOnError_ExceptionSilentlyIgnored()
		{
			EventHandler<EventArgs> handler = (s, e) => { throw new Exception("ignored"); };
			handler.SafeInvoke(this, EventArgs.Empty, null);
		}

		[TestMethod]
		public void SafeInvoke_PassesSenderAndArgsCorrectly()
		{
			object receivedSender = null;
			EventArgs receivedArgs = null;
			EventHandler<EventArgs> handler = (s, e) => { receivedSender = s; receivedArgs = e; };
			var args = new EventArgs();
			handler.SafeInvoke("mySender", args);
			Assert.AreEqual("mySender", receivedSender);
			Assert.AreSame(args, receivedArgs);
		}
	}
}

namespace UnitTests.AppUtilitiesTests
{
}
