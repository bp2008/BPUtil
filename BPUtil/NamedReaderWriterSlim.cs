// Copyright (c) 2009, Nick Curry
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the <organization> nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY <copyright holder> ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

//
// Originally from: https://github.com/unintelligible/NamedReaderWriterLockSlim/blob/master/NamedReaderWriterLock/NamedReaderWriterLockSlim.cs, copied in 2018.
//
// Rewritten by [Brian Pearce] on 2026-02-24 to work around race conditions and add XML documentation.
//

using System;
using System.Collections.Generic;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// <para>Provides named reader/writer locks keyed by values of type <typeparamref name="T"/>.</para>
	/// <para>Each distinct name maps to an independent <see cref="ReaderWriterLockSlim"/> instance.</para>
	/// <para>Callers receive an <see cref="IDisposable"/> token that releases the acquired lock when disposed.</para>
	/// <para>Usage Examples:</para>
	/// <code>
	/// private static NamedReaderWriterLockSlim&lt;string> locker = new NamedReaderWriterLockSlim&lt;string>();
	/// public void DoSomethingConcurrent(string lockName)
	/// {
	///		using (locker.LockRead(url))
	///  	{
	///  	   // Do something that requires read access to the resource
	///  	}
	///  	using (locker.LockUpgradeableRead(url))
	///  	{
	///  	   // Do something that requires read access to the resource but that may require
	///  	   //   upgrading to a Write lock later before releasing the read lock.
	///  	}
	///  	using (locker.LockWrite(url))
	///  	{
	///  	   // Do something that requires write access to the resource
	///  	}
	/// }
	/// </code>
	/// </summary>
	/// <typeparam name="T">The type used as the key for the internal Dictionary of locks. Values of this type must not be null. Typically, the type is <see cref="string"/>.</typeparam>
	public class NamedReaderWriterLockSlim<T>
	{
		/// <summary>
		/// Dictionary holding locks keyed by name.
		/// </summary>
		private readonly Dictionary<T, RefCounter> _locks = new Dictionary<T, RefCounter>();
		/// <summary>
		/// The default timeout in milliseconds to use if not specified when obtaining a lock.
		/// </summary>
		public const int DEFAULT_TIMEOUT_MILLISECONDS = 5000;
		/// <summary>
		/// Throws ArgumentNullException if <paramref name="name"/> is null.
		/// </summary>
		/// <param name="name">The name to validate.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
		private static void ValidateName(T name)
		{
			if ((object)name == null)
				throw new ArgumentNullException(nameof(name));
		}
		/// <summary>
		/// <para>Acquires a read lock for the specified name, blocking for a default timeout.</para>
		/// <para>The returned token releases the lock on disposal.</para>
		/// <para>If the timeout expires before the lock is obtained, throws <see cref="TimeoutException"/>.</para>
		/// </summary>
		/// <param name="name">The name of the lock to acquire. Cannot be null.</param>
		/// <returns>An <see cref="IDisposable"/> token that releases the read lock when disposed.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
		/// <exception cref="System.TimeoutException">Thrown if the lock could not be acquired within the configured timeout.</exception>
		public IDisposable LockRead(T name)
		{
			return LockRead(name, DEFAULT_TIMEOUT_MILLISECONDS);
		}
		/// <summary>
		/// <para>Acquires a read lock for the specified name, blocking up to the specified timeout.</para>
		/// <para>The returned token releases the lock on disposal.</para>
		/// <para>If the timeout expires before the lock is obtained, throws <see cref="TimeoutException"/>.</para>
		/// </summary>
		/// <param name="name">The name of the lock to acquire. Cannot be null.</param>
		/// <param name="timeoutMilliseconds">Maximum time, in milliseconds, to wait for the lock.</param>
		/// <returns>An <see cref="IDisposable"/> token that releases the read lock when disposed.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
		/// <exception cref="System.TimeoutException">Thrown if the lock could not be acquired within <paramref name="timeoutMilliseconds"/>.</exception>
		public IDisposable LockRead(T name, int timeoutMilliseconds)
		{
			ValidateName(name);
			return WithLock(name,
				refCounter =>
				{
					if (!refCounter.RWLock.TryEnterReadLock(timeoutMilliseconds))
						throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting to acquire read lock on '{name}' - possible deadlock");
				},
				refCounter =>
				{
					refCounter.RWLock.ExitReadLock();
				});
		}
		/// <summary>
		/// <para>Acquires a write lock for the specified name, blocking for a default timeout.</para>
		/// <para>The returned token releases the lock on disposal.</para>
		/// <para>If the timeout expires before the lock is obtained, throws <see cref="TimeoutException"/>.</para>
		/// </summary>
		/// <param name="name">The name of the lock to acquire. Cannot be null.</param>
		/// <returns>An <see cref="IDisposable"/> token that releases the write lock when disposed.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
		/// <exception cref="System.TimeoutException">Thrown if the lock could not be acquired within the configured timeout.</exception>
		public IDisposable LockWrite(T name)
		{
			return LockWrite(name, DEFAULT_TIMEOUT_MILLISECONDS);
		}
		/// <summary>
		/// <para>Acquires a write lock for the specified name, blocking up to the specified timeout.</para>
		/// <para>The returned token releases the lock on disposal.</para>
		/// <para>If the timeout expires before the lock is obtained, throws <see cref="TimeoutException"/>.</para>
		/// </summary>
		/// <param name="name">The name of the lock to acquire. Cannot be null.</param>
		/// <param name="timeoutMilliseconds">Maximum time, in milliseconds, to wait for the lock.</param>
		/// <returns>An <see cref="IDisposable"/> token that releases the write lock when disposed.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
		/// <exception cref="System.TimeoutException">Thrown if the lock could not be acquired within <paramref name="timeoutMilliseconds"/>.</exception>
		public IDisposable LockWrite(T name, int timeoutMilliseconds)
		{
			ValidateName(name);
			return WithLock(name,
				refCounter =>
				{
					if (!refCounter.RWLock.TryEnterWriteLock(timeoutMilliseconds))
						throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting to acquire write lock on '{name}' - possible deadlock");
				},
				refCounter =>
				{
					refCounter.RWLock.ExitWriteLock();
				});
		}

		/// <summary>
		/// <para>Acquires an upgradeable read lock for the specified name, blocking for a default timeout.</para>
		/// <para>The returned token releases the lock on disposal.</para>
		/// <para>If the timeout expires before the lock is obtained, throws <see cref="TimeoutException"/>.</para>
		/// </summary>
		/// <param name="name">The name of the lock to acquire. Cannot be null.</param>
		/// <returns>An <see cref="IDisposable"/> token that releases the upgradeable read lock when disposed.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
		/// <exception cref="System.TimeoutException">Thrown if the lock could not be acquired within the configured timeout.</exception>
		public IDisposable LockUpgradeableRead(T name)
		{
			return LockUpgradeableRead(name, DEFAULT_TIMEOUT_MILLISECONDS);
		}

		/// <summary>
		/// <para>Acquires an upgradeable read lock for the specified name, blocking up to the specified timeout.</para>
		/// <para>The returned token releases the lock on disposal.</para>
		/// <para>If the timeout expires before the lock is obtained, throws <see cref="TimeoutException"/>.</para>
		/// </summary>
		/// <param name="name">The name of the lock to acquire. Cannot be null.</param>
		/// <param name="timeoutMilliseconds">Maximum time, in milliseconds, to wait for the lock.</param>
		/// <returns>An <see cref="IDisposable"/> token that releases the upgradeable read lock when disposed.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
		/// <exception cref="System.TimeoutException">Thrown if the lock could not be acquired within <paramref name="timeoutMilliseconds"/>.</exception>
		public IDisposable LockUpgradeableRead(T name, int timeoutMilliseconds)
		{
			ValidateName(name);
			return WithLock(name,
				refCounter =>
				{
					if (!refCounter.RWLock.TryEnterUpgradeableReadLock(timeoutMilliseconds))
						throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting to acquire upgradeable read lock on '{name}' - possible deadlock");
				},
				refCounter =>
				{
					refCounter.RWLock.ExitUpgradeableReadLock();
				});
		}

		/// <summary>
		/// Releases the lock and removes the lock from the dictionary if there are no more holders.  Throws an exception if the lock doesn't exist.
		/// </summary>
		/// <param name="name">The name of the lock to release.</param>
		/// <param name="unlockAction">The action to perform to release the lock.  Can be null if and only if the lock was not actually acquired.</param>
		/// <exception cref="ApplicationException">Thrown if the lock does not exist in the dictionary.</exception>
		private void WithUnlock(T name, Action<RefCounter> unlockAction)
		{
			lock (_locks)
			{
				if (!_locks.TryGetValue(name, out RefCounter refCounter))
					throw new ApplicationException("Attempting to release a lock that doesn't exist for name '" + name + "'.");
				if (unlockAction != null)
					unlockAction(refCounter);
				int refs = refCounter.Decrement();
				if (refs == 0)
					_locks.Remove(name);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="lockAction">The action which obtains the lock.  If it can't obtain the lock before the timeout period expires, it must throw an exception.</param>
		/// <param name="unlockAction">The action which releases the lock.</param>
		/// <returns></returns>
		private IDisposable WithLock(T name, Action<RefCounter> lockAction, Action<RefCounter> unlockAction)
		{
			RefCounter refCounter;
			lock (_locks)
			{
				if (!_locks.TryGetValue(name, out refCounter))
				{
					refCounter = new RefCounter();
					_locks.Add(name, refCounter);
				}

				// Prevent removal while this thread is between lookup and acquisition.
				refCounter.Increment();
			}

			try
			{
				lockAction(refCounter);
				return new Token(() => WithUnlock(name, unlockAction));
			}
			catch
			{
				// If an exception was thrown, the caller will not be able to release the lock, so we need to make sure the refCounter gets decremented.
				WithUnlock(name, null);
				throw;
			}
		}
		/// <summary>
		/// <para>Stores a <see cref="ReaderWriterLockSlim"/> and a separate reference count.</para>
		/// <para>This type is public only for use in unit tests.</para>
		/// </summary>
		public class RefCounter
		{
			/// <summary>
			/// The lock instance stored in this RefCounter.
			/// </summary>
			public readonly ReaderWriterLockSlim RWLock = new ReaderWriterLockSlim();
			/// <summary>
			/// The current reference count.
			/// </summary>
			private int _holders;
			/// <summary>
			/// Increments the reference count.
			/// </summary>
			public void Increment()
			{
				Interlocked.Increment(ref _holders);
			}
			/// <summary>
			/// Decrements the reference count and returns the current count after decrementing.
			/// </summary>
			public int Decrement()
			{
				return Interlocked.Decrement(ref _holders);
			}
		}

		/// <summary>
		/// A token that stores an Action to be run when <see cref="Dispose"/> is called.
		/// </summary>
		class Token : IDisposable
		{
			/// <summary>
			/// Action to run when <see cref="Dispose"/> is called.
			/// </summary>
			private readonly Action _runOnDispose;

			/// <summary>
			/// Constructs a token that stores an Action to be run when <see cref="Dispose"/> is called.
			/// </summary>
			/// <param name="runOnDispose">Action to run when <see cref="Dispose"/> is called.</param>
			public Token(Action runOnDispose)
			{
				_runOnDispose = runOnDispose;
			}
			/// <summary>
			/// Diposes the token, running the Action provided at construction.
			/// </summary>
			public void Dispose()
			{
				_runOnDispose();
			}
		}
	}
}