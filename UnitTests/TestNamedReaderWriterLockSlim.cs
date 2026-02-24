using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BPUtil.NamedReaderWriterLockSlim<string>;

namespace UnitTests
{
	[TestClass]
	public class TestNamedReaderWriterLockSlim
	{
		private const bool allowSlowTests = false;
		/// <summary>
		/// Gets the value of <see cref="AllowSlowTests"/> in a way that does not trigger a compiler warning when it causes unreachable code.
		/// </summary>
		private bool AllowSlowTests => allowSlowTests.ToString() == "True";
		private NamedReaderWriterLockSlim<string> _locker;
		private Dictionary<string, RefCounter> _lockDictionary;

		[TestInitialize]
		public void TestInitialize()
		{
			_locker = new NamedReaderWriterLockSlim<string>();
			_lockDictionary = (Dictionary<string, RefCounter>)typeof(NamedReaderWriterLockSlim<string>)
				.GetField("_locks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.GetValue(_locker);
		}
		private int? GetRefCount(string key)
		{
			if (_lockDictionary.TryGetValue(key, out RefCounter refCounter))
			{
				return PrivateAccessor.GetFieldValue<int>(refCounter, "_holders");
			}
			return null;
		}

		[TestMethod]
		public void LockRead_Counts_References()
		{
			string name = Guid.NewGuid().ToString();
			Assert.IsNull(GetRefCount(name), "Initially, there should be no lock for the name");
			using (_locker.LockRead(name))
			{
				Assert.AreEqual(1, GetRefCount(name), "expected ref count == 1");
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockRead(name, 0))
					{
						Assert.AreEqual(2, GetRefCount(name), "expected ref count == 2");
					}
				});
				Assert.AreEqual(1, GetRefCount(name), "expected ref count == 1");
			}
			Assert.IsNull(GetRefCount(name), "Finally, there should be no lock for the name");
		}

		[TestMethod]
		public void LockRead_ValidName_ReturnsDisposableToken()
		{
			string name = Guid.NewGuid().ToString();
			using (IDisposable token = _locker.LockRead(name))
			{
				Assert.IsNotNull(token);
			}
		}

		[TestMethod]
		public void LockWrite_ValidName_ReturnsDisposableToken()
		{
			string name = Guid.NewGuid().ToString();
			using (IDisposable token = _locker.LockWrite(name))
			{
				Assert.IsNotNull(token);
			}
		}

		[TestMethod]
		public void LockUpgradeableRead_ValidName_ReturnsDisposableToken()
		{
			string name = Guid.NewGuid().ToString();
			using (IDisposable token = _locker.LockUpgradeableRead(name))
			{
				Assert.IsNotNull(token);
			}
		}

		[TestMethod]
		public void LockRead_AllowsMultipleReaders()
		{
			if (!AllowSlowTests)
				return;
			string name = Guid.NewGuid().ToString();
			List<Task> tasks = new List<Task>();
			for (int i = 0; i < 5; i++)
			{
				tasks.Add(Task.Run(() =>
				{
					using (_locker.LockRead(name))
					{
						Thread.Sleep(100); // simulate work
					}
				}));
			}
			Task.WaitAll(tasks.ToArray());
		}

		[TestMethod]
		public void LockThreadingTestSlow()
		{
			if (!AllowSlowTests)
				return;
			string name = Guid.NewGuid().ToString();
			const int threadCount = 50;
			const int lockSleepTime = 100;
			ManualResetEvent[] manualResetEvents = new ManualResetEvent[threadCount];
			for (int i = 0; i < threadCount; i++)
			{
				ManualResetEvent mre = new ManualResetEvent(false);
				manualResetEvents[i] = mre;
				int j = i;
				Task.Run(() =>
				{
					if (j % 3 == 0)
						using (_locker.LockRead(name))
						{
							Thread.Sleep(lockSleepTime);
						}
					else if (j % 3 == 1)
						using (_locker.LockUpgradeableRead(name, 10000))
						{
							Thread.Sleep(lockSleepTime);
						}
					else
						using (_locker.LockWrite(name))
						{
							Thread.Sleep(lockSleepTime);
						}
					//at this point, the lock should be released
					mre.Set();
				});
			}
			for (int i = 0; i < threadCount; i++)
			{
				if (!manualResetEvents[i].WaitOne(threadCount * lockSleepTime))
					Assert.Fail("Failed waiting for the manualResetEvent to be set - likely error in the test");
			}
			//check all locks have been released
			Assert.AreEqual(0, _lockDictionary.Count, "Expect the lock to be removed from the dictionary when no more threads hold the lock");
		}

		[TestMethod]
		public void DifferentNames_IndependentLocks()
		{
			string name1 = Guid.NewGuid().ToString();
			string name2 = Guid.NewGuid().ToString();
			using (_locker.LockWrite(name1))
			{
				// Should not block name2
				Task task = Task.Run(() =>
				{
					using (_locker.LockWrite(name2))
					{
						// success
					}
				});
				task.Wait(1000); // should complete quickly
				Assert.IsTrue(task.IsCompleted);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void LockRead_NullName_ThrowsArgumentNullException()
		{
			_locker.LockRead(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void LockWrite_NullName_ThrowsArgumentNullException()
		{
			_locker.LockWrite(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void LockUpgradeableRead_NullName_ThrowsArgumentNullException()
		{
			_locker.LockUpgradeableRead(null);
		}

		#region Concurrent Lock Holding Tests
		#region Hold Read
		[TestMethod]
		public void HoldRead_LockRead_Succeeds()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockRead(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockRead(name, 0))
					{
					}
				});
			}
		}
		[TestMethod]
		public void HoldRead_LockUpgadeableRead_Succeeds()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockRead(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockUpgradeableRead(name, 0))
					{
					}
				});
			}
		}
		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void HoldRead_LockWrite_Throws()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockRead(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockWrite(name, 0))
					{
					}
				});
			}
		}
		#endregion Hold Read
		#region Hold Upgradeable Read
		[TestMethod]
		public void HoldUpgradeableRead_LockRead_Succeeds()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockUpgradeableRead(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockRead(name, 0))
					{
					}
				});
			}
		}
		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void HoldUpgradeableRead_LockUpgadeableRead_Throws()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockUpgradeableRead(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockUpgradeableRead(name, 0)) // Upgradeable reads cannot be held concurrently.
					{
					}
				});
			}
		}
		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void HoldUpgradeableRead_LockWrite_Throws()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockUpgradeableRead(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockWrite(name, 0))
					{
					}
				});
			}
		}
		#endregion Hold Upgradeable Read
		#region Hold Write

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void HoldWrite_LockRead_Throws()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockWrite(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockRead(name, 0))
					{
					}
				});
			}
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void HoldWrite_LockUpgradeableRead_Throws()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockWrite(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockUpgradeableRead(name, 0))
					{
					}
				});
			}
		}
		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void HoldWrite_LockWrite_Throws()
		{
			string name = Guid.NewGuid().ToString();
			using (_locker.LockWrite(name))
			{
				RunOnBackgroundThreadAndWait(() =>
				{
					using (_locker.LockWrite(name, 0))
					{
					}
				});
			}
		}
		#endregion Hold Write
		#endregion Concurrent Lock Holding Tests
		private static void RunOnBackgroundThreadAndWait(Action action)
		{
			Task task = Task.Run(action);
			task.Wait();
		}
	}
}
