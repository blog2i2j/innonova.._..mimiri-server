using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class DeleteAccountRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public DeleteAccountRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public DeleteAccountRequest(JsonObject json) {
			_json = json;
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
					return !(string.IsNullOrWhiteSpace(Username));
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
