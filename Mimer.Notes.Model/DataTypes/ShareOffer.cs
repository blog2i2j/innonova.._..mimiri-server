using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class ShareOffer {
		private JsonObject _json;


		public ShareOffer(JsonObject json) {
			_json = json;
		}

		public string Sender {
			get {
				return _json.String("sender");
			}
			set {
				_json.String("sender", value);
			}
		}

		public JsonObject Data {
			get {
				return _json.Object("data");
			}
			set {
				_json.Object("data", value);
			}
		}

		public string ToJsonString() {
			return _json.ToString();
		}

	}
}
