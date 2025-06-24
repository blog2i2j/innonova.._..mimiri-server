using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class WriteNoteRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public WriteNoteRequest() {
			_json = new JsonObject();
			_json.Array("items", new JsonArray());
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public WriteNoteRequest(JsonObject json) {
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

		public Guid KeyName {
			get {
				return _json.Guid("keyName");
			}
			set {
				_json.Guid("keyName", value);
			}
		}

		public Guid OldKeyName {
			get {
				if (_json.Has("oldKeyName")) {
					return _json.Guid("oldKeyName");
				}
				return Guid.Empty;
			}
			set {
				_json.Guid("oldKeyName", value);
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

		public void AddItem(long version, string type, string data) {
			_json.Array("items").Add(new JsonObject()
				.Int64("version", version)
				.String("type", type)
				.String("data", data)
			);
		}

		public List<INoteItem> Items {
			get {
				if (_json.Has("items")) {
					return _json.Array("items").AsObjects().Select(json => new NoteItem(json)).ToList<INoteItem>();
				}
				return new List<INoteItem>();
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
						Id == Guid.Empty ||
						KeyName == Guid.Empty
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
