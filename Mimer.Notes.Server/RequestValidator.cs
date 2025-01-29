using Mimer.Framework;
using Mimer.Notes.Model;

namespace Mimer.Notes.Server {

	public class RequestValidator {
		private class RequestLink {
			public Guid Id;
			public DateTime TimeStamp;
			public volatile RequestLink? Next = null;
		}

		private Thread _pruneThread;
		private Dictionary<Guid, DateTime> _executedRequests = new Dictionary<Guid, DateTime>();
		private RequestLink _first;
		private object _lock = new object();
		private RequestLink _last;

		public RequestValidator() {
			_first = new RequestLink();
			_last = _first;
			_pruneThread = new Thread(ExecutePruneRequests);
			_pruneThread.IsBackground = true;
			_pruneThread.Start();
		}

		private void ExecutePruneRequests() {
			var lastPrune = DateTime.UtcNow;
			while (true) {
				try {
					int pruneCount = 0;
					if ((DateTime.UtcNow - lastPrune).TotalMinutes > 5) {
						lastPrune = DateTime.UtcNow;
						// never prune _last
						if (_first.Next != null) {
							// leave dummy first in place
							var prev = _first;
							var current = _first.Next;
							while (current.Next != null) {
								if ((DateTime.UtcNow - current.TimeStamp).TotalMinutes > 21) {
									pruneCount++;
									prev.Next = current.Next;
									lock (_executedRequests) {
										_executedRequests.Remove(current.Id);
									}
								}
								else {
									// only advance prev if we left current in place
									prev = current;
								}
								current = current.Next;
							}
						}
					}
				}
				catch (Exception ex) {
					Dev.Log(ex);
					Thread.Sleep(1000);
				}
				Thread.Sleep(100);
			}
		}

		public bool ValidateRequest(INonRepeatableRequest request) {
			try {
				bool reject = false;
				lock (_executedRequests) {
					if (_executedRequests.ContainsKey(request.RequestId)) {
						reject = true;
					}
				}
				if (reject) {
					Dev.Log("Rejected Replay (In List)", request.RequestId, request.TimeStamp);
					return false;
				}
				if ((DateTime.UtcNow - request.TimeStamp).TotalMinutes > 20) {
					Dev.Log("Rejected Replay (Old)", request.RequestId, request.TimeStamp);
					return false;
				}
				lock (_executedRequests) {
					if (_executedRequests.ContainsKey(request.RequestId)) {
						return false;
					}
					_executedRequests.Add(request.RequestId, request.TimeStamp);
				}
				lock (_lock) {
					_last.Next = new RequestLink {
						Id = request.RequestId,
						TimeStamp = request.TimeStamp
					};
					_last = _last.Next;
				}
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
			return request.IsValid;
		}


	}
}
