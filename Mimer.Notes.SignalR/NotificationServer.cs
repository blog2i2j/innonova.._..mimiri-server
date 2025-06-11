using Microsoft.AspNetCore.SignalR;
using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.SignalR.Hubs;

namespace Mimer.Notes.SignalR {
	public class NotificationServer {
		public static string? CertPath { get; set; }
		private static Dictionary<string, List<string>> _connectionIds = new Dictionary<string, List<string>>();
		private IHubClients _clients;
		private CryptSignature _signature;

		public static void AddClient(string userId, string connectionId) {
			lock (_connectionIds) {
				if (!_connectionIds.ContainsKey(userId)) {
					_connectionIds.Add(userId, new List<string>());
				}
				if (!_connectionIds[userId].Contains(connectionId)) {
					_connectionIds[userId].Add(connectionId);
				}
			}
		}

		public static void RemoveClient(string userId, string connectionId) {
			lock (_connectionIds) {
				if (_connectionIds.ContainsKey(userId)) {
					if (_connectionIds[userId].Contains(connectionId)) {
						_connectionIds[userId].Remove(connectionId);
					}
					if (_connectionIds[userId].Count == 0) {
						_connectionIds.Remove(userId);
					}
				}
			}
		}

		public NotificationServer(IHubContext<NotificationsHub> hub) {
			_clients = hub.Clients;
			_signature = new CryptSignature("RSA;3072", File.ReadAllText(Path.Combine(CertPath!, "server.pub")));
		}

		public async Task Send(ServerNotificationRequest request) {
			try {
				if (_signature.VerifySignature("mimer", request)) {
					if (request.Type == "bundle-update") {
						await _clients.All.SendAsync("notification", "", request.Type);
					}
					else if (request.Type == "blog-post") {
						await _clients.All.SendAsync("notification", "", request.Type);
					}
					else {
						string[] userIds = request.Recipients.Split(",");
						List<string> connectionIds = new List<string>();
						foreach (var userId in userIds) {
							lock (_connectionIds) {
								if (_connectionIds.ContainsKey(userId)) {
									connectionIds.AddRange(_connectionIds[userId]);
								}
							}
						}
						if (connectionIds.Count > 0) {
							await _clients.Clients(connectionIds).SendAsync("notification", request.Sender, request.Type, request.Payload);
						}
					}
				}
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}

	}
}
