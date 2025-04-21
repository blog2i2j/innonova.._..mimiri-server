
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class UserDataResponse : IResponseObject {
		private JsonObject _json;

		public UserDataResponse() {
			_json = new JsonObject();
		}

		public UserDataResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public string Data {
			get {
				return _json.String("data");
			}
			set {
				_json.String("data", value);
			}
		}

		public string Config {
			get {
				return _json.String("config");
			}
			set {
				_json.String("config", value);
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

		public long NoteCount {
			get {
				return _json.Int64("noteCount");
			}
			set {
				_json.Int64("noteCount", value);
			}
		}

		public long MaxTotalBytes {
			get {
				return _json.Int64("maxTotalBytes");
			}
			set {
				_json.Int64("maxTotalBytes", value);
			}
		}

		public long MaxNoteBytes {
			get {
				return _json.Int64("maxNoteBytes");
			}
			set {
				_json.Int64("maxNoteBytes", value);
			}
		}

		public long MaxNoteCount {
			get {
				return _json.Int64("maxNoteCount");
			}
			set {
				_json.Int64("maxNoteCount", value);
			}
		}

		public long MaxHistoryEntries {
			get {
				return _json.Int64("maxHistoryEntries");
			}
			set {
				_json.Int64("maxHistoryEntries", value);
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
