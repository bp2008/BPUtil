using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>A class which manages<seealso cref="ServicePointManager.ServerCertificateValidationCallback"/>, providing support for multiple callbacks. If any of the registered callbacks return true, the certificate will be considered valid.</para>
	/// <para>By default, only valid certificates are considered valid.</para>
	/// <para>This should be the only class which sets <seealso cref="ServicePointManager.ServerCertificateValidationCallback"/></para>
	/// </summary>
	public static class CertificateValidation
	{
		private static object myLock = new object();
		private static RemoteCertificateValidationCallback[] callbacks = new RemoteCertificateValidationCallback[0];
		/// <summary>
		/// If true, the CertificateValidation class will always call all validation callbacks. If false, it will stop as soon as one returns true.
		/// </summary>
		public static bool AlwaysCallAllCallbacks = false;
		static CertificateValidation()
		{
			RegisterCallback(DefaultValidationCallback);
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CallAllCallbacks);
		}

		/// <summary>
		/// <para>Registers a new callback to be called in <see cref="ServicePointManager.ServerCertificateValidationCallback"/>.</para>
		/// <para>DO NOT ASSIGN <see cref="ServicePointManager.ServerCertificateValidationCallback"/> yourself.  This class assigns a special callback method which calls all callbacks registered via <see cref="CertificateValidation.RegisterCallback"/>. If any of the registered callbacks returns true, the certificate will be accepted.</para>
		/// <para>If you want a template for the callback which you can copy, see the <see cref="DefaultValidationCallback"/> private method in this class.</para>
		/// </summary>
		/// <param name="callback"></param>
		public static void RegisterCallback(RemoteCertificateValidationCallback callback)
		{
			lock (myLock)
			{
				RemoteCertificateValidationCallback[] local = new RemoteCertificateValidationCallback[callbacks.Length + 1];
				for (int i = 0; i < callbacks.Length; i++)
					local[i] = callbacks[i];
				local[local.Length - 1] = callback;
				callbacks = local;
			}
		}

		/// <summary>
		/// Returns true if sslPolicyErrors == SslPolicyErrors.None.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		private static bool DefaultValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			if (sslPolicyErrors == SslPolicyErrors.None)
				return true;
			return false;
		}

		private static bool CallAllCallbacks(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			bool result = false;
			RemoteCertificateValidationCallback[] localCallbacks = callbacks;
			foreach (RemoteCertificateValidationCallback callback in localCallbacks)
				if (callback(sender, certificate, chain, sslPolicyErrors))
				{
					result = true;
					if (!AlwaysCallAllCallbacks)
						return result;
				}
			return result;
		}
		/// <summary>
		/// An example validation callback which simply returns true.  Registering this callback will effectively disable certificate validation.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		public static bool DoNotValidate_ValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}
		/// <summary>
		/// An example validation callback which approves the certificate without looking at it, if the target hostname is exactly "127.0.0.1".
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		public static bool Allow_127_0_0_1_ValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			if (sender is HttpWebRequest)
			{
				HttpWebRequest request = (HttpWebRequest)sender;
				return request.Host == "127.0.0.1";
			}
			return false;
		}
	}
}
