using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace OGL.Network
{
	public class Packet
	{
		private uint type = uint.MinValue;
		private byte[] buffer = null;
		private int offset = 0;

		/// <summary>
		/// Initialize packet data
		/// </summary>
		/// <param name="type">packet type</param>
		public Packet(uint type, int bufferSize)
		{
			this.type = type;
			buffer = new byte[bufferSize];
		}

		private void EncodeImpl(byte[] data)
		{
			Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
			offset += data.Length;
		}

		public void Encode(bool data)
		{
			EncodeImpl(BitConverter.GetBytes(data));
		}

		public void Encode(int data)
		{
			EncodeImpl(BitConverter.GetBytes(data));
		}

		public void Encode(string data)
		{
			EncodeImpl(Encoding.UTF8.GetBytes(data));
		}

		public int GetBufferSize()
		{
			return buffer.Length;
		}

		public int GetLength()
		{
			return offset;
		}

		public byte[] GetBuffer()
		{
			return buffer;
		}
	}
}
