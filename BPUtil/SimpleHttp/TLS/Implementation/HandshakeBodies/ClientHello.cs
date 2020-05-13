using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS.Implementation
{
	public class ClientHello : HandshakeBody
	{
		public string serverName;
		public ClientHello(IDataStream stream)
		{

		}
	}
}
