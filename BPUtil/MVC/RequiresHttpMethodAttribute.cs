using BPUtil.SimpleHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	/// <summary>
	/// <para>Specifies a set of HTTP methods that are allowed for a particular Action Method.</para>
	/// <para>This attribute may be applied to an Action Method or a <see cref="Controller"/> or both (if both, the attribute on the <see cref="Controller"/> is ignored).</para>
	/// <para>By default, all HTTP methods are allowed to be used with all Action Methods.</para>
	/// </summary>
	public class RequiresHttpMethodAttribute : Attribute
	{
		private readonly string[] _allowedHttpMethods;
		/// <summary>
		/// Gets a copy of the collection of HTTP methods that are allowed.
		/// </summary>
		public string[] AllowedHttpMethods => (string[])_allowedHttpMethods.Clone();
		/// <summary>
		/// Constructs a new RequiresHttpMethodAttribute with the given HTTP methods.
		/// </summary>
		/// <param name="allowedHttpMethods">The HTTP methods that are allowed ("GET", "POST", etc.)</param>
		public RequiresHttpMethodAttribute(params string[] allowedHttpMethods)
		{
			foreach (string allowedMethod in allowedHttpMethods)
				if (!HttpMethods.IsValid(allowedMethod))
					throw new ArgumentException("The HTTP method \"" + allowedMethod + "\" is not recognized. Valid methods are: " + string.Join(", ", HttpMethods.AllValidMethods));
			this._allowedHttpMethods = (string[])allowedHttpMethods.Clone();
		}
		/// <summary>
		/// Returns true if the given HTTP method string is in the list of allowed HTTP Methods, case-insensitive.
		/// </summary>
		/// <param name="method">HTTP method, case-insensitive. E.g. "GET" or "POST".</param>
		/// <returns></returns>
		public bool IsHttpMethodAllowed(string method)
		{
			if (method == null)
				return false;
			return _allowedHttpMethods.Contains(method.ToUpper());
		}
	}
}