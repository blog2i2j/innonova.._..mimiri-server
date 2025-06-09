using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class Comment {
		private JsonObject _json;

		public Comment() {
			_json = new JsonObject();
		}

		public Comment(JsonObject json) {
			_json = json;
		}

		public string Id {
			get {
				return _json.String("id");
			}
			set {
				_json.String("id", value);
			}
		}

		public Guid PostId {
			get {
				return _json.Guid("postId");
			}
			set {
				_json.Guid("postId", value);
			}
		}

		public Guid UserId {
			get {
				return _json.Guid("userId");
			}
			set {
				_json.Guid("userId", value);
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

		public string CommentText {
			get {
				return _json.String("comment");
			}
			set {
				_json.String("comment", value);
			}
		}

		public string ModerationState {
			get {
				var value = _json.String("moderationState");
				return string.IsNullOrEmpty(value) ? "pending" : value;
			}
			set {
				_json.String("moderationState", value);
			}
		}

		public DateTime Created {
			get {
				return _json.DateTime("created");
			}
			set {
				_json.DateTime("created", value);
			}
		}

		public DateTime Modified {
			get {
				return _json.DateTime("modified");
			}
			set {
				_json.DateTime("modified", value);
			}
		}

		public JsonObject Json() {
			return _json;
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
