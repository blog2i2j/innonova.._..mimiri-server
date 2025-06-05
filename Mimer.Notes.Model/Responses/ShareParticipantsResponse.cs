
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class ShareParticipantsResponse : IResponseObject {
		private JsonObject _json;

		public ShareParticipantsResponse() {
			_json = new JsonObject();
			_json.Array("participants", new JsonArray());
		}

		public ShareParticipantsResponse(JsonObject json) {
			_json = json;
		}

		public void AddParticipant(string username, DateTime since) {
			_json.Array("participants").Add(new JsonObject()
				.String("username", username)
				.DateTime("since", since)
			);
		}

		public List<JsonObject> Participants {
			get {
				var result = new List<JsonObject>();
				foreach (var share in _json.Array("participants").AsObjects()) {
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
