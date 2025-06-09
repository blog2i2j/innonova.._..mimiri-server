
using Mimer.Framework;

namespace Mimer.Notes.Server {
	public class UserStats {
		public Guid UserId { get; set; }
		public DateTime LastActivity { get; set; } = DateTime.UtcNow;
		public long Logins { get; set; }
		public long Reads { get; set; }
		public long ReadBytes { get; set; }
		public long Writes { get; set; }
		public long WriteBytes { get; set; }
		public long Notifications { get; set; }
		public long Creates { get; set; }
		public long Deletes { get; set; }
	}

	public class UserStatsManager {
		private object _lock = new object();
		private PostgresDataSource _dataSource;
		private Dictionary<Guid, UserStats> _stats = new Dictionary<Guid, UserStats>();

		public UserStatsManager(PostgresDataSource dataSource) {
			_dataSource = dataSource;
			Task.Run(Execute);
		}

		private async Task Execute() {
			DateTime nextWrite = DateTime.UtcNow.AddSeconds(10);
			while (true) {
				Thread.Sleep(100);
				try {
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
			Dictionary<Guid, UserStats> stats;
			lock (_lock) {
				stats = _stats;
				_stats = new Dictionary<Guid, UserStats>();
			}
			if (stats.Count > 0) {
				await _dataSource.UpdateUserStats(stats.Values);
			}
			return stats.Count;
		}

		private UserStats GetUserStats(Guid userId) {
			lock (_lock) {
				if (!_stats.ContainsKey(userId)) {
					_stats.Add(userId, new UserStats() { UserId = userId });
				}
				return _stats[userId];
			}
		}

		public void RegisterLogin(Guid userId) {
			var stats = GetUserStats(userId);
			lock (stats) {
				stats.Logins++;
				stats.LastActivity = DateTime.UtcNow;
			}
		}

		public void RegisterRead(Guid userId, long bytes) {
			var stats = GetUserStats(userId);
			lock (stats) {
				stats.Reads++;
				stats.ReadBytes += bytes;
				stats.LastActivity = DateTime.UtcNow;
			}
		}

		public void RegisterWrite(Guid userId, long bytes) {
			var stats = GetUserStats(userId);
			lock (stats) {
				stats.Writes++;
				stats.WriteBytes += bytes;
				stats.LastActivity = DateTime.UtcNow;
			}
		}

		public void RegisterCreateNote(Guid userId, long bytes) {
			var stats = GetUserStats(userId);
			lock (stats) {
				stats.Creates++;
				stats.Writes++;
				stats.WriteBytes += bytes;
				stats.LastActivity = DateTime.UtcNow;
			}
		}

		public void RegisterDeleteNote(Guid userId) {
			var stats = GetUserStats(userId);
			lock (stats) {
				stats.Deletes++;
				stats.Writes++;
				stats.LastActivity = DateTime.UtcNow;
			}
		}

		public void RegisterNotification(Guid userId) {
			var stats = GetUserStats(userId);
			lock (stats) {
				stats.Notifications++;
				stats.LastActivity = DateTime.UtcNow;
			}
		}

		public void AddStats(Guid userId, UserStats delta) {
			var stats = GetUserStats(userId);
			lock (stats) {
				stats.LastActivity = DateTime.UtcNow;
				stats.Logins += delta.Logins;
				stats.Reads += delta.Reads;
				stats.ReadBytes += delta.ReadBytes;
				stats.Writes += delta.Writes;
				stats.WriteBytes += delta.WriteBytes;
				stats.Notifications += delta.Notifications;
				stats.Creates += delta.Creates;
				stats.Deletes += delta.Deletes;
			}
		}


	}
}
