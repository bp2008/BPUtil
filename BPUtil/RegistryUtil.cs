#if NETFRAMEWORK || NET6_PLUS_WIN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace BPUtil
{
	/// <summary>
	/// Provides utility methods for accessing the Windows Registry.
	/// </summary>
	public static class RegistryUtil
	{
		/// <summary>
		/// Set = true to read entries written by 32 bit programs on 64 bit Windows.
		/// 
		/// On 32 bit Windows, this setting has no effect.
		/// </summary>
		public static bool Force32BitRegistryAccess = false;

		/// <summary>
		/// Gets HKEY_LOCAL_MACHINE in either the 32 or 64 bit view depending on RegistryUtil configuration and OS version.
		/// </summary>
		/// <returns></returns>
		public static RegistryKey HKLM
		{
			get
			{
				if (!Force32BitRegistryAccess && Environment.Is64BitOperatingSystem)
					return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
				else
					return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
			}
		}
		/// <summary>
		/// Gets HKEY_CURRENT_USER in either the 32 or 64 bit view depending on RegistryUtil configuration and OS version.
		/// </summary>
		/// <returns></returns>
		public static RegistryKey HKCU
		{
			get
			{
				if (!Force32BitRegistryAccess && Environment.Is64BitOperatingSystem)
					return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
				else
					return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
			}
		}

		/// <summary>
		/// Returns the requested RegistryKey or null if the key does not exist.
		/// </summary>
		/// <param name="path">A path relative to HKEY_LOCAL_MACHINE.  E.g. "SOFTWARE\\Microsoft"</param>
		/// <returns></returns>
		public static RegistryKey GetHKLMKey(string path)
		{
			return HKLM.OpenSubKey(path);
		}
		/// <summary>
		/// Returns the requested RegistryKey or null if the key does not exist.
		/// </summary>
		/// <param name="path">A path relative to HKEY_CURRENT_USER.  E.g. "SOFTWARE\\Microsoft"</param>
		/// <returns></returns>
		public static RegistryKey GetHKCUKey(string path)
		{
			return HKCU.OpenSubKey(path);
		}

		/// <summary>
		/// Gets the value of a registry key in HKEY_LOCAL_MACHINE.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path">A path relative to HKEY_LOCAL_MACHINE.  E.g. "SOFTWARE\\Microsoft"</param>
		/// <param name="key">Key</param>
		/// <param name="defaultValue">Value to return if the key does not exist.</param>
		/// <returns></returns>
		public static T GetHKLMValue<T>(string path, string key, T defaultValue)
		{
			object value = HKLM.OpenSubKey(path)?.GetValue(key);
			if (value == null)
				return defaultValue;
			return (T)value;
		}
		/// <summary>
		/// Gets the value of a registry key in HKEY_CURRENT_USER.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path">A path relative to HKEY_CURRENT_USER.  E.g. "SOFTWARE\\Microsoft"</param>
		/// <param name="key">Key</param>
		/// <param name="defaultValue">Value to return if the key does not exist.</param>
		/// <returns></returns>
		public static T GetHKCUValue<T>(string path, string key, T defaultValue)
		{
			object value = HKCU.OpenSubKey(path)?.GetValue(key);
			if (value == null)
				return defaultValue;
			return (T)value;
		}

		/// <summary>
		/// Attempts to set the value of the specified registry key, throwing an exception if it fails.
		/// </summary>
		/// <param name="path">Path to the folder where the key is located, relative to HKEY_LOCAL_MACHINE.</param>
		/// <param name="key">Name of the key to set the value of.</param>
		/// <param name="value">Value to set.</param>
		/// <param name="valueKind">The type of value stored in this registry key.</param>
		/// <returns></returns>
		public static void SetHKLMValue(string path, string key, object value, RegistryValueKind valueKind = RegistryValueKind.Unknown)
		{
			RegistryKey sk = HKLM.CreateSubKey(path);
			if (valueKind == RegistryValueKind.Unknown)
				sk.SetValue(key, value);
			sk.SetValue(key, value, valueKind);
		}
		/// <summary>
		/// Attempts to set the value of the specified registry key, throwing an exception if it fails.
		/// </summary>
		/// <param name="path">Path to the folder where the key is located, relative to HKEY_CURRENT_USER.</param>
		/// <param name="key">Name of the key to set the value of.</param>
		/// <param name="value">Value to set.</param>
		/// <param name="valueKind">The type of value stored in this registry key.</param>
		/// <returns></returns>
		public static void SetHKCUValue(string path, string key, object value, RegistryValueKind valueKind = RegistryValueKind.Unknown)
		{
			RegistryKey sk = HKCU.CreateSubKey(path);
			if (valueKind == RegistryValueKind.Unknown)
				sk.SetValue(key, value);
			sk.SetValue(key, value, valueKind);
		}

		/// <summary>
		/// Attempts to set the value of the specified registry key, returning true if successful or false if not.
		/// </summary>
		/// <param name="path">Path to the folder where the key is located, relative to HKEY_LOCAL_MACHINE.</param>
		/// <param name="key">Name of the key to set the value of.</param>
		/// <param name="value">Value to set.</param>
		/// <param name="valueKind">The type of value stored in this registry key.</param>
		/// <returns></returns>
		public static bool SetHKLMValueSafe(string path, string key, object value, RegistryValueKind valueKind = RegistryValueKind.Unknown)
		{
			try
			{
				SetHKLMValue(path, key, value, valueKind);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Attempts to set the value of the specified registry key, returning true if successful or false if not.
		/// </summary>
		/// <param name="path">Path to the folder where the key is located, relative to HKEY_CURRENT_USER.</param>
		/// <param name="key">Name of the key to set the value of.</param>
		/// <param name="value">Value to set.</param>
		/// <param name="valueKind">The type of value stored in this registry key.</param>
		/// <returns></returns>
		public static bool SetHKCUValueSafe(string path, string key, object value, RegistryValueKind valueKind = RegistryValueKind.Unknown)
		{
			try
			{
				SetHKCUValue(path, key, value, valueKind);
				return true;
			}
			catch
			{
				return false;
			}
		}
		public static string GetStringValue(RegistryKey key, string name)
		{
			object obj = key.GetValue(name);
			if (obj == null)
				return "";
			return obj.ToString();
		}
		public static int GetIntValue(RegistryKey key, string name, int defaultValue)
		{
			object obj = key.GetValue(name);
			if (obj == null)
				return defaultValue;
			if (typeof(int).IsAssignableFrom(obj.GetType()))
				return (int)obj;
			int val;
			if (int.TryParse(obj.ToString(), out val))
				return val;
			return defaultValue;
		}
		public static long GetLongValue(RegistryKey key, string name, long defaultValue)
		{
			//if (ThrowWhenReadingIncompatibleValueTypes)
			//{
			//	RegistryValueKind kind = key.GetValueKind(name);
			//	if (kind != RegistryValueKind.QWord)
			//		throw new Exception("Type of \"" + key.Name + "/" + name + "\" is " + kind + ". Expected QWord.");
			//}
			object obj = key.GetValue(name);
			if (obj == null)
				return defaultValue;
			if (typeof(long).IsAssignableFrom(obj.GetType()))
				return (long)obj;
			long val;
			if (long.TryParse(obj.ToString(), out val))
				return val;
			return defaultValue;
		}
	}
	/// <summary>
	/// Provides a cleaner interface for reading registry values from a RegistryKey.
	/// </summary>
	public class RegEdit
	{
		public readonly RegistryKey key;
		/// <summary>
		/// If true, values must already exist and be the expected type, or else an exception will be thrown.
		/// </summary>
		public bool typeCheck = false;
		public RegEdit(RegistryKey key)
		{
			this.key = key;
		}
		/// <summary>
		/// Reads a String value.
		/// </summary>
		/// <param name="name">Case-insensitive value name.</param>
		/// <returns></returns>
		public string String(string name)
		{
			if (typeCheck)
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.String)
					throw new Exception("Type of \"" + key.Name + "/" + name + "\" is " + kind + ". Expected String.");
			}
			else if (key == null || !key.GetValueNames().Contains(name, true))
				return null;
			else
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.String)
					return null;
			}
			return (string)this.key.GetValue(name);
		}
		/// <summary>
		/// Reads a DWord (32 bit integer) value.
		/// </summary>
		/// <param name="name">Case-insensitive value name.</param>
		/// <returns></returns>
		public int DWord(string name)
		{
			if (typeCheck)
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.DWord)
					throw new Exception("Type of \"" + key.Name + "/" + name + "\" is " + kind + ". Expected DWord.");
			}
			else if (key == null || !key.GetValueNames().Contains(name, true))
				return 0;
			else
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.DWord)
					return 0;
			}
			return (int)this.key.GetValue(name);
		}
		/// <summary>
		/// Reads a QWord (64 bit integer) value.
		/// </summary>
		/// <param name="name">Case-insensitive value name.</param>
		/// <returns></returns>
		public long QWord(string name)
		{
			if (typeCheck)
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.QWord)
					throw new Exception("Type of \"" + key.Name + "/" + name + "\" is " + kind + ". Expected QWord.");
			}
			else if (key == null || !key.GetValueNames().Contains(name, true))
				return 0;
			else
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.QWord)
					return 0;
			}
			return (long)this.key.GetValue(name);
		}
		/// <summary>
		/// Writes a String value.
		/// </summary>
		/// <param name="name">Value name</param>
		/// <param name="value"></param>
		public void String(string name, string value)
		{
			if (typeCheck)
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.String)
					throw new Exception("Type of \"" + key.Name + "/" + name + "\" is " + kind + ". Expected String.");
			}
			if (value == null)
				value = "";
			this.key.SetValue(name, value, RegistryValueKind.String);
		}
		/// <summary>
		/// Writes a DWord (32 bit integer) value.
		/// </summary>
		/// <param name="name">Value name</param>
		/// <param name="value"></param>
		public void DWord(string name, int value)
		{
			if (typeCheck)
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.DWord)
					throw new Exception("Type of \"" + key.Name + "/" + name + "\" is " + kind + ". Expected DWord.");
			}
			this.key.SetValue(name, value, RegistryValueKind.DWord);
		}
		/// <summary>
		/// Writes a QWord (64 bit integer) value.
		/// </summary>
		/// <param name="name">Value name</param>
		/// <param name="value"></param>
		public void QWord(string name, long value)
		{
			if (typeCheck)
			{
				RegistryValueKind kind = key.GetValueKind(name);
				if (kind != RegistryValueKind.QWord)
					throw new Exception("Type of \"" + key.Name + "/" + name + "\" is " + kind + ". Expected QWord.");
			}
			this.key.SetValue(name, value, RegistryValueKind.QWord);
		}
	}
}

#endif