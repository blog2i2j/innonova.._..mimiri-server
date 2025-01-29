using Microsoft.AspNetCore.Mvc;

namespace Mimer.Notes.SignalR.Controllers {
	[ApiController]
	[Route("/")]
	public class HealthController : ControllerBase {
		private NotificationServer _server;

		public HealthController(NotificationServer server) {
			_server = server;
		}

		[HttpGet()]
		public IActionResult Health() {
			return Content("OK");
		}

		[HttpHead()]
		public IActionResult Head() {
			return Ok();
		}
	}

}
