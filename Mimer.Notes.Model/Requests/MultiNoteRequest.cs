using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public interface INoteItem {
		long Version { get; set; }
		string Type { get; set; }
		string Data { get; set; }
		DateTime Modified { get; set; }
		DateTime Created { get; set; }
		int Size { get; set; }
	}

	public class NoteItem : INoteItem {
		private JsonObject _json;

		public NoteItem() {
			_json = new JsonObject();
		}

		public NoteItem(JsonObject json) {
			_json = json;
		}

		public string Type {
			get {
				return _json.String("type");
			}
			set {
				_json.String("type", value);
			}
		}

		public long Version {
			get {
				return _json.Int64("version");
			}
			set {
				_json.Int64("version", value);
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

		public DateTime Modified {
			get {
				return _json.DateTime("modified");
			}
			set {
				_json.DateTime("modified", value);
			}
		}

		public DateTime Created {
			get {
				return _json.DateTime("created");
			}
			set {
				_json.DateTime("created", value);
			}
		}

		public int Size {
			get {
				return _json.Int32("size");
			}
			set {
				_json.Int32("size", value);
			}
		}
	}

	public class NoteAction {
		private JsonObject _json;

		public NoteAction() {
			_json = new JsonObject();
			_json.Array("items", new JsonArray());
		}

		public NoteAction(JsonObject json) {
			_json = json;
		}

		public string Type {
			get {
				return _json.String("type");
			}
			set {
				_json.String("type", value);
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

		public void AddItem(long version, string type, string data) {
			_json.Array("items").Add(new JsonObject()
				.Int64("version", version)
				.String("type", type)
				.String("data", data)
			);
		}

		public List<INoteItem> Items {
			get {
				return _json.Array("items").AsObjects().Select(json => new NoteItem(json)).ToList<INoteItem>();
			}
		}

		public JsonObject Json {
			get {
				return _json;
			}
		}
		public bool IsValid {
			get {
				try {
					return !(
						string.IsNullOrWhiteSpace(Type) ||
						Id == Guid.Empty ||
						KeyName == Guid.Empty
					);
				}
				catch {
					return false;
				}
			}
		}


	}

	public class MultiNoteRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public MultiNoteRequest() {
			_json = new JsonObject();
			_json.Array("actions", new JsonArray());
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public MultiNoteRequest(JsonObject json) {
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

		public void AddAction(NoteAction action) {
			_json.Array("actions").Add(action.Json);
		}

		public List<NoteAction> Actions {
			get {
				return _json.Array("actions").AsObjects().Select(json => new NoteAction(json)).ToList();
			}
		}

		public List<Guid> KeyNames {
			get {
				var keys = new HashSet<Guid>();
				foreach (var action in Actions) {
					keys.Add(action.KeyName);
					if (action.OldKeyName != Guid.Empty) {
						keys.Add(action.OldKeyName);
					}
				}
				return keys.ToList();
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
					return !string.IsNullOrWhiteSpace(Username) && !Actions.Any(action => !action.IsValid);
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
