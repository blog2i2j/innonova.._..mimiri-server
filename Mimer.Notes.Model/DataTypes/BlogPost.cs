using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class BlogPost {
		private JsonObject _json;

		public BlogPost() {
			_json = new JsonObject();
		}

		public BlogPost(JsonObject json) {
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

		public string Title {
			get {
				return _json.String("title");
			}
			set {
				_json.String("title", value);
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

		public string Content {
			get {
				return _json.String("content");
			}
			set {
				_json.String("content", value);
			}
		}

		public bool Published {
			get {
				return _json.Boolean("published");
			}
			set {
				_json.Boolean("published", value);
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
