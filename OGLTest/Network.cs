using System;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading;

namespace OGLTest
{		
	[TestFixture]
	public class Network
	{
		OGL.Network.TCP.Server server;
		OGL.Network.TCP.Client client;

		ManualResetEvent connect = new ManualResetEvent(false);
		ManualResetEvent receive = new ManualResetEvent(false);

		[TestFixtureSetUp]
		public void Setup()
		{
			server = new OGL.Network.TCP.Server();
			server.StartListen("127.0.0.1", 11000, o => { }, ServerReceived);
		}
			
		[Test]
		public void AClientStart()
		{
			client = new OGL.Network.TCP.Client();
			client.StartConnect("127.0.0.1", 11000, Connected, Received);
			connect.WaitOne();
		}

		[Test]
		public void BClientSendRecv()
		{
			client.Send(1, BitConverter.GetBytes(123));
			receive.WaitOne();
		}

		private void Connected()
		{
			connect.Set();
		}

		private void Received(object obj)
		{
			receive.Set();
		}

		private void ServerReceived(uint id, byte[] data)
		{
			server.Send(id, data);
		}

		[TestFixtureTearDown]
		public void CleanUp()
		{
			server.Stop();
			Trace.Write("Test End");
		}
	}
}
