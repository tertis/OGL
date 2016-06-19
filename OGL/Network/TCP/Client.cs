using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OGL.Network.TCP
{
	public class Client : Base
	{
		private Socket client = null;
		private Action callbackConnect = null;
		private Action callbackDisconnect = null;
		private Action callbackConnectFail = null;
		protected Action<byte[]> callbackRecv = null;

		public bool StartConnect(string ipAddr, int port,
			Action connCallback,
			Action<byte[]> recvCallback,
			Action disconnCallback,
			Action connFailCallback)
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

				callbackConnect = connCallback;
				callbackRecv = recvCallback;
				callbackDisconnect = disconnCallback;
				callbackConnectFail = connFailCallback;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}

			return true;
		}

		public bool Send(int type, byte[] data)
		{
			if (client == null)
			{
				Console.WriteLine("[Err] Not Connected!");
				return false;
			}

			SendToRemote(client, data);

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

				// 로직의 콜백 함수 호출
				callbackConnect();

				Receive(client);

				Console.WriteLine("[Log] Socket connected to {0}",
					client.RemoteEndPoint.ToString());
			}
			catch (Exception e)
			{
				callbackConnectFail();
				Console.WriteLine(e.ToString());
			}
		}

		protected void Receive(Socket socket)
		{
			try
			{
				// Create the state object.
				StateObject state = new StateObject();
				state.workSocket = socket;

				// Begin receiving the data from the remote device.
				socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(ReceiveCallback), state);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		protected void ReceiveCallback(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket socket = state.workSocket;
			try
			{
				// 소켓에서 데이터를 읽어 온다.
				int bytesRead = socket.EndReceive(ar);

				// 원격지에서 정상적으로 소켓을 종료했다.
				if (bytesRead == 0)
				{
					callbackDisconnect();
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
					return;
				}

				if (bytesRead > 0)
				{
					// 한번에 받는 데이터가 버퍼 사이즈보다 크면 잘못된 패킷으로 간주한다.
					if (bytesRead >= StateObject.BufferSize)
					{
						Console.WriteLine("[Err] bufferSize overflow!");
					}
					else
					{
						callbackRecv(state.buffer);
					}
				}

				// Buffer 클리어
				Array.Clear(state.buffer, 0, StateObject.BufferSize);
				// Recv 다시 대기
				socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(ReceiveCallback), state);
			}
			catch (Exception e)
			{
				// 소켓이 강제로 끊김
				if (!socket.Connected)
				{
					callbackDisconnect();
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
				}
				else
				{
					Console.WriteLine(e.ToString());
				}
			}
		}
	}
}
