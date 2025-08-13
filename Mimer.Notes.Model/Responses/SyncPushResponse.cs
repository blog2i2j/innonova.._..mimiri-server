using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Model.Responses {
	public class SyncPushResponse : IResponseObject {
		private JsonObject _json;

		public SyncPushResponse() {
			_json = new JsonObject();
		}

		public SyncPushResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public string Status {
			get {
				return _json.String("status");
			}
			set {
				_json.String("status", value);
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
