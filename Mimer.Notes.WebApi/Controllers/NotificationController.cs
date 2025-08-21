using Microsoft.AspNetCore.Mvc;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;
using System.Text;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/notification")]
	public class NotificationController : MimerController {
		private MimerServer _server;

		public NotificationController(MimerServer server) {
			_server = server;
		}

		[HttpPost("create-url")]
		public async Task<IActionResult> CreateUrl([FromBody] JsonObject json) {
			_server.RegisterAction(Info, "notification/create-url");
			var response = await _server.CreateNotificationUrl(new BasicRequest(json), Info);
			if (response == null) {
				return Conflict();
			}
			return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
		}

		[HttpPost("send")]
		public IActionResult Send([FromBody] JsonObject json) {
			_server.RegisterAction(Info, "notification/send");
			return Content(new BasicResponse().ToJsonString(), "text/plain", Encoding.UTF8);
		}

		[HttpGet("notify-update")]
		[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
		public async Task<IActionResult> NotifyUpdate() {
			_server.RegisterAction(Info, "notification/notify-update");
			await _server.NotifyUpdate();
			return Content(new BasicResponse().ToJsonString(), "text/plain", Encoding.UTF8);
		}


	}
}
