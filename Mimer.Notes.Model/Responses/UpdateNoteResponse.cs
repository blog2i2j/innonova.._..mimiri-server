
using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Model.Responses {
	public class UpdateNoteResponse : IResponseObject {
		private JsonObject _json;

		public UpdateNoteResponse() {
			_json = new JsonObject();
		}

		public UpdateNoteResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public void AddVersionConflict(VersionConflict conflict) {
			if (!_json.Has("conflicts")) {
				_json.Array("conflicts", new JsonArray());
			}
			_json.Array("conflicts").Add(new JsonObject()
				.String("type", conflict.Type)
				.Int64("expected", conflict.Expected)
				.Int64("actual", conflict.Actual)
			);
		}

		public IReadOnlyList<VersionConflict> Conflicts {
			get {
				List<VersionConflict> result = new List<VersionConflict>();
				if (_json.Has("conflicts")) {
					foreach (var conflict in _json.Array("conflicts").AsObjects()) {
						result.Add(new VersionConflict(conflict.String("type"), conflict.Int64("expected"), conflict.Int64("actual")));
					}
				}
				return result;
			}
		}

		public bool Success {
			get {
				return _json.Boolean("success");
			}
			set {
				_json.Boolean("success", value);
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

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
