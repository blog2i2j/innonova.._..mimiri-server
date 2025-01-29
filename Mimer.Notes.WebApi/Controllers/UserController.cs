using Microsoft.AspNetCore.Mvc;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;
using System.Text;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/user")]
	public class UserController : MimerController {
		private MimerServer _server;

		public UserController(MimerServer server) {
			_server = server;
		}

		[HttpPost("create")]
		public async Task<IActionResult> Create([FromBody] JsonObject json) {
			if (json.Has("keyId")) {
				json = _server.DecryptRequest(json);
			}
			var response = await _server.CreateUser(new CreateUserRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return Conflict();
		}

		[HttpPost("update")]
		public async Task<IActionResult> Update([FromBody] JsonObject json) {
			if (json.Has("keyId")) {
				json = _server.DecryptRequest(json);
			}
			var response = await _server.UpdateUser(new UpdateUserRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return Conflict();
		}

		[HttpPost("update-data")]
		public async Task<IActionResult> UpdateData([FromBody] JsonObject json) {
			var response = await _server.UpdateUserData(new UpdateUserDataRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return Conflict();
		}

		[HttpGet("pre-login/{username}")]
		public async Task<IActionResult> PreLogin(string username) {
			var response = await _server.PreLogin(username);
			return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] JsonObject json) {
			var response = await _server.Login(new LoginRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return NotFound();
		}

		[HttpPost("get-data")]
		public async Task<IActionResult> GetUserData([FromBody] JsonObject json) {
			var response = await _server.GetUserData(new BasicRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return NotFound();
		}

		[HttpPost("public-key")]
		public async Task<IActionResult> PublicKey([FromBody] JsonObject json) {
			var response = await _server.GetPublicKey(new PublicKeyRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return NotFound();
		}

		[HttpPost("delete")]
		public async Task<IActionResult> DeleteUser([FromBody] JsonObject json) {
			var response = await _server.DeleteUser(new DeleteAccountRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return NotFound();
		}

		[HttpPost("available")]
		public async Task<IActionResult> UsernameAvailable([FromBody] JsonObject json) {
			var response = await _server.UsernameAvailable(new CheckUsernameRequest(json));
			if (response != null) {
				return Content(response.ToJsonString(), "text/plain", Encoding.UTF8);
			}
			return NotFound();
		}

	}
}
