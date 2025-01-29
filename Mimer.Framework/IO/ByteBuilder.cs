using System.Net;
using System.Text;

namespace Mimer.Framework.IO {
	public class ByteBuilder {
		private List<byte[]> FSegments = new List<byte[]>();
		private int FLength = 0;
		private static Encoding FEncoding = Encoding.UTF8;

		public void Append(sbyte data) {
			Add(new byte[] { (byte)data });
		}

		public void Append(byte data) {
			Add(new byte[] { data });
		}

		public void Append(short data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(ushort data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(char data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(int data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(bool data) {
			Add(new byte[] { (byte)(data ? 1 : 0) });
		}

		public void Append(uint data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(long data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(ulong data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(decimal data) {
			int[] OData = decimal.GetBits(data);
			byte[] OBytes = new byte[OData.Length * 4];
			Buffer.BlockCopy(OData, 0, OBytes, 0, OBytes.Length);
			Add(OBytes);
		}

		public void Append(float data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(double data) {
			Add(BitConverter.GetBytes(data));
		}

		public void Append(Guid data) {
			Add(data.ToByteArray());
		}

		public void Append(DateTime data) {
			Append(data.ToBinary());
		}

		public void Append(IPEndPoint endpoint) {
			byte[] OData = endpoint.Address.GetAddressBytes();
			Append(OData.Length);
			Add(OData);
			Append(endpoint.Port);
		}

		public void Append(string data) {
			if (data == null) {
				Append(-1);
				return;
			}
			byte[] OData = FEncoding.GetBytes(data);
			Append(OData.Length);
			Add(OData);
		}

		public void Append(string data, Encoding encoding) {
			if (data == null) {
				Append(-1);
				return;
			}
			byte[] OData = encoding.GetBytes(data);
			Append(OData.Length);
			Add(OData);
		}

		public void Append(string[] data) {
			if (data == null) {
				Append(-1);
				return;
			}
			Append(data.Length);
			for (int i = 0; i < data.Length; i++) {
				Append(data[i]);
			}
		}

		public void Append(Guid[] data) {
			if (data == null) {
				Append(-1);
				return;
			}
			Append(data.Length);
			for (int i = 0; i < data.Length; i++) {
				Append(data[i]);
			}
		}

		public void Append(byte[] data) {
			if (data == null) {
				Append(-1);
				return;
			}
			Append(data.Length);
			Add(data);
		}

		public void Append(IByteConvertible data) {
			if (data == null) {
				Append(-1);
				return;
			}
			byte[] OData = new byte[data.ByteLength];
			data.CopyToByteArray(OData, 0);
			Add(OData);
		}

		public void AppendRaw(byte[] data) {
			Add(data);
		}

		private void Add(byte[] data) {
			FSegments.Add(data);
			FLength += data.Length;
		}

		public byte[] ToArray() {
			return ToArray(false);
		}

		public byte[] ToArray(bool prependLength) {
			byte[] OResult;
			int OOffset = 0;
			if (prependLength) {
				OResult = new byte[FLength + 4];
				Buffer.BlockCopy(BitConverter.GetBytes(FLength), 0, OResult, 0, 4);
				OOffset += 4;
			}
			else {
				OResult = new byte[FLength];
			}
			for (int i = 0; i < FSegments.Count; i++) {
				byte[] OSegment = FSegments[i];
				Buffer.BlockCopy(OSegment, 0, OResult, OOffset, OSegment.Length);
				OOffset += OSegment.Length;
			}
			return OResult;
		}


	}
}
