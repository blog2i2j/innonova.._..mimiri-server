using Mimer.Framework;
using Mimer.Framework.Json;
using System.Security.Cryptography;
using System.Text;

namespace Mimer.Notes.Model.Cryptography {
	public class CryptSignature : IDisposable {
		private bool disposedValue;
		private RSA? _rsa;
		private string _algorithm;
		private const string DEFAULT_SYMMETRIC_ALGORITHM = "AES;CBC;PKCS7;32";


		public CryptSignature(string algorithm) {
			if (algorithm != "RSA;3072" && algorithm != "RSA;4096") {
				throw new ArgumentException($"Algorithm not supported {algorithm}");
			}
			_algorithm = algorithm;
			_rsa = RSA.Create();
		}

		public CryptSignature(string algorithm, string key) {
			if (algorithm != "RSA;3072" && algorithm != "RSA;4096") {
				throw new ArgumentException($"Algorithm not supported {algorithm}");
			}
			_algorithm = algorithm;
			_rsa = RSA.Create();
			_rsa.ImportFromPem(key);
		}

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (_rsa != null) {
					_rsa.Dispose();
					_rsa = null;
				}
				disposedValue = true;
			}
		}

		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		public void SignRequest(string name, ISignable signable) {
			var payload = Encoding.UTF8.GetBytes(signable.PayloadToSign);
			var signature = _rsa!.SignData(payload, 0, payload.Length, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			signable.AddSignature(name, Convert.ToBase64String(signature));
		}

		public bool VerifySignature(string name, ISignable signable) {
			var payload = Encoding.UTF8.GetBytes(signable.PayloadToSign);
			var signature = signable.GetSignature(name);
			if (signature == null) {
				throw new KeyNotFoundException(name);
			}
			var result = _rsa!.VerifyData(payload, Convert.FromBase64String(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			if (!result) {
				Dev.Log("Signature validation failed", name, signature);
			}
			return result;
		}

		public string Encrypt(string data) {
			return Encrypt(Encoding.UTF8.GetBytes(data));
		}

		public string Encrypt(byte[] data) {
			var crypt = new SymmetricCrypt(DEFAULT_SYMMETRIC_ALGORITHM);
			var encryptedData = crypt.Encrypt(data);
			var encryptedKey = Convert.ToBase64String(_rsa!.Encrypt(crypt.Key, RSAEncryptionPadding.OaepSHA256));
			JsonObject json = new JsonObject();
			json.String("data", encryptedData);
			json.String("encryptedKey", encryptedKey);
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(json.ToString()));
		}

		public string Decrypt(string data) {
			var json = new JsonObject(Encoding.UTF8.GetString(Convert.FromBase64String(data)));
			byte[] key = _rsa!.Decrypt(Convert.FromBase64String(json.String("encryptedKey")), RSAEncryptionPadding.OaepSHA256);
			var crypt = new SymmetricCrypt(DEFAULT_SYMMETRIC_ALGORITHM, key);
			return crypt.Decrypt(json.String("data"));
		}

		public string PrivateKey {
			get {
				return _rsa!.ExportPkcs8PrivateKeyPem();
			}
		}

		public string PublicKey {
			get {
				return _rsa!.ExportSubjectPublicKeyInfoPem();
			}
		}

		public string Algorithm {
			get {
				return _algorithm;
			}
		}

	}
}
