
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class CheckUsernameResponse : IResponseObject {
		private JsonObject _json;

		public CheckUsernameResponse() {
			_json = new JsonObject();
		}

		public CheckUsernameResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public string Username {
			get {
				return _json.String("username");
			}
			set {
				_json.String("username", value);
			}
		}

		public bool Available {
			get {
				return _json.Boolean("available");
			}
			set {
				_json.Boolean("available", value);
			}
		}

		public bool ProofAccepted {
			get {
				return _json.Boolean("proofAccepted");
			}
			set {
				_json.Boolean("proofAccepted", value);
			}
		}

		public int BitsExpected {
			get {
				return _json.Int32("bitsExpected");
			}
			set {
				_json.Int32("bitsExpected", value);
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
