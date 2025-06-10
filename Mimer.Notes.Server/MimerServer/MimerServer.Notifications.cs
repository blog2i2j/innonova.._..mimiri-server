using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;
using System.Text;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Notification services for MimerServer
	/// </summary>
	public partial class MimerServer {


		private async Task SendNoteUpdate(Guid senderId, Guid keyName, Guid noteId) {
			try {
				var note = await _dataSource.GetNote(noteId);
				JsonObject payload = new JsonObject();
				payload.Guid("id", noteId);
				payload.Array("versions", new JsonArray());
				foreach (var item in note!.Items) {
					payload.Array("versions").Add(new JsonObject().String("type", item.Type).Int64("version", item.Version));
				}
				var userIds = await _dataSource.GetUserIdsByKeyName(keyName);
				await SendNotification(senderId, userIds, "note-update", payload.ToString());
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}

		public async Task<NotificationUrlResponse?> CreateNotificationUrl(BasicRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var token = new MimerNotificationToken();
					token.Url = $"{WebsocketUrl}/notifications";
					token.Username = user.Username;
					token.UserId = user.Id.ToString();
					_signature.SignRequest("mimer", token);
					var response = new NotificationUrlResponse();
					response.Url = token.Url;
					response.Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(token.ToJsonString()));
					return response;
				}
			}
			return null;
		}

		private async Task SendNotification(Guid sender, List<Guid> recipients, string type, string payload) {
			var serverRequest = new ServerNotificationRequest();
			serverRequest.Sender = sender;
			serverRequest.Recipients = string.Join(",", recipients);
			serverRequest.Type = type;
			serverRequest.Payload = payload;
			_signature.SignRequest("mimer", serverRequest);
			var content = new StringContent(serverRequest.ToJsonString(), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync($"{NotificationsUrl}/api/notification/send", content);
			response.EnsureSuccessStatusCode();
			foreach (Guid userId in recipients) {
				_userStatsManager.RegisterNotification(userId);
			}
		}

		public async Task NotifyUpdate() {
			try {
				var serverRequest = new ServerNotificationRequest();
				serverRequest.Type = "bundle-update";
				_signature.SignRequest("mimer", serverRequest);
				var content = new StringContent(serverRequest.ToJsonString(), Encoding.UTF8, "application/json");
				var response = await _httpClient.PostAsync($"{NotificationsUrl}/api/notification/send", content);
				response.EnsureSuccessStatusCode();
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}
	}
}
