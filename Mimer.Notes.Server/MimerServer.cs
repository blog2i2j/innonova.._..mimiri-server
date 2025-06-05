using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mimer.Notes.Server {
	public class MimerServer {
		private static string[] InvalidUsernames = [
			"mimiri",
			"innonova",
			"admin",
			"administrator",
			"moderator",
			"support",
			"staff",
			"owner",
			"system",
			"root",
			"null",
			"undefined",
			"user",
			"guest",
			"anonymous",
			"api",
			"config",
			"localhost",
			"server",
			"help",
			"login",
			"signup",
			"register",
			"home",
			"dashboard",
			"about",
			"bot",
			"spammer",
			"hacker",
			"phisher"
		];
		public static string? CertPath { get; set; } = "";
		public static string? WebsocketUrl { get; set; } = "";
		public static string? NotificationsUrl { get; set; } = "";
		public static string DefaultPostgresConnectionString { get; set; } = "";
		public static byte[] AesKey { get; set; } = new byte[0];
		public static string PrivateKey { get; set; } = "";
		public static string PublicKey { get; set; } = "";

		private const int SYSTEM_NOTE_COUNT = 3;

		private CryptSignature _signature;
		private HttpClient _httpClient = new HttpClient();
		private UserStatsManager _userStatsManager;
		private GlobalStatsManager _globalStatsManager;
		private Regex _invalidChars = new Regex("[!\"#$%&@'()*/=?[\\]{}~\\^\\\\\\s`]");
		private Regex _anonUserPattern = new Regex(@"MIMIRI_A_\d+_\d+");
		private PostgresDataSource _dataSource;
		private List<UserType> _userTypes = new List<UserType>();
		private ChallengeManager _challengeManager = new ChallengeManager();
		private RequestValidator _requestValidator = new RequestValidator();

		public MimerServer() {
			_dataSource = new PostgresDataSource(DefaultPostgresConnectionString, AesKey);
			_dataSource.CreateDatabase();
			_userTypes.AddRange(_dataSource.GetUserTypes());
			_userStatsManager = new UserStatsManager(_dataSource);
			_globalStatsManager = new GlobalStatsManager(_dataSource);
			if (CertPath != null) {
				if (File.Exists(Path.Combine(CertPath, "server.key"))) {
					try {
						_signature = new CryptSignature("RSA;3072", File.ReadAllText(Path.Combine(CertPath, "server.key")));
					}
					catch (Exception ex) {
						Dev.Log(ex);
						Dev.Log("Push notification failure, cert file invalid, push notifications are offline!");
						_signature = new CryptSignature("RSA;3072");
					}
				}
				else {
					_signature = new CryptSignature("RSA;3072");
					File.WriteAllText(Path.Combine(CertPath, "server.key"), _signature.PrivateKey);
					File.WriteAllText(Path.Combine(CertPath, "server.pub"), _signature.PublicKey);
				}
			}
			else {
				Dev.Log("Push notification failure, CertPath not defined, push notifications are offline!");
				_signature = new CryptSignature("RSA;3072");
			}
		}

		// public MimerServer(string testId) {
		// 	_dataSource = new SqlLiteDataSource(testId);
		// 	_dataSource.CreateDatabase();
		// 	_userStatsManager = new UserStatsManager(_dataSource);
		// 	_globalStatsManager = new GlobalStatsManager(_dataSource);
		// 	_signature = new CryptSignature("RSA;3072");
		// 	_userTypes.AddRange(_dataSource.GetUserTypes());
		// }

		private UserType GetUserType(long id) {
			return _userTypes.First(type => type.Id == id);
		}

		public void TearDown(bool keepLogs) {
			_dataSource.TearDown(keepLogs);
		}

		public void RegisterAction(ClientInfo info, string action) {
			_globalStatsManager.RegisterAction(info, action);
		}

		public async Task<PreLoginResponse> PreLogin(string username) {
			var user = await _dataSource.GetUser(username);
			if (user != null) {
				var response = new PreLoginResponse();
				response.Salt = user.PasswordSalt;
				response.Iterations = user.PasswordIterations;
				response.Algorithm = user.PasswordAlgorithm;
				response.Challenge = _challengeManager.IssueChallenge(user.Username);
				return response;
			}
			else {
				// Generate fake response to prevent scanning for usernames
				using var random = RandomNumberGenerator.Create();
				var saltBytes = new byte[32];
				var challengeBytes = new byte[32];
				random.GetBytes(saltBytes);
				random.GetBytes(challengeBytes);

				var response = new PreLoginResponse();
				response.Salt = Convert.ToHexString(saltBytes);
				response.Iterations = 1000000;
				response.Algorithm = "PBKDF2;SHA512;1024";
				response.Challenge = Convert.ToHexString(challengeBytes);
				return response;
			}
		}

		public async Task<LoginResponse?> Login(LoginRequest request) {
			if (!request.IsValid) { // challenge response ensures non repeatability here
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if ((user != null)) {
				if (_challengeManager.ValidateChallenge(user.Username, user.PasswordHash, request.Response, request.HashLength)) {
					var userType = GetUserType(user.TypeId);
					var response = new LoginResponse();
					response.UserId = user.Id;
					response.PublicKey = user.PublicKey;
					response.PrivateKey = user.PrivateKey;
					response.AsymmetricAlgorithm = user.AsymmetricAlgorithm;
					response.Salt = user.Salt;
					response.Iterations = user.Iterations;
					response.Algorithm = user.Algorithm;
					response.SymmetricAlgorithm = user.SymmetricAlgorithm;
					response.SymmetricKey = user.SymmetricKey;
					response.Data = user.Data;
					response.Config = user.ClientConfig;
					response.Size = user.Size;
					response.NoteCount = user.NoteCount;
					response.MaxTotalBytes = userType.MaxTotalBytes;
					response.MaxNoteBytes = userType.MaxNoteBytes;
					response.MaxNoteCount = userType.MaxNoteCount;
					response.MaxHistoryEntries = userType.MaxHistoryEntries;
					_userStatsManager.RegisterLogin(user.Id);
					return response;
				}
			}
			return null;
		}

		public async Task<UserDataResponse?> GetUserData(BasicRequest request) {
			if (!request.IsValid) { // challenge response ensures non repeatability here
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var userType = GetUserType(user.TypeId);
					var response = new UserDataResponse();
					response.Data = user.Data;
					response.Config = user.ClientConfig;
					response.Size = user.Size;
					response.NoteCount = user.NoteCount;
					response.MaxTotalBytes = userType.MaxTotalBytes;
					response.MaxNoteBytes = userType.MaxNoteBytes;
					response.MaxNoteCount = userType.MaxNoteCount;
					response.MaxHistoryEntries = userType.MaxHistoryEntries;
					return response;
				}
			}
			return null;
		}

		private bool ValidatePoW(string username, string powData, int bitsExpected) {
			var pow = powData.Split("::");
			var hash = pow[0];
			var data = pow[1];
			var dataAry = data.Split(':');
			var timestamp = dataAry[0];
			var value = dataAry[3];

			if (value != username) {
				return false;
			}

			if (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(timestamp)) > TimeSpan.FromMinutes(100)) {
				return false;
			}

			if (hash != BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower()) {
				return false;
			}

			ulong hashULong = ulong.Parse(hash.Substring(0, 16), System.Globalization.NumberStyles.HexNumber);
			if ((hashULong & (0xFFFFFFFFFFFFFFFF << (64 - bitsExpected))) != 0) {
				return false;
			}
			return true;
		}

		public async Task<CheckUsernameResponse?> UsernameAvailable(CheckUsernameRequest request) {
			if (!request.IsValid) { // Not worth prevent repeats here
				return null;
			}
			var response = new CheckUsernameResponse();
			response.BitsExpected = 15;
			response.Username = request.Username;
			response.Available = false;

			response.ProofAccepted = ValidatePoW(request.Username, request.Pow, response.BitsExpected);
			if (!response.ProofAccepted) {
				return response;
			}

			if (!IsValidUserName(request.Username)) {
				response.Available = false;
			}
			else {
				response.Available = null == await _dataSource.GetUser(request.Username);
			}
			return response;
		}

		private bool IsValidUserName(string name) {
			if (_invalidChars.IsMatch(name)) {
				return false;
			}
			string upperName = name.Trim().ToUpper();
			if (_anonUserPattern.IsMatch(upperName)) {
				return true;
			}
			if (upperName.StartsWith("INNONOVA")) {
				return false;
			}
			if (InvalidUsernames.Any(entry => {
				string entryUpper = entry.ToUpper();
				if (entryUpper == upperName) {
					return true;
				}
				if (upperName.StartsWith(entryUpper + "_")) {
					return true;
				}
				if (upperName.StartsWith(entryUpper)) {
					if (char.IsWhiteSpace(upperName[entryUpper.Length])) {
						return true;
					}
				}
				return false;
			})) {
				return false;
			}
			return true;
		}

		public async Task<BasicResponse?> CreateUser(CreateUserRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var signer = new CryptSignature(request.AsymmetricAlgorithm, request.PublicKey);
			// Do not create user that will never work
			if (!signer.VerifySignature("user", request)) {
				return null;
			}
			if (!IsValidUserName(request.Username)) {
				return null;
			}
			var user = new MimerUser();
			user.Username = request.Username.Trim();
			user.PublicKey = request.PublicKey;
			user.PrivateKey = request.PrivateKey;
			user.AsymmetricAlgorithm = request.AsymmetricAlgorithm;
			user.Salt = request.Salt;
			user.Iterations = request.Iterations;
			user.Algorithm = request.Algorithm;
			user.PasswordSalt = request.PasswordSalt;
			user.PasswordHash = request.PasswordHash;
			user.PasswordIterations = request.PasswordIterations;
			user.PasswordAlgorithm = request.PasswordAlgorithm;
			user.SymmetricAlgorithm = request.SymmetricAlgorithm;
			user.SymmetricKey = request.SymmetricKey;
			user.Data = request.Data;
			if (await _dataSource.CreateUser(user)) {
				return new BasicResponse();
			}
			return null;
		}

		public async Task<BasicResponse?> UpdateUser(UpdateUserRequest request, ClientInfo clientInfo) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var signer = new CryptSignature(request.AsymmetricAlgorithm, request.PublicKey);
			// Do not create user that will never work
			if (!signer.VerifySignature("user", request)) {
				return null;
			}
			var oldUser = await _dataSource.GetUser(request.OldUsername);
			if (oldUser != null) {
				if (request.OldUsername != request.Username) {
					if (!IsValidUserName(request.Username)) {
						return null;
					}
				}

				// TODO remove response length 0 check at some point in the future (backwards compatability)
				if (request.Response.Length == 0 || _challengeManager.ValidateChallenge(oldUser.Username, oldUser.PasswordHash, request.Response, request.HashLength)) {
					var oldSigner = new CryptSignature(oldUser.AsymmetricAlgorithm, oldUser.PublicKey);
					if (!oldSigner.VerifySignature("old-user", request)) {
						return null;
					}
					if (_anonUserPattern.IsMatch(oldUser.Username.ToUpper())) {
						this.RegisterAction(clientInfo, "promote-account");
					}
					var user = new MimerUser();
					user.Username = request.Username;
					user.PublicKey = request.PublicKey;
					user.PrivateKey = request.PrivateKey;
					user.AsymmetricAlgorithm = request.AsymmetricAlgorithm;
					user.Salt = request.Salt;
					user.Iterations = request.Iterations;
					user.Algorithm = request.Algorithm;
					user.PasswordSalt = request.PasswordSalt;
					user.PasswordHash = request.PasswordHash;
					user.PasswordIterations = request.PasswordIterations;
					user.PasswordAlgorithm = request.PasswordAlgorithm;
					user.SymmetricAlgorithm = request.SymmetricAlgorithm;
					user.SymmetricKey = request.SymmetricKey;
					user.Data = request.Data;
					if (await _dataSource.UpdateUser(request.OldUsername, user)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> UpdateUserData(UpdateUserDataRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					user.Data = request.Data;
					if (await _dataSource.UpdateUser(user.Username, user)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteUser(DeleteAccountRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				// TODO remove response length 0 check at some point in the future (backwards compatability)
				if (request.Response.Length == 0 || _challengeManager.ValidateChallenge(user.Username, user.PasswordHash, request.Response, request.HashLength)) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					if (signer.VerifySignature("user", request)) {
						if (await _dataSource.DeleteUser(user.Id)) {
							return new BasicResponse();
						}
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> CreateKey(CreateKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				var keySigner = new CryptSignature(user.AsymmetricAlgorithm, request.PublicKey);
				if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
					var existingKeyWithName = await _dataSource.GetKeyByName(request.Name);
					if (existingKeyWithName != null) {
						var existingKeySigner = new CryptSignature(user.AsymmetricAlgorithm, existingKeyWithName.PublicKey);
						if (!existingKeySigner.VerifySignature("key", request)) {
							// If you are creating a key with the same name (sharing) you must prove that you know the contents of the key to prevent denial of service
							return null;
						}
					}
					var key = new MimerKey();
					key.Id = request.Id;
					key.UserId = user.Id;
					key.Name = request.Name;
					key.Algorithm = request.Algorithm;
					key.AsymmetricAlgorithm = user.AsymmetricAlgorithm;
					key.PublicKey = request.PublicKey;
					key.PrivateKey = request.PrivateKey;
					key.KeyData = request.KeyData;
					key.Metadata = request.Metadata;
					if (await _dataSource.CreateKey(key)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<AllKeysResponse?> ReadAllKeys(BasicRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var response = new AllKeysResponse();
					var keys = await _dataSource.GetAllKeys(user.Id);
					foreach (var key in keys) {
						var info = new KeyInfo();
						info.Id = key.Id;
						info.Name = key.Name;
						info.Algorithm = key.Algorithm;
						info.KeyData = key.KeyData;
						info.AsymmetricAlgorithm = key.AsymmetricAlgorithm;
						info.PublicKey = key.PublicKey;
						info.PrivateKey = key.PrivateKey;
						info.Metadata = key.Metadata;
						response.AddKeyInfo(info);
					}
					return response;
				}
			}
			return null;
		}

		public async Task<KeyResponse?> ReadKey(ReadKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var response = new KeyResponse();
					var key = await _dataSource.GetKey(request.Id, user.Id);
					if (key != null) {
						response.Id = key.Id;
						response.Name = key.Name;
						response.Algorithm = key.Algorithm;
						response.KeyData = key.KeyData;
						response.AsymmetricAlgorithm = key.AsymmetricAlgorithm;
						response.PublicKey = key.PublicKey;
						response.PrivateKey = key.PrivateKey;
						response.Metadata = key.Metadata;
						return response;
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteKey(DeleteKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var key = await _dataSource.GetKey(request.Id, user.Id);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						if (await _dataSource.DeleteKey(request.Id)) {
							return new BasicResponse();
						}
					}
				}
			}
			return null;
		}

		public async Task<PublicKeyResponse?> GetPublicKey(PublicKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var response = new PublicKeyResponse();
			response.BitsExpected = 15;
			response.ProofAccepted = true;
			if (request.HasPow) { // TODO remove check when clients are updated
				response.ProofAccepted = ValidatePoW(request.KeyOwnerName, request.Pow, response.BitsExpected);
				if (!response.ProofAccepted) {
					return response;
				}
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var targetUser = await _dataSource.GetUser(request.KeyOwnerName);
					if (targetUser != null) {
						response.AsymmetricAlgorithm = targetUser.AsymmetricAlgorithm;
						response.PublicKey = targetUser.PublicKey;
						return response;
					}
				}
			}
			return null;
		}

		public async Task<UpdateNoteResponse?> MultiNote(MultiNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var userType = GetUserType(user.TypeId);
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					foreach (var keyName in request.KeyNames) {
						var key = await _dataSource.GetKeyByName(keyName);
						if (key == null) {
							return null;
						}
						var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
						if (!keySigner.VerifySignature(keyName.ToString(), request)) {
							return null;
						}
					}
					long createCount = 0;
					long totalSizeAdded = 0;
					foreach (var action in request.Actions) {
						if (action.Type == "create") {
							if (++createCount + user.NoteCount > userType.MaxNoteCount + SYSTEM_NOTE_COUNT) {
								Dev.Log("create declined, too many notes");
								return null;
							}
							long size = 0;
							foreach (var item in action.Items) {
								size += item.Data.Length;
							}
							if (size > userType.MaxNoteBytes) {
								Dev.Log("create declined, note too big");
								return null;
							}
							totalSizeAdded += size;
						}
						if (action.Type == "update") {
							// if we are just moving notes around or renaming them take the cheap way out
							if (!action.Items.Any(item => item.Type != "metadata")) {
								if (action.Items[0].Data.Length > userType.MaxNoteBytes / 2) {
									Dev.Log("update declined, meta data larger then 50% of total allowed note size");
									return null;
								}
							}
							else {
								var current = await _dataSource.GetNote(action.Id);
								long delta;
								var size = CalcNewSize(current!.Items, action.Items, out delta);
								if (size > userType.MaxNoteBytes) {
									Dev.Log("update declined, note too big");
									return null;
								}
								totalSizeAdded += delta;
							}
						}
					}
					// Make it easier on the client to get the math right by allowing overshooting total by one max size note
					if (totalSizeAdded > 0 && user.Size + totalSizeAdded > userType.MaxTotalBytes + userType.MaxNoteBytes) {
						Dev.Log("multi action declined, would exceed max total bytes, and action causes growth");
						return null;
					}

					var stats = new UserStats();
					var conflicts = await _dataSource.MultiApply(request.Actions, stats);
					if (conflicts == null) {
						return null;
					}
					else if (conflicts.Count == 0) {
						_userStatsManager.AddStats(user.Id, stats);
						foreach (var action in request.Actions) {
							if (action.Type == "update") {
								_ = SendNoteUpdate(user.Id, action.KeyName, action.Id);
							}
						}
						var response = new UpdateNoteResponse();
						var userSize = await _dataSource.GetUserSize(user.Id);
						response.Size = userSize.Size;
						response.NoteCount = userSize.NoteCount;
						response.Success = true;
						return response;
					}
					else {
						var response = new UpdateNoteResponse();
						response.Success = false;
						foreach (var item in conflicts) {
							response.AddVersionConflict(item);
						}
						return response;
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> CreateNote(WriteNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var userType = GetUserType(user.TypeId);
				if (user.Size > userType.MaxTotalBytes) {
					Dev.Log("create declined, user has exceeded limit");
					return null;
				}
				if (user.NoteCount > userType.MaxNoteCount + SYSTEM_NOTE_COUNT) {
					Dev.Log("create declined, too many notes");
					return null;
				}
				var key = await _dataSource.GetKeyByName(request.KeyName);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						var note = new DbNote();
						note.Id = request.Id;
						note.KeyName = request.KeyName;
						var size = 0;
						foreach (var item in request.Items) {
							var data = item.Data;
							size += data.Length;
							note.Items.Add(new DbNoteItem(item.Version, item.Type, data));
						}
						if (size > userType.MaxNoteBytes) {
							Dev.Log("create declined, note too big");
							return null;
						}
						if (await _dataSource.CreateNote(note)) {
							_userStatsManager.RegisterCreateNote(user.Id, size);
							return new BasicResponse();
						}
					}
				}
			}
			return null;
		}

		private long CalcNewSize(List<INoteItem> current, List<INoteItem> update, out long delta) {
			delta = 0;
			long newSize = 0;
			foreach (var item in current) {
				var reqItem = update.FirstOrDefault(r => r.Type == item.Type);
				if (reqItem != null) {
					newSize += reqItem.Data.Length;
					delta += reqItem.Data.Length - item.Data.Length;
				}
				else {
					newSize += item.Data.Length;
				}
			}
			foreach (var item in update) {
				if (!current.Any(existing => existing.Type == item.Type)) {
					newSize += item.Data.Length;
					delta += item.Data.Length;
				}
			}
			return newSize;
		}

		public async Task<UpdateNoteResponse?> UpdateNote(WriteNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var userType = GetUserType(user.TypeId);
				var key = await _dataSource.GetKeyByName(request.KeyName);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						var note = await _dataSource.GetNote(request.Id);
						if (note != null) {
							if (request.OldKeyName != Guid.Empty) {
								if (note.KeyName != request.OldKeyName) {
									return null;
								}
								var oldKey = await _dataSource.GetKeyByName(request.OldKeyName);
								if (oldKey == null) {
									return null;
								}
								var oldKeySigner = new CryptSignature(oldKey.AsymmetricAlgorithm, oldKey.PublicKey);
								if (!oldKeySigner.VerifySignature("old-key", request)) {
									return null;
								}
							}
							else if (note.KeyName != request.KeyName) {
								return null;
							}
							note.KeyName = request.KeyName;

							long delta;
							if (CalcNewSize(note.Items, request.Items, out delta) > GetUserType(user.TypeId).MaxNoteBytes) {
								Dev.Log("update declined, resulting note too big");
								return null;
							}
							if (delta > 0 && user.Size > userType.MaxTotalBytes) {
								Dev.Log("create declined, user has exceeded limit, and note would grow");
								return null;
							}
							note.Items.Clear();
							var size = 0;
							foreach (var item in request.Items) {
								var data = item.Data;
								size += data.Length;
								note.Items.Add(new DbNoteItem(item.Version, item.Type, data));
							}
							var conflicts = await _dataSource.UpdateNote(note, request.OldKeyName);
							if (conflicts == null) {
								return null;
							}
							if (conflicts.Count == 0) {
								_ = SendNoteUpdate(user.Id, request.KeyName, note.Id);
								_userStatsManager.RegisterWrite(user.Id, size);
								var response = new UpdateNoteResponse();
								var userSize = await _dataSource.GetUserSize(user.Id);
								response.Size = userSize.Size;
								response.NoteCount = userSize.NoteCount;
								response.Success = true;
								return response;
							}
							else {
								var response = new UpdateNoteResponse();
								response.Success = false;
								foreach (var item in conflicts) {
									response.AddVersionConflict(item);
								}
								return response;
							}
						}
					}
				}
			}
			return null;
		}

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

		public async Task<ReadNoteResponse?> ReadNote(ReadNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var note = await _dataSource.GetNote(request.Id);
					if (note != null) {
						var response = new ReadNoteResponse();
						response.Id = note.Id;
						response.KeyName = note.KeyName;
						var size = 0;
						foreach (var item in note.Items) {
							if (request.Include != "*") {
								if (!request.Include.Contains(item.Type)) {
									continue;
								}
							}
							if (!request.isNewer(item.Type, item.Version)) {
								response.AddItem(item.Version, item.Type);
								continue;
							}
							size += item.Data.Length;
							response.AddItem(item.Version, item.Type, item.Data);
						}
						_userStatsManager.RegisterRead(user.Id, size);
						return response;
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteNote(DeleteNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var note = await _dataSource.GetNote(request.Id);
				if (note != null) {
					var key = await _dataSource.GetKeyByName(note.KeyName);
					if (key != null) {
						var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
						var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
						if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
							if (await _dataSource.DeleteNote(note.Id)) {
								_userStatsManager.RegisterDeleteNote(user.Id);
								return new BasicResponse();
							}
						}
					}
				}
			}
			return null;
		}

		public async Task<ShareResponse?> ShareNote(ShareNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var key = await _dataSource.GetKeyByName(request.KeyName);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						string? code = await _dataSource.CreateNoteShareOffer(user.Username, request.Recipient, request.KeyName, new Random().Next(1000, 10000).ToString(), request.Data);
						if (code != null) {
							var response = new ShareResponse();
							response.Code = code;
							return response;
						}
					}
				}
			}
			return null;
		}


		public async Task<ShareOffersResponse?> ReadShareOffers(BasicRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var offers = await _dataSource.GetShareOffers(user.Username);
					var response = new ShareOffersResponse();
					foreach (var offer in offers) {
						response.AddOffer(offer.Id, offer.Sender, offer.Data);
					}
					return response;
				}
			}
			return null;
		}

		public async Task<ShareOffersResponse?> GetShareOffer(ShareOfferRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var offer = await _dataSource.GetShareOffer(user.Username, request.Code);
					if (offer != null) {
						var response = new ShareOffersResponse();
						response.AddOffer(offer.Id, offer.Sender, offer.Data);
						return response;
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteShare(DeleteShareRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					if (await _dataSource.DeleteNoteShareOffer(request.Id)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<ShareParticipantsResponse?> GetSharedWith(ShareParticipantsRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var participants = await _dataSource.GetShareParticipants(request.Id);
					if (participants.Any(item => item.id == user.Id)) {
						var result = new ShareParticipantsResponse();
						foreach (var item in participants) {
							result.AddParticipant(item.username, item.since);
						}
						return result;
					}
				}
			}
			return null;
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

		public JsonObject DecryptRequest(JsonObject json) {
			try {
				return new JsonObject(_signature.Decrypt(json.String("data")));
			}
			catch (Exception ex) {
				Dev.Log(ex);
				throw;
			}
		}

	}
}
