using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BPUtil
{
	public static class PrivateAccessor
	{
		private const BindingFlags instance_flags = BindingFlags.Instance | BindingFlags.NonPublic;
		private const BindingFlags static_flags = BindingFlags.Static | BindingFlags.NonPublic;
		/// <summary>
		/// Gets the value of the specified private field of the specified object.
		/// </summary>
		/// <typeparam name="T">The type of the field.</typeparam>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private field.</param>
		/// <returns></returns>
		public static T GetFieldValue<T>(object obj, string name)
		{
			return (T)obj.GetType().GetField(name, instance_flags).GetValue(obj);
		}
		/// <summary>
		/// Sets the value of the specified private field of the specified object.
		/// </summary>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private field.</param>
		/// <param name="value">The value to set.</param>
		public static void SetFieldValue(object obj, string name, object value)
		{
			obj.GetType().GetField(name, instance_flags).SetValue(obj, value);
		}
		/// <summary>
		/// Gets the value of the specified private property of the specified object.
		/// </summary>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private property.</param>
		/// <returns></returns>
		public static T GetPropertyValue<T>(object obj, string name)
		{
			return (T)obj.GetType().GetProperty(name, instance_flags).GetValue(obj, null);
		}
		/// <summary>
		/// Sets the value of the specified private property of the specified object.
		/// </summary>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private property.</param>
		/// <param name="value">The value to set.</param>
		public static void SetPropertyValue(object obj, string name, object value)
		{
			obj.GetType().GetProperty(name, instance_flags).SetValue(obj, value, null);
		}
		/// <summary>
		/// Calls the specified private method of the specified object.
		/// </summary>
		/// <typeparam name="T">The return value type.  If the private method's return type is `void`, simply pass `object` here and the call will return null.</typeparam>
		/// <param name="obj">The object on which to call a private method.</param>
		/// <param name="name">The name of the private method.</param>
		/// <param name="param">Arguments to the private method.</param>
		/// <returns></returns>
		public static T CallMethod<T>(object obj, string name, params object[] param)
		{
			return (T)obj.GetType().GetMethod(name, instance_flags).Invoke(obj, param);
		}
		/// <summary>
		/// Gets the value of the specified private static field.
		/// </summary>
		/// <typeparam name="T">The type of the field.</typeparam>
		/// <param name="t">The type to which the static field belongs.</param>
		/// <param name="name">The name of the private field.</param>
		/// <returns></returns>
		public static T GetStaticFieldValue<T>(Type t, string name)
		{
			return (T)t.GetField(name, static_flags).GetValue(null);
		}
		/// <summary>
		/// Sets the value of the specified private static field.
		/// </summary>
		/// <param name="t">The type to which the static field belongs.</param>
		/// <param name="name">The name of the private field.</param>
		/// <param name="value">The value to set.</param>
		public static void SetStaticFieldValue(Type t, string name, object value)
		{
			t.GetField(name, static_flags).SetValue(null, value);
		}
		/// <summary>
		/// Gets the value of the specified private static property.
		/// </summary>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <param name="t">The type to which the static property belongs.</param>
		/// <param name="name">The name of the private property.</param>
		/// <returns></returns>
		public static T GetStaticPropertyValue<T>(Type t, string name)
		{
			return (T)t.GetProperty(name, static_flags).GetValue(null, null);
		}
		/// <summary>
		/// Sets the value of the specified private static property.
		/// </summary>
		/// <param name="t">The type to which the static property belongs.</param>
		/// <param name="name">The name of the private property.</param>
		/// <param name="value">The value to set.</param>
		public static void SetStaticPropertyValue(Type t, string name, object value)
		{
			t.GetProperty(name, static_flags).SetValue(null, value, null);
		}
		/// <summary>
		/// Calls the specified private static method.
		/// </summary>
		/// <typeparam name="T">The return value type.  If the private method's return type is `void`, simply pass `object` here and the call will return null.</typeparam>
		/// <param name="t">The type to which the static method belongs.</param>
		/// <param name="name">The name of the private method.</param>
		/// <param name="param">Arguments to the private method.</param>
		/// <returns></returns>
		public static T CallStaticMethod<T>(Type t, string name, params object[] param)
		{
			return (T)t.GetMethod(name, instance_flags).Invoke(null, param);
		}
	}
}
