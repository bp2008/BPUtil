using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	public static class MvcJson
	{
		/// <summary>
		/// <para>Converts an object to a JSON string. Must be set by consumers of the MVC utility prior to using <see cref="JsonResult"/> or <see cref="Controller.Json(object)"/>.</para>
		/// <para>e.g. MvcJson.SerializeObject = JsonConvert.SerializeObject; MvcJson.DeserializeObject = JsonConvert.DeserializeObject;</para>
		/// </summary>
		public static Func<object, string> SerializeObject = obj => throw new Exception("MvcJson functions must be assigned prior to using JsonResult objects.");
		/// <summary>
		/// <para>Converts a JSON string to an object. Must be set by consumers of the MVC utility prior to using <see cref="JsonResult"/> or <see cref="Controller.Json(object)"/>.</para>
		/// <para>e.g. MvcJson.SerializeObject = JsonConvert.SerializeObject; MvcJson.DeserializeObject = JsonConvert.DeserializeObject;</para>
		/// </summary>
		public static Func<string, object> DeserializeObject = str => throw new Exception("MvcJson functions must be assigned prior to using JsonResult objects.");
	}
}
