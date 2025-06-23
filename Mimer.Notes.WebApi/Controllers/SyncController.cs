using Microsoft.AspNetCore.Mvc;
using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;
using System.Text;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/sync")]
	public class SyncController : MimerController {
		private MimerServer _server;

		public SyncController(MimerServer server) {
			_server = server;
		}

		[HttpPost("changes-since")]
		public async Task<IActionResult> ChangesSince([FromBody] JsonObject json) {
			_server.RegisterAction(Info, "sync/changes-since");
			var response = await _server.Sync(new SyncRequest(json));
			if (response == null) {
				return Unauthorized();
			}
			return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
		}
	}
}