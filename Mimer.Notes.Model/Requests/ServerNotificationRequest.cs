using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class ServerNotificationRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public ServerNotificationRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public ServerNotificationRequest(JsonObject json) {
			_json = json;
		}

		public Guid Sender {
			get {
				return _json.Guid("sender");
			}
			set {
				_json.Guid("sender", value);
			}
		}

		public string Recipients {
			get {
				return _json.String("recipients");
			}
			set {
				_json.String("recipients", value);
			}
		}

		public string Type {
			get {
				return _json.String("type");
			}
			set {
				_json.String("type", value);
			}
		}

		public string Payload {
			get {
				return _json.String("payload");
			}
			set {
				_json.String("payload", value);
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
			foreach (var signature in _json.Array("signatures").AsObjects()) {
				if (signature.String("name") == name) {
					return signature.String("signature");
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
				try {
					return !(
						string.IsNullOrWhiteSpace(Recipients) ||
						Sender == Guid.Empty ||
						string.IsNullOrWhiteSpace(Type) ||
						string.IsNullOrWhiteSpace(Payload)
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
