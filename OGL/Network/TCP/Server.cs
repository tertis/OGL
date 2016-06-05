using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace OGL.Network.TCP
{
	public class Server : Base
	{
		private Action<uint> callbackConn = null;
		protected Action<uint, byte[]> callbackRecv = null;
		private readonly List<Socket> connections = new List<Socket>();
		private readonly Dictionary<uint, Socket> connectionsMap = new Dictionary<uint, Socket>();
		private uint curConnIdx = uint.MinValue;

		public Server()
		{
		}

		/// <summary>
		/// 클라이언트로부터 연결을 받는 Listen Socket 생성
		/// </summary>
		/// <param name="ipAddr">IPv4 주소</param>
		/// <param name="port">Listening 포트</param>
		/// <param name="callBack">클라이언트가 Connect 되었을 때 호출할 함수</param>
		/// <returns>소켓 생성 실패 시 false</returns>
		public bool StartListen(string ipAddr, int port, Action<uint> conn, Action<uint, byte[]> recv)
		{
			try
			{
				IPAddress ipAddress = IPAddress.Parse(ipAddr);
				IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

				Socket listener = new Socket(AddressFamily.InterNetwork,
					SocketType.Stream, ProtocolType.Tcp);

				listener.Bind(localEndPoint);
				listener.Listen(100);

				listener.BeginAccept(
							new AsyncCallback(AcceptCallback),
							listener);

				callbackConn = conn;
				callbackRecv = recv;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}

			return true;
		}

		public void AcceptCallback(IAsyncResult ar)
		{
			// Get the socket that handles the client request.
			Socket listener = (Socket)ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			// Create the state object.
			ServerStateObject state = new ServerStateObject();
			state.workSocket = handler;
			state.clientID = ++curConnIdx;
			handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None,
				new AsyncCallback(ReceiveCallback), state);

			callbackConn(state.clientID);
			connections.Add(handler);
			connectionsMap.Add(state.clientID, handler);

			// 다시 연결 대기
			listener.BeginAccept(
							new AsyncCallback(AcceptCallback),
							listener);
		}

		public bool Send(uint clientID, byte[] data)
		{
			Socket client = null;
			if (connectionsMap.TryGetValue(clientID, out client))
			{
				SendToRemote(client, data);
				return true;
			}
			return false;
		}

		protected void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the state object and the client socket 
				// from the asynchronous state object.
				ServerStateObject state = (ServerStateObject)ar.AsyncState;
				Socket socket = state.workSocket;

				// 소켓에서 데이터를 읽어 온다.
				int bytesRead = socket.EndReceive(ar);

				if (bytesRead > 0)
				{
					// 한번에 받는 데이터가 버퍼 사이즈보다 크면 잘못된 패킷으로 간주한다.
					if (bytesRead >= StateObject.BufferSize)
					{
						Console.WriteLine("[Err] bufferSize overflow!");
					}
					else
					{
						byte[] dest = new byte[bytesRead];
						Array.Copy(state.buffer, dest, bytesRead);
						callbackRecv(state.clientID, dest);
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
				Console.WriteLine(e.ToString());
			}
		}

		public void Stop()
		{
			foreach (var connection in connections)
			{
				Disconnect(connection);
			}
		}
	}
}
