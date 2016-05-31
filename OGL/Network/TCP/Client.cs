using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OGL.Network.TCP
{
    public class Client
    {
		public void Start(string ipAddr)
		{
			AsynchronousClient.StartClient("127.0.0.1");
		}
    }
}
