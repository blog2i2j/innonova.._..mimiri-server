using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class ReadNoteRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public ReadNoteRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public ReadNoteRequest(JsonObject json) {
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

		public string Include {
			get {
				return _json.String("include");
			}
			set {
				_json.String("include", value);
			}
		}

		public void AddVersion(string type, long version) {
			if (!_json.Has("versions")) {
				_json.Array("versions", new JsonArray());
			}
			_json.Array("versions").Add(new JsonObject()
				.String("type", type)
				.Int64("version", version)
			);
		}

		public bool isNewer(string type, long version) {
			if (!_json.Has("versions")) {
				return true;
			}
			foreach (var item in _json.Array("versions").AsObjects()) {
				if (item.String("type") == type) {
					return version > item.Int64("version");
				}
			}
			return true;
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
					string.IsNullOrWhiteSpace(Include) ||
					Id == Guid.Empty
				);
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
