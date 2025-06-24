using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class CreateKeyRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public CreateKeyRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public CreateKeyRequest(JsonObject json) {
			_json = json;
		}

		public string Username {
			get {
				return _json.String("username");
			}
			set {
				_json.String("username", value);
			}
		}

		public Guid Id {
			get {
				return _json.Guid("id");
			}
			set {
				_json.Guid("id", value);
			}
		}

		public Guid Name {
			get {
				return _json.Guid("name");
			}
			set {
				_json.Guid("name", value);
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

		public string KeyData {
			get {
				return _json.String("keyData");
			}
			set {
				_json.String("keyData", value);
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

		public string Metadata {
			get {
				return _json.String("metadata");
			}
			set {
				_json.String("metadata", value);
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
				try {
					return !(
						string.IsNullOrWhiteSpace(Username) ||
						string.IsNullOrWhiteSpace(PrivateKey) ||
						string.IsNullOrWhiteSpace(Algorithm) ||
						string.IsNullOrWhiteSpace(AsymmetricAlgorithm) ||
						string.IsNullOrWhiteSpace(PublicKey) ||
						string.IsNullOrWhiteSpace(PrivateKey) ||
						string.IsNullOrWhiteSpace(Metadata) ||
						Id == Guid.Empty ||
						Name == Guid.Empty
					);
				}
				catch {
					return false;
				}
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
