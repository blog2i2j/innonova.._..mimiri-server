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

		public Guid Id {
			get {
				return _json.Guid("id");
			}
			set {
				_json.Guid("id", value);
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
