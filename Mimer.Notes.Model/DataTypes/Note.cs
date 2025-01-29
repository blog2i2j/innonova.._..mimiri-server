using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class Note {
		private JsonObject _json;
		private bool _isCache;

		public Note(bool isCache = false) {
			_json = new JsonObject();
			Id = Guid.NewGuid();
			_json.Object("items", new JsonObject());
			_isCache = isCache;
		}

		public Note(JsonObject json, bool isCache = false) {
			_json = json;
			isCache = false;
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
		public void ClearItems() {
			_json.Array("items", new JsonArray());
		}

		public bool Has(string type) {
			return _json.Object("items").Has(type);
		}

		public NoteItem this[string type] {
			get {
				if (!_json.Object("items").Has(type)) {
					_json.Object("items").Object(type, new JsonObject()
							.Int64("version", 0)
							.String("type", type)
							.Object("data", new JsonObject())
					);
				}
				return new NoteItem(_json.Object("items").Object(type));
			}
		}

		public long GetVersion(string type) {
			if (_json.Object("items").Has(type)) {
				return _json.Object("items").Object(type).Int64("version");
			}
			return 0;
		}

		public JsonObject? GetItem(string type) {
			if (!_json.Object("items").Has(type)) {
				return null;
			}
			return _json.Object("items").Object(type);

		}

		public void LoadItem(long version, string type, string data) {
			_json.Object("items").Object(type, new JsonObject()
				.Int64("version", version)
				.String("type", type)
				.Object("data", new JsonObject(data))
			);
		}

		public List<JsonObject> Items {
			get {
				var result = new List<JsonObject>();
				foreach (var key in _json.Object("items").Keys) {
					result.Add(_json.Object("items").Object(key));
				}
				return result;
			}
		}

		public List<JsonObject> ChangedItems {
			get {
				var result = new List<JsonObject>();
				foreach (var key in _json.Object("items").Keys) {
					JsonObject obj = _json.Object("items").Object(key);
					if (obj.Has("changed") && obj.Boolean("changed")) {
						result.Add(obj);
					}
				}
				return result;
			}
		}

		public List<string> Types {
			get {
				return _json.Object("items").Keys;
			}
		}

		public bool IsCache {
			get {
				return _isCache;
			}
		}

		public override string ToString() {
			return _json.ToString(true);
		}
	}
}
