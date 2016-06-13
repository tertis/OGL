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

		[Test]
		public void PacketMake()
		{
			var packet = new OGL.Network.Packet(0, 100);
			Assert.IsTrue(packet.GetBufferSize() == 100);
		}

		[Test]
		public void PacketEncode()
		{
			var packet = new OGL.Network.Packet(0, 100);
			var length = BitConverter.GetBytes(100).Length;
			packet.Encode(100);	// +4
			packet.Encode(200); // +4
			packet.Encode("ㅁ"); // +3
			packet.Encode("11.aㅁ김"); // +10

			Assert.IsTrue(packet.GetLength() == 21);
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
