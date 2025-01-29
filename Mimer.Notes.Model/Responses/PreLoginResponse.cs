
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class PreLoginResponse : IResponseObject {
		private JsonObject _json;

		public PreLoginResponse() {
			_json = new JsonObject();
		}

		public PreLoginResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public string Salt {
			get {
				return _json.String("salt");
			}
			set {
				_json.String("salt", value);
			}
		}

		public int Iterations {
			get {
				return _json.Int32("iterations");
			}
			set {
				_json.Int32("iterations", value);
			}
		}

		public string Algorithm {
			get {
				return _json.String("algorithm");
			}
			set {
				_json.String("algorithm", value);
			}
		}

		public string Challenge {
			get {
				return _json.String("challenge");
			}
			set {
				_json.String("challenge", value);
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
