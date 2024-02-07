using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	/// <summary>
	/// ObjectChangeReplay is effectively ObjectMerge.ThreeWayMerge that always uses ConflictResolution.TakeYours.
	/// </summary>
	[TestClass]
	public class TestObjectChangeReplay
	{
		[TestInitialize]
		public void TestInitialize()
		{
			ObjectMerge.SerializeObject = obj =>
			{
				System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
				return serializer.Serialize(obj);
			};
			ObjectMerge.DeserializeObject = (json, type) =>
			{
				System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
				return serializer.Deserialize(json, type);
			};
		}
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
			differentW = changeHelper.Apply(differentW);

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
			obj = changeHelper.Apply(obj);
			Assert.AreEqual(4, obj.A, changeHelper.ToString());
			Assert.AreEqual(3, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject2()
		{
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(new SO(2, 1), new SO(3, 1));
			SO obj = new SO(4, 4);
			obj = changeHelper.Apply(obj);
			Assert.AreEqual(3, obj.A, changeHelper.ToString());
			Assert.AreEqual(4, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_SetNull()
		{
			SO result = new ObjectChangeReplay(new SO(1, 2), null).Apply(new SO(9, 8));
			Assert.IsNull(result);
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_SetFromNull()
		{
			SO result = new ObjectChangeReplay(null, new SO(1, 3)).Apply(new SO(9, 8));
			// 1,3 was recorded, so it should be the result
			Assert.AreEqual(1, result.A);
			Assert.AreEqual(3, result.B);
		}
		[TestMethod]
		public void TestObjectChangeReplay_NullNull()
		{
			SO result = new ObjectChangeReplay(null, null).Apply(new SO(9, 8));
			// No change was recorded, so there should be no change to the applied item
			Assert.AreEqual(9, result.A);
			Assert.AreEqual(8, result.B);
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_ApplyToNull()
		{
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(new SO(1, 2), new SO(1, 3));
			SO obj = null;
			SO result = changeHelper.Apply(obj);
			// B=3 was recorded, so it should be the result
			Assert.AreEqual(1, result.A);
			Assert.AreEqual(3, result.B);
		}
		[TestMethod]
		public void TestObjectChangeReplay_SimpleObject_NoChanges()
		{
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(new SO(1, 2), new SO(1, 2));
			SO obj = new SO(4, 4);
			obj = changeHelper.Apply(obj);
			Assert.AreEqual(4, obj.A, changeHelper.ToString());
			Assert.AreEqual(4, obj.B, changeHelper.ToString());
		}
		class CO
		{
			public SO X { get; set; }
			public SO Y;
			public int Z;
			public CO() { }
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
			obj = changeHelper.Apply(obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.IsNull(obj.Y, changeHelper.ToString());
			Assert.AreEqual(2, obj.Z, changeHelper.ToString());

			// Test setting a null field when the field is already null.
			A = new CO(new SO(1, 2), null, 3);
			B = A.Copy();
			B.Y = new SO(4, 5);
			changeHelper = new ObjectChangeReplay(A, B);
			obj = new CO(null, null, 3);
			obj = changeHelper.Apply(obj);
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
			obj = changeHelper.Apply(obj);
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
			obj = changeHelper.Apply(obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.AreEqual(4, obj.Y.A, changeHelper.ToString());
			Assert.AreEqual(5, obj.Y.B, changeHelper.ToString());
			Assert.AreEqual(3, obj.Z, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeReplay_TypeMismatch_Throws()
		{
			Expect.Exception(() =>
			{
				new ObjectChangeReplay(new SO(1, 2), new CO(null, null, 1)).Apply(new SO(3, 4));
			});
			Expect.Exception(() =>
			{
				new ObjectChangeReplay(new SO(1, 2), new CO(null, null, 1)).Apply(new CO(null, null, 1));
			});
			Expect.Exception(() =>
			{
				ObjectChangeReplay c = new ObjectChangeReplay(new SO(1, 2), new SO(1, 3));
				CO src = new CO(null, null, 1);
				object result = c.Apply(src);
			});
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
		public void TestObjectChangeReplay_Array_Nullification()
		{
			L4 A = new L4(new int[] { 1, 5, 9, 10 });
			L4 B = new L4(null);
			L4 C = new L4(new int[] { 2, 5, 9, 12 });
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			L4 result = changeHelper.Apply(C);
			Assert.IsNull(result.list);
		}
		[TestMethod]
		public void TestObjectChangeReplay_CollectionMerge_YouChange()
		{
			// This is a 3-way merge where A is base version, B is your version, C is their version.
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 B = new L3(new List<int>(new int[] { 1, 5, 9, 10 })); // < You remove the last element
			L3 C = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			// Collections are compared as a whole.
			// C did not change the collection, therefore no conflict.
			L3 result = changeHelper.Apply(C);
			Expect.Equal(B.list, result.list);
		}
		[TestMethod]
		public void TestObjectChangeReplay_CollectionMerge_TheyChange()
		{
			// This is a 3-way merge where A is base version, B is your version, C is their version.
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 B = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 C = new L3(new List<int>(new int[] { 1, 5, 8, 10, 7 })); // < They change the middle element
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			// Collections are compared as a whole.
			// B did not change the collection, therefore no conflict.
			L3 result = changeHelper.Apply(C);
			Expect.Equal(C.list, result.list);
		}
		[TestMethod]
		public void TestObjectChangeReplay_CollectionMerge_BothChange()
		{
			// This is a 3-way merge where A is base version, B is your version, C is their version.
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 B = new L3(new List<int>(new int[] { 1, 5, 9, 10 })); // < You remove the last element
			L3 C = new L3(new List<int>(new int[] { 1, 5, 9, 10 })); // < They remove the last element
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			// Collections are compared as a whole.
			// Both made the same change, therefore no conflict.
			L3 result = changeHelper.Apply(C);
			Expect.Equal(B.list, result.list);
			Expect.Equal(C.list, result.list);
		}
		[TestMethod]
		public void TestObjectChangeReplay_CollectionMerge_NoChange()
		{
			// This is a 3-way merge where A is base version, B is your version, C is their version.
			L3 A = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 B = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			L3 C = new L3(new List<int>(new int[] { 1, 5, 9, 10, 7 }));
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			// Collections are compared as a whole.
			L3 result = changeHelper.Apply(C);
			Expect.Equal(A.list, result.list);
		}
		[TestMethod]
		public void TestObjectChangeReplay_CollectionMerge_ConflictTakesYours()
		{
			// This is a 3-way merge where A is base version, B is your version, C is their version.
			L4 A = new L4(new int[] { 1, 5, 9, 10, 7 });
			L4 B = new L4(new int[] { 1, 4, 9, 10, 7 }); // You changed one value
			L4 C = new L4(new int[] { 1, 5, 9, 11, 7 }); // They changed a different value.
			ObjectChangeReplay changeHelper = new ObjectChangeReplay(A, B);
			L4 result = changeHelper.Apply(C);
			// Collections are compared as a whole.
			// This is a merge conflict, but the ObjectChangeReplay class resolves conflicts by taking your version.
			// Therefore, result's list should match B's list.
			Expect.NotEqual(A.list, result.list);
			Expect.Equal(B.list, result.list);
			Expect.NotEqual(C.list, result.list);
		}
	}
}
