
using Mimer.Framework;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mimer.Notes.Server {
	public class GlobalStatistic {
		public string Id { get; set; } = "";
		public string Type { get; set; } = "";
		public string Action { get; set; } = "";
		public string Key { get; set; } = "";
		public long Value { get; set; }
		public DateTime LastActivity { get; set; }
	}

	public class ClientInfo {
		private static Regex UserAgentPart = new Regex(@"(?<name>\S+)/(?<value>\S+)");
		public string UserAgent { get; set; }
		public string MimiriVersion { get; set; }
		public string Client { get; set; }
		public string Environment { get; set; }
		public string BundleVersion { get; set; }
		public string HostVersion { get; set; }
		public string ElectronVersion { get; set; }
		public string ChromeVersion { get; set; }
		public string FirefoxVersion { get; set; }

		public ClientInfo(string userAgent, string mimiriVersion) {
			UserAgent = userAgent.Trim();
			MimiriVersion = mimiriVersion.Trim();
			if (MimiriVersion.Length > 0) {
				var items = MimiriVersion.Split(';');
				var clientEnvironment = items[0].Split('-');
				Client = clientEnvironment[0];
				Environment = clientEnvironment.Length > 1 ? clientEnvironment[1] : "";
				BundleVersion = items[1];
				HostVersion = items[2];
			}
			else {
				Client = "unknown";
				Environment = "unknown";
				BundleVersion = "2.1.0";
				HostVersion = "2.1.0";
			}
			ElectronVersion = "";
			ChromeVersion = "";
			FirefoxVersion = "";
			if (UserAgent.Length > 0) {
				foreach (var match in UserAgentPart.Matches(UserAgent).ToList<Match>()) {
					Dev.Log(match);
					if (match.Success) {
						if (match.Groups[1].Value == "MimiriNotes" && HostVersion == "2.1.0") {
							HostVersion = match.Groups[2].Value;
						}
						if (match.Groups[1].Value == "Electron") {
							ElectronVersion = match.Groups[2].Value;
							if (Client == "unknown") {
								Client = "Electron";
							}
							if (HostVersion == "") {
								HostVersion = match.Groups[2].Value;
							}
						}
						if (match.Groups[1].Value == "Chrome") {
							ChromeVersion = match.Groups[2].Value;
						}
						if (match.Groups[1].Value == "Firefox") {
							FirefoxVersion = match.Groups[2].Value;
						}
					}
				}
			}
			if (MimiriVersion.Length == 0) {
				MimiriVersion = $"{Client}-{Environment};{BundleVersion};{HostVersion}";
			}
		}

		public override string ToString() {
			return $"Client: {Client}, Environment: {Environment}, BundleVersion: {BundleVersion}, HostVersion: {HostVersion}, ElectronVersion: {ElectronVersion}";
		}

	}

	class ClientAction {
		public ClientInfo Info { get; set; }
		public string Action { get; set; }
		public ClientAction(ClientInfo info, string action) {
			Info = info;
			Action = action;
		}
	}

	public class GlobalStatsManager {
		private IMimerDataSource _dataSource;
		private Dictionary<string, GlobalStatistic> _stats = new Dictionary<string, GlobalStatistic>();
		private Queue<ClientAction> _queue = new Queue<ClientAction>();

		public GlobalStatsManager(IMimerDataSource dataSource) {
			_dataSource = dataSource;
			Task.Run(Execute);
			//var info = new ClientInfo("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) MimiriNotes/2.1.70 Chrome/128.0.6613.84 Electron/32.0.2 Safari/537.36", "Electron-Windows;2.1.100;2.1.70");
			//Dev.Log(info);
		}

		private async Task Execute() {
			DateTime nextWrite = DateTime.UtcNow.AddSeconds(10);
			while (true) {
				Thread.Sleep(100);
				try {
					ProcessQueue();
					if (DateTime.UtcNow > nextWrite) {
						await WriteStats();
						nextWrite = DateTime.UtcNow.AddSeconds(10);
					}
				}
				catch (Exception ex) {
					if (ex is ThreadAbortException || ex is ThreadInterruptedException) {
						throw;
					}
					Dev.Log(ex);
					Thread.Sleep(1000);
				}
			}
		}

		private async Task<int> WriteStats() {
			var count = _stats.Count;
			if (count > 0) {
				await _dataSource.UpdateGlobalStats(_stats.Values);
				_stats.Clear();
			}
			return count;
		}

		private string CreateId(string longIdentifier) {
			return Convert.ToHexString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(longIdentifier))).ToLower();
		}

		private void IncrementStats(string key, string type, string action) {
			if (key.Length > 0) {
				string identifier = $"{type}:{action}:{key}";
				var now = DateTime.UtcNow;
				if (!_stats.ContainsKey(identifier)) {
					_stats.Add(identifier, new GlobalStatistic() { Id = CreateId(identifier), Type = type, Action = action, Key = key, Value = 0 });
				}
				var stats = _stats[identifier];
				stats.Value++;
				stats.LastActivity = now;
			}
		}

		private void ProcessQueue() {
			while (true) {
				ClientAction? item;
				lock (_queue) {
					_queue.TryDequeue(out item);
				}
				if (item == null) {
					break;
				}
				IncrementStats(item.Info.UserAgent, "user-agent", item.Action);
				IncrementStats(item.Info.MimiriVersion, "mimiri-version", item.Action);
				IncrementStats(item.Info.Client, "client", item.Action);
				IncrementStats(item.Info.Environment, "environment", item.Action);
				IncrementStats(item.Info.BundleVersion, "bundle-version", item.Action);
				IncrementStats(item.Info.HostVersion, "host-version", item.Action);
				IncrementStats(item.Info.ElectronVersion, "electron", item.Action);
				IncrementStats(item.Info.ChromeVersion, "chrome", item.Action);
				IncrementStats(item.Info.FirefoxVersion, "firefox", item.Action);
			}
		}

		public void RegisterAction(ClientInfo info, string action) {
			lock (_queue) {
				_queue.Enqueue(new ClientAction(info, action));
			}
		}


	}
}
