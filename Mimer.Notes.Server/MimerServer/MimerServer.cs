using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using System.Text.RegularExpressions;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Base class for MimerServer containing configuration, initialization, and core dependencies
	/// </summary>
	public partial class MimerServer {
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

		// Configuration Properties
		public static string? CertPath { get; set; } = "";
		public static string? WebsocketUrl { get; set; } = "";
		public static string? NotificationsUrl { get; set; } = "";
		public static string DefaultPostgresConnectionString { get; set; } = "";
		public static byte[] AesKey { get; set; } = new byte[0];
		public static string PrivateKey { get; set; } = "";
		public static string PublicKey { get; set; } = "";

		// Constants
		private const int SYSTEM_NOTE_COUNT = 3;

		// Core Dependencies
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

		private UserType GetUserType(long id) {
			return _userTypes.First(type => type.Id == id);
		}

		public void TearDown(bool keepLogs) {
			_dataSource.TearDown(keepLogs);
		}

		public void RegisterAction(ClientInfo info, string action) {
			_globalStatsManager.RegisterAction(info, action);
		}
	}
}
