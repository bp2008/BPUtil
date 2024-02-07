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
	public class TestObjectMerge
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
		/// <summary>
		/// Object containing other custom object types.
		/// </summary>
		class CO
		{
			public SO so { get; set; }
			public LO lo;
			public CO() { }
			public CO(SO so, LO lo) { this.so = so; this.lo = lo; }
			public static CO CreateAlpha()
			{
				CO co = new CO();
				co.so = new SO(1);
				co.lo = new LO(new int[] { 2, 3 });
				return co;
			}
		}
		/// <summary>
		/// Object containing a simple integer.
		/// </summary>
		class SO
		{
			public int A;
			public SO() { }
			public SO(int a) { A = a; }
		}
		/// <summary>
		/// Object containing a List.
		/// </summary>
		class LO
		{
			public IList<int> A;
			public LO() { }
			public LO(int[] a) { A = new List<int>(a); }
		}
		private void AssertEqual(object expected, object actual)
		{
			Assert.AreEqual(ObjectMerge.SerializeObject(expected), ObjectMerge.SerializeObject(actual));
		}
		private void AssertNotEqual(object notExpected, object actual)
		{
			Assert.AreNotEqual(ObjectMerge.SerializeObject(notExpected), ObjectMerge.SerializeObject(actual));
		}
		[TestMethod]
		public void TestObjectMerge_NoChanges()
		{
			CO alpha = CO.CreateAlpha();
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(alpha, baseObject);
			AssertEqual(alpha, yours);
			AssertEqual(alpha, theirs);
			AssertEqual(alpha, result);
		}
		[TestMethod]
		public void TestObjectMerge_NonConflictingChanges()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = null;
			theirs.lo = null;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			// Both parties changed a different field, so there is no conflict, both changes should be in the result.
			Assert.IsNull(result.so);
			Assert.IsNull(result.lo);
		}
		[TestMethod]
		public void TestObjectMerge_NonConflictingChanges2()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = null;
			theirs.so = null;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			// Both parties changed the same field the same way, so there is no conflict, changes should be in the result.
			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_NonConflictingChanges3()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so.A = 512;
			theirs.so.A = 512;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			// Both parties changed the same field the same way, so there is no conflict, changes should be in the result.
			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_NonConflictingChanges4()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so.A = 512;
			theirs.lo.A[0] = 512;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			// Both parties changed a different field, so there is no conflict, both changes should be in the result.
			AssertNotEqual(baseObject, result);
			AssertNotEqual(yours, result);
			AssertNotEqual(theirs, result);
			AssertEqual(yours.so, result.so);
			AssertEqual(theirs.lo, result.lo);
		}
		[TestMethod]
		[ExpectedException(typeof(ObjectMergeException))]
		public void TestObjectMerge_ConflictingChanges_DefaultThrows()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = null;
			theirs.so.A = 512;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);
		}
		[TestMethod]
		[ExpectedException(typeof(ObjectMergeException))]
		public void TestObjectMerge_ConflictingChanges_DefaultThrows2()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so.A = 511;
			theirs.so.A = 512;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);
		}
		[TestMethod]
		public void TestObjectMerge_ConflictingChanges_TakeBase()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = null;
			theirs.so.A = 512;

			ObjectMerge.MergeOptions options = new ObjectMerge.MergeOptions();
			options.ConflictResolution = ObjectMerge.ConflictResolution.TakeBase;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs, options);

			AssertEqual(baseObject, result);
		}
		[TestMethod]
		public void TestObjectMerge_ConflictingChanges_TakeYours()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = null;
			theirs.so.A = 512;

			ObjectMerge.MergeOptions options = new ObjectMerge.MergeOptions();
			options.ConflictResolution = ObjectMerge.ConflictResolution.TakeYours;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs, options);

			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_ConflictingChanges_TakeTheirs()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = null;
			theirs.so.A = 512;

			ObjectMerge.MergeOptions options = new ObjectMerge.MergeOptions();
			options.ConflictResolution = ObjectMerge.ConflictResolution.TakeTheirs;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs, options);

			AssertEqual(theirs, result);
		}
		[TestMethod]
		public void TestObjectMerge_YoursCanSetField()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so.A = 897;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_TheirsCanSetField()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			theirs.so.A = 897;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(theirs, result);
		}
		[TestMethod]
		public void TestObjectMerge_YoursCanSetNullObject()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = null;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_TheirsCanSetNullObject()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			theirs.so = null;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(theirs, result);
		}
		[TestMethod]
		public void TestObjectMerge_YoursCanUnsetNullObject()
		{
			CO baseObject = CO.CreateAlpha();
			baseObject.so = null;
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.so = new SO(5);

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_TheirsCanUnsetNullObject()
		{
			CO baseObject = CO.CreateAlpha();
			baseObject.so = null;
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			theirs.so = new SO(5);

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(theirs, result);
		}
		[TestMethod]
		public void TestObjectMerge_YoursCanSetNullList()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.lo.A = null;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_TheirsCanSetNullList()
		{
			CO baseObject = CO.CreateAlpha();
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			theirs.lo.A = null;

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(theirs, result);
		}
		[TestMethod]
		public void TestObjectMerge_YoursCanUnsetNullList()
		{
			CO baseObject = CO.CreateAlpha();
			baseObject.lo.A = null;
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			yours.lo.A = new List<int>(new int[] { 7, 8, 9 });

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(yours, result);
		}
		[TestMethod]
		public void TestObjectMerge_TheirsCanUnsetNullList()
		{
			CO baseObject = CO.CreateAlpha();
			baseObject.lo.A = null;
			CO yours = baseObject.Copy();
			CO theirs = baseObject.Copy();
			theirs.lo.A = new List<int>(new int[] { 7, 8, 9 });

			CO result = ObjectMerge.ThreeWayMerge(baseObject, yours, theirs);

			AssertEqual(theirs, result);
		}
	}
}
