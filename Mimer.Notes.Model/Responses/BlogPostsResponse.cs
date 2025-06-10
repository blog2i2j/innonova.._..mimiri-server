using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Model.Responses {

	public class BlogPostsResponse : IResponseObject {
		private JsonObject _json;

		public BlogPostsResponse() {
			_json = new JsonObject();
			_json.Array("posts", new JsonArray());
		}

		public BlogPostsResponse(JsonObject json) {
			_json = json;
		}

		public void AddBlogPost(BlogPost blogPost) {
			_json.Array("posts").Add(blogPost.Json());
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
