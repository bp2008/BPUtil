using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
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
		/// If the request/thread was aborted, this will be true.
		/// </summary>
		public bool canceled = false;

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
	/// Provides HTTP GET and POST methods which are useful in situations where the WebClient class falls short.  An instance of this is intended to be used with only a single remote host.  This class is thread-safe.
	/// </summary>
	public class WebRequestUtility
	{
		/// <summary>
		/// Gets or sets the value of the "User-Agent" HTTP header.
		/// </summary>
		public string UserAgent
		{
			get
			{
				if (client.DefaultRequestHeaders.TryGetValues("User-Agent", out IEnumerable<string> values))
					foreach (string value in values)
						return value;
				return null;
			}
			set
			{
				try
				{
					if (client.DefaultRequestHeaders.Contains("User-Agent"))
						client.DefaultRequestHeaders.Remove("User-Agent");
				}
				catch { }
				if (!string.IsNullOrEmpty(value))
					client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", value);
			}
		}
		/// <summary>
		/// Gets or sets the timespan to wait before the request times out.
		/// </summary>
		public TimeSpan RequestTimeout
		{
			get
			{
				return client.Timeout;
			}
			set
			{
				client.Timeout = value;
			}
		}
		/// <summary>
		/// <para>You can provide a proxy server here, if desired.  Example: new WebProxy("127.0.0.1", 8888)</para>
		/// <para>See <seealso cref="IWebProxy"/>, <seealso cref="WebProxy"/>.</para>
		/// <para>Also see <seealso cref="UseProxy"/>.</para>
		/// </summary>
		public IWebProxy Proxy
		{
			get
			{
				return httpClientHandler.Proxy;
			}
			set
			{
				httpClientHandler.Proxy = value;
			}
		}
		/// <summary>
		/// If false, no web proxy server will be used.  If true, the <seealso cref="HttpClient"/> will use the <see cref="Proxy"/> server. If Proxy is null, the <seealso cref="HttpClient"/> will use automatic proxy settings.
		/// </summary>
		public bool UseProxy
		{
			get
			{
				return httpClientHandler.UseProxy;
			}
			set
			{
				httpClientHandler.UseProxy = value;
			}
		}
		/// <summary>
		/// If provided, all web requests will attempt basic authentication using these credentials.
		/// </summary>
		public NetworkCredential BasicAuthCredentials
		{
			get
			{
				return (NetworkCredential)httpClientHandler.Credentials;
			}
			set
			{
				httpClientHandler.Credentials = value;
				httpClientHandler.PreAuthenticate = value != null;
			}
		}

		protected HttpClient client;
		protected HttpClientHandler httpClientHandler;

		/// <summary>
		/// Constructs a WebRequestUtility which can be used for multiple HTTP requests, even concurrent ones.  This class is thread-safe.
		/// </summary>
		/// <param name="userAgent">User-Agent header value.</param>
		/// <param name="requestTimeout">Initial request timeout in milliseconds. If 0 or less, this value is ignored. To modify after construction, see <see cref="RequestTimeout"/>.</param>
		public WebRequestUtility(string userAgent, int requestTimeout = 600000)
		{
			if (!ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12))
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			if (ServicePointManager.DefaultConnectionLimit < 16)
				ServicePointManager.DefaultConnectionLimit = 16;

			httpClientHandler = new HttpClientHandler();
			client = new HttpClient(httpClientHandler);
			client.DefaultRequestHeaders.ExpectContinue = false;

			if (!string.IsNullOrEmpty(userAgent))
				this.UserAgent = userAgent;
			if (requestTimeout > 0)
				RequestTimeout = TimeSpan.FromMilliseconds(requestTimeout);
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
			Task<BpWebResponse> task = GETAsync(url, headers, earlyTerminationBytes);
			task.Wait();
			return task.Result;
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
		public async Task<BpWebResponse> GETAsync(string url, string[] headers = null, int earlyTerminationBytes = int.MaxValue)
		{
			return await internal_GET_or_POST(url, null, null, headers, earlyTerminationBytes).ConfigureAwait(false);
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
			Task<BpWebResponse> task = POSTAsync(url, keysAndValues, headers, earlyTerminationBytes);
			task.Wait();
			return task.Result;
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
		public async Task<BpWebResponse> POSTAsync(string url, string[] keysAndValues, string[] headers = null, int earlyTerminationBytes = int.MaxValue)
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
			return await internal_GET_or_POST(url, postBody, contentType, headers, earlyTerminationBytes).ConfigureAwait(false);
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
			Task<BpWebResponse> task = POSTAsync(url, postBody, contentType, headers, earlyTerminationBytes);
			task.Wait();
			return task.Result;
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
		public async Task<BpWebResponse> POSTAsync(string url, byte[] postBody, string contentType, string[] headers = null, int earlyTerminationBytes = int.MaxValue)
		{
			return await internal_GET_or_POST(url, postBody, contentType, headers, earlyTerminationBytes).ConfigureAwait(false);
		}
		protected virtual async Task<BpWebResponse> internal_GET_or_POST(string url, byte[] postBody, string contentType, string[] headers, int earlyTerminationBytes)
		{
			BpWebResponse response = new BpWebResponse();

			HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
			if (headers != null)
			{
				for (int i = 0; i + 1 < headers.Length; i += 2)
				{
					string key = headers[i];
					string value = headers[i + 1];
					requestMessage.Headers.TryAddWithoutValidation(key, value);
				}
			}
			if (postBody != null && postBody.Length > 1 && !string.IsNullOrWhiteSpace(contentType))
			{
				ByteArrayContent postBodyContent = new ByteArrayContent(postBody);
				postBodyContent.Headers.Add("Content-Type", contentType);
				//postBodyContent.Headers.Add("Content-Length", postBody.Length.ToString());
				requestMessage.Method = HttpMethod.Post;
				requestMessage.Content = postBodyContent;
			}
			try
			{
				HttpResponseMessage httpResponse = await client.SendAsync(requestMessage).ConfigureAwait(false);

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
					response.data = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
				}
				else
				{
					using (MemoryStream ms = new MemoryStream())
					{
						using (Stream responseStream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false))
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
				if (ex.GetExceptionWhere(e2 => e2 is ThreadAbortException || e2.Message.Contains("The request was aborted: The request was canceled")) != null)
					response.canceled = true;
			}
			return response;
		}
	}
}
