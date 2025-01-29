using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using System.Security.Cryptography;

namespace Mimer.Notes.Server {
	public class ChallengeManager {
		private class IssuedChallenge {
			public string Username { get; set; } = "";
			public string Challenge { get; set; } = "";
			public DateTime IssueTime { get; set; }
		}
		private Thread _pruneThread;
		private Dictionary<string, IssuedChallenge> _issuedChallenges = new Dictionary<string, IssuedChallenge>();

		public ChallengeManager() {
			_pruneThread = new Thread(ExecutePruneChallenges);
			_pruneThread.IsBackground = true;
			_pruneThread.Start();
		}

		private IssuedChallenge? GetChallenge(string username) {
			IssuedChallenge? issuedChallenge = null;
			lock (_issuedChallenges) {
				_issuedChallenges.TryGetValue(username, out issuedChallenge);
			}
			return issuedChallenge;
		}

		private void ClearChallenge(string username) {
			lock (_issuedChallenges) {
				if (_issuedChallenges.ContainsKey(username)) {
					_issuedChallenges.Remove(username);
				}
			}
		}

		public string IssueChallenge(string username) {
			var challengeBytes = new byte[32];
			using var random = RandomNumberGenerator.Create();
			random.GetBytes(challengeBytes);
			var challenge = new IssuedChallenge();
			challenge.Username = username;
			challenge.IssueTime = DateTime.UtcNow;
			challenge.Challenge = Convert.ToHexString(challengeBytes);
			lock (_issuedChallenges) {
				_issuedChallenges[username] = challenge;
			}
			return challenge.Challenge!;
		}

		public bool ValidateChallenge(string username, string passwordHash, string response, int hashLength) {
			var challenge = GetChallenge(username);
			if (challenge != null) {
				if (hashLength >= 256 && hashLength * 2 < passwordHash.Length) {
					passwordHash = passwordHash.Substring(0, hashLength * 2);
				}
				var expectedResponse = PasswordHasher.Instance.ComputeResponse(passwordHash, challenge.Challenge);
				if (response == expectedResponse) {
					ClearChallenge(username);
					return true;
				}
			}
			return false;
		}

		private void ExecutePruneChallenges() {
			var lastPrune = DateTime.UtcNow;
			while (true) {
				try {
					if ((DateTime.UtcNow - lastPrune).TotalMinutes > 10) {
						lastPrune = DateTime.UtcNow;
						IssuedChallenge[] items;
						lock (_issuedChallenges) {
							items = _issuedChallenges.Values.ToArray();
						}
						foreach (var item in items) {
							if ((DateTime.UtcNow - item.IssueTime).TotalMinutes > 10) {
								ClearChallenge(item.Username);
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
	}
}
