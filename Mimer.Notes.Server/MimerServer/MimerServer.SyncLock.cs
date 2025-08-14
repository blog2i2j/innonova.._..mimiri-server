using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	internal class SyncLockItem {
		public bool IsWriter { get; set; } = false;
		public int Readers { get; set; } = 0;
	}

	internal class LockManager {
		private readonly Dictionary<Guid, SyncLockItem> _locks = new();

		public bool IsOpenForWrite(Guid id) {
			return !_locks.ContainsKey(id);
		}

		public bool IsOpenForRead(Guid id) {
			return !_locks.ContainsKey(id) || !_locks[id].IsWriter;
		}

		public bool AddReader(Guid id) {
			if (!_locks.ContainsKey(id)) {
				_locks.Add(id, new SyncLockItem() { Readers = 1 });
				return true;
			}
			else if (!_locks[id].IsWriter) {
				_locks[id].Readers++;
				return true;
			}
			return false;
		}

		public void RemoveReader(Guid id) {
			if (_locks.ContainsKey(id)) {
				if (--_locks[id].Readers == 0) {
					_locks.Remove(id);
				}
			}
		}

		public bool AddWriter(Guid id) {
			if (!_locks.ContainsKey(id)) {
				_locks.Add(id, new SyncLockItem() { IsWriter = true });
				return true;
			}
			return false;
		}

		public void RemoveWriter(Guid id) {
			_locks.Remove(id);
		}
	}

	public class SyncLock : IDisposable {
		private MimerServer _owner;
		private Guid _userId;
		private Guid[] _keyNames;
		private bool _writer = false;

		internal SyncLock(MimerServer owner, Guid userId, Guid[] keyNames, bool writer) {
			_owner = owner;
			_userId = userId;
			_keyNames = keyNames;
			_writer = writer;
		}

		public void Dispose() {
			_owner.ReleaseLock(this);
		}

		public Guid UserId => _userId;
		public Guid[] KeyNames => _keyNames;
		public bool IsWriter => _writer;
	}

	public partial class MimerServer {
		private readonly LockManager _userLockManager = new();
		private readonly LockManager _keyLockManager = new();
		private readonly object _syncLock = new object();

		internal void ReleaseLock(SyncLock syncLock) {
			lock (_syncLock) {
				if (syncLock.IsWriter) {
					_userLockManager.RemoveWriter(syncLock.UserId);
					foreach (var keyName in syncLock.KeyNames) {
						_keyLockManager.RemoveWriter(keyName);
					}
				}
				else {
					_userLockManager.RemoveReader(syncLock.UserId);
					foreach (var keyName in syncLock.KeyNames) {
						_keyLockManager.RemoveReader(keyName);
					}
				}
			}
		}

		public async Task<SyncLock> TakeSyncReaderLock(Guid userId, int timeout = 10000) {
			var startTime = DateTime.UtcNow;
			var retryDelay = 200;
			while (true) {
				bool userLocked;
				lock (_syncLock) {
					userLocked = _userLockManager.AddReader(userId);
				}
				if (userLocked) {
					try {
						// user key names are assumed to only be changed while under a SyncWriteLock
						var userKeyNames = await _dataSource.GetUserKeyNames(userId);
						lock (_syncLock) {
							var allKeysAvailable = true;
							foreach (var keyName in userKeyNames) {
								if (!_keyLockManager.IsOpenForRead(keyName)) {
									allKeysAvailable = false;
									break;
								}
							}
							if (!allKeysAvailable) {
								_userLockManager.RemoveReader(userId);
							}
							else {
								foreach (var keyName in userKeyNames) {
									_keyLockManager.AddReader(keyName);
								}
								return new SyncLock(this, userId, userKeyNames.ToArray(), false);
							}
						}
					}
					catch {
						lock (_syncLock) {
							_userLockManager.RemoveReader(userId);
						}
						throw;
					}
				}
				if ((DateTime.UtcNow - startTime).TotalMilliseconds >= timeout) {
					throw new TimeoutException("Failed to acquire sync lock within the specified timeout.");
				}
				Dev.Log("Failed to take sync lock on first try (A)", userId, (DateTime.UtcNow - startTime).TotalMilliseconds);
				await Task.Delay(retryDelay);
			}
		}

		public async Task<SyncLock> TakeSyncWriterLock(Guid userId, Guid[] keyNames, int timeout = 10000) {
			var startTime = DateTime.UtcNow;
			var retryDelay = 200;
			while (true) {
				lock (_syncLock) {
					if (_userLockManager.IsOpenForWrite(userId)) {
						var allKeysAvailable = true;
						foreach (var keyName in keyNames) {
							if (!_keyLockManager.IsOpenForWrite(keyName)) {
								allKeysAvailable = false;
								break;
							}
						}
						if (allKeysAvailable) {
							if (_userLockManager.AddWriter(userId)) {
								foreach (var keyName in keyNames) {
									_keyLockManager.AddWriter(keyName);
								}
								return new SyncLock(this, userId, keyNames, true);
							}
						}
					}
				}
				if ((DateTime.UtcNow - startTime).TotalMilliseconds >= timeout) {
					throw new TimeoutException("Failed to acquire sync lock within the specified timeout.");
				}
				Dev.Log("Failed to take sync lock on first try (B)", userId, (DateTime.UtcNow - startTime).TotalMilliseconds);
				await Task.Delay(retryDelay);
			}
		}
	}
}