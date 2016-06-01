using System;
using NUnit.Framework;
using System.Diagnostics;

namespace OGLTest
{		
	[TestFixture]
	public class Network
	{
		OGL.Network.TCP.Server server;
		OGL.Network.TCP.Client client;

		[TestFixtureSetUp]
		public void Setup()
		{
			server = new OGL.Network.TCP.Server();
			server.Start();
		}
			
		[Test]
		public void StartClient()
		{
			client = new OGL.Network.TCP.Client();
			client.Start("127.0.0.1");
		}

		[TestFixtureTearDown]
		public void CleanUp()
		{
			server.Stop();
			Trace.Write("Test End");
		}
	}
}
