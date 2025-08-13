
using Microsoft.AspNetCore.Diagnostics;
using Mimer.Framework;

namespace Mimer.Notes.WebApi.Base {
	public class DevExceptionHandler : IExceptionHandler {
		public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {
			Dev.Log($"Unhandled exception occurred in {httpContext.Request.Method} {httpContext.Request.Path}");
			Dev.Log($"Exception Type: {exception.GetType().Name}");
			Dev.Log(exception);
			return ValueTask.FromResult(false);
		}
	}
}
