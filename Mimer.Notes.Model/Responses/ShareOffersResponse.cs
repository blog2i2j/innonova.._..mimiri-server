
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class ShareOffersResponse : IResponseObject {
		private JsonObject _json;

		public ShareOffersResponse() {
			_json = new JsonObject();
			_json.Array("offers", new JsonArray());
		}

		public ShareOffersResponse(JsonObject json) {
			_json = json;
		}

		public void AddOffer(Guid id, string sender, string data) {
			_json.Array("offers").Add(new JsonObject()
				.Guid("id", id)
				.String("sender", sender)
				.String("data", data)
			);
		}

		public List<JsonObject> Offers {
			get {
				var result = new List<JsonObject>();
				foreach (var share in _json.Array("offers").AsObjects()) {
					result.Add(share);
				}
				return result;
			}
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
