using Microsoft.AspNetCore.Mvc;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;
using System.Text;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/feedback")]
	public class FeedbackController : MimerController {
		private MimerServer _server;

		public FeedbackController(MimerServer server) {
			_server = server;
		}

		[HttpPost("add-comment")]
		public async Task<IActionResult> Create([FromBody] JsonObject json) {
			_server.RegisterAction(Info, "feedback/add-comment");
			var response = await _server.AddComment(new AddCommentRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return Conflict();
		}

		[HttpPost("get-comments")]
		public async Task<IActionResult> GetComments([FromBody] JsonObject json) {
			_server.RegisterAction(Info, "feedback/get-comments");
			var response = await _server.GetCommentsByPost(new GetCommentsRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return NotFound();
		}

		[HttpPost("latest-posts")]
		public async Task<IActionResult> GetLatestBlogPosts([FromBody] JsonObject json) {
			_server.RegisterAction(Info, "feedback/latest-posts");
			var response = await _server.GetLatestBlogPosts(new GetBlogPostsRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return NotFound();
		}
	}
}
