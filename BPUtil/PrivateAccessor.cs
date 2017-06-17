using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BPUtil
{
	public static class PrivateAccessor
	{
		private const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
		/// <summary>
		/// Gets the value of the specified private field of the specified object.
		/// </summary>
		/// <typeparam name="T">The type of the field.</typeparam>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private field.</param>
		/// <returns></returns>
		public static T GetFieldValue<T>(object obj, string name)
		{
			return (T)obj.GetType().GetField(name, flags).GetValue(obj);
		}
		/// <summary>
		/// Gets the value of the specified private property of the specified object.
		/// </summary>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private field.</param>
		/// <param name="value">The value to set.</param>
		public static void SetFieldValue(object obj, string name, object value)
		{
			obj.GetType().GetField(name, flags).SetValue(obj, value);
		}
		/// <summary>
		/// Sets the value of the specified private field of the specified object.
		/// </summary>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private property.</param>
		/// <returns></returns>
		public static T GetPropertyValue<T>(object obj, string name)
		{
			return (T)obj.GetType().GetProperty(name, flags).GetValue(obj, null);
		}
		/// <summary>
		/// Sets the value of the specified private property of the specified object.
		/// </summary>
		/// <param name="obj">The object to change.</param>
		/// <param name="name">The name of the private property.</param>
		/// <param name="value">The value to set.</param>
		public static void SetPropertyValue(object obj, string name, object value)
		{
			obj.GetType().GetProperty(name, flags).SetValue(obj, value, null);
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
			return (T)obj.GetType().GetMethod(name, flags).Invoke(obj, param);
		}
	}
}
