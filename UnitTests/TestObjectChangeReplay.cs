using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestObjectChangeReplay
	{
		[TestMethod]
		public void TestObjectChangeReplay_Int()
		{
			TestPrimitive(true, 0, 1, 5);
			TestPrimitive(true, 0, 1, (int?)null);
			TestPrimitive(true, 0, (int?)null, 5);
			TestPrimitive(true, 0, (int?)null, (int?)null);
			TestPrimitive(true, (int?)null, 1, 5);
			TestPrimitive(true, (int?)null, 1, (int?)null);
			TestPrimitive(false, (int?)null, (int?)null, 5);
			TestPrimitive(false, (int?)null, (int?)null, (int?)null);
		}
		[TestMethod]
		public void TestObjectChangeReplay_String()
		{
			// Test basic
			TestPrimitive(true, "A", "B", "C");
			TestPrimitive(true, "A", "B", null);
			TestPrimitive(true, "A", null, "C");
			TestPrimitive(true, "A", null, null);
			TestPrimitive(true, null, "B", "C");
			TestPrimitive(true, null, "B", null);
			TestPrimitive(false, null, null, "C");
			TestPrimitive(false, (string)null, null, null);
		}
		private void TestPrimitive<T>(bool expectModification, T original, T modified, T src)
		{
			PrimitiveWrapper originalW = new PrimitiveWrapper(original);
			PrimitiveWrapper modifiedW = new PrimitiveWrapper(modified);
			PrimitiveWrapper differentW = new PrimitiveWrapper(src);

			ObjectChangeReplay changeHelper = new ObjectChangeReplay(originalW, modifiedW);
			changeHelper.Apply(differentW);

			if (expectModification)
				Assert.AreEqual(modifiedW.val, differentW.val);
			else
				Assert.AreEqual(src, differentW.val);
		}
		class PrimitiveWrapper
		{
			public object val;
			public PrimitiveWrapper() { }
			public PrimitiveWrapper(object val) { this.val = val; }
		}
		class SO
		{
			public int A { get; set; }
			public int B;
			public SO() { }
			public SO(int a, int b) { A = a; B = b; }
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject()
		{
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(new SO(1, 2), new SO(1, 3));
			SO obj = new SO(4, 4);
			changeHelper.Apply(obj);
			Assert.AreEqual(4, obj.A, changeHelper.ToString());
			Assert.AreEqual(3, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject2()
		{
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(new SO(2, 1), new SO(3, 1));
			SO obj = new SO(4, 4);
			changeHelper.Apply(obj);
			Assert.AreEqual(3, obj.A, changeHelper.ToString());
			Assert.AreEqual(4, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_SetNull_Throws()
		{
			try
			{
				new ObjectChangeReplay(new SO(1, 2), null);
				Assert.Fail("Expected Exception");
			}
			catch { }
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_SetFromNull_Throws()
		{
			try
			{
				new ObjectChangeReplay(null, new SO(1, 3));
				Assert.Fail("Expected Exception");
			}
			catch { }
		}
		[TestMethod]
		public void TestObjectChangeReplay_NullNull_Throws()
		{
			try
			{
				new ObjectChangeReplay(null, null);
				Assert.Fail("Expected Exception");
			}
			catch { }
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_NoChanges()
		{
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(new SO(1, 2), new SO(1, 2));
			SO obj = new SO(4, 4);
			changeHelper.Apply(obj);
			Assert.AreEqual(4, obj.A, changeHelper.ToString());
			Assert.AreEqual(4, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_ApplyToNull_Throws()
		{
			try
			{
				ObjectChangeReplay changeHelper = new ObjectChangeReplay(new SO(1, 2), new SO(1, 3));
				SO obj = null;
				changeHelper.Apply(obj);
				Assert.Fail("Expected Exception");
			}
			catch { }
		}
		class CO
		{
			public SO X { get; set; }
			public SO Y;
			public int Z;
			public CO(SO x, SO y, int z) { X = x; Y = y; Z = z; }
		}
		[TestMethod]
		public void TestObjectChangeReplay_ComplexObject()
		{
			// Test assignment of the int field
			CO A = new CO(null, null, 1);
			CO B = A.Copy();
			B.Z = 2;
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			CO obj = new CO(null, null, 3);
			changeHelper.Apply(obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.IsNull(obj.Y, changeHelper.ToString());
			Assert.AreEqual(2, obj.Z, changeHelper.ToString());

			// Test setting a null field when the field is already null.
			A = new CO(new SO(1, 2), null, 3);
			B = A.Copy();
			B.Y = new SO(4, 5);
			changeHelper = new ObjectChangeReplay(A, B);
			obj = new CO(null, null, 3);
			changeHelper.Apply(obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.AreEqual(4, obj.Y.A, changeHelper.ToString());
			Assert.AreEqual(5, obj.Y.B, changeHelper.ToString());
			Assert.AreEqual(3, obj.Z, changeHelper.ToString());

			// Test the same thing again but this time also set X to null.
			A = new CO(new SO(1, 2), null, 1);
			B = A.Copy();
			B.X = null;
			B.Y = new SO(4, 5);
			changeHelper = new ObjectChangeReplay(A, B);
			obj = new CO(null, null, 3);
			changeHelper.Apply(obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.AreEqual(4, obj.Y.A, changeHelper.ToString());
			Assert.AreEqual(5, obj.Y.B, changeHelper.ToString());
			Assert.AreEqual(3, obj.Z, changeHelper.ToString());

			// Test the same thing again but this time C should begin with X and Y initialized to non-null values.  This tests that Apply can nullify a field that wasn't already set to null.
			A = new CO(new SO(1, 2), null, 1);
			B = A.Copy();
			B.X = null;
			B.Y = new SO(4, 5);
			changeHelper = new ObjectChangeReplay(A, B);
			obj = new CO(new SO(10, 11), new SO(12, 13), 3);
			changeHelper.Apply(obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.AreEqual(4, obj.Y.A, changeHelper.ToString());
			Assert.AreEqual(5, obj.Y.B, changeHelper.ToString());
			Assert.AreEqual(3, obj.Z, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeReplay_TypeMismatch_Throws()
		{
			try
			{
				new ObjectChangeReplay(new SO(1, 2), new CO(null, null, 1));
				Assert.Fail("Expected exception");
			}
			catch { }
			try
			{
				ObjectChangeReplay c = new ObjectChangeReplay(new SO(1, 2), new SO(1, 3));
				CO src = new CO(null, null, 1);
				c.Apply(src);
				Assert.Fail("Expected exception");
			}
			catch { }
		}
		class L1
		{
			public IList list;
			public L1() { }
			public L1(IList list) { this.list = list; }
		}
		class L2
		{
			public IList<int> list;
			public L2() { }
			public L2(IList<int> list) { this.list = list; }
		}
		class L3
		{
			public List<int> list;
			public L3() { }
			public L3(List<int> list) { this.list = list; }
		}
		class L4
		{
			public int[] list;
			public L4() { }
			public L4(int[] list) { this.list = list; }
		}
		[TestMethod]
		public void TestObjectChangeReplay_IList()
		{
			L1 A = new L1(new int[] { 1, 5, 9, 10 });
			L1 B = new L1(new List<int>(new int[] { 1, 4, 9, 11 })); // Simulate User B changing the second element to 4, fourth element to 11. User B's changes are committed last.
			L1 C = new L1(new int[] { 2, 5, 9, 12 }); // Simulate User C changing the first element to 2, fourth element to 12.
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(9, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_IList_int()
		{
			L2 A = new L2(new int[] { 1, 5, 9, 10 });
			L2 B = new L2(new List<int>(new int[] { 1, 4, 9, 11 })); // Simulate User B changing the second element to 4, fourth element to 11. User B's changes are committed last.
			L2 C = new L2(new int[] { 2, 5, 9, 12 }); // Simulate User C changing the first element to 2, fourth element to 12.
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(9, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_List_int()
		{
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10 }));
			L3 B = new L3(new List<int>(new int[] { 1, 4, 9, 11 })); // Simulate User B changing the second element to 4, fourth element to 11. User B's changes are committed last.
			L3 C = new L3(new List<int>(new int[] { 2, 5, 9, 12 })); // Simulate User C changing the first element to 2, fourth element to 12.
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(9, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_Array_int()
		{
			L4 A = new L4(new int[] { 1, 5, 9, 10 });
			L4 B = new L4(new int[] { 1, 4, 9, 11 }); // Simulate User B changing the second element to 4, fourth element to 11. User B's changes are committed last.
			L4 C = new L4(new int[] { 2, 5, 9, 12 }); // Simulate User C changing the first element to 2, fourth element to 12.
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			Assert.AreEqual(4, C.list.Length);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(9, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_Array_Nullification()
		{
			L4 A = new L4(new int[] { 1, 5, 9, 10 });
			L4 B = new L4(null);
			L4 C = new L4(new int[] { 2, 5, 9, 12 });
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			Assert.IsNull(C.list);
		}
		[TestMethod]
		public void TestObjectChangeReplay_IList_Construction()
		{
			L1 A = new L1(new int[] { 1, 5, 9, 10, 7 });
			L1 B = new L1(new int[] { 1, 4, 9, 11, 7 });
			L1 C = new L1(null);
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4, and it should have default values in the 1st and 3rd slots.
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(0, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_IList_int_Construction()
		{
			L2 A = new L2(new int[] { 1, 5, 9, 10, 7 });
			L2 B = new L2(new int[] { 1, 4, 9, 11, 7 });
			L2 C = new L2(null);
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4, and it should have default values in the 1st and 3rd slots.
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(0, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_List_int_Construction()
		{
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 B = new L3(new List<int>(new int[] { 1, 4, 9, 11, 7 }));
			L3 C = new L3(null);
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4, and it should have default values in the 1st and 3rd slots.
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(0, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_Array_Construction()
		{
			L4 A = new L4(new int[] { 1, 5, 9, 10, 7 });
			L4 B = new L4(new int[] { 1, 4, 9, 11, 7 });
			L4 C = new L4(null);
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4, and it should have default values in the 1st and 3rd slots.
			Assert.AreEqual(4, C.list.Length);
			Assert.AreEqual(0, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_IList_Lengthen()
		{
			L1 A = new L1(new int[] { 1, 5, 9, 10, 7 });
			L1 B = new L1(new List<int>(new int[] { 1, 4, 9, 11, 7 }));
			L1 C = new L1(new int[] { 2 });
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4.
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_IList_int_Lengthen()
		{
			L2 A = new L2(new int[] { 1, 5, 9, 10, 7 });
			L2 B = new L2(new List<int>(new int[] { 1, 4, 9, 11, 7 }));
			L2 C = new L2(new int[] { 2 });
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4.
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_List_int_Lengthen()
		{
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 B = new L3(new List<int>(new int[] { 1, 4, 9, 11, 7 }));
			L3 C = new L3(new List<int>(new int[] { 2 }));
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4.
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_Array_Lengthen()
		{
			L4 A = new L4(new int[] { 1, 5, 9, 10, 7 });
			L4 B = new L4(new int[] { 1, 4, 9, 11, 7 });
			L4 C = new L4(new int[] { 2 });
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4.
			Assert.AreEqual(4, C.list.Length);
			Assert.AreEqual(2, C.list[0]);
			Assert.AreEqual(4, C.list[1]);
			Assert.AreEqual(0, C.list[2]);
			Assert.AreEqual(11, C.list[3]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_List_Weakness_Example()
		{
			// In this test, we demonstrate the undesirable behavior when conflicting actions occur to a list.
			// In this example, User B tries to sort the list, but meanwhile, User C has changed the `5` at index 2 into a `95`.
			// The final result does not have a `95`.
			L3 A = new L3(new List<int>(new int[] { 4, 42, 5, 16, 15, 23 }));
			L3 B = A.Copy();
			B.list.Sort();
			// B is now [4, 5, 15, 16, 23, 42]
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);

			L3 C = A.Copy();
			C.list[2] = 95;
			changeHelper.Apply(C);

			Assert.AreEqual(6, C.list.Count);
			Assert.AreEqual(4, C.list[0]);
			Assert.AreEqual(5, C.list[1]); // User C replaced the 5 with a 95, but it got overwritten by User B's change.
			Assert.AreEqual(15, C.list[2]);
			Assert.AreEqual(16, C.list[3]);
			Assert.AreEqual(23, C.list[4]);
			Assert.AreEqual(42, C.list[5]);

			// Now to drive home the point, what if User C had changed an element in one of the slots that User B wasn't going to move during the sort operation??
			A = new L3(new List<int>(new int[] { 4, 42, 5, 16, 15, 23 }));
			B = A.Copy();
			B.list.Sort();
			// B is now [4, 5, 15, 16, 23, 42]
			changeHelper = new ObjectChangeReplay(A, B);

			C = A.Copy();
			C.list[3] = 95;
			changeHelper.Apply(C);

			Assert.AreEqual(6, C.list.Count);
			Assert.AreEqual(4, C.list[0]);
			Assert.AreEqual(5, C.list[1]);
			Assert.AreEqual(15, C.list[2]);
			Assert.AreEqual(95, C.list[3]); // User C replaced the 16 with a 95, and this time User B's sorting didn't affect that particular list index, so the result is "unexpected".
			Assert.AreEqual(23, C.list[4]);
			Assert.AreEqual(42, C.list[5]);
		}
		[TestMethod]
		public void TestObjectChangeReplay_List_int_Shorten()
		{
			// In this test, we see what happens when we try to shorten an IList.
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 B = new L3(new List<int>(new int[] { 1, 5, 9, 10 })); // < User B tries to remove the last element
			L3 C = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			changeHelper.Apply(C);
			// The only changes that were made were to the 2nd and 4th elements, so C's length is expected to be only 4.
			Assert.AreEqual(4, C.list.Count);
			Assert.AreEqual(1, C.list[0]);
			Assert.AreEqual(5, C.list[1]);
			Assert.AreEqual(9, C.list[2]);
			Assert.AreEqual(10, C.list[3]);
		}
	}
}
