using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace OGL.Network.TCP
{
	public class Client
	{
		private Socket client = null;

		private Action connCallback = null;
		private readonly Dictionary<int, Action<object>> recvCallbacks = new Dictionary<int, Action<object>>();

		public bool StartConnect(string ipAddr, int port, Action callback)
		{
			try
			{
				IPAddress ipAddress = IPAddress.Parse(ipAddr);
				IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

				// Create a TCP/IP socket.
				client = new Socket(AddressFamily.InterNetwork,
					SocketType.Stream, ProtocolType.Tcp);

				// Connect to the remote endpoint.
				client.BeginConnect(remoteEP,
					new AsyncCallback(ConnectCallback), client);
				connCallback = callback;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}

			return true;
		}

		public bool RegisterRecvCallback(int type, Action<object> callback)
		{
			if (recvCallbacks.ContainsKey(type))
			{
				Console.WriteLine("[Err] Exist Callback Type!");
				return false;
			}

			recvCallbacks.Add(type, callback);

			return true;
		}

		public bool Send(int type, string msg)
		{
			if (client == null)
			{
				Console.WriteLine("[Err] Not Connected!");
				return false;
			}

			Action<object> callback = null;
			if (!recvCallbacks.TryGetValue(type, out callback))
			{
				Console.WriteLine("[Err] Unknown Send Type!");
				return false;
			}

			Send(client, msg);
			Receive(client, callback);

			return true;
		}

		public bool Disconnect()
		{
			try
			{
				client.Shutdown(SocketShutdown.Both);
				client.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}

			return true;
		}

		private void ConnectCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				Socket client = (Socket)ar.AsyncState;

				// Complete the connection.
				client.EndConnect(ar);

				Console.WriteLine("[Log] Socket connected to {0}",
					client.RemoteEndPoint.ToString());
				connCallback();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		private void Receive(Socket client, Action<object> callback)
		{
			try
			{
				// Create the state object.
				StateObject state = new StateObject();
				state.callback = callback;
				state.workSocket = client;

				// Begin receiving the data from the remote device.
				client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(ReceiveCallback), state);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the state object and the client socket 
				// from the asynchronous state object.
				StateObject state = (StateObject)ar.AsyncState;
				Socket client = state.workSocket;

				// Read data from the remote device.
				int bytesRead = client.EndReceive(ar);

				if (bytesRead > 0)
				{
					// There might be more data, so store the data received so far.
					state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

					// Get the rest of the data.
					client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
						new AsyncCallback(ReceiveCallback), state);
				}
				else
				{
					// All the data has arrived; put it in response.
					if (state.sb.Length > 1)
					{
						state.callback(state.sb.ToString());
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		private void Send(Socket client, String data)
		{
			// Convert the string data to byte data using ASCII encoding.
			byte[] byteData = Encoding.ASCII.GetBytes(data);

			// Begin sending the data to the remote device.
			client.BeginSend(byteData, 0, byteData.Length, 0,
				new AsyncCallback(SendCallback), client);
		}

		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				Socket client = (Socket)ar.AsyncState;

				// Complete sending the data to the remote device.
				int bytesSent = client.EndSend(ar);
				Console.WriteLine("Sent {0} bytes to server.", bytesSent);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}
	}
}
