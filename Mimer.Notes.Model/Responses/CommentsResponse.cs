using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Model.Responses {

	public class CommentsResponse : IResponseObject {
		private JsonObject _json;

		public CommentsResponse() {
			_json = new JsonObject();
			_json.Array("comments", new JsonArray());
		}

		public CommentsResponse(JsonObject json) {
			_json = json;
		}

		public void AddCommentInfo(Comment comment) {
			_json.Array("comments").Add(comment.Json());
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
