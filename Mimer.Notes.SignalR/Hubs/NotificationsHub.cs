using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mimer.Notes.SignalR.Hubs {
	[Authorize()]
	public class NotificationsHub : Hub {

		public override async Task OnConnectedAsync() {
			await base.OnConnectedAsync();
			if (Context != null) {
				var user = Context.User;
				if (user != null) {
					var id = user.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
					if (id != null) {
						NotificationServer.AddClient(id, Context.ConnectionId);
					}
				}
			}
		}

		public override async Task OnDisconnectedAsync(Exception? exception) {
			await base.OnDisconnectedAsync(exception);
			if (Context != null) {
				var user = Context.User;
				if (user != null) {
					var id = user.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
					if (id != null) {
						NotificationServer.RemoveClient(id, Context.ConnectionId);
					}

				}
			}

		}

	}
}