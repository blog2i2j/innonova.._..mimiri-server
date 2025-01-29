using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Mimer.Notes.Model.Cryptography {
	public class SymmetricCrypt : IDisposable {
		private string _algorithm;
		private const int IV_SIZE = 16;
		private byte[] _key;
		private bool disposedValue;

		public SymmetricCrypt(string algorithm, byte[] key) {
			if (algorithm != "AES;CBC;PKCS7;32") {
				throw new ArgumentException($"Algorithm not supported {algorithm}");
			}
			_algorithm = algorithm;
			_key = key;
		}

		public SymmetricCrypt(string algorithm) {
			if (algorithm != "AES;CBC;PKCS7;32") {
				throw new ArgumentException($"Algorithm not supported {algorithm}");
			}
			_algorithm = algorithm;
			var seed = new byte[32];
			var salt = new byte[32];
			using var random = RandomNumberGenerator.Create();
			random.GetBytes(seed);
			random.GetBytes(salt);
			using Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(seed, salt, 10000, HashAlgorithmName.SHA512);
			_key = deriver.GetBytes(32);
		}

		public SymmetricCrypt(string algorithm, string password, string salt, int iterations) {
			if (algorithm != "AES;CBC;PKCS7;32") {
				throw new ArgumentException($"Algorithm not supported {algorithm}");
			}
			_algorithm = algorithm;
			using Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(password, Convert.FromHexString(salt), iterations, HashAlgorithmName.SHA512);
			_key = deriver.GetBytes(32);
		}

		public byte[] DecryptBytes(string data) {
			using var aes = Aes.Create();
			aes.KeySize = _key.Length * 8;
			aes.Key = _key;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			byte[] dataBytes = Convert.FromBase64String(data);
			byte[] iv = new byte[IV_SIZE];
			Array.Copy(dataBytes, 0, iv, 0, IV_SIZE);
			aes.IV = iv;
			using ICryptoTransform transform = aes.CreateDecryptor();
			using MemoryStream dataStream = new MemoryStream(dataBytes, IV_SIZE, dataBytes.Length - IV_SIZE);
			using CryptoStream cryptoStream = new CryptoStream(dataStream, transform, CryptoStreamMode.Read);
			using MemoryStream outStream = new MemoryStream();
			cryptoStream.CopyTo(outStream);
			return outStream.ToArray();
		}

		public string Decrypt(string data) {
			using var aes = Aes.Create();
			aes.KeySize = _key.Length * 8;
			aes.Key = _key;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			byte[] dataBytes = Convert.FromBase64String(data);
			byte[] iv = new byte[IV_SIZE];
			Array.Copy(dataBytes, 0, iv, 0, IV_SIZE);
			aes.IV = iv;
			using ICryptoTransform transform = aes.CreateDecryptor();
			using MemoryStream dataStream = new MemoryStream(dataBytes, IV_SIZE, dataBytes.Length - IV_SIZE);
			using CryptoStream cryptoStream = new CryptoStream(dataStream, transform, CryptoStreamMode.Read);
			using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
			return reader.ReadToEnd();
		}

		public string Encrypt(string data) {
			return Encrypt(Encoding.UTF8.GetBytes(data));
		}

		public string Encrypt(byte[] data) {
			using var aes = Aes.Create();
			aes.KeySize = _key.Length * 8;
			aes.Key = _key;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			aes.GenerateIV();
			Debug.Assert(aes.IV.Length == IV_SIZE);
			using MemoryStream dataStream = new MemoryStream();
			dataStream.Write(aes.IV, 0, IV_SIZE);
			using ICryptoTransform transform = aes.CreateEncryptor();
			using (CryptoStream cryptoStream = new CryptoStream(dataStream, transform, CryptoStreamMode.Write)) {
				cryptoStream.Write(data, 0, data.Length);
			}
			return Convert.ToBase64String(dataStream.ToArray());
		}

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				disposedValue = true;
			}
		}

		public void Dispose() {
			Dispose(true);
		}

		public string Algorithm {
			get {
				return _algorithm;
			}
		}

		public byte[] Key {
			get {
				return _key;
			}
		}


	}
}
