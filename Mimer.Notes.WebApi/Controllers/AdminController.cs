using Microsoft.AspNetCore.Mvc;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;
using System.Text;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/admin")]
	public class AdminController : MimerController {
		private MimerServer _server;

		public AdminController(MimerServer server) {
			_server = server;
		}

		[HttpPost("promote-user")]
		public async Task<IActionResult> PromoteUser([FromBody] JsonObject json) {
			_server.RegisterAction(Info, "admin/promote-user");
			var response = await _server.PromoteUserToAdmin(new PromoteUserRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return Conflict();
		}

	}
}
