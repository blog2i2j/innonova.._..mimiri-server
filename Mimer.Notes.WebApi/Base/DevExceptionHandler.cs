
using Microsoft.AspNetCore.Diagnostics;
using Mimer.Framework;

namespace Mimer.Notes.WebApi.Base
{
    public class DevExceptionHandler : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            Dev.Log(exception);
            return ValueTask.FromResult(false);
        }
    }
}
