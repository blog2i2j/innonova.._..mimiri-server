using Microsoft.AspNetCore.Mvc;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;
using System.Text;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/key")]
	public class KeyController : MimerController {
		private MimerServer _server;

		public KeyController(MimerServer server) {
			_server = server;
		}

		[HttpPost("create")]
		public async Task<IActionResult> Create([FromBody] JsonObject json) {
			var response = await _server.CreateKey(new CreateKeyRequest(json));
			if (response == null) {
				return Conflict();
			}
			return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
		}

		[HttpPost("read-all")]
		public async Task<IActionResult> ReadAllKeys([FromBody] JsonObject json) {
			var response = await _server.ReadAllKeys(new BasicRequest(json));
			if (response == null) {
				return NotFound();
			}
			return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
		}

		[HttpPost("read")]
		public async Task<IActionResult> Read([FromBody] JsonObject json) {
			var response = await _server.ReadKey(new ReadKeyRequest(json));
			if (response == null) {
				return NotFound();
			}
			return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
		}

		[HttpPost("delete")]
		public async Task<IActionResult> Delete([FromBody] JsonObject json) {
			var response = await _server.DeleteKey(new DeleteKeyRequest(json));
			if (response == null) {
				return NotFound();
			}
			return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
		}


	}
}
