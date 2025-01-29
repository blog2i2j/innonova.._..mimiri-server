
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class PublicKeyResponse : IResponseObject {
		private JsonObject _json;

		public PublicKeyResponse() {
			_json = new JsonObject();
		}

		public PublicKeyResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public string AsymmetricAlgorithm {
			get {
				return _json.String("asymmetricAlgorithm");
			}
			set {
				_json.String("asymmetricAlgorithm", value);
			}
		}

		public string PublicKey {
			get {
				return _json.String("publicKey");
			}
			set {
				_json.String("publicKey", value);
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
