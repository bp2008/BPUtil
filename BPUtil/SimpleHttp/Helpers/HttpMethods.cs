using System.Collections.Generic;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// <para>HTTP request methods</para>
	/// <para>This static class defines all the methods from https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods on 2023-06-27, but SimpleHttpServer only fully supports GET and POST.</para>
	/// <para>HTTP defines a set of request methods to indicate the desired action to be performed for a given resource. Although they can also be nouns, these request methods are sometimes referred to as HTTP verbs. Each of them implements a different semantic, but some common features are shared by a group of them: e.g. a request method can be safe, idempotent, or cacheable.</para>
	/// </summary>
	public static class HttpMethods
	{
		/// <summary>
		/// The GET method requests a representation of the specified resource. Requests using GET should only retrieve data.
		/// </summary>
		public const string GET = "GET";
		/// <summary>
		/// The HEAD method asks for a response identical to a GET request, but without the response body.
		/// </summary>
		public const string HEAD = "HEAD";
		/// <summary>
		/// The POST method submits an entity to the specified resource, often causing a change in state or side effects on the server.
		/// </summary>
		public const string POST = "POST";
		/// <summary>
		/// The PUT method replaces all current representations of the target resource with the request payload.
		/// </summary>
		public const string PUT = "PUT";
		/// <summary>
		/// The DELETE method deletes the specified resource.
		/// </summary>
		public const string DELETE = "DELETE";
		/// <summary>
		/// The CONNECT method establishes a tunnel to the server identified by the target resource.
		/// </summary>
		public const string CONNECT = "CONNECT";
		/// <summary>
		/// The OPTIONS method describes the communication options for the target resource.
		/// </summary>
		public const string OPTIONS = "OPTIONS";
		/// <summary>
		/// The TRACE method performs a message loop-back test along the path to the target resource.
		/// </summary>
		public const string TRACE = "TRACE";
		/// <summary>
		/// The PATCH method applies partial modifications to a resource.
		/// </summary>
		public const string PATCH = "PATCH";
		/// <summary>
		/// Static HashSet containing all valid HTTP request method strings.
		/// </summary>
		private static HashSet<string> validMethods = GetValidMethods();
		/// <summary>
		/// Returns a HashSet containing all valid HTTP request method strings.
		/// </summary>
		/// <returns></returns>
		private static HashSet<string> GetValidMethods()
		{
			return new HashSet<string>(new string[] { GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE, PATCH });
		}
		/// <summary>
		/// Returns true if the given HTTP method (a.k.a. verb) is recognized as one that is valid.
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static bool IsValid(string method)
		{
			return validMethods.Contains(method);
		}
	}
}