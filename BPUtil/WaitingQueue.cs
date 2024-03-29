﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A thread-safe queue which provides a mechanism to block the current thread until an item is available from the queue.
	/// </summary>
	public class WaitingQueue<T>
	{
		private ConcurrentQueue<T> innerQueue = new ConcurrentQueue<T>();
		private SemaphoreSlim sem = new SemaphoreSlim(0, int.MaxValue);
		private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		private CancellationToken cancellationToken;
		public WaitingQueue()
		{
			cancellationToken = cancellationTokenSource.Token;
		}

		/// <summary>
		/// Cancels all <see cref="TryDequeue"/> requests and prevents further blocking if <see cref="TryDequeue"/> is called again.
		/// </summary>
		public void CancelAllRequests()
		{
			if (!cancellationToken.IsCancellationRequested)
				cancellationTokenSource.Cancel();
		}

		/// <summary>
		/// Adds an object to the end of the <see cref="WaitingQueue{T}"/>.
		/// </summary>
		/// <param name="item">The object to add to the end of the <see cref="WaitingQueue{T}"/>. The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
		public void Enqueue(T item)
		{
			innerQueue.Enqueue(item);
			sem.Release();
		}


		/// <summary>
		/// Tries to remove and return the object at the beginning of the <see cref="WaitingQueue{T}"/>. This function will block until an object is obtained or the timeout expires.
		/// </summary>
		/// <param name="result">When this method returns, if the operation was successful, result contains the object removed. If no object was available to be removed, the value is unspecified.</param>
		/// <param name="millisecondsTimeout">Maximum number of milliseconds to wait for an item to become available. If -1, the wait may be indefinite.</param>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0034:Simplify 'default' expression", Justification = "Old syntax for VS2017 support")]
		public bool TryDequeue(out T result, int millisecondsTimeout)
		{
			if (sem.Wait(millisecondsTimeout, cancellationToken))
				return innerQueue.TryDequeue(out result);
			else
				result = default(T);
			return false;
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="WaitingQueue{T}"/>.
		/// </summary>
		public int Count
		{
			get
			{
				return innerQueue.Count;
			}
		}
	}
}
