using System;
using System.Collections;
using System.Collections.Generic;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Reperesents one HTTP Cookie.
	/// </summary>
	public class Cookie
	{
		/// <summary>
		/// Name of the cookie.
		/// </summary>
		public string name;
		/// <summary>
		/// Value of the cookie.
		/// </summary>
		public string value;
		/// <summary>
		/// Expiration time of the cookie.
		/// </summary>
		public TimeSpan expire;

		/// <summary>
		/// Creates a new HTTP Cookie.
		/// </summary>
		/// <param name="name">Name of the cookie.</param>
		/// <param name="value">Value of the cookie.</param>
		/// <param name="expire">Expiration time of the cookie.</param>
		public Cookie(string name, string value, TimeSpan expire)
		{
			this.name = name;
			this.value = value;
			this.expire = expire;
		}
	}
	/// <summary>
	/// A class for managing a collection of HTTP cookies.
	/// </summary>
	public class Cookies : IEnumerable<Cookie>
	{
		SortedList<string, Cookie> cookieCollection = new SortedList<string, Cookie>();
		/// <summary>
		/// Adds or updates a cookie with the specified name and value.  The cookie is set to expire immediately at the end of the browsing session.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		public void Add(string name, string value)
		{
			Add(name, value, TimeSpan.Zero);
		}
		/// <summary>
		/// Adds or updates a cookie with the specified name, value, and lifespan.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		/// <param name="expireTime">The amount of time before the cookie should expire.</param>
		public void Add(string name, string value, TimeSpan expireTime)
		{
			if (name == null)
				return;
			cookieCollection[name.ToLower()] = new Cookie(name, value, expireTime);
		}
		/// <summary>
		/// Gets the cookie with the specified name.  If the cookie is not found, null is returned;
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		/// <returns></returns>
		public Cookie Get(string name)
		{
			Cookie cookie;
			if (!cookieCollection.TryGetValue(name.ToLower(), out cookie))
				cookie = null;
			return cookie;
		}
		/// <summary>
		/// Gets the value of the cookie with the specified name.  If the cookie is not found, an empty string is returned;
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		/// <returns></returns>
		public string GetValue(string name)
		{
			Cookie cookie = Get(name);
			if (cookie == null)
				return "";
			return cookie.value;
		}
		/// <summary>
		/// Deletes the cookie with the specified name, returning true if the cookie was removed, false if it did not exist or was not removed.
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		public bool Remove(string name)
		{
			if (name != null)
				return cookieCollection.Remove(name.ToLower());
			return false;
		}
		/// <summary>
		/// Returns a string of "Set-Cookie: ..." headers (one for each cookie in the collection) separated by "\r\n".  There is no leading or trailing "\r\n".
		/// </summary>
		/// <returns>A string of "Set-Cookie: ..." headers (one for each cookie in the collection) separated by "\r\n".  There is no leading or trailing "\r\n".</returns>
		public override string ToString()
		{
			List<string> cookiesStr = new List<string>();
			foreach (Cookie cookie in cookieCollection.Values)
				cookiesStr.Add("Set-Cookie: " + cookie.name + "=" + cookie.value + (cookie.expire == TimeSpan.Zero ? "" : "; Max-Age=" + (long)cookie.expire.TotalSeconds) + "; Path=/");
			return string.Join("\r\n", cookiesStr);
		}
		/// <summary>
		/// Returns a Cookies instance populated by parsing the specified string.  The string should be the value of the "Cookie" header that was received from the remote client.  If the string is null or empty, an empty cookies collection is returned.
		/// </summary>
		/// <param name="str">The value of the "Cookie" header sent by the remote client.</param>
		/// <returns></returns>
		public static Cookies FromString(string str)
		{
			Cookies cookies = new Cookies();
			if (str == null)
				return cookies;
			str = Uri.UnescapeDataString(str);
			string[] parts = str.Split(';');
			for (int i = 0; i < parts.Length; i++)
			{
				int idxEquals = parts[i].IndexOf('=');
				if (idxEquals < 1)
					continue;
				string name = parts[i].Substring(0, idxEquals).Trim();
				string value = parts[i].Substring(idxEquals + 1).Trim();
				cookies.Add(name, value);
			}
			return cookies;
		}
		IEnumerator<Cookie> IEnumerable<Cookie>.GetEnumerator()
		{
			return cookieCollection.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return cookieCollection.Values.GetEnumerator();
		}
	}
}