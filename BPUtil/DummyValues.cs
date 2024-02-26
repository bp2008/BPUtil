using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BPUtil
{
	/// <summary>
	/// A static class which offers a method for setting dummy values to the public fields and properties of any object.
	/// </summary>
	public static class DummyValues
	{
		/// <summary>
		/// Sets dummy values for all public fields and properties of an object.
		/// </summary>
		/// <param name="obj">The object to set dummy values for.</param>
		public static void SetDummyValues(object obj)
		{
			Type type = obj.GetType();

			foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				field.SetValue(obj, GetDummyValue(field.FieldType));
			}

			foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				prop.SetValue(obj, GetDummyValue(prop.PropertyType));
			}
		}

		/// <summary>
		/// Returns a dummy value for a given type.
		/// </summary>
		/// <param name="type">The type to get a dummy value for.</param>
		/// <returns>A dummy value for the given type.</returns>
		public static object GetDummyValue(Type type)
		{
			if (type == typeof(bool))
				return true;
			else if (type == typeof(string))
				return "test";
			else if (type == typeof(sbyte))
				return (sbyte)1;
			else if (type == typeof(byte))
				return (byte)2;
			else if (type == typeof(short))
				return (short)3;
			else if (type == typeof(ushort))
				return (ushort)4;
			else if (type == typeof(int))
				return (int)5;
			else if (type == typeof(uint))
				return (uint)6;
			else if (type == typeof(long))
				return 7L;
			else if (type == typeof(ulong))
				return 8UL;
			else if (type == typeof(float))
				return 9.0f;
			else if (type == typeof(double))
				return 10.0d;
			else if (type == typeof(decimal))
				return 11.0m;
			else if (type.IsEnum)
				return Enum.ToObject(type, 12);
			else if (type.IsArray)
			{
				Type elementType = type.GetElementType();
				Array arr = Array.CreateInstance(elementType, 2);
				arr.SetValue(GetDummyValue(elementType), 0);
				arr.SetValue(GetDummyValue(elementType), 1);
				return arr;
			}
			else if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				Type itemType = type.GetElementType() ?? type.GetGenericArguments()[0];
				IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
				list.Add(GetDummyValue(itemType));
				list.Add(GetDummyValue(itemType));
				return list;
			}
			else if (!type.IsValueType)
			{
				object obj = Activator.CreateInstance(type);
				SetDummyValues(obj);
				return obj;
			}
			else
			{
				throw new Exception("DummyValues does not know how to handle type " + type.FullName);
			}
		}
	}
}
