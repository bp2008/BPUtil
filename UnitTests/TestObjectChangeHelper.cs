using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestObjectChangeHelper
	{
		[TestMethod]
		public void TestObjectChangeHelper_Int()
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
		public void TestObjectChangeHelper_String()
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
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(original, modified);
			T differentSource = src;
			T srcUnchanged = src;
			changeHelper.Apply(ref differentSource);
			if (expectModification)
				Assert.AreEqual(modified, differentSource);
			else
				Assert.AreEqual(srcUnchanged, differentSource);
		}
		class SO
		{
			public int A { get; set; }
			public int B;
			public SO(int a, int b) { A = a; B = b; }
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(new SO(1, 2), new SO(1, 3));
			SO obj = new SO(4, 4);
			changeHelper.Apply(ref obj);
			Assert.AreEqual(4, obj.A, changeHelper.ToString());
			Assert.AreEqual(3, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject2()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(new SO(2, 1), new SO(3, 1));
			SO obj = new SO(4, 4);
			changeHelper.Apply(ref obj);
			Assert.AreEqual(3, obj.A, changeHelper.ToString());
			Assert.AreEqual(4, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject_SetNull()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(new SO(1, 2), null);
			SO obj = new SO(4, 4);
			changeHelper.Apply(ref obj);
			Assert.IsNull(obj, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject_SetFromNull()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(null, new SO(1, 3));
			SO obj = new SO(4, 4);
			changeHelper.Apply(ref obj);
			Assert.AreEqual(1, obj.A, changeHelper.ToString());
			Assert.AreEqual(3, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject_NullNull()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(null, null);
			SO obj = new SO(4, 4);
			changeHelper.Apply(ref obj);
			Assert.AreEqual(4, obj.A, changeHelper.ToString());
			Assert.AreEqual(4, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject_NoChanges()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(new SO(1, 2), new SO(1, 2));
			SO obj = new SO(4, 4);
			changeHelper.Apply(ref obj);
			Assert.AreEqual(4, obj.A, changeHelper.ToString());
			Assert.AreEqual(4, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject_ApplyToNull()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(new SO(1, 2), new SO(1, 3));
			SO obj = null;
			changeHelper.Apply(ref obj);
			Assert.AreEqual(1, obj.A, changeHelper.ToString());
			Assert.AreEqual(3, obj.B, changeHelper.ToString());
		}
		[TestMethod]
		public void TestObjectChangeHelper_SimpleObject_NullsAllAround()
		{
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(null, null);
			SO obj = null;
			changeHelper.Apply(ref obj);
			Assert.IsNull(obj, changeHelper.ToString());
		}
		class CO
		{
			public SO X { get; set; }
			public SO Y;
			public int Z;
			public CO(SO x, SO y, int z) { X = x; Y = y; Z = z; }
		}
		[TestMethod]
		public void TestObjectChangeHelper_ComplexObject()
		{
			// Test assignment of the int field
			CO A = new CO(null, null, 1);
			CO B = A.Copy();
			B.Z = 2;
			ObjectChangeHelper changeHelper = new ObjectChangeHelper(A, B);
			CO obj = new CO(null, null, 3);
			changeHelper.Apply(ref obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.IsNull(obj.Y, changeHelper.ToString());
			Assert.AreEqual(2, obj.Z, changeHelper.ToString());

			// Test setting a null field when the field is already null.
			A = new CO(new SO(1,2), null, 3);
			B = A.Copy();
			B.Y = new SO(4, 5);
			changeHelper = new ObjectChangeHelper(A, B);
			obj = new CO(null, null, 3);
			changeHelper.Apply(ref obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.AreEqual(4, obj.Y.A, changeHelper.ToString());
			Assert.AreEqual(5, obj.Y.B, changeHelper.ToString());
			Assert.AreEqual(3, obj.Z, changeHelper.ToString());

			// Test the same thing again but this time also set X to null.
			A = new CO(new SO(1, 2), null, 1);
			B = A.Copy();
			B.X = null;
			B.Y = new SO(4, 5);
			changeHelper = new ObjectChangeHelper(A, B);
			obj = new CO(null, null, 3);
			changeHelper.Apply(ref obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.AreEqual(4, obj.Y.A, changeHelper.ToString());
			Assert.AreEqual(5, obj.Y.B, changeHelper.ToString());
			Assert.AreEqual(3, obj.Z, changeHelper.ToString());

			// Test the same thing again but this time C should begin with X and Y initialized to non-null values.  This tests that Apply can nullify a field that wasn't already set to null.
			A = new CO(new SO(1, 2), null, 1);
			B = A.Copy();
			B.X = null;
			B.Y = new SO(4, 5);
			changeHelper = new ObjectChangeHelper(A, B);
			obj = new CO(new SO(10,11), new SO(12,13), 3);
			changeHelper.Apply(ref obj);
			Assert.IsNull(obj.X, changeHelper.ToString());
			Assert.AreEqual(4, obj.Y.A, changeHelper.ToString());
			Assert.AreEqual(5, obj.Y.B, changeHelper.ToString());
			Assert.AreEqual(3, obj.Z, changeHelper.ToString());
		}
		class IntHaver
		{
			public int field;
			public IntHaver() { }
			public IntHaver(int field) { this.field = field; }
		}
		class StringHaver
		{
			public string field;
			public StringHaver() { }
			public StringHaver(string field) { this.field = field; }
		}
		[TestMethod]
		public void TestObjectChangeHelper_TypeMismatch()
		{
			new ObjectChangeHelper(1, "A");
			new ObjectChangeHelper("B", 2);
			new ObjectChangeHelper(new IntHaver(1), new StringHaver("A"));
			new ObjectChangeHelper(new StringHaver("B"), new IntHaver(2));
			new ObjectChangeHelper(new SO(1, 2), new CO(null, null, 1));

			Expect.Exception(() =>
			{
				ObjectChangeHelper c = new ObjectChangeHelper(new SO(1, 2), new SO(1, 3));
				CO src = new CO(null, null, 1);
				c.Apply(ref src);
			});
		}
	}
}
