using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>A list that maintains its elements in sorted order, implementing <see cref="IList{T}"/>,
	/// <see cref="IList"/>, <see cref="IReadOnlyList{T}"/>, and related interfaces.</para>
	/// <para>This class is designed as a drop-in replacement for <see cref="List{T}"/> where a sorted
	/// view of elements is always presented to callers during read operations.</para>
	/// <para>Sorting is performed lazily: write operations (Add, Insert, etc.) simply append items to the
	/// internal list and mark it as unsorted. The list is only sorted when a read operation requires it
	/// (e.g., enumeration, indexer access, Contains, IndexOf, etc.). This allows efficient batch additions
	/// without redundant intermediate sorts.</para>
	/// <para>Thread safety: All operations are fully thread-safe. Read operations use a
	/// <see cref="ReaderWriterLockSlim"/> read lock, allowing multiple concurrent readers when the list is
	/// already sorted. If a lazy sort is needed, a write lock is briefly acquired to perform the sort before
	/// the read proceeds. Write operations always acquire an exclusive write lock. If a write occurs while
	/// another thread is enumerating, the enumerator will throw an <see cref="InvalidOperationException"/>
	/// (consistent with standard collection behavior). Callers should not rely on the thread safety for
	/// complex compound operations; external synchronization may still be needed for atomic read-then-write
	/// sequences.</para>
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	public class ListSorted<T> : IList<T>, IList, IReadOnlyList<T>, ICollection<T>, ICollection, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
	{
		private readonly List<T> _items;
		private readonly IComparer<T> _comparer;
		private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
		private volatile bool _isDirty;
		private int _version;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of <see cref="ListSorted{T}"/> using the default comparer for <typeparamref name="T"/>.
		/// </summary>
		public ListSorted()
		{
			_items = new List<T>();
			_comparer = Comparer<T>.Default;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListSorted{T}"/> with the specified initial capacity,
		/// using the default comparer for <typeparamref name="T"/>.
		/// </summary>
		/// <param name="capacity">The initial capacity of the internal list.</param>
		public ListSorted(int capacity)
		{
			_items = new List<T>(capacity);
			_comparer = Comparer<T>.Default;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListSorted{T}"/> using the specified comparer.
		/// </summary>
		/// <param name="comparer">The <see cref="IComparer{T}"/> to use for sorting. If null, <see cref="Comparer{T}.Default"/> is used.</param>
		public ListSorted(IComparer<T> comparer)
		{
			_items = new List<T>();
			_comparer = comparer ?? Comparer<T>.Default;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListSorted{T}"/> using the specified comparison delegate.
		/// </summary>
		/// <param name="comparison">The <see cref="Comparison{T}"/> delegate to use for sorting.</param>
		public ListSorted(Comparison<T> comparison)
		{
			if (comparison == null)
				throw new ArgumentNullException(nameof(comparison));
			_items = new List<T>();
			_comparer = Comparer<T>.Create(comparison);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListSorted{T}"/> with elements copied from the specified collection,
		/// using the default comparer for <typeparamref name="T"/>.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new list.</param>
		public ListSorted(IEnumerable<T> collection)
		{
			_items = new List<T>(collection);
			_comparer = Comparer<T>.Default;
			_isDirty = _items.Count > 1;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListSorted{T}"/> with elements copied from the specified collection,
		/// using the specified comparer.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new list.</param>
		/// <param name="comparer">The <see cref="IComparer{T}"/> to use for sorting. If null, <see cref="Comparer{T}.Default"/> is used.</param>
		public ListSorted(IEnumerable<T> collection, IComparer<T> comparer)
		{
			_items = new List<T>(collection);
			_comparer = comparer ?? Comparer<T>.Default;
			_isDirty = _items.Count > 1;
		}

		#endregion

		#region Internal helpers

		/// <summary>
		/// Sorts the internal list if it has been marked as dirty.
		/// Must be called while holding the write lock.
		/// </summary>
		private void SortIfDirty()
		{
			if (!_isDirty)
				return;
			_items.Sort(_comparer);
			_isDirty = false;
		}

		/// <summary>
		/// Ensures the list is sorted, then enters a read lock.
		/// If the list is dirty, a write lock is briefly acquired to sort it first.
		/// After this method returns, the caller holds a read lock that must be released via <see cref="ExitRead"/>.
		/// </summary>
		private void EnterReadSorted()
		{
			if (_isDirty)
			{
				_rwLock.EnterWriteLock();
				try
				{
					SortIfDirty();
				}
				finally
				{
					_rwLock.ExitWriteLock();
				}
			}
			_rwLock.EnterReadLock();
		}

		/// <summary>
		/// Enters a read lock without ensuring sorted order (for operations that don't need sorting, like Count or Contains).
		/// </summary>
		private void EnterReadUnsorted()
		{
			_rwLock.EnterReadLock();
		}

		private void ExitRead()
		{
			_rwLock.ExitReadLock();
		}

		/// <summary>
		/// Enters the write lock and ensures sorted order (for write operations that need the sorted state, like RemoveAt).
		/// </summary>
		private void EnterWriteSorted()
		{
			_rwLock.EnterWriteLock();
			SortIfDirty();
		}

		private void EnterWrite()
		{
			_rwLock.EnterWriteLock();
		}

		private void ExitWrite()
		{
			_rwLock.ExitWriteLock();
		}

		#endregion

		#region IList<T>

		/// <summary>
		/// Gets or sets the element at the specified index in the sorted list.
		/// Setting a value replaces the element at that index and may cause a re-sort on next read.
		/// </summary>
		public T this[int index]
		{
			get
			{
				EnterReadSorted();
				try
				{
					return _items[index];
				}
				finally
				{
					ExitRead();
				}
			}
			set
			{
				EnterWriteSorted();
				try
				{
					_items[index] = value;
					_isDirty = true;
					_version++;
				}
				finally
				{
					ExitWrite();
				}
			}
		}

		/// <summary>
		/// Returns the index of the specified item in the sorted list, or -1 if not found.
		/// </summary>
		public int IndexOf(T item)
		{
			EnterReadSorted();
			try
			{
				return _items.IndexOf(item);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Inserts the item into the list. Because the list is always presented in sorted order,
		/// the specified index is ignored and the item is simply added to the list (it will be
		/// placed in the correct sorted position on the next read operation).
		/// </summary>
		/// <param name="index">Ignored. The item will be placed at the correct sorted position.</param>
		/// <param name="item">The item to add.</param>
		public void Insert(int index, T item)
		{
			EnterWrite();
			try
			{
				_items.Add(item);
				_isDirty = true;
				_version++;
			}
			finally
			{
				ExitWrite();
			}
		}

		/// <summary>
		/// Removes the element at the specified index in the sorted list.
		/// </summary>
		public void RemoveAt(int index)
		{
			EnterWriteSorted();
			try
			{
				_items.RemoveAt(index);
				_version++;
			}
			finally
			{
				ExitWrite();
			}
		}

		#endregion

		#region ICollection<T>

		/// <summary>
		/// Gets the number of elements in the list.
		/// </summary>
		public int Count
		{
			get
			{
				EnterReadUnsorted();
				try
				{
					return _items.Count;
				}
				finally
				{
					ExitRead();
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether the list is read-only. Always returns false.
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		/// Adds an item to the list. The item will be placed in the correct sorted position on the next read.
		/// </summary>
		public void Add(T item)
		{
			EnterWrite();
			try
			{
				_items.Add(item);
				_isDirty = true;
				_version++;
			}
			finally
			{
				ExitWrite();
			}
		}

		/// <summary>
		/// Removes all elements from the list.
		/// </summary>
		public void Clear()
		{
			EnterWrite();
			try
			{
				_items.Clear();
				_isDirty = false;
				_version++;
			}
			finally
			{
				ExitWrite();
			}
		}

		/// <summary>
		/// Determines whether the list contains the specified item.
		/// </summary>
		public bool Contains(T item)
		{
			EnterReadUnsorted();
			try
			{
				return _items.Contains(item);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Copies the sorted elements of the list to an array, starting at the specified array index.
		/// </summary>
		public void CopyTo(T[] array, int arrayIndex)
		{
			EnterReadSorted();
			try
			{
				_items.CopyTo(array, arrayIndex);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Removes the first occurrence of the specified item from the list.
		/// </summary>
		/// <returns>true if the item was found and removed; otherwise, false.</returns>
		public bool Remove(T item)
		{
			EnterWriteSorted();
			try
			{
				bool removed = _items.Remove(item);
				if (removed)
					_version++;
				return removed;
			}
			finally
			{
				ExitWrite();
			}
		}

		#endregion

		#region IEnumerable<T> / IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the sorted list.
		/// If the list is modified during enumeration, the enumerator will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			// Ensure sorted before starting enumeration.
			if (_isDirty)
			{
				EnterWrite();
				try
				{
					SortIfDirty();
				}
				finally
				{
					ExitWrite();
				}
			}

			// Capture version for concurrent modification detection.
			int ver = _version;
			for (int i = 0; i < _items.Count; i++)
			{
				if (_version != ver)
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
				yield return _items[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Additional List<T>-like methods

		/// <summary>
		/// Adds the elements of the specified collection to the list.
		/// </summary>
		public void AddRange(IEnumerable<T> collection)
		{
			EnterWrite();
			try
			{
				_items.AddRange(collection);
				_isDirty = true;
				_version++;
			}
			finally
			{
				ExitWrite();
			}
		}

		/// <summary>
		/// Searches the sorted list for an element using a binary search algorithm and the list's comparer.
		/// The list is sorted before the search if necessary.
		/// </summary>
		/// <param name="item">The item to search for.</param>
		/// <returns>The zero-based index of the item if found; otherwise, a negative number that is the
		/// bitwise complement of the index of the next element that is larger.</returns>
		public int BinarySearch(T item)
		{
			EnterReadSorted();
			try
			{
				return _items.BinarySearch(item, _comparer);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Determines whether the list contains elements that match the conditions defined by the specified predicate.
		/// </summary>
		public bool Exists(Predicate<T> match)
		{
			EnterReadUnsorted();
			try
			{
				return _items.Exists(match);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Searches for an element that matches the conditions defined by the specified predicate,
		/// and returns the first occurrence in the sorted list.
		/// </summary>
		public T Find(Predicate<T> match)
		{
			EnterReadSorted();
			try
			{
				return _items.Find(match);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Retrieves all elements that match the conditions defined by the specified predicate.
		/// </summary>
		public List<T> FindAll(Predicate<T> match)
		{
			EnterReadSorted();
			try
			{
				return _items.FindAll(match);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Performs the specified action on each element of the sorted list.
		/// </summary>
		public void ForEach(Action<T> action)
		{
			EnterReadSorted();
			try
			{
				_items.ForEach(action);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Creates a shallow copy of a range of elements in the sorted list.
		/// </summary>
		public List<T> GetRange(int index, int count)
		{
			EnterReadSorted();
			try
			{
				return _items.GetRange(index, count);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Removes all elements that match the conditions defined by the specified predicate.
		/// </summary>
		/// <returns>The number of elements removed.</returns>
		public int RemoveAll(Predicate<T> match)
		{
			EnterWrite();
			try
			{
				int removed = _items.RemoveAll(match);
				if (removed > 0)
					_version++;
				return removed;
			}
			finally
			{
				ExitWrite();
			}
		}

		/// <summary>
		/// Reverses the order of the elements in the list. Note that subsequent additions will cause the list
		/// to be re-sorted in the comparer's order.
		/// </summary>
		public void Reverse()
		{
			EnterWriteSorted();
			try
			{
				_items.Reverse();
				_version++;
			}
			finally
			{
				ExitWrite();
			}
		}

		/// <summary>
		/// Copies the sorted elements of the list to a new array.
		/// </summary>
		public T[] ToArray()
		{
			EnterReadSorted();
			try
			{
				return _items.ToArray();
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Sets the capacity to the actual number of elements in the list,
		/// if that number is less than a threshold value.
		/// </summary>
		public void TrimExcess()
		{
			EnterWrite();
			try
			{
				_items.TrimExcess();
			}
			finally
			{
				ExitWrite();
			}
		}

		/// <summary>
		/// Determines whether every element in the list matches the conditions defined by the specified predicate.
		/// </summary>
		public bool TrueForAll(Predicate<T> match)
		{
			EnterReadUnsorted();
			try
			{
				return _items.TrueForAll(match);
			}
			finally
			{
				ExitRead();
			}
		}

		/// <summary>
		/// Gets or sets the total number of elements the internal data structure can hold without resizing.
		/// </summary>
		public int Capacity
		{
			get
			{
				EnterReadUnsorted();
				try
				{
					return _items.Capacity;
				}
				finally
				{
					ExitRead();
				}
			}
			set
			{
				EnterWrite();
				try
				{
					_items.Capacity = value;
				}
				finally
				{
					ExitWrite();
				}
			}
		}

		#endregion

		#region IList (non-generic)


		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (T)value; }
		}

		int IList.Add(object value)
		{
			Add((T)value);
			return Count - 1;
		}

		bool IList.Contains(object value)
		{
			if (value is T item)
				return Contains(item);
			return false;
		}

		int IList.IndexOf(object value)
		{
			if (value is T item)
				return IndexOf(item);
			return -1;
		}

		void IList.Insert(int index, object value)
		{
			Insert(index, (T)value);
		}

		void IList.Remove(object value)
		{
			if (value is T item)
				Remove(item);
		}

		void IList.RemoveAt(int index)
		{
			RemoveAt(index);
		}

		#endregion

		#region ICollection (non-generic)

		bool ICollection.IsSynchronized => true;

		object ICollection.SyncRoot => _rwLock;

		void ICollection.CopyTo(Array array, int index)
		{
			EnterReadSorted();
			try
			{
				((ICollection)_items).CopyTo(array, index);
			}
			finally
			{
				ExitRead();
			}
		}

		#endregion
	}
}
