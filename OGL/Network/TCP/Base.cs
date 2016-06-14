using System;
using System.Net.Sockets;

namespace OGL.Network.TCP
{
	public abstract class Base
	{
		#region Send
		protected void SendToRemote(Socket socket, byte[] data)
		{
			// Begin sending the data to the remote device.
			socket.BeginSend(data, 0, data.Length, 0,
				new AsyncCallback(SendCallback), socket);
		}

		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				Socket socket = (Socket)ar.AsyncState;

				// Complete sending the data to the remote device.
				int bytesSent = socket.EndSend(ar);
				Console.WriteLine("Sent {0} bytes to server.", bytesSent);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}
		#endregion

		protected bool Disconnect(Socket socket)
		{
			try
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}

			return true;
		}

		protected void DisconnectCallback(IAsyncResult ar)
		{
			Socket client = (Socket)ar.AsyncState;
			client.EndDisconnect(ar);
		}

	}
}
