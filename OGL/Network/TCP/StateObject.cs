using System.Net.Sockets;
using System.Text;
using System;

namespace OGL.Network.TCP
{
	public class StateObject
	{
		public Action<object> callback = null;
		// Client  socket.
		public Socket workSocket = null;
		// Size of receive buffer.
		public const int BufferSize = 1024;
		// Receive buffer.
		public byte[] buffer = new byte[BufferSize];
		// Received data string.
		public StringBuilder sb = new StringBuilder();
	}
}
