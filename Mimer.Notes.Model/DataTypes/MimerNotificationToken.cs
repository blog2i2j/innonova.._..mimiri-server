using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class MimerNotificationToken : ISignable {
		private JsonObject _json;

		public MimerNotificationToken() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			TokenId = Guid.NewGuid();
		}

		public MimerNotificationToken(JsonObject json) {
			_json = json;
		}

		public string Url {
			get {
				return _json.String("url");
			}
			set {
				_json.String("url", value);
			}
		}

		public string UserId {
			get {
				return _json.String("userId");
			}
			set {
				_json.String("userId", value);
			}
		}

		public string Username {
			get {
				return _json.String("username");
			}
			set {
				_json.String("username", value);
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

		public Guid TokenId {
			get {
				return _json.Guid("tokenId");
			}
			set {
				_json.Guid("tokenId", value);
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
