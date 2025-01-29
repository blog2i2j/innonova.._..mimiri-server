
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

		public void AddItem(long version, string type, string data) {
			_json.Array("items").Add(new JsonObject()
				.Int64("version", version)
				.String("type", type)
				.Boolean("updated", true)
				.String("data", data)
			);
		}

		public void AddItem(long version, string type) {
			_json.Array("items").Add(new JsonObject()
				.Int64("version", version)
				.String("type", type)
				.Boolean("updated", false)
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

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
