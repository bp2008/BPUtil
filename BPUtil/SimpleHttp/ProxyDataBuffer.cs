using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Contains a list of ProxyDataItems in the order that they were proxied.
	/// </summary>
	public class ProxyDataBuffer
	{
		public List<ProxyDataItem> Items = new List<ProxyDataItem>();
		public void AddItem(ProxyDataItem item)
		{
			lock (Items)
				Items.Add(item);
		}
		public override string ToString()
		{
			return string.Join("", Items.Select(i => i.ToString()));
		}
	}
	/// <summary>
	/// Represents a chunk of data that was proxied to or from a remote server.
	/// </summary>
	public class ProxyDataItem
	{
		/// <summary>
		/// The direction that this ProxyDataItem was traveling.
		/// </summary>
		public ProxyDataDirection Direction;
		private byte[] _payloadBytes;
		private string _payloadString;
		/// <summary>
		/// The payload of this ProxyDataItem as a byte array.  If this payload was sourced as a string, it will be converted to a byte array each time you access this property.  The string encoding is treated as UTF8 without a byte order mark.
		/// </summary>
		public byte[] PayloadBytes
		{
			get
			{
				if (_payloadBytes == null && _payloadString != null)
					return HttpProcessor.Utf8NoBOM.GetBytes(_payloadString);
				return _payloadBytes;
			}
		}
		/// <summary>
		/// The payload of this ProxyDataItem as a string.  If this payload was sourced as a byte array, it will be converted to a string each time you access this property.  The string encoding is treated as UTF8 without a byte order mark.
		/// </summary>
		public string PayloadAsString
		{
			get
			{
				if (_payloadString == null && _payloadBytes != null)
					return HttpProcessor.Utf8NoBOM.GetString(_payloadBytes);
				return _payloadString;
			}
		}
		public ProxyDataItem()
		{
		}
		/// <summary>
		/// Creates a ProxyDataItem with a byte array payload.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="payload"></param>
		public ProxyDataItem(ProxyDataDirection direction, byte[] payload)
		{
			Direction = direction;
			_payloadBytes = payload;
		}
		/// <summary>
		/// Creates a ProxyDataItem with a string payload.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="payload"></param>
		public ProxyDataItem(ProxyDataDirection direction, string payload)
		{
			Direction = direction;
			_payloadString = payload;
		}
		public override string ToString()
		{
			return (Direction == ProxyDataDirection.RequestToServer ? "< " : "> ") + PayloadAsString;
		}
	}
	public enum ProxyDataDirection
	{
		/// <summary>
		/// RequestToServer indicates that the Payload is part of the data sent to the server.
		/// </summary>
		RequestToServer,
		/// <summary>
		/// ResponseFromServer indicates that the Payload is part of a response from the server.
		/// </summary>
		ResponseFromServer
	}
}
