using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class UpdateUserRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public UpdateUserRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public UpdateUserRequest(JsonObject json) {
			_json = json;
		}

		public string OldUsername {
			get {
				return _json.String("oldUsername");
			}
			set {
				_json.String("oldUsername", value);
			}
		}

		public string Response {
			get {
				if (_json.Has("response")) {
					return _json.String("response");
				}
				return "";
			}
			set {
				_json.String("response", value);
			}
		}

		public int HashLength {
			get {
				if (!_json.Has("hashLength")) {
					return 8192;
				}
				return _json.Int32("hashLength");
			}
			set {
				_json.Int32("hashLength", value);
			}
		}

		public string Username {
			get {
				return _json.String("username");
			}
			set {
				_json.String("username", value);
			}
		}

		public string PublicKey {
			get {
				return _json.String("publicKey");
			}
			set {
				_json.String("publicKey", value);
			}
		}

		public string PrivateKey {
			get {
				return _json.String("privateKey");
			}
			set {
				_json.String("privateKey", value);
			}
		}

		public string AsymmetricAlgorithm {
			get {
				return _json.String("asymmetricAlgorithm");
			}
			set {
				_json.String("asymmetricAlgorithm", value);
			}
		}

		public string Salt {
			get {
				return _json.String("salt");
			}
			set {
				_json.String("salt", value);
			}
		}

		public int Iterations {
			get {
				return _json.Int32("iterations");
			}
			set {
				_json.Int32("iterations", value);
			}
		}

		public string Algorithm {
			get {
				return _json.String("algorithm");
			}
			set {
				_json.String("algorithm", value);
			}
		}

		public string PasswordSalt {
			get {
				return _json.Object("password").String("salt");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").String("salt", value);
			}
		}

		public string PasswordHash {
			get {
				return _json.Object("password").String("hash");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").String("hash", value);
			}
		}

		public int PasswordIterations {
			get {
				return _json.Object("password").Int32("iterations");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").Int32("iterations", value);
			}
		}

		public string PasswordAlgorithm {
			get {
				return _json.Object("password").String("algorithm");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").String("algorithm", value);
			}
		}

		public string SymmetricAlgorithm {
			get {
				return _json.String("symmetricAlgorithm");
			}
			set {
				_json.String("symmetricAlgorithm", value);
			}
		}

		public string SymmetricKey {
			get {
				return _json.String("symmetricKey");
			}
			set {
				_json.String("symmetricKey", value);
			}
		}

		public string Data {
			get {
				return _json.String("data");
			}
			set {
				_json.String("data", value);
			}
		}


		public string PayloadToSign {
			get {
				return _json.ToFilteredString(SignatureFilter.Default);
			}
		}

		public void AddSignature(string name, string signature) {
			if (!_json.Has("signatures")) {
				_json.Array("signatures", new JsonArray());
			}
			_json.Array("signatures").Add(new JsonObject()
				.String("name", name)
				.String("signature", signature)
			);
		}

		public string? GetSignature(string name) {
			foreach (var signature in _json.Array("signatures").AsObjects()) {
				if (signature.String("name") == name) {
					return signature.String("signature");
				}
			}
			return null;
		}

		public DateTime TimeStamp {
			get {
				return _json.DateTime("timestamp");
			}
			set {
				_json.DateTime("timestamp", value);
			}
		}

		public Guid RequestId {
			get {
				return _json.Guid("requestId");
			}
			set {
				_json.Guid("requestId", value);
			}
		}

		public bool IsValid {
			get {
				return !(
				string.IsNullOrWhiteSpace(Username) ||
				string.IsNullOrWhiteSpace(PrivateKey) ||
				string.IsNullOrWhiteSpace(Salt) ||
				string.IsNullOrWhiteSpace(Algorithm) ||
				string.IsNullOrWhiteSpace(PasswordSalt) ||
				string.IsNullOrWhiteSpace(PasswordHash) ||
				string.IsNullOrWhiteSpace(PasswordAlgorithm) ||
				string.IsNullOrWhiteSpace(SymmetricAlgorithm) ||
				string.IsNullOrWhiteSpace(Data) ||
				string.IsNullOrWhiteSpace(SymmetricKey));
			}
		}

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}

	}
}
