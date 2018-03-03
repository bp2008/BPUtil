﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace BPUtil
{
	/// <summary>
	/// A Cookie-aware WebClient that will store authentication cookie information and persist it through subsequent requests.
	/// From: https://github.com/rionmonster/CookieAwareWebClient
	/// </summary>
	[System.ComponentModel.DesignerCategory("Code")]
	public class CookieAwareWebClient : WebClient
	{
		//Properties to handle implementing a timeout
		private int? _timeout = null;
		public int? Timeout
		{
			get
			{
				return _timeout;
			}
			set
			{
				_timeout = value;
			}
		}

		//A CookieContainer class to house the Cookie once it is contained within one of the Requests
		public CookieContainer CookieContainer { get; private set; }

		//Constructor
		public CookieAwareWebClient()
		{
			CookieContainer = new CookieContainer();
		}

		//Method to handle setting the optional timeout (in milliseconds)
		public void SetTimeout(int timeout)
		{
			_timeout = timeout;
		}

		//This handles using and storing the Cookie information as well as managing the Request timeout
		protected override WebRequest GetWebRequest(Uri address)
		{
			//Handles the CookieContainer
			WebRequest _req = base.GetWebRequest(address);
			var request = (HttpWebRequest)_req;
			request.CookieContainer = CookieContainer;
			//Sets the Timeout if it exists
			if (_timeout.HasValue)
			{
				request.Timeout = _timeout.Value;
			}
			return request;
		}
	}
}
