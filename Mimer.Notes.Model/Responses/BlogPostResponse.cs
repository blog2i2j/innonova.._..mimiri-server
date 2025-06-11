using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Model.Responses {
	public class BlogPostResponse : IResponseObject {
		private JsonObject _json;

		public BlogPostResponse() {
			_json = new JsonObject();
		}

		public BlogPostResponse(JsonObject json) {
			_json = json;
		}

		public BlogPostResponse(BlogPost blogPost) {
			_json = blogPost.Json();
		}

		public void SetBlogPost(BlogPost blogPost) {
			_json = blogPost.Json();
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
