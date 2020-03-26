using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BPUtil
{
	/// <summary>
	/// <para>A class which calculates the number of bytes required to store an object in memory.</para>
	/// <para>Important Notes:</para>
	/// <para>
	/// <list type="bullet">
	/// <item>Returned sizes are approximate. .NET is a complex framework and I do not know all the intricacies.</item>
	/// <item>It is assumed there are bugs in this class.  Please test this class with your object types before using it in published code.</item>
	/// <item>When using this with types that have not been accommodated, stack overflows and other exceptions may occur, or the accuracy of the calculated size may be wildly inaccurate.</item>
	/// <item>This class uses Reflection and is therefore not very fast.</item>
	/// </list>
	/// </para>
	/// </summary>
	public static class ObjectSize
	{
		/// <summary>
		/// The size in bytes of an object reference in the current process. (8 or 4)
		/// </summary>
		public static readonly int ReferenceSize = Marshal.SizeOf(typeof(IntPtr));
		private const BindingFlags findFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		/// <summary>
		/// Returns the size in bytes of this value or reference type instance. Types passed in should be relatively simple.  Exceptions may be thrown if certain unsupported value types are used.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long SizeOf(object obj)
		{
			if (obj == null)
				return 0;

			Type t = obj.GetType();

			if (t.IsPointer)
				return ReferenceSize;

			long size = 0;

			if (typeof(string).IsAssignableFrom(t))
				size += ((string)obj).Length * Marshal.SystemDefaultCharSize;

			foreach (FieldInfo fi in t.GetFields(findFields))
			{
				if (fi.FieldType.IsPrimitive)
					size += SizeOfPrimitiveType(fi.FieldType);
				else if (fi.FieldType.IsValueType)
					size += SizeOfValueType(fi.GetValue(obj));
				else if (fi.FieldType.IsPointer)
					size += ReferenceSize;
				else
				{
					size += ReferenceSize;
					size += SizeOf(fi.GetValue(obj));
				}
			}

			if (t.IsArray)
			{
				Array arr = (Array)obj;
				Type tEle = t.GetElementType();
				if (tEle.IsPrimitive)
					size += SizeOfPrimitiveType(tEle) * arr.LongLength;
				else
				{
					if (!tEle.IsValueType)
						size += ReferenceSize * arr.LongLength;
					foreach (object item in arr)
						size += SizeOf(item);
				}
			}

			return size;
		}
		/// <summary>
		/// Returns the size in bytes of a value of a primitive type.
		/// </summary>
		/// <param name="t">A primitive type</param>
		/// <returns></returns>
		private static long SizeOfPrimitiveType(Type t)
		{
			if (t == typeof(bool))
				return 1;
			if (t == typeof(char))
				return 2;
			return Marshal.SizeOf(t);
		}
		/// <summary>
		/// Returns the size in bytes of this value.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		private static long SizeOfValueType(object obj)
		{
			if (obj is DateTime)
				return 8; // <- Guess
			return Marshal.SizeOf(obj);
		}
	}
}
