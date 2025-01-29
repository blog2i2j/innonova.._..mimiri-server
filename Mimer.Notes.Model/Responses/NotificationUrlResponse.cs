
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class NotificationUrlResponse : IResponseObject {
		private JsonObject _json;

		public NotificationUrlResponse() {
			_json = new JsonObject();
		}

		public NotificationUrlResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public string Url {
			get {
				return _json.String("url");
			}
			set {
				_json.String("url", value);
			}
		}

		public string Token {
			get {
				return _json.String("token");
			}
			set {
				_json.String("token", value);
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
