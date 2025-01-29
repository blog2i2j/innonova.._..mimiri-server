using Microsoft.AspNetCore.Mvc;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Requests;

namespace Mimer.Notes.SignalR.Controllers {
	[ApiController]
	[Route("/api/notification")]
	public class NotificationController : ControllerBase {
		private NotificationServer _server;

		public NotificationController(NotificationServer server) {
			_server = server;
		}

		[HttpPost("send")]
		public async Task<IActionResult> Send([FromBody] JsonObject json) {
			await _server.Send(new ServerNotificationRequest(json));
			return Ok();
		}
	}

}
