using System.Net;
using System.Text;

namespace Mimer.Framework.IO {
	public class ByteWriter {
		private static Encoding FEncoding = Encoding.UTF8;
		private byte[] FData;
		private int FOffset;

		public ByteWriter(byte[] array, int offset) {
			FData = array;
			FOffset = offset;
		}

		public void Append(sbyte data) {
			FData[FOffset++] = (byte)data;
		}

		public void Append(byte data) {
			FData[FOffset++] = data;
		}

		public void Append(short data) {
			FData[FOffset++] = (byte)(data & 0xFF);
			FData[FOffset++] = (byte)((data >> 8) & 0xFF);
		}

		public void Append(ushort data) {
			FData[FOffset++] = (byte)(data & 0xFF);
			FData[FOffset++] = (byte)((data >> 8) & 0xFF);
		}

		public void Append(char data) {
			FData[FOffset++] = (byte)(data & 0xFF);
			FData[FOffset++] = (byte)((data >> 8) & 0xFF);
			FData[FOffset++] = (byte)((data >> 16) & 0xFF);
			FData[FOffset++] = (byte)((data >> 24) & 0xFF);
		}

		public void Append(int data) {
			FData[FOffset++] = (byte)(data & 0xFF);
			FData[FOffset++] = (byte)((data >> 8) & 0xFF);
			FData[FOffset++] = (byte)((data >> 16) & 0xFF);
			FData[FOffset++] = (byte)((data >> 24) & 0xFF);
		}

		public void Append(bool data) {
			FData[FOffset++] = (byte)(data ? 1 : 0);
		}

		public void Append(uint data) {
			FData[FOffset++] = (byte)(data & 0xFF);
			FData[FOffset++] = (byte)((data >> 8) & 0xFF);
			FData[FOffset++] = (byte)((data >> 16) & 0xFF);
			FData[FOffset++] = (byte)((data >> 24) & 0xFF);
		}

		public void Append(long data) {
			FData[FOffset++] = (byte)(data & 0xFF);
			FData[FOffset++] = (byte)((data >> 8) & 0xFF);
			FData[FOffset++] = (byte)((data >> 16) & 0xFF);
			FData[FOffset++] = (byte)((data >> 24) & 0xFF);
			FData[FOffset++] = (byte)((data >> 32) & 0xFF);
			FData[FOffset++] = (byte)((data >> 40) & 0xFF);
			FData[FOffset++] = (byte)((data >> 48) & 0xFF);
			FData[FOffset++] = (byte)((data >> 56) & 0xFF);
		}

		public void Append(ulong data) {
			FData[FOffset++] = (byte)(data & 0xFF);
			FData[FOffset++] = (byte)((data >> 8) & 0xFF);
			FData[FOffset++] = (byte)((data >> 16) & 0xFF);
			FData[FOffset++] = (byte)((data >> 24) & 0xFF);
			FData[FOffset++] = (byte)((data >> 32) & 0xFF);
			FData[FOffset++] = (byte)((data >> 40) & 0xFF);
			FData[FOffset++] = (byte)((data >> 48) & 0xFF);
			FData[FOffset++] = (byte)((data >> 56) & 0xFF);
		}

		public void Append(decimal data) {
			int[] OData = decimal.GetBits(data);
			int OLength = OData.Length * 4;
			Buffer.BlockCopy(OData, 0, FData, FOffset, OLength);
			FOffset += OLength;
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
			int OOffset = FOffset;
			FOffset += 4;
			int OLength = FEncoding.GetBytes(data, 0, data.Length, FData, FOffset);
			FOffset += OLength;
			FData[OOffset++] = (byte)(OLength & 0xFF);
			FData[OOffset++] = (byte)((OLength >> 8) & 0xFF);
			FData[OOffset++] = (byte)((OLength >> 16) & 0xFF);
			FData[OOffset] = (byte)((OLength >> 24) & 0xFF);
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
			data.CopyToByteArray(FData, FOffset);
			FOffset += data.ByteLength;
		}

		public void AppendRaw(byte[] data) {
			Add(data);
		}

		private void Add(byte[] data) {
			Buffer.BlockCopy(data, 0, FData, FOffset, data.Length);
			FOffset += data.Length;
		}


		public static int GetLength(sbyte data) {
			return 1;
		}

		public static int GetLength(byte data) {
			return 1;
		}

		public static int GetLength(short data) {
			return sizeof(short);
		}

		public static int GetLength(ushort data) {
			return sizeof(ushort);
		}

		public static int GetLength(char data) {
			return sizeof(char);
		}

		public static int GetLength(int data) {
			return sizeof(int);
		}

		public static int GetLength(bool data) {
			return 1;
		}

		public static int GetLength(uint data) {
			return sizeof(uint);
		}

		public static int GetLength(long data) {
			return sizeof(long);
		}

		public static int GetLength(ulong data) {
			return sizeof(ulong);
		}

		public static int GetLength(decimal data) {
			return 16;
		}

		public static int GetLength(float data) {
			return sizeof(float);
		}

		public static int GetLength(double data) {
			return sizeof(double);
		}

		public static int GetLength(Guid data) {
			return 16;
		}

		public static int GetLength(DateTime data) {
			return sizeof(long);
		}

		public static int GetLength(IPEndPoint endpoint) {
			if (endpoint == null) {
				return GetLength(-1);
			}
			return 4 + endpoint.Address.GetAddressBytes().Length + 4;
		}

		public static int GetLength(string data) {
			if (data == null) {
				return GetLength(-1);
			}
			return 4 + FEncoding.GetByteCount(data);
		}

		public static int GetLength(byte[] data) {
			if (data == null) {
				return GetLength(-1);
			}
			return 4 + data.Length;
		}

		public static int GetLength(IByteConvertible data) {
			if (data == null) {
				return GetLength(-1);
			}
			return data.ByteLength;
		}

		public int Offset {
			get {
				return FOffset;
			}
		}


	}
}
