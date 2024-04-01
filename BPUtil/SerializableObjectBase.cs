﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// <para>Any class inheriting from this may be loaded and saved from file easily. Uses <see cref="System.Xml.Serialization.XmlSerializer"/>.</para>
	/// <para>Note that strings stored via this class will have '\r' characters removed by the xml writer.</para>
	/// </summary>
	public abstract class SerializableObjectBase
	{
		private static ConcurrentDictionary<string, object> fileLocks = new ConcurrentDictionary<string, object>();
		private static object MakeLockKey(string filePath)
		{
			return filePath;
		}
		/// <summary>
		/// Saves this instance to file.  Returns true if successful.
		/// </summary>
		/// <param name="filePath">Optional file path. If null, the default file path is used.</param>
		/// <returns></returns>
		public virtual bool Save(string filePath = null)
		{
			int tries = 0;
			while (tries++ < 5)
				try
				{
					if (filePath == null)
						filePath = GetDefaultFilePath();
					object lockObj = fileLocks.GetOrAdd(filePath.ToLower(), MakeLockKey);
					lock (lockObj)
					{
						FileInfo fi = new FileInfo(filePath);
						if (!fi.Exists)
						{
							if (!fi.Directory.Exists)
								Directory.CreateDirectory(fi.Directory.FullName);
						}
						using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
							SerializeObject(this, fs);
					}
					return true;
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex)
				{
					if (tries >= 5)
						Logger.Debug(ex);
					else
						Thread.Sleep(1);
				}
			return false;
		}
		/// <summary>
		/// Saves this instance from file.  Returns true if successful.
		/// </summary>
		/// <param name="filePath">Optional file path. If null, the default file path is used.</param>
		/// <returns></returns>
		public virtual bool Load(string filePath = null)
		{
			int tries = 0;
			while (tries++ < 5)
				try
				{
					Type thistype = this.GetType();
					if (filePath == null)
						filePath = GetDefaultFilePath();
					object lockObj = fileLocks.GetOrAdd(filePath.ToLower(), MakeLockKey);
					lock (lockObj)
					{
						if (!File.Exists(filePath))
							return false;
						object obj;
						using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
							obj = DeserializeObject(fs);
						if (obj != null)
						{
							foreach (FieldInfo sourceField in obj.GetType().GetFields())
							{
								try
								{
									FieldInfo targetField = thistype.GetField(sourceField.Name);
									if (targetField != null && targetField.MemberType == sourceField.MemberType)
										targetField.SetValue(this, sourceField.GetValue(obj));
								}
								catch (ThreadAbortException) { throw; }
								catch (Exception) { }
							}
							if (obj.GetType().GetCustomAttributes(typeof(SerializeProperties), false).FirstOrDefault() != null)
							{
								foreach (PropertyInfo sourceProperty in obj.GetType().GetProperties())
								{
									try
									{
										PropertyInfo targetProperty = thistype.GetProperty(sourceProperty.Name);
										if (targetProperty != null && targetProperty.MemberType == sourceProperty.MemberType)
											targetProperty.SetValue(this, sourceProperty.GetValue(obj));
									}
									catch (ThreadAbortException) { throw; }
									catch (Exception) { }
								}
							}
						}
					}
					return true;
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex)
				{
					if (tries >= 5)
						Logger.Debug(ex);
					else
						Thread.Sleep(1);
				}
			return false;
		}
		/// <summary>
		/// (Thread-)Safely checks if the settings file exists, and returns true if it does.
		/// </summary>
		/// <param name="filePath">Optional file path. If null, the default file path is used.</param>
		public virtual bool FileExists(string filePath = null)
		{
			if (filePath == null)
				filePath = GetDefaultFilePath();
			object lockObj = fileLocks.GetOrAdd(filePath.ToLower(), MakeLockKey);
			lock (lockObj)
				return File.Exists(filePath);
		}
		/// <summary>
		/// (Thread-)Safely checks if the settings file exists, and if not, saves the current instance.  Returns true if a file was saved.
		/// </summary>
		/// <param name="filePath">Optional file path. If null, the default file path is used.</param>
		public virtual bool SaveIfNoExist(string filePath = null)
		{
			if (filePath == null)
				filePath = GetDefaultFilePath();
			object lockObj = fileLocks.GetOrAdd(filePath.ToLower(), MakeLockKey);
			lock (lockObj)
			{
				if (!File.Exists(filePath))
					return Save(filePath);
			}
			return false;
		}

		protected virtual string GetDefaultFilePath()
		{
			return this.GetType().Name + ".cfg";
		}

		/// <summary>
		/// <para>Writes the object to a FileStream.  The default implementation in SerializableObjectBase uses XML.</para>
		/// </summary>
		protected virtual void SerializeObject(object obj, FileStream stream)
		{
			System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(this.GetType());
			x.Serialize(stream, this);
		}
		/// <summary>
		/// <para>Reads the object from a FileStream.  The default implementation in SerializableObjectBase uses XML.</para>
		/// <para>Must return a type inheriting from SerializableObjectBase.</para>
		/// </summary>
		protected virtual SerializableObjectBase DeserializeObject(FileStream stream)
		{
			System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(this.GetType());
			object obj = x.Deserialize(stream);
			return (SerializableObjectBase)obj;
		}
	}
	public abstract class SerializableObjectJson : SerializableObjectBase
	{
		/// <summary>
		/// <para>Writes the object to a FileStream using JSON.</para>
		/// </summary>
		protected override void SerializeObject(object obj, FileStream stream)
		{
			using (StreamWriter sw = new StreamWriter(stream, ByteUtil.Utf8NoBOM))
			{
				sw.Write(SerializeToJson(obj));
			}
		}
		/// <summary>
		/// <para>Reads the object from a FileStream using JSON.</para>
		/// </summary>
		protected override SerializableObjectBase DeserializeObject(FileStream stream)
		{
			using (StreamReader sr = new StreamReader(stream, ByteUtil.Utf8NoBOM))
			{
				string str = sr.ReadToEnd();
				return (SerializableObjectBase)DeserializeFromJson(str);
			}
		}

		protected override string GetDefaultFilePath()
		{
			return this.GetType().Name + ".json";
		}
		/// <summary>
		/// <para>Converts an object to a JSON string. Must be implemented by classes deriving from SerializableObjectJson.</para>
		/// <para>e.g. JsonConvert.SerializeObject</para>
		/// </summary>
		protected abstract string SerializeToJson(object obj);
		/// <summary>
		/// <para>Converts a JSON string to an object. Must be implemented by classes deriving from SerializableObjectJson.</para>
		/// <para>e.g.JsonConvert.DeserializeObject</para>
		/// <para>You must deserialize to an object of the derived type.</para>
		/// </summary>
		protected abstract SerializableObjectJson DeserializeFromJson(string str);
	}
	/// <summary>
	/// Annotate the serializable object with this in order to load serialized properties (otherwise only fields are loaded from file).
	/// </summary>
	public class SerializeProperties : Attribute
	{
		public SerializeProperties() { }
	}
}
