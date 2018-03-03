using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace BPUtil
{
	/// <summary>
	/// Contains HTTP response data.  This is returned by GET and POST methods in <see cref="BPUtil.WebRequestUtility"/>.
	/// </summary>
	public class BpWebResponse
	{
		private static Encoding Utf8NoBOM = new UTF8Encoding(false);
		/// <summary>
		/// The raw response payload as a byte array.
		/// </summary>
		public byte[] data;
		/// <summary>
		/// A collection of http response headers.  Keys are case sensitive.
		/// </summary>
		public SortedList<string, string> headers;
		/// <summary>
		/// The value of the Content Type response header.
		/// </summary>
		public string ContentType;
		/// <summary>
		/// The HTTP status code of the response.  200 is normal success, 404 is Not Found, and so on.
		/// </summary>
		public int StatusCode = 0;
		/// <summary>
		/// A cache for the string value of the response.  This is populated the first time [str] is requested.
		/// </summary>
		private string _str;
		/// <summary>
		/// The remote IP address of the server, in case a DNS hostname was used in the URL.
		/// </summary>
		public IPAddress remoteIp;
		/// <summary>
		/// Returns the response in string format.
		/// </summary>
		/// <remarks>The response is assumed to be UTF8-formatted string data.</remarks>
		public string str
		{
			get
			{
				if (_str == null && data != null)
					_str = Utf8NoBOM.GetString(data);
				if (_str == null)
					return "";
				return _str;
			}
		}
		public BpWebResponse()
		{
			headers = new SortedList<string, string>();
		}
	}
	/// <summary>
	/// Provides HTTP GET and POST methods which are useful in situations where the WebClient class falls short.
	/// </summary>
	public class WebRequestUtility
	{
		private static Encoding basicAuthEncoding = Encoding.GetEncoding("ISO-8859-1");
		string UserAgentString;
		/// <summary>
		/// Time in milliseconds to wait for web responses.
		/// </summary>
		public int requestTimeout = 600000; // 10 minutes
											/// <summary>
											/// If provided, all web requests will attempt basic authentication using these credentials.
											/// </summary>
		public NetworkCredential BasicAuthCredentials;

		//public bool skipCertificateValidation = false;
		public WebRequestUtility(string UserAgentString, int requestTimeout = 600000)
		{
			this.UserAgentString = UserAgentString;
			this.requestTimeout = requestTimeout;
		}

		/// <summary>
		/// Performs an HTTP GET request.
		/// </summary>
		/// <param name="url">The url to GET.</param>
		/// <param name="headers"><para>An array of strings containing header names and values. The array should be populated in the order of "name", "value", "name", "value", and so on.</para>
		/// <para>
		/// For example:
		/// </para>
		/// <code>new string[] { "HeaderNameOne", "Header Value One!", "HeaderNameTwo", "Header Value Two!", "User-Agent", "Mozilla" }</code></param>
		/// <param name="earlyTerminationBytes">(Advanced use) If specified, the connection will be dropped as soon as this many bytes are read, and this much data will be returned. If the full response is shorter than this, then the full response will be returned.</param>
		/// <returns></returns>
		public BpWebResponse GET(string url, string[] headers = null, int earlyTerminationBytes = int.MaxValue)
		{
			return internal_GET_or_POST(url, null, null, headers, earlyTerminationBytes);
		}
		/// <summary>
		/// Performs an HTTP POST request, sending key and value strings to the server using the content type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="url">The url to POST.</param>
		/// <param name="keysAndValues">An array of strings containing parameter names and values. The array should be populated in the order of "name", "value", "name", "value", and so on.
		/// <para>
		/// For example:
		/// </para>
		/// <code>new string[] { "ParamOne", "Value One!", "ParamTwo", "Value Two!" }</code></param>
		/// <param name="headers">An array of strings containing header names and values. The array should be populated in the order of "name", "value", "name", "value", and so on.
		/// <para>
		/// For example:
		/// </para>
		/// <code>new string[] { "HeaderNameOne", "Header Value One!", "HeaderNameTwo", "Header Value Two!", "User-Agent", "Mozilla" }</code></param>
		/// <param name="earlyTerminationBytes">If specified, the connection will be dropped as soon as this many bytes are read, and this much data will be returned. If the full response is shorter than this, then the full response will be returned.</param>
		/// <returns></returns>
		public BpWebResponse POST(string url, string[] keysAndValues, string[] headers = null, int earlyTerminationBytes = int.MaxValue)
		{
			byte[] postBody = null;
			string contentType = null;
			if (keysAndValues != null && keysAndValues.Length > 1 && keysAndValues.Length % 2 == 0)
			{
				contentType = "application/x-www-form-urlencoded";
				List<string> args = new List<string>();
				for (int i = 0; i + 1 < keysAndValues.Length; i += 2)
					args.Add(Uri.EscapeDataString(keysAndValues[i]) + "=" + Uri.EscapeDataString(keysAndValues[i + 1]));
				string content = string.Join("&", args);
				postBody = Encoding.UTF8.GetBytes(content);
			}
			return internal_GET_or_POST(url, postBody, contentType, headers, earlyTerminationBytes);
		}
		/// <summary>
		/// Performs an HTTP POST request, sending the specified body content.
		/// </summary>
		/// <param name="url">The url to POST.</param>
		/// <param name="postBody">The content to post.</param>
		/// <param name="earlyTerminationBytes">If specified, the connection will be dropped as soon as this many bytes are read, and this much data will be returned. If the full response is shorter than this, then the full response will be returned.</param>
		///<param name="contentType">The value of the content-type header to set.</param>
		///<param name="headers">Additional header keys and values to set in the request, provided as an array of strings ordered as [key, value, key, value] and so on. e.g.: { "User-Agent", "Mozilla", "Server", "BPUtil" }</param>
		/// <returns></returns>
		public BpWebResponse POST(string url, byte[] postBody, string contentType, string[] headers = null, int earlyTerminationBytes = int.MaxValue)
		{
			return internal_GET_or_POST(url, postBody, contentType, headers, earlyTerminationBytes);
		}
		protected virtual BpWebResponse internal_GET_or_POST(string url, byte[] postBody, string contentType, string[] headers, int earlyTerminationBytes)
		{
			BpWebResponse response = new BpWebResponse();

			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
			webRequest.Timeout = requestTimeout;
			webRequest.Proxy = null;
			//if (skipCertificateValidation)
			//	webRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
			if (!string.IsNullOrEmpty(UserAgentString))
				webRequest.UserAgent = UserAgentString;

			if (headers != null)
			{
				for (int i = 0; i + 1 < headers.Length; i += 2)
				{
					string key = headers[i];
					string keyLower = key.ToLower();
					string value = headers[i + 1];
					if (keyLower == "user-agent")
						webRequest.UserAgent = value;
					else if (key == "STARTBYTE")
						webRequest.AddRange(int.Parse(value));
					else
						webRequest.Headers[key] = value;
				}
			}
			if (BasicAuthCredentials != null)
			{
				webRequest.Credentials = BasicAuthCredentials;
				webRequest.PreAuthenticate = true;
			}
			try
			{
				if (postBody != null && postBody.Length > 1 && !string.IsNullOrWhiteSpace(contentType))
				{
					webRequest.Method = "POST";
					webRequest.ContentType = contentType;
					webRequest.ContentLength = postBody.Length;
					using (Stream reqStream = webRequest.GetRequestStream())
					{
						reqStream.Write(postBody, 0, postBody.Length);
					}
				}

				using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
				{
					using (MemoryStream ms = new MemoryStream())
					{
						using (Stream responseStream = webResponse.GetResponseStream())
						{
							foreach (string key in webResponse.Headers.AllKeys)
								response.headers[key] = webResponse.Headers[key];
							response.ContentType = webResponse.ContentType;
							response.StatusCode = (int)webResponse.StatusCode;
							// Dump the response stream into the MemoryStream ms
							int bytesRead = 1;
							byte[] buffer = new byte[8000];
							while (bytesRead > 0)
							{
								if (earlyTerminationBytes - ms.Length < buffer.Length)
									buffer = new byte[earlyTerminationBytes - ms.Length];

								bytesRead = responseStream.Read(buffer, 0, buffer.Length);
								if (bytesRead > 0)
									ms.Write(buffer, 0, bytesRead);
								if (ms.Length >= earlyTerminationBytes)
									break;
							}
							// Dump the data into the byte array
							response.data = ms.ToArray();
						}
					}
				}
			}
			catch (WebException ex)
			{
				if (ex.Response == null)
					response.StatusCode = 0;
				else
					response.StatusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
			}
			return response;
		}
	}
}
