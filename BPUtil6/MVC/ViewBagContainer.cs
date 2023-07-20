using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	/// <summary>
	/// A dynamic wrapper around ViewDataContainer. The getter returns null for all keys that are not found.
	/// </summary>
	public class ViewBagContainer : DynamicObject
	{
		private readonly ViewDataContainer ViewData;

		public ViewBagContainer(ViewDataContainer ViewData)
		{
			this.ViewData = ViewData;
		}
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			ViewData.Set(binder.Name, (string)value);
			return true;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = ViewData.Get(binder.Name);
			return true;
		}
	}
}
