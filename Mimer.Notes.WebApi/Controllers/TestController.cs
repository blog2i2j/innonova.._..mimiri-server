using Microsoft.AspNetCore.Mvc;
using Mimer.Framework.Json;
using Mimer.Notes.Server;
using Mimer.Notes.WebApi.Base;
using System.Text;

namespace Mimer.Notes.WebApi.Controllers {
	[ApiController]
	[Route("api/test")]
	public class TestController : MimerController {
		private class TestContext {
			public MimerServer? Server;
			public KeyController? Key;
			public NoteController? Note;
			public UserController? User;
			public NotificationController? Notification;
		}

		private static Dictionary<string, TestContext> _testServers = new Dictionary<string, TestContext>();

		private TestContext? GetTestContext(string testId) {
			lock (_testServers) {
				if (_testServers.ContainsKey(testId)) {
					return _testServers[testId];
				}
			}
			return null;
		}

		[HttpGet("{testId}/begin")]
		public IActionResult Begin(string testId) {
			lock (_testServers) {
				if (!_testServers.ContainsKey(testId)) {
					var testContext = new TestContext();
					testContext.Server = new MimerServer(testId);
					testContext.Key = new KeyController(testContext.Server);
					testContext.Note = new NoteController(testContext.Server);
					testContext.User = new UserController(testContext.Server);
					testContext.Notification = new NotificationController(testContext.Server);
					_testServers.Add(testId, testContext);
				}
			}
			return Content("{}", "text/plain", Encoding.UTF8);
		}

		[HttpGet("{testId}/end/{keepLogs}")]
		public IActionResult End(string testId, bool keepLogs) {
			var testContext = GetTestContext(testId);
			if (testContext != null) {
				testContext.Server!.TearDown(keepLogs);
				lock (_testServers) {
					_testServers.Remove(testId);
				}
			}
			return Content("{}", "text/plain", Encoding.UTF8);
		}

		// user controller
		[HttpPost("{testId}/user/create")]
		public async Task<IActionResult> UserCreate(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.User!.Create(json);
		}

		[HttpPost("{testId}/user/update")]
		public async Task<IActionResult> UserUpdate(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.User!.Update(json);
		}

		[HttpPost("{testId}/user/update-data")]
		public async Task<IActionResult> UserUpdateData(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.User!.UpdateData(json);
		}

		[HttpGet("{testId}/user/pre-login/{username}")]
		public async Task<IActionResult> UserPreLogin(string testId, string username) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.User!.PreLogin(username);
		}

		[HttpPost("{testId}/user/login")]
		public async Task<IActionResult> UserLogin(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.User!.Login(json);
		}

		[HttpPost("{testId}/user/public-key")]
		public async Task<IActionResult> PublicKey(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.User!.PublicKey(json);
		}

		// key controller
		[HttpPost("{testId}/key/create")]
		public async Task<IActionResult> KeyCreate(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Key!.Create(json);
		}

		[HttpPost("{testId}/key/read-all")]
		public async Task<IActionResult> KeyAll(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Key!.ReadAllKeys(json);
		}

		[HttpPost("{testId}/key/read")]
		public async Task<IActionResult> KeyRead(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Key!.Read(json);
		}

		[HttpPost("{testId}/key/delete")]
		public async Task<IActionResult> Delete(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Key!.Delete(json);
		}

		// note controller
		[HttpPost("{testId}/note/create")]
		public async Task<IActionResult> NoteCreate(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.Create(json);
		}

		[HttpPost("{testId}/note/read")]
		public async Task<IActionResult> NoteRead(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.Read(json);
		}

		[HttpPost("{testId}/note/update")]
		public async Task<IActionResult> NoteUpdate(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.Update(json);
		}

		[HttpPost("{testId}/note/delete")]
		public async Task<IActionResult> NoteDelete(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.Delete(json);
		}

		[HttpPost("{testId}/note/multi")]
		public async Task<IActionResult> NoteMulti(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.Multi(json);
		}

		[HttpPost("{testId}/note/share")]
		public async Task<IActionResult> NoteShare(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.Share(json);
		}

		[HttpPost("{testId}/note/share-offers")]
		public async Task<IActionResult> ShareOffers(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.ReadShareOffers(json);
		}

		[HttpPost("{testId}/note/share/delete")]
		public async Task<IActionResult> NoteDeleteShare(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Note!.DeleteShare(json);
		}

		[HttpPost("{testId}/notification/create-url")]
		public async Task<IActionResult> NotificationsCreateUrl(string testId, [FromBody] JsonObject json) {
			var context = GetTestContext(testId);
			if (context == null) {
				return NotFound();
			}
			return await context.Notification!.CreateUrl(json);
		}
	}
}
