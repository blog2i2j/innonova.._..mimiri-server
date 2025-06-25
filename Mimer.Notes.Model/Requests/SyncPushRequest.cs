using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class NoteSyncItem {
		private JsonObject _json;
		public NoteSyncItem(JsonObject json) {
			_json = json;
		}

		public string Type {
			get {
				return _json.String("type");
			}
		}

		public string Data {
			get {
				return _json.String("data");
			}
		}

		public long Version {
			get {
				return _json.Int64("version");
			}
		}

	}

	public class NoteSyncAction {
		private JsonObject _json;

		public NoteSyncAction(JsonObject json) {
			_json = json;
		}

		public Guid Id {
			get {
				return _json.Guid("id");
			}
		}

		public Guid KeyName {
			get {
				return _json.Guid("keyName");
			}
		}

		public string Type {
			get {
				return _json.String("type");
			}
		}

		public List<NoteSyncItem> Items {
			get {
				return _json.Array("items").AsObjects().Select(json => new NoteSyncItem(json)).ToList();
			}
		}
	}

	public class KeySyncAction {
		private JsonObject _json;
		public KeySyncAction(JsonObject json) {
			_json = json;
		}

		public Guid Id {
			get {
				return _json.Guid("id");
			}
		}

		public Guid Name {
			get {
				return _json.Guid("name");
			}
		}

		public string Type {
			get {
				return _json.String("type");
			}
		}

		public string Data {
			get {
				return _json.String("data");
			}
		}
	}

	public class SyncPushRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public SyncPushRequest(JsonObject json) {
			_json = json;
		}

		public string Username {
			get {
				return _json.String("username");
			}
		}

		public List<NoteSyncAction> Notes {
			get {
				return _json.Array("notes").AsObjects().Select(json => new NoteSyncAction(json)).ToList();
			}
		}

		public List<KeySyncAction> Keys {
			get {
				return _json.Array("keys").AsObjects().Select(json => new KeySyncAction(json)).ToList();
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
			if (_json.Has("signatures")) {
				foreach (var sig in _json.Array("signatures").AsObjects()) {
					if (sig.String("name") == name) {
						return sig.String("signature");
					}
				}
			}
			return null;
		}

		public DateTime TimeStamp {
			get {
				return _json.DateTime("timestamp");
			}
		}

		public Guid RequestId {
			get {
				return _json.Guid("requestId");
			}
		}

		public bool IsValid {
			get {
				try {
					return !string.IsNullOrWhiteSpace(Username);
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
