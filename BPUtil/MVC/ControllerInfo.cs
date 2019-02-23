using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BPUtil.SimpleHttp;

namespace BPUtil.MVC
{
	internal class ControllerInfo
	{
		/// <summary>
		/// Type type of Controller-derived class represented by this ControllerInfo.
		/// </summary>
		protected Type ControllerType;
		/// <summary>
		/// A map of ActionResult-returning methods available in the controller.
		/// </summary>
		protected SortedList<string, MethodInfo> methodMap = new SortedList<string, MethodInfo>();
		public ControllerInfo(Type controllerType)
		{
			if (!controllerType.IsSubclassOf(typeof(Controller)))
				throw new Exception("Type \"" + controllerType.FullName + "\" does not inherit from \"" + typeof(Controller).FullName + "\".");

			this.ControllerType = controllerType;

			IEnumerable<MethodInfo> allMethods = controllerType.GetMethods().Where(IsActionMethod);
			foreach (MethodInfo methodInfo in allMethods)
				this.methodMap[methodInfo.Name] = methodInfo;
		}

		public ActionResult CallMethod(RequestContext context)
		{
			if (!methodMap.TryGetValue(context.ActionName, out MethodInfo methodInfo))
				return null;
			Controller controller = (Controller)Activator.CreateInstance(ControllerType);
			controller.Context = context;
			return (ActionResult)methodInfo.Invoke(controller, null);
		}

		private bool IsActionMethod(MethodInfo mi)
		{
			if (mi.ReturnType != typeof(ActionResult))
				return false;
			if (mi.GetParameters().Length > 0) // Inline arguments are not currently supported.
				return false;
			return true;
		}
	}
}
