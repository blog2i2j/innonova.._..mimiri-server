using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Requests {
	public class GetBlogPostsRequest : ISignable, IRequestObject, IRepeatableRequest {
		private JsonObject _json;

		public GetBlogPostsRequest() {
			_json = new JsonObject();
			TimeStamp = DateTime.UtcNow;
			RequestId = Guid.NewGuid();
		}

		public GetBlogPostsRequest(JsonObject json) {
			_json = json;
		}

		public int Count {
			get {
				return _json.Int32("count");
			}
			set {
				_json.Int32("count", value);
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
					Count == 0
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
