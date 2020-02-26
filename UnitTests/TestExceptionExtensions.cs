using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	[TestClass]
	public class TestExceptionExtensions
	{
		[TestMethod]
		public void TestGetExceptionOfType()
		{
			bool threw = false;
			try
			{
				ThrowComplex();
			}
			catch (Exception ex)
			{
				threw = true;
				Assert.AreEqual("L1", ex.GetExceptionOfType<Exception>(false).Message);
				Assert.AreEqual("L1", ex.GetExceptionOfType<Exception>(true).Message);

				Assert.AreEqual("L2", ex.GetExceptionOfType<ArgumentException>(false).Message);
				Assert.AreEqual("L5", ex.GetExceptionOfType<ArgumentException>(true).Message);

				Assert.IsNull(ex.GetExceptionOfType<ApplicationException>(false));
				Assert.IsNull(ex.GetExceptionOfType<ApplicationException>(true));

				Assert.AreEqual("L3", ex.GetExceptionOfType<AggregateException>(false).Message);
				Assert.AreEqual("L3", ex.GetExceptionOfType<AggregateException>(true).Message);

				Assert.AreEqual("L4", ex.GetExceptionOfType<ArithmeticException>(false).Message);
				Assert.AreEqual("L4", ex.GetExceptionOfType<ArithmeticException>(true).Message);

				Assert.AreEqual("L5", ex.GetExceptionOfType<IndexOutOfRangeException>(false).Message);
				Assert.AreEqual("L5", ex.GetExceptionOfType<IndexOutOfRangeException>(true).Message);
			}
			Assert.IsTrue(threw, "Expected exception to be thrown");
		}
		[TestMethod]
		public void TestGetExceptionWhere()
		{
			bool threw = false;
			try
			{
				ThrowComplex();
				Assert.Fail("Expected exception to be thrown");
			}
			catch (Exception ex)
			{
				threw = true;
				Exception found = ex.GetExceptionWhere(e2 => e2 is ArithmeticException);
				Assert.IsNotNull(found);
				Assert.IsTrue(found is ArithmeticException);
			}
			Assert.IsTrue(threw, "Expected exception to be thrown");
		}
		/// <summary>
		/// <para>Throws an exception with a complex tree of InnerExceptions.</para>
		/// <para>Exception</para>
		/// <para> --> ArgumentOutOfRangeException</para>
		/// <para>      --> AggregateException</para>
		/// <para>          --> AggregateException</para>
		/// <para>              --> IndexOutOfRangeException</para>
		/// <para>              --> ArgumentException</para>
		/// <para>          --> AggregateException</para>
		/// <para>              --> ArithmeticException</para>
		/// </summary>
		private void ThrowComplex()
		{
			try
			{
				try
				{
					try
					{
						throw new AggregateException("L4", new IndexOutOfRangeException("L5"), new ArgumentException("L5"));
					}
					catch (Exception ex)
					{
						throw new AggregateException("L3", ex, new AggregateException(new ArithmeticException("L4")));
					}
				}
				catch (Exception ex)
				{
					throw new ArgumentOutOfRangeException("L2", ex);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("L1", ex);
			}
		}
	}
}
