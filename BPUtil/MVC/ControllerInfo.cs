using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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

			IEnumerable<MethodInfo> allMethods = controllerType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(IsActionMethod);
			foreach (MethodInfo methodInfo in allMethods)
			{
				if (this.methodMap.ContainsKey(methodInfo.Name.ToUpper()))
					throw new Exception("Type \"" + controllerType.FullName + "\" defines multiple ActionMethods with the same name: \"" + methodInfo.Name + "\". This is unsupported.");
				this.methodMap[methodInfo.Name.ToUpper()] = methodInfo;
			}
		}

		/// <summary>
		/// Calls the method specified by the request context's ActionName. Tries to map arguments from the URL to parameters defined on the method.
		/// </summary>
		/// <param name="context">The request context.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		public async Task<ActionResult> CallMethod(RequestContext context, CancellationToken cancellationToken = default)
		{
			if (!methodMap.TryGetValue(context.ActionName.ToUpper(), out MethodInfo methodInfo))
			{
				context.AssumeActionNameIsArgumentForIndex();
				if (!methodMap.TryGetValue(context.ActionName.ToUpper(), out methodInfo))
					return null;
			}
			Controller controller = (Controller)Activator.CreateInstance(ControllerType);
			controller.Context = context;
			controller.CancellationToken = cancellationToken;
			ActionResult result = null;
			controller.OnAuthorization(ref result);
			if (result == null)
				result = await CallActionMethod(controller, methodInfo).ConfigureAwait(false);
			controller.PreprocessResult(result);
			return result;
		}
		/// <summary>
		/// Calls the specified method on the controller, getting arguments from the controller's Context property.
		/// </summary>
		/// <param name="controller">A controller instance.</param>
		/// <param name="mi">Metadata about the method that is to be called.</param>
		/// <returns></returns>
		private async Task<ActionResult> CallActionMethod(Controller controller, MethodInfo mi)
		{
			ParameterInfo[] parameters = mi.GetParameters();
			object[] converted;
			try
			{
				converted = ConvertInputParameters(parameters, controller.Context.ActionArgs);
			}
			catch (Exception ex)
			{
				throw new ClientException("Error processing arguments for method " + controller.GetType().FullName + "." + mi.Name + "(" + string.Join(", ", parameters.Select(p => p.ParameterType.Name)) + ")", ex);
			}
			if (mi.ReturnType == typeof(Task<ActionResult>))
			{
				Task<ActionResult> resultTask = (Task<ActionResult>)mi.Invoke(controller, converted);
				return await resultTask.ConfigureAwait(false);
			}
			else
			{
				return await Task.Run(() => (ActionResult)mi.Invoke(controller, converted), controller.CancellationToken).ConfigureAwait(false);
			}
		}
		/// <summary>
		/// Converts the specified input arguments to the formats demanded by required parameters.
		/// </summary>
		/// <param name="requiredParameters"></param>
		/// <param name="args"></param>
		/// <exception cref="ArgumentException">
		/// <para>
		/// Throws ArgumentException if:
		/// </para>
		/// <list type="bullet">
		/// <item>Too many arguments were provided.</item>
		/// <item>Too few arguments were provided.</item>
		/// <item>Any of the provided arguments cannot be converted to the required type.</item>
		/// </list>
		/// </exception>
		/// <returns></returns>
		private object[] ConvertInputParameters(ParameterInfo[] requiredParameters, string[] args)
		{
			if (requiredParameters.Length == 0 && args.Length == 1 && args[0] == "")
				return null;
			else if (requiredParameters.Length == 1 && requiredParameters[0].ParameterType == typeof(string[]))
				return new object[] { args };
			else if (requiredParameters.Length == 1 && requiredParameters[0].ParameterType == typeof(object[]))
				return new object[] { args.Cast<object>().ToArray() };
			else if (requiredParameters.Length < args.Length)
				throw new ArgumentException("Too many arguments were provided.");
			object[] converted = new object[requiredParameters.Length];
			for (int i = 0; i < requiredParameters.Length; i++)
			{
				if (args.Length > i)
					converted[i] = ConvertStringToType(args[i], requiredParameters[i].ParameterType);
				else if (requiredParameters[i].HasDefaultValue)
					converted[i] = requiredParameters[i].RawDefaultValue;
				else
					throw new ArgumentException("Not enough arguments were provided.");
			}
			return converted;
		}
		/// <summary>
		/// Converts a string to a specific type.
		/// </summary>
		/// <param name="str">The string to convert. Should not be null.</param>
		/// <param name="t">The type to convert the string to.</param>
		/// <exception cref="ArgumentException">Throws ArgumentException if the string cannot be converted to the required type.</exception>
		/// <returns></returns>
		private object ConvertStringToType(string str, Type t)
		{
			if (t == typeof(string))
				return str;
			else if (t == typeof(byte))
				return byte.Parse(str);
			else if (t == typeof(short))
				return short.Parse(str);
			else if (t == typeof(ushort))
				return ushort.Parse(str);
			else if (t == typeof(int))
				return int.Parse(str);
			else if (t == typeof(uint))
				return uint.Parse(str);
			else if (t == typeof(long))
				return long.Parse(str);
			else if (t == typeof(ulong))
				return ulong.Parse(str);
			else if (t == typeof(float))
				return float.Parse(str);
			else if (t == typeof(double))
				return double.Parse(str);
			else if (t == typeof(decimal))
				return decimal.Parse(str);
			else if (t == typeof(bool))
				return str != null && (str == "1" || str.ToLower() == "true");
			else
				throw new ArgumentException("Unable to convert string to type: " + t.FullName);
		}

		/// <summary>
		/// Returns true if the specified method is public, not static, and has a return value that can be cast to <see cref="ActionResult"/> or <see cref="Task"/>&lt;<see cref="ActionResult"/>&gt;.
		/// </summary>
		/// <param name="mi">Metadata about the method.</param>
		/// <returns></returns>
		private bool IsActionMethod(MethodInfo mi)
		{
			if (mi.IsPublic && !mi.IsStatic)
			{
				return typeof(ActionResult).IsAssignableFrom(mi.ReturnType) || typeof(Task<ActionResult>).IsAssignableFrom(mi.ReturnType);
			}
			return false;
		}
	}
}
