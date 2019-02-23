using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
		/// The HTTP status code of the response.  200 is normal success, 404 is Not Found, and so on. 0 indicates an exception was caught, and could indicate DNS resolution failure or connection failure.
		/// </summary>
		public int StatusCode = 0;
		/// <summary>
		/// A cache for the string value of the response.  This is populated the first time <see cref="str"/> is requested.
		/// </summary>
		private string _str;
		/// <summary>
		/// The remote IP address of the server, in case a DNS hostname was used in the URL.
		/// </summary>
		public IPAddress remoteIp;
		/// <summary>
		/// If an exception was caught, it will be here.
		/// </summary>
		public Exception ex;
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
		string UserAgentString;
		/// <summary>
		/// Time in milliseconds to wait for web responses. Default is 10 minutes.
		/// </summary>
		public int requestTimeout = 600000;
		/// <summary>
		/// <para>You can provide a proxy server here, if desired. Example: new WebProxy("127.0.0.1", 8888)</para>
		/// <seealso cref="IWebProxy"/>
		/// <seealso cref="WebProxy"/>
		/// </summary>
		public IWebProxy proxy = null;
		/// <summary>
		/// If true, <see cref="proxy"/> is ignored and the <seealso cref="HttpClient"/> will use automatic proxy settings.  Default: false
		/// </summary>
		public bool automaticProxy = false;
		/// <summary>
		/// If provided, all web requests will attempt basic authentication using these credentials.
		/// </summary>
		public NetworkCredential BasicAuthCredentials;

		public WebRequestUtility(string UserAgentString, int requestTimeout = 600000)
		{
			if (!ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12))
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
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
					args.Add(HttpUtility.UrlEncode(keysAndValues[i]) + "=" + HttpUtility.UrlEncode(keysAndValues[i + 1]));
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
		/// <param name="contentType">The value of the content-type header to set.</param>
		/// <param name="headers">Additional header keys and values to set in the request, provided as an array of strings ordered as [key, value, key, value] and so on. e.g.: { "User-Agent", "Mozilla", "Server", "MyServer" }</param>
		/// <returns></returns>
		public BpWebResponse POST(string url, byte[] postBody, string contentType, string[] headers = null, int earlyTerminationBytes = int.MaxValue)
		{
			return internal_GET_or_POST(url, postBody, contentType, headers, earlyTerminationBytes);
		}
		protected virtual BpWebResponse internal_GET_or_POST(string url, byte[] postBody, string contentType, string[] headers, int earlyTerminationBytes)
		{
			BpWebResponse response = new BpWebResponse();

			HttpClientHandler httpClientHandler = new HttpClientHandler();
			if (automaticProxy)
				httpClientHandler.UseProxy = true;
			else
			{
				httpClientHandler.UseProxy = proxy != null;
				if (proxy != null)
					httpClientHandler.Proxy = proxy;
			}

			HttpClient client = new HttpClient(httpClientHandler);
			client.Timeout = TimeSpan.FromMilliseconds(requestTimeout);
			client.DefaultRequestHeaders.ExpectContinue = false;

			bool addedUserAgentHeader = false;
			if (headers != null)
			{
				for (int i = 0; i + 1 < headers.Length; i += 2)
				{
					string key = headers[i];
					string value = headers[i + 1];
					if (key.ToLower() == "user-agent")
						addedUserAgentHeader = true;
					client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
				}
			}
			if (!addedUserAgentHeader && !string.IsNullOrEmpty(UserAgentString))
				client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgentString);
			if (BasicAuthCredentials != null)
			{
				httpClientHandler.Credentials = BasicAuthCredentials;
				httpClientHandler.PreAuthenticate = true;
			}
			try
			{
				Task<HttpResponseMessage> responseTask;
				if (postBody != null && postBody.Length > 1 && !string.IsNullOrWhiteSpace(contentType))
				{
					ByteArrayContent postBodyContent = new ByteArrayContent(postBody);
					postBodyContent.Headers.Add("Content-Type", contentType);
					//postBodyContent.Headers.Add("Content-Length", postBody.Length.ToString());
					responseTask = client.PostAsync(url, postBodyContent);
				}
				else
					responseTask = client.GetAsync(url);

				responseTask.Wait();

				HttpResponseMessage httpResponse = responseTask.Result;
				response.StatusCode = (int)httpResponse.StatusCode;

				foreach (var kvp in httpResponse.Headers)
					response.headers[kvp.Key] = string.Join(",", kvp.Value);
				foreach (var kvp in httpResponse.Content.Headers)
					response.headers[kvp.Key] = string.Join(",", kvp.Value);

				if (httpResponse.Content.Headers.ContentType != null)
					response.ContentType = httpResponse.Content.Headers.ContentType.ToString();
				else if (response.headers.ContainsKey("Content-Type"))
					response.ContentType = response.headers["Content-Type"];
				else
					response.ContentType = "";

				if (earlyTerminationBytes == int.MaxValue)
				{
					Task<byte[]> getResponse = httpResponse.Content.ReadAsByteArrayAsync();
					getResponse.Wait();
					response.data = getResponse.Result;
				}
				else
				{
					using (MemoryStream ms = new MemoryStream())
					{
						Task<Stream> getStream = httpResponse.Content.ReadAsStreamAsync();
						getStream.Wait();
						using (Stream responseStream = getStream.Result)
						{
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
			catch (Exception ex)
			{
				response.StatusCode = 0;
				response.ex = ex;
			}
			return response;
		}
	}
}
