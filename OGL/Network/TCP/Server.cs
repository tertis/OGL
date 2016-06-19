using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace OGL.Network.TCP
{
	public class Server : Base
	{
		private Action<uint> callbackConnect = null;
		private Action<uint> callbackDisconnect = null;
		protected Action<uint, byte[]> callbackRecv = null;
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
		/// <param name="connCallback">클라이언트가 Connect 되었을 때 호출되는 함수</param>
		/// <param name="recvCallback">클라이언트에서 데이터를 받았을 때 호출되는 함수</param>
		/// <param name="disconnCallback">클라이언트가 Disconnect 되었을 때 호출할 함수</param>
		/// <returns>소켓 생성 성공 시 true</returns>
		public bool StartListen(string ipAddr, int port, Action<uint> connCallback, Action<uint, byte[]> recvCallback, Action<uint> disconnCallback)
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

				callbackConnect = connCallback;
				callbackRecv = recvCallback;
				callbackDisconnect = disconnCallback;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}

			return true;
		}

		/// <summary>
		/// 클라이언트로부터 연결 요청이 왔을 때 호출되는 콜백
		/// </summary>
		/// <param name="ar">비동기 요청</param>
		private void AcceptCallback(IAsyncResult ar)
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

			callbackConnect(state.clientID);
			connectionsMap.Add(state.clientID, handler);

			// 다시 연결 대기
			listener.BeginAccept(
							new AsyncCallback(AcceptCallback),
							listener);
		}

		/// <summary>
		/// 지정한 클라이언트로 데이터 전송
		/// </summary>
		/// <param name="clientID">클라이언트 ID</param>
		/// <param name="data">전송할 데이터</param>
		/// <returns>전송 성공 시 true</returns>
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

		/// <summary>
		/// 패킷을 받았을 때, 호출하는 함수
		/// </summary>
		/// <param name="ar">비동기 요청</param>
		private void ReceiveCallback(IAsyncResult ar)
		{
			ServerStateObject state = (ServerStateObject)ar.AsyncState;
			Socket socket = state.workSocket;

			try
			{
				// 소켓에서 데이터를 읽어 온다.
				int bytesRead = socket.EndReceive(ar);

				// 원격지에서 정상적으로 소켓을 종료했다.
				if (bytesRead == 0)
				{
					callbackDisconnect(state.clientID);
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
					connectionsMap.Remove(state.clientID);
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
				if (!socket.Connected)
				{
					callbackDisconnect(state.clientID);
					socket.Close();
					connectionsMap.Remove(state.clientID);
				}
				else
				{
					Console.WriteLine(e.ToString());
				}
			}
		}

		/// <summary>
		/// 모든 클라이언트와의 연결을 종료한다.
		/// </summary>
		public void Stop()
		{
			List<Socket> sockets = new List<Socket>(connectionsMap.Values);
			foreach (var connection in sockets)
			{
				Disconnect(connection);
			}

			connectionsMap.Clear();
		}
	}
}
