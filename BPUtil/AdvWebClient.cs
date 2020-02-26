using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace BPUtil
{
	/// <summary>
	/// <para>A WebClient with custom extensions:</para>
	/// <list type="bullet">
	/// <item>Settable request timeout.</item>
	/// <item>Settable request cookie collection.</item>
	/// </list>
	/// </summary>
	[System.ComponentModel.DesignerCategory("Code")]
	public class AdvWebClient : WebClient
	{
		/// <summary>
		/// Cookies to include in requests.  May be null.
		/// </summary>
		public CookieContainer CookieContainer { get; private set; }
		/// <summary>
		/// Number of milliseconds a request can remain open (without a response?) before it times out. If null, the default HttpWebRequest. Timeout will be kept.
		/// </summary>
		public int? Timeout;
		/// <summary>
		/// Number of milliseconds a request can remain open (without a response?) before it times out. If null, the default HttpWebRequest. Timeout will be kept.
		/// </summary>
		public TimeSpan? TimeoutTime
		{
			get
			{
				if (Timeout == null)
					return null;
				else
					return TimeSpan.FromMilliseconds(Timeout.Value);
			}
			set
			{
				if (value == null)
					Timeout = null;
				else
					Timeout = (int)value.Value.TotalMilliseconds;
			}
		}
		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest _req = base.GetWebRequest(address);
			HttpWebRequest request = (HttpWebRequest)_req;
			if (CookieContainer != null)
				request.CookieContainer = CookieContainer;
			if (Timeout != null)
				request.Timeout = Timeout.Value;
			return request;
		}
	}
	/// <summary>
	/// A Cookie-aware WebClient that will store authentication cookie information and persist it through subsequent requests.
	/// From: https://github.com/rionmonster/CookieAwareWebClient
	/// Base functionality rolled into AdvWebClient.
	/// </summary>
	[System.ComponentModel.DesignerCategory("Code")]
	[Obsolete("Use AdvWebClient instead.")]
	public class CookieAwareWebClient : AdvWebClient
	{
	}
}
