
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class ShareResponse : IResponseObject {
		private JsonObject _json;

		public ShareResponse() {
			_json = new JsonObject();
		}

		public ShareResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public string Code {
			get {
				return _json.String("code");
			}
			set {
				_json.String("code", value);
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
