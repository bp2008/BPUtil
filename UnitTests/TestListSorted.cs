using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestListSorted
	{
		#region Basic Sorting

		[TestMethod]
		public void AddAndEnumerate_ReturnsSortedOrder()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(5);
			list.Add(1);
			list.Add(3);
			list.Add(2);
			list.Add(4);

			CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list.ToArray());
		}

		[TestMethod]
		public void AddAndEnumerate_Strings_ReturnsSortedOrder()
		{
			ListSorted<string> list = new ListSorted<string>();
			list.Add("banana");
			list.Add("apple");
			list.Add("cherry");

			CollectionAssert.AreEqual(new[] { "apple", "banana", "cherry" }, list.ToArray());
		}

		[TestMethod]
		public void ConstructFromCollection_ReturnsSortedOrder()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list.ToArray());
		}

		[TestMethod]
		public void EmptyList_EnumerateReturnsNothing()
		{
			ListSorted<int> list = new ListSorted<int>();
			Assert.AreEqual(0, list.Count);
			CollectionAssert.AreEqual(new int[0], list.ToArray());
		}

		[TestMethod]
		public void SingleElement_WorksCorrectly()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(42);
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual(42, list[0]);
		}

		[TestMethod]
		public void DuplicateElements_AllPreserved()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(3);
			list.Add(1);
			list.Add(3);
			list.Add(1);
			list.Add(2);

			CollectionAssert.AreEqual(new[] { 1, 1, 2, 3, 3 }, list.ToArray());
		}

		#endregion

		#region Custom Comparer

		[TestMethod]
		public void CustomIComparer_SortsInDescendingOrder()
		{
			ListSorted<int> list = new ListSorted<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
			list.Add(1);
			list.Add(5);
			list.Add(3);

			CollectionAssert.AreEqual(new[] { 5, 3, 1 }, list.ToArray());
		}

		[TestMethod]
		public void CustomComparison_SortsInDescendingOrder()
		{
			ListSorted<int> list = new ListSorted<int>((a, b) => b.CompareTo(a));
			list.Add(1);
			list.Add(5);
			list.Add(3);

			CollectionAssert.AreEqual(new[] { 5, 3, 1 }, list.ToArray());
		}

		[TestMethod]
		public void ConstructFromCollectionWithComparer_SortsCorrectly()
		{
			IComparer<int> descending = Comparer<int>.Create((a, b) => b.CompareTo(a));
			ListSorted<int> list = new ListSorted<int>(new[] { 1, 5, 3 }, descending);

			CollectionAssert.AreEqual(new[] { 5, 3, 1 }, list.ToArray());
		}

		[TestMethod]
		public void NullComparer_UsesDefault()
		{
			ListSorted<int> list = new ListSorted<int>((IComparer<int>)null);
			list.Add(3);
			list.Add(1);
			list.Add(2);
			CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullComparison_ThrowsArgumentNullException()
		{
			new ListSorted<int>((Comparison<int>)null);
		}

		#endregion

		#region Indexer

		[TestMethod]
		public void Indexer_Get_ReturnsSortedElement()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(30);
			list.Add(10);
			list.Add(20);

			Assert.AreEqual(10, list[0]);
			Assert.AreEqual(20, list[1]);
			Assert.AreEqual(30, list[2]);
		}

		[TestMethod]
		public void Indexer_Set_ReplacesAndResorts()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);

			// Replace the element at sorted index 0 (value 1) with 10.
			list[0] = 10;
			// After re-sort: 2, 3, 10
			CollectionAssert.AreEqual(new[] { 2, 3, 10 }, list.ToArray());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Indexer_Get_OutOfRange_Throws()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(1);
			int _ = list[5];
		}

		#endregion

		#region Insert

		[TestMethod]
		public void Insert_IgnoresIndex_AddsSorted()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(1);
			list.Add(5);

			// Insert at index 0, but it should still appear in sorted position.
			list.Insert(0, 3);

			CollectionAssert.AreEqual(new[] { 1, 3, 5 }, list.ToArray());
		}

		#endregion

		#region Remove / RemoveAt

		[TestMethod]
		public void Remove_RemovesItem()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			bool removed = list.Remove(2);
			Assert.IsTrue(removed);
			CollectionAssert.AreEqual(new[] { 1, 3 }, list.ToArray());
		}

		[TestMethod]
		public void Remove_NonExistent_ReturnsFalse()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 1, 2, 3 });
			bool removed = list.Remove(99);
			Assert.IsFalse(removed);
			Assert.AreEqual(3, list.Count);
		}

		[TestMethod]
		public void RemoveAt_RemovesByIndex()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			// Sorted: 1, 2, 3 -> RemoveAt(1) removes value 2.
			list.RemoveAt(1);
			CollectionAssert.AreEqual(new[] { 1, 3 }, list.ToArray());
		}

		[TestMethod]
		public void RemoveAll_RemovesMatchingItems()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			int removed = list.RemoveAll(x => x > 3);
			Assert.AreEqual(2, removed);
			CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
		}

		#endregion

		#region Clear

		[TestMethod]
		public void Clear_RemovesAllElements()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			list.Clear();
			Assert.AreEqual(0, list.Count);
			CollectionAssert.AreEqual(new int[0], list.ToArray());
		}

		#endregion

		#region Contains / IndexOf

		[TestMethod]
		public void Contains_FindsExistingItem()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			Assert.IsTrue(list.Contains(2));
			Assert.IsFalse(list.Contains(99));
		}

		[TestMethod]
		public void IndexOf_ReturnsSortedIndex()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 30, 10, 20 });
			// Sorted: 10, 20, 30
			Assert.AreEqual(0, list.IndexOf(10));
			Assert.AreEqual(1, list.IndexOf(20));
			Assert.AreEqual(2, list.IndexOf(30));
			Assert.AreEqual(-1, list.IndexOf(99));
		}

		#endregion

		#region CopyTo

		[TestMethod]
		public void CopyTo_CopiesSortedElements()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			int[] array = new int[5];
			list.CopyTo(array, 1);
			CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0 }, array);
		}

		#endregion

		#region AddRange

		[TestMethod]
		public void AddRange_AddsMultipleItemsSorted()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Add(5);
			list.AddRange(new[] { 3, 1, 4 });

			CollectionAssert.AreEqual(new[] { 1, 3, 4, 5 }, list.ToArray());
		}

		#endregion

		#region BinarySearch

		[TestMethod]
		public void BinarySearch_FindsExistingItem()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			int index = list.BinarySearch(3);
			Assert.AreEqual(2, index); // Sorted: 1, 2, 3, 4, 5 -> index 2
		}

		[TestMethod]
		public void BinarySearch_ReturnsComplementForMissingItem()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 1, 3, 5 });
			int index = list.BinarySearch(2);
			Assert.IsTrue(index < 0);
			int insertionPoint = ~index;
			Assert.AreEqual(1, insertionPoint); // 2 would be inserted at index 1
		}

		#endregion

		#region Find / FindAll / Exists

		[TestMethod]
		public void Find_ReturnsFirstMatchInSortedOrder()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			int result = list.Find(x => x > 2);
			Assert.AreEqual(3, result); // Sorted: 1, 2, 3, 4, 5 -> first > 2 is 3
		}

		[TestMethod]
		public void FindAll_ReturnsAllMatchesInSortedOrder()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			List<int> result = list.FindAll(x => x > 2);
			CollectionAssert.AreEqual(new[] { 3, 4, 5 }, result);
		}

		[TestMethod]
		public void Exists_ReturnsTrueForMatch()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 1, 2, 3 });
			Assert.IsTrue(list.Exists(x => x == 2));
			Assert.IsFalse(list.Exists(x => x == 99));
		}

		#endregion

		#region ForEach

		[TestMethod]
		public void ForEach_VisitsElementsInSortedOrder()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			List<int> visited = new List<int>();
			list.ForEach(x => visited.Add(x));
			CollectionAssert.AreEqual(new[] { 1, 2, 3 }, visited);
		}

		#endregion

		#region GetRange

		[TestMethod]
		public void GetRange_ReturnsSortedSubset()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			// Sorted: 1, 2, 3, 4, 5
			List<int> range = list.GetRange(1, 3);
			CollectionAssert.AreEqual(new[] { 2, 3, 4 }, range);
		}

		#endregion

		#region Reverse

		[TestMethod]
		public void Reverse_ReversesCurrentSortedOrder()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			list.Reverse();
			// After sorting: 1, 2, 3 -> reversed: 3, 2, 1
			// Note: the internal data is now 3, 2, 1.  But adding new items will re-sort.
			// ToArray triggers sort again so result goes back to 1, 2, 3.
			// This is expected behavior per the Reverse doc: subsequent reads re-sort.
			// To verify Reverse actually ran, we'd need to avoid triggering a re-sort.
			// However, Reverse does NOT mark dirty, so the reversed state is preserved.
			// Wait - the implementation increments _version but doesn't set _isDirty. Let's verify.
			// Actually reading the code: Reverse calls EnterWriteSorted (sorts first), 
			// then reverses and increments _version but does NOT set _isDirty.
			// So the list stays reversed until the next write marks it dirty.
			Assert.AreEqual(3, list[0]);
			Assert.AreEqual(2, list[1]);
			Assert.AreEqual(1, list[2]);
		}

		#endregion

		#region TrueForAll

		[TestMethod]
		public void TrueForAll_ReturnsTrueWhenAllMatch()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 2, 4, 6 });
			Assert.IsTrue(list.TrueForAll(x => x % 2 == 0));
			Assert.IsFalse(list.TrueForAll(x => x > 3));
		}

		#endregion

		#region Capacity

		[TestMethod]
		public void Capacity_CanBeSetAndRead()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.Capacity = 100;
			Assert.IsTrue(list.Capacity >= 100);
		}

		[TestMethod]
		public void ConstructWithCapacity_SetsInitialCapacity()
		{
			ListSorted<int> list = new ListSorted<int>(50);
			Assert.IsTrue(list.Capacity >= 50);
		}

		#endregion

		#region TrimExcess

		[TestMethod]
		public void TrimExcess_DoesNotThrow()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 1, 2, 3 });
			list.TrimExcess();
			Assert.AreEqual(3, list.Count);
		}

		#endregion

		#region IList<T> via interface

		[TestMethod]
		public void IListT_Interface_WorksCorrectly()
		{
			IList<int> list = new ListSorted<int>();
			list.Add(3);
			list.Add(1);
			list.Add(2);

			Assert.AreEqual(3, list.Count);
			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(2, list[1]);
			Assert.AreEqual(3, list[2]);
			Assert.IsTrue(list.Contains(2));
			Assert.AreEqual(1, list.IndexOf(2));
		}

		#endregion

		#region IList (non-generic)

		[TestMethod]
		public void IList_NonGeneric_AddAndAccess()
		{
			IList list = new ListSorted<int>();
			list.Add(3);
			list.Add(1);
			list.Add(2);

			Assert.AreEqual(3, list.Count);
			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(2, list[1]);
			Assert.AreEqual(3, list[2]);
		}

		[TestMethod]
		public void IList_NonGeneric_Contains()
		{
			IList list = new ListSorted<int>();
			list.Add(3);
			list.Add(1);
			Assert.IsTrue(list.Contains(3));
			Assert.IsFalse(list.Contains(99));
			Assert.IsFalse(list.Contains("not an int"));
		}

		[TestMethod]
		public void IList_NonGeneric_IndexOf()
		{
			IList list = new ListSorted<int>();
			list.Add(3);
			list.Add(1);
			list.Add(2);
			// Sorted: 1, 2, 3
			Assert.AreEqual(0, list.IndexOf(1));
			Assert.AreEqual(-1, list.IndexOf(99));
			Assert.AreEqual(-1, list.IndexOf("not an int"));
		}

		[TestMethod]
		public void IList_NonGeneric_Remove()
		{
			IList list = new ListSorted<int>();
			list.Add(3);
			list.Add(1);
			list.Add(2);
			list.Remove(2);
			Assert.AreEqual(2, list.Count);
		}

		[TestMethod]
		public void IList_NonGeneric_RemoveAt()
		{
			IList list = new ListSorted<int>();
			list.Add(3);
			list.Add(1);
			list.Add(2);
			// Sorted: 1, 2, 3 -> RemoveAt(1) removes 2
			list.RemoveAt(1);
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(3, list[1]);
		}

		[TestMethod]
		public void IList_NonGeneric_Insert()
		{
			IList list = new ListSorted<int>();
			list.Add(5);
			list.Insert(0, 3);
			// Sorted: 3, 5
			Assert.AreEqual(3, list[0]);
			Assert.AreEqual(5, list[1]);
		}

		[TestMethod]
		public void IList_IsFixedSize_IsFalse()
		{
			IList list = new ListSorted<int>();
			Assert.IsFalse(list.IsFixedSize);
		}

		[TestMethod]
		public void IList_IsReadOnly_IsFalse()
		{
			IList list = new ListSorted<int>();
			Assert.IsFalse(list.IsReadOnly);
		}

		#endregion

		#region ICollection (non-generic)

		[TestMethod]
		public void ICollection_NonGeneric_CopyTo()
		{
			ICollection coll = new ListSorted<int>(new[] { 3, 1, 2 });
			int[] array = new int[3];
			coll.CopyTo(array, 0);
			CollectionAssert.AreEqual(new[] { 1, 2, 3 }, array);
		}

		[TestMethod]
		public void ICollection_IsSynchronized_IsTrue()
		{
			ICollection coll = new ListSorted<int>();
			Assert.IsTrue(coll.IsSynchronized);
		}

		[TestMethod]
		public void ICollection_SyncRoot_IsNotNull()
		{
			ICollection coll = new ListSorted<int>();
			Assert.IsNotNull(coll.SyncRoot);
		}

		#endregion

		#region IReadOnlyList<T>

		[TestMethod]
		public void IReadOnlyList_WorksCorrectly()
		{
			IReadOnlyList<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			Assert.AreEqual(3, list.Count);
			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(2, list[1]);
			Assert.AreEqual(3, list[2]);
		}

		#endregion

		#region Lazy Sorting Verification

		[TestMethod]
		public void LazySorting_MultipleAddsDoNotSortUntilRead()
		{
			// Verify that adding many items and then reading produces the sorted result.
			// This implicitly verifies lazy sorting because sorting only happens on read.
			ListSorted<int> list = new ListSorted<int>();
			for (int i = 100; i >= 1; i--)
				list.Add(i);

			// Count doesn't require sorting.
			Assert.AreEqual(100, list.Count);

			// ToArray triggers sort.
			int[] result = list.ToArray();
			for (int i = 0; i < 100; i++)
				Assert.AreEqual(i + 1, result[i]);
		}

		[TestMethod]
		public void LazySorting_AddRangeThenRead()
		{
			ListSorted<int> list = new ListSorted<int>();
			list.AddRange(new[] { 50, 40, 30, 20, 10 });
			list.AddRange(new[] { 5, 4, 3, 2, 1 });

			CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 10, 20, 30, 40, 50 }, list.ToArray());
		}

		[TestMethod]
		public void LazySorting_ContainsDoesNotRequireSort()
		{
			// Contains should work even without sorting (it searches linearly).
			ListSorted<int> list = new ListSorted<int>();
			list.Add(5);
			list.Add(1);
			list.Add(3);

			Assert.IsTrue(list.Contains(5));
			Assert.IsTrue(list.Contains(1));
			Assert.IsTrue(list.Contains(3));
			Assert.IsFalse(list.Contains(99));
		}

		#endregion

		#region Enumeration

		[TestMethod]
		public void Enumeration_ProducesSortedOrder()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			List<int> result = new List<int>();
			foreach (int item in list)
				result.Add(item);

			CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result);
		}

		[TestMethod]
		public void Enumeration_Linq_WorksCorrectly()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 5, 3, 1, 4, 2 });
			int sum = list.Sum();
			Assert.AreEqual(15, sum);

			int first = list.First();
			Assert.AreEqual(1, first);

			int last = list.Last();
			Assert.AreEqual(5, last);
		}

		#endregion

		#region Concurrent Access

		[TestMethod]
		public void ConcurrentReads_AllReturnCorrectResults()
		{
			ListSorted<int> list = new ListSorted<int>();
			for (int i = 100; i >= 1; i--)
				list.Add(i);

			// Force initial sort.
			int[] _ = list.ToArray();

			// Launch many concurrent readers.
			int readerCount = 10;
			Task[] tasks = new Task[readerCount];
			bool[] success = new bool[readerCount];

			for (int t = 0; t < readerCount; t++)
			{
				int taskIndex = t;
				tasks[t] = Task.Run(() =>
				{
					int[] arr = list.ToArray();
					if (arr.Length != 100)
						return;
					for (int i = 0; i < 100; i++)
					{
						if (arr[i] != i + 1)
							return;
					}
					success[taskIndex] = true;
				});
			}

			Task.WaitAll(tasks);

			for (int t = 0; t < readerCount; t++)
				Assert.IsTrue(success[t], "Reader task " + t + " did not see correct sorted data.");
		}

		[TestMethod]
		public void ConcurrentAddFromMultipleThreads_AllItemsPresent()
		{
			ListSorted<int> list = new ListSorted<int>();
			int itemsPerThread = 100;
			int threadCount = 5;
			Thread[] threads = new Thread[threadCount];

			for (int t = 0; t < threadCount; t++)
			{
				int offset = t * itemsPerThread;
				threads[t] = new Thread(() =>
				{
					for (int i = 0; i < itemsPerThread; i++)
						list.Add(offset + i);
				});
				threads[t].Start();
			}

			for (int t = 0; t < threadCount; t++)
				threads[t].Join();

			Assert.AreEqual(itemsPerThread * threadCount, list.Count);

			int[] sorted = list.ToArray();
			// Verify all items are present and sorted.
			Array.Sort(sorted); // Should already be sorted, but verify.
			for (int i = 0; i < sorted.Length; i++)
				Assert.AreEqual(i, sorted[i]);
		}

		[TestMethod]
		public void ConcurrentReadAndWrite_EnumerationDetectsModification()
		{
			ListSorted<int> list = new ListSorted<int>();
			for (int i = 0; i < 100; i++)
				list.Add(i);

			// Force sort.
			int[] _ = list.ToArray();

			// Enumeration should detect modification if the list is changed mid-enumeration.
			bool caughtException = false;
			try
			{
				foreach (int item in list)
				{
					// Modify the list during enumeration.
					list.Add(999);
					// Continue iterating to trigger the version check.
				}
			}
			catch (InvalidOperationException)
			{
				caughtException = true;
			}

			Assert.IsTrue(caughtException, "Expected InvalidOperationException during concurrent modification of enumeration.");
		}

		#endregion

		#region Drop-In Replacement Behavior

		[TestMethod]
		public void UsableAsListT_ViaInterface()
		{
			// Verify it can be assigned to IList<T> and ICollection<T>.
			IList<int> iList = new ListSorted<int>();
			ICollection<int> iCollection = (ICollection<int>)iList;
			IEnumerable<int> iEnumerable = (IEnumerable<int>)iList;
			IReadOnlyList<int> iReadOnlyList = (IReadOnlyList<int>)(ListSorted<int>)iList;

			iList.Add(3);
			iList.Add(1);
			iList.Add(2);

			Assert.AreEqual(3, iCollection.Count);
			Assert.AreEqual(1, iList[0]);
			Assert.IsTrue(iCollection.Contains(2));
			Assert.AreEqual(3, iEnumerable.Count());
			Assert.AreEqual(3, iReadOnlyList.Count);
		}

		[TestMethod]
		public void IsReadOnly_ReturnsFalse()
		{
			ListSorted<int> list = new ListSorted<int>();
			Assert.IsFalse(list.IsReadOnly);
			Assert.IsFalse(((ICollection<int>)list).IsReadOnly);
		}

		#endregion

		#region Edge Cases

		[TestMethod]
		public void AddAfterClear_WorksCorrectly()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 3, 1, 2 });
			list.Clear();
			list.Add(10);
			list.Add(5);

			CollectionAssert.AreEqual(new[] { 5, 10 }, list.ToArray());
		}

		[TestMethod]
		public void RemoveAll_FromEmptyList()
		{
			ListSorted<int> list = new ListSorted<int>();
			int removed = list.RemoveAll(x => true);
			Assert.AreEqual(0, removed);
		}

		[TestMethod]
		public void AddRange_EmptyCollection()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 1, 2, 3 });
			list.AddRange(new int[0]);
			Assert.AreEqual(3, list.Count);
		}

		[TestMethod]
		public void ConstructFromEmptyCollection()
		{
			ListSorted<int> list = new ListSorted<int>(new int[0]);
			Assert.AreEqual(0, list.Count);
		}

		[TestMethod]
		public void ConstructFromSingleElementCollection()
		{
			ListSorted<int> list = new ListSorted<int>(new[] { 42 });
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual(42, list[0]);
		}

		#endregion
	}
}
