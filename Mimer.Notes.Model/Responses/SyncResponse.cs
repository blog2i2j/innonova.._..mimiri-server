using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {

	public class SyncNoteItemInfo {
		private JsonObject _json;
		public SyncNoteItemInfo() {
			_json = new JsonObject();
		}

		public SyncNoteItemInfo(JsonObject json) {
			_json = json;
		}

		public Guid NoteId {
			get {
				return _json.Guid("noteId");
			}
			set {
				_json.Guid("noteId", value);
			}
		}

		public string ItemType {
			get {
				return _json.String("itemType");
			}
			set {
				_json.String("itemType", value);
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

		public JsonObject Json {
			get {
				return _json;
			}
		}
	}

	public class SyncNoteInfo {
		private JsonObject _json;

		public SyncNoteInfo() {
			_json = new JsonObject();
			_json.Array("items", new JsonArray());
		}

		public SyncNoteInfo(JsonObject json) {
			_json = json;
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

		public DateTime Modified {
			get {
				return _json.DateTime("modified");
			}
			set {
				_json.DateTime("modified", value);
			}
		}

		public void AddItem(SyncNoteItemInfo item) {
			_json.Array("items").Add(item.Json);
		}

		public JsonObject Json {
			get {
				return _json;
			}
		}
	}

	public class SyncKeyInfo {
		private JsonObject _json;

		public SyncKeyInfo() {
			_json = new JsonObject();
		}

		public SyncKeyInfo(JsonObject json) {
			_json = json;
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

		public JsonObject Json {
			get {
				return _json;
			}
		}
	}
	public class SyncResponse : IResponseObject {
		private JsonObject _json;

		public SyncResponse() {
			_json = new JsonObject();
			_json.Array("notes", new JsonArray());
			_json.Array("keys", new JsonArray());
		}

		public SyncResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public void AddNote(SyncNoteInfo note) {
			_json.Array("notes").Add(note.Json);
		}

		public void AddKey(SyncKeyInfo key) {
			_json.Array("keys").Add(key.Json);
		}

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
