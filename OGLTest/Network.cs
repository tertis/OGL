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
			server.Start();
		}
			
		[Test]
		public void AClientStart()
		{
			client = new OGL.Network.TCP.Client();
			client.StartConnect("127.0.0.1", 11000, Connected);
			connect.WaitOne();
		}

		[Test]
		public void BClientSendRecv()
		{
			client.RegisterRecvCallback(1, Received);
			client.Send(1, "HI<EOF>");
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

		[TestFixtureTearDown]
		public void CleanUp()
		{
			server.Stop();
			Trace.Write("Test End");
		}
	}
}
