using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class CheckUsernameRequest : IRequestObject {
		private JsonObject _json;

		public CheckUsernameRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public CheckUsernameRequest(JsonObject json) {
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

		public string Pow {
			get {
				return _json.String("pow");
			}
			set {
				_json.String("pow", value);
			}
		}

		public DateTime TimeStamp {
			get {
				return _json.DateTime("timestamp");
			}
			set {
				_json.DateTime("timestamp", value);
			}
		}

		public Guid RequestId {
			get {
				return _json.Guid("requestId");
			}
			set {
				_json.Guid("requestId", value);
			}
		}
		public bool IsValid {
			get {
				try {
					return !(
						string.IsNullOrWhiteSpace(Username) ||
						string.IsNullOrWhiteSpace(Pow)
					);
				}
				catch {
					return false;
				}
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
