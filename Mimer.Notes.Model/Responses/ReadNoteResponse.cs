
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class ReadNoteResponse : IResponseObject {
		private JsonObject _json;

		public ReadNoteResponse() {
			_json = new JsonObject();
			_json.Array("items", new JsonArray());
		}

		public ReadNoteResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public void AddItem(long version, string type, string data, DateTime created, DateTime modified, int size) {
			_json.Array("items").Add(new JsonObject()
				.Int64("version", version)
				.String("type", type)
				.Boolean("updated", true)
				.String("data", data)
				.DateTime("created", created)
				.DateTime("modified", modified)
				.Int32("size", size)
			);
		}

		public void AddItem(long version, string type, DateTime created, DateTime modified, int size) {
			_json.Array("items").Add(new JsonObject()
				.Int64("version", version)
				.String("type", type)
				.Boolean("updated", false)
				.DateTime("created", created)
				.DateTime("modified", modified)
				.Int32("size", size)
			);
		}

		public List<JsonObject> Items {
			get {
				var result = new List<JsonObject>();
				if (_json.Has("items")) {
					foreach (var item in _json.Array("items").AsObjects()) {
						result.Add(item);
					}
				}
				return result;
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

		public long Sync {
			get {
				return _json.Int64("sync");
			}
			set {
				_json.Int64("sync", value);
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

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
