
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class CreateKeyResponse : IResponseObject {
		private JsonObject _json;

		public CreateKeyResponse() {
			_json = new JsonObject();
		}

		public CreateKeyResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public bool Success {
			get {
				return _json.Boolean("success");
			}
			set {
				_json.Boolean("success", value);
			}
		}

		public long MaxCount {
			get {
				return _json.Int64("maxCount");
			}
			set {
				_json.Int64("maxCount", value);
			}
		}

		public long MaxSize {
			get {
				return _json.Int64("maxSize");
			}
			set {
				_json.Int64("maxSize", value);
			}
		}

		public long Count {
			get {
				return _json.Int64("count");
			}
			set {
				_json.Int64("count", value);
			}
		}

		public long Size {
			get {
				return _json.Int64("size");
			}
			set {
				_json.Int64("size", value);
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
