using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	[DebuggerDisplay("Count = {Count}")]
	public class BPQueue<T>
	{
		protected List<T> data = new List<T>();
		protected int offset = 0;

#pragma warning disable IDE0034 // Simplify 'default' expression -- Old syntax kept for VS2017 suport
		/// <summary>
		/// A default value of the generic type (typically null or 0)
		/// </summary>
		protected readonly T DefaultValue = default(T);
#pragma warning restore IDE0034 // Simplify 'default' expression

		/// <summary>
		/// Gets the number of elements contained in the <see cref="BPQueue{T}"/>.
		/// </summary>
		public int Count
		{
			get
			{
				return data.Count - offset;
			}
		}

		/// <summary>
		/// Returns true if the <see cref="BPQueue{T}"/> is empty.
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				return Count == 0;
			}
		}

		/// <summary>
		/// Adds an object to the end of the <see cref="BPQueue{T}"/>.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="BPQueue{T}"/>. The value can be null for reference types.</param>
		public void Enqueue(T item)
		{
			data.Add(item);
		}

		/// <summary>
		/// Tries to retrieve and remove the object at the beginning of the <see cref="BPQueue{T}"/>. Returns true if successful.
		/// </summary>
		/// <param name="result">When this method returns, if the operation was successful, result contains the object removed. If no object was available to be removed, result contains the default value of the type.</param>
		/// <returns></returns>
		public bool TryDequeue(out T result)
		{
			if (IsEmpty)
			{
				result = DefaultValue;
				return false;
			}

			result = data[offset];

			data[offset] = DefaultValue; // Remove the item from the queue so it can be garbage collected.

			offset++;
			if (data.Count > 16 && offset * 2 >= data.Count)
			{
				data = data.GetRange(offset, Count);
				offset = 0;
			}

			return true;
		}

		/// <summary>
		/// Tries to retrieve but not remove the object at the beginning of the <see cref="BPQueue{T}"/>. Returns true if successful.
		/// </summary>
		/// <param name="result">When this method returns, if the operation was successful, result contains the object at the front of the queue. If no object was available, result contains the default value of the type.</param>
		/// <returns></returns>
		public bool TryPeek(out T result)
		{
			if (IsEmpty)
			{
				result = DefaultValue;
				return false;
			}

			result = data[offset];

			return true;
		}

		/// <summary>
		/// Replaces the object at the beginning of the <see cref="BPQueue{T}"/> with the specified object. If the queue is empty, Enqueues the item instead.
		/// </summary>
		/// <param name="newFront">The new item to add to the front of the queue.</param>
		public void ReplaceFront(T newFront)
		{
			if (IsEmpty)
				Enqueue(newFront);
			else
				data[offset] = newFront;
		}

		/// <summary>
		/// Returns an array containing all the items in the queue, in the order they would have been dequeued in.
		/// </summary>
		/// <returns>Returns an array containing all the items in the queue, in the order they would have been dequeued in.</returns>
		public T[] ToArray()
		{
			T[] arr = new T[Count];
			for (int i = offset, n = 0; i < data.Count; i++, n++)
				arr[n] = data[i];
			return arr;
		}

		/// <summary>
		/// Returns the first item that causes the specified predicate to return true.
		/// </summary>
		/// <param name="where">A function to which each item from the queue is passed. The function should return true when the desired item is passed in.</param>
		/// <returns>Returns the first item that causes the specified predicate to return true.</returns>
		public T Find(Func<T, bool> where)
		{
			for (int i = offset; i < data.Count; i++)
				if (where(data[i]))
					return data[i];
			return DefaultValue;
		}
	}
}
