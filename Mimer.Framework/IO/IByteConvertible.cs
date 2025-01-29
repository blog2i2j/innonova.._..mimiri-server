namespace Mimer.Framework.IO {
	public interface IByteConvertible {

		void CopyToByteArray(byte[] data, int offset);
		int FromByteArray(byte[] data, int offset);
		int ByteLength { get; }

	}
}
