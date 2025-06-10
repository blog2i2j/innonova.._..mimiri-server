using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class AddBlogPostRequest : ISignable, IRequestObject, INonRepeatableRequest {
		private JsonObject _json;

		public AddBlogPostRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public AddBlogPostRequest(JsonObject json) {
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

		public string Title {
			get {
				return _json.String("title");
			}
			set {
				_json.String("title", value);
			}
		}

		public string FileName {
			get {
				return _json.String("fileName");
			}
			set {
				_json.String("fileName", value);
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

		public bool IsValid {
			get {
				return !(
					string.IsNullOrWhiteSpace(Username) ||
					string.IsNullOrWhiteSpace(Title) ||
					string.IsNullOrWhiteSpace(FileName)
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
