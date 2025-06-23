using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class SyncRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public SyncRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public SyncRequest(JsonObject json) {
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

		public DateTime Since {
			get {
				return _json.DateTime("since");
			}
			set {
				_json.DateTime("since", value);
			}
		}

		public string PayloadToSign {
			get {
				return _json.ToFilteredString(SignatureFilter.Default);
			}
		}

		public void AddSignature(string name, string signature) {
			if (!_json.Has("signatures")) {
				_json.Array("signatures", new JsonArray());
			}
			_json.Array("signatures").Add(new JsonObject()
				.String("name", name)
				.String("signature", signature)
			);
		}

		public string? GetSignature(string name) {
			if (_json.Has("signatures")) {
				foreach (var sig in _json.Array("signatures").AsObjects()) {
					if (sig.String("name") == name) {
						return sig.String("signature");
					}
				}
			}
			return null;
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
				return !(
					string.IsNullOrWhiteSpace(Username)
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
