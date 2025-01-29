using Microsoft.AspNetCore.Mvc;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/health")]
	public class HealthController : MimerController {
		private MimerServer _server;

		public HealthController(MimerServer server) {
			_server = server;
		}

		[HttpGet()]
		public IActionResult Get() {
			return Ok("OK");
		}

		[HttpHead()]
		public IActionResult Head() {
			return Ok();
		}

		//[HttpGet("error")]
		//public IActionResult GetError() {
		//	throw new Exception("Test Error");
		//}

	}
}
