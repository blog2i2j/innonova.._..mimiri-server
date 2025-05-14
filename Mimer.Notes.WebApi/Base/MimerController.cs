using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Mimer.Notes.Server;

namespace Mimer.Notes.WebApi.Base {
	public class MimerController : ControllerBase {

		public ClientInfo Info {
			get {
				string userAgent = "";
				StringValues userAgentValues;
				if (this.HttpContext.Request.Headers.TryGetValue("User-Agent", out userAgentValues)) {
					userAgent = userAgentValues[0] ?? "";
				}
				string mimriVersion = "";
				StringValues mimriVersionValues;
				if (this.HttpContext.Request.Headers.TryGetValue("X-Mimiri-Version", out mimriVersionValues)) {
					mimriVersion = mimriVersionValues[0] ?? "";
				}
				return new ClientInfo(userAgent, mimriVersion);
			}
		}
	}
}
