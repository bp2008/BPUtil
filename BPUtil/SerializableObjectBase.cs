using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BPUtil
{
	public abstract class SerializableObjectBase
	{
		private static ConcurrentDictionary<string, object> fileLocks = new ConcurrentDictionary<string, object>();
		private static object MakeLockKey(string filePath)
		{
			return filePath;
		}
		public bool Save(string filePath = null)
		{
			int tries = 0;
			while (tries++ < 5)
				try
				{
					if (filePath == null)
						filePath = this.GetType().Name + ".cfg";
					object lockObj = fileLocks.GetOrAdd(filePath.ToLower(), MakeLockKey);
					lock (lockObj)
					{
						FileInfo fi = new FileInfo(filePath);
						if (!fi.Exists)
						{
							if (!fi.Directory.Exists)
								Directory.CreateDirectory(fi.Directory.FullName);
						}
						System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(this.GetType());
						using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
							x.Serialize(fs, this);
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
		public bool Load(string filePath = null)
		{
			int tries = 0;
			while (tries++ < 5)
				try
				{
					Type thistype = this.GetType();
					if (filePath == null)
						filePath = thistype.Name + ".cfg";
					object lockObj = fileLocks.GetOrAdd(filePath.ToLower(), MakeLockKey);
					lock (lockObj)
					{
						if (!File.Exists(filePath))
							return false;
						System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(this.GetType());
						object obj;
						using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
							obj = x.Deserialize(fs);
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
		/// (Thread-)Safely checks if the settings file exists, and if not, saves the current instance.
		/// </summary>
		/// <param name="filePath"></param>
		public void SaveIfNoExist(string filePath = null)
		{
			if (filePath == null)
				filePath = this.GetType().Name + ".cfg";
			object lockObj = fileLocks.GetOrAdd(filePath.ToLower(), MakeLockKey);
			lock (lockObj)
			{
				if (!File.Exists(filePath))
					Save(filePath);
			}
		}
	}
}
