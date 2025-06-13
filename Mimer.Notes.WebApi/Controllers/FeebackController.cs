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

	}
}
