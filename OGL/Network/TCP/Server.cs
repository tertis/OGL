using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OGL.Network.TCP
{
	public class Server
	{
		Thread thread;
		public void Start()
		{
			thread = new Thread(AsynchronousSocketListener.StartListening);
			thread.Start();
		}

		public void Stop()
		{
			thread.Abort();
		}
	}
}
