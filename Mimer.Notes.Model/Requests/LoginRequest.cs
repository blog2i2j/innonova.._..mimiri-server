using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class LoginRequest : IRequestObject {
		private JsonObject _json;

		public LoginRequest() {
			_json = new JsonObject();
		}

		public LoginRequest(JsonObject json) {
			_json = json;
		}

		public string Username {
			get {
				return _json.String("username");
			}
			set {
				_json.String("username", value);
			}
		}

		public string Response {
			get {
				return _json.String("response");
			}
			set {
				_json.String("response", value);
			}
		}

		public int HashLength {
			get {
				if (!_json.Has("hashLength")) {
					return 8192;
				}
				return _json.Int32("hashLength");
			}
			set {
				_json.Int32("hashLength", value);
			}
		}

		public bool IsValid {
			get {
				return !(
					string.IsNullOrWhiteSpace(Username) ||
					string.IsNullOrWhiteSpace(Response)
				);
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
