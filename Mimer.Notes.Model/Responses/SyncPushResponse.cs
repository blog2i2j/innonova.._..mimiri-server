using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Model.Responses {
	public class SyncPushResponse : IResponseObject {
		private JsonObject _json;

		public SyncPushResponse() {
			_json = new JsonObject();
			_json.Array("results", new JsonArray());
		}

		public SyncPushResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public void AddSyncResult(SyncResult result) {
			if (!_json.Has("results")) {
				_json.Array("results", new JsonArray());
			}
			_json.Array("results").Add(new JsonObject()
				.String("itemType", result.ItemType)
				.String("action", result.Action)
				.Guid("id", result.Id)
				.String("type", result.Type)
				.Int64("expected", result.Expected)
				.Int64("actual", result.Actual)
			);
		}

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
