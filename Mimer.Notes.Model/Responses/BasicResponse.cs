
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class BasicResponse : IResponseObject {
		private JsonObject _json;

		public BasicResponse() {
			_json = new JsonObject();
		}

		public BasicResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
