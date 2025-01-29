using System.Net;
using System.Text;

namespace Mimer.Framework.IO {
	public class ByteReader {
		private byte[] FData;
		private int FOffset;
		private int FLength;
		private static Encoding FEncoding = Encoding.UTF8;

		public ByteReader(byte[] data) {
			FOffset = 0;
			FLength = data.Length;
			FData = data;
		}

		public ByteReader(byte[] data, int offset, int length) {
			FOffset = offset;
			FLength = FOffset + length;
			FData = data;
		}

		public ByteReader(byte[] data, int offset) {
			FOffset = offset;
			FLength = data.Length;
			FData = data;
		}

		public T Read<T>() where T : IByteConvertible, new() {
			T OResult = new T();
			int OLength = ReadInt32();
			if (OLength == -1) {
				return default(T)!;
			}
			((IByteConvertible)OResult).FromByteArray(FData, FOffset);
			FOffset += OLength;
			return OResult;
		}

		public sbyte ReadSByte() {
			return (sbyte)FData[FOffset++];
		}

		public byte ReadByte() {
			return FData[FOffset++];
		}

		public bool ReadBoolean() {
			return FData[FOffset++] == 1;
		}

		public short ReadInt16() {
			short rtn = BitConverter.ToInt16(FData, FOffset);
			FOffset += 2;
			return rtn;
		}

		public ushort ReadUInt16() {
			ushort rtn = BitConverter.ToUInt16(FData, FOffset);
			FOffset += 2;
			return rtn;
		}

		public int ReadInt32() {
			int rtn = BitConverter.ToInt32(FData, FOffset);
			FOffset += 4;
			return rtn;
		}

		public static int ReadInt32(byte[] data, int offset) {
			int rtn = BitConverter.ToInt32(data, offset);
			return rtn;
		}

		public uint ReadUInt32() {
			uint rtn = BitConverter.ToUInt32(FData, FOffset);
			FOffset += 4;
			return rtn;
		}

		public long ReadInt64() {
			long rtn = BitConverter.ToInt64(FData, FOffset);
			FOffset += 8;
			return rtn;
		}

		public ulong ReadUInt64() {
			ulong rtn = BitConverter.ToUInt64(FData, FOffset);
			FOffset += 8;
			return rtn;
		}

		public decimal ReadDecimal() {
			int[] OData = new int[4];
			Buffer.BlockCopy(FData, FOffset, OData, 0, 16);
			FOffset += 16;
			return new decimal(OData);
		}

		public float ReadSingle() {
			float rtn = BitConverter.ToSingle(FData, FOffset);
			FOffset += 4;
			return rtn;
		}

		public double ReadDouble() {
			double rtn = BitConverter.ToDouble(FData, FOffset);
			FOffset += 4;
			return rtn;
		}

		public Guid ReadGuid() {
			int a = ReadInt32();
			short b = ReadInt16();
			short c = ReadInt16();
			byte d = ReadByte();
			byte e = ReadByte();
			byte f = ReadByte();
			byte g = ReadByte();
			byte h = ReadByte();
			byte i = ReadByte();
			byte j = ReadByte();
			byte k = ReadByte();
			return new Guid(a, b, c, d, e, f, g, h, i, j, k);
		}

		public static Guid ReadGuid(byte[] data, int offset) {
			int a = BitConverter.ToInt32(data, offset);
			short b = BitConverter.ToInt16(data, offset + 4);
			short c = BitConverter.ToInt16(data, offset + 6);
			return new Guid(a, b, c, data[offset + 8], data[offset + 9], data[offset + 10], data[offset + 11], data[offset + 12], data[offset + 13], data[offset + 14], data[offset + 15]);
		}

		public DateTime ReadDateTime() {
			return DateTime.FromBinary(ReadInt64());
		}

		public IPEndPoint ReadIPEndPoint() {
			byte[] OData = ReadBytes(ReadInt32());
			return new IPEndPoint(new IPAddress(OData), ReadInt32());
		}

		public string? ReadString() {
			int OLength = ReadInt32();
			if (OLength == -1) {
				return null;
			}
			string rtn = FEncoding.GetString(FData, FOffset, OLength);
			FOffset += OLength;
			return rtn;
		}

		public static string? ReadString(byte[] data, int offset) {
			int OLength = ReadInt32(data, offset);
			if (OLength == -1) {
				return null;
			}
			string rtn = FEncoding.GetString(data, offset, OLength);
			return rtn;
		}

		public string?[]? ReadStrings() {
			int OCount = ReadInt32();
			if (OCount == -1) {
				return null;
			}
			string?[] OResult = new string?[OCount];
			for (int i = 0; i < OCount; i++) {
				OResult[i] = ReadString();
			}
			return OResult;
		}


		public Guid[]? ReadGuids() {
			int OCount = ReadInt32();
			if (OCount == -1) {
				return null;
			}
			Guid[] OResult = new Guid[OCount];
			for (int i = 0; i < OCount; i++) {
				OResult[i] = ReadGuid();
			}
			return OResult;
		}

		public byte[]? ReadBytes() {
			int OLength = ReadInt32();
			if (OLength == -1) {
				return null;
			}
			return ReadBytes(OLength);
		}

		public byte[] ReadBytes(int count) {
			byte[] OData = new byte[count];
			Buffer.BlockCopy(FData, FOffset, OData, 0, count);
			FOffset += count;
			return OData;
		}

		public byte[] ReadToEnd() {
			byte[] OData = new byte[FData.Length - FOffset];
			Buffer.BlockCopy(FData, FOffset, OData, 0, FData.Length - FOffset);
			FOffset = FData.Length - 1;
			return OData;
		}

		public int Offset {
			get {
				return FOffset;
			}
		}

		public void SetReadOffset(int offset) {
			FOffset = offset;
		}

		public bool HasMoreData {
			get {
				return FOffset < FLength;
			}
		}

	}
}
