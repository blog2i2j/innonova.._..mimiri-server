using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class MimerUser {
		private JsonObject _json;
		private Guid _stableId;
		private long _size;
		private long _noteCount;
		private long _typeId;
		private string _serverConfig = "{}";
		private string _clientConfig = "{}";

		public MimerUser() {
			_json = new JsonObject();
		}

		public MimerUser(JsonObject json, Guid stableId, long size, long noteCount, long typeId, string serverConfig, string clientConfig) {
			_json = json;
			_stableId = stableId;
			_size = size;
			_noteCount = noteCount;
			_typeId = typeId;
			_serverConfig = serverConfig;
			_clientConfig = clientConfig;
		}

		public string Username {
			get {
				return _json.String("username");
			}
			set {
				_json.String("username", value);
			}
		}

		public string PublicKey {
			get {
				return _json.String("publicKey");
			}
			set {
				_json.String("publicKey", value);
			}
		}

		public string PrivateKey {
			get {
				return _json.String("privateKey");
			}
			set {
				_json.String("privateKey", value);
			}
		}

		public string AsymmetricAlgorithm {
			get {
				return _json.String("asymmetricAlgorithm");
			}
			set {
				_json.String("asymmetricAlgorithm", value);
			}
		}

		public string Salt {
			get {
				return _json.String("salt");
			}
			set {
				_json.String("salt", value);
			}
		}

		public int Iterations {
			get {
				return _json.Int32("iterations");
			}
			set {
				_json.Int32("iterations", value);
			}
		}

		public string Algorithm {
			get {
				return _json.String("algorithm");
			}
			set {
				_json.String("algorithm", value);
			}
		}

		public string PasswordSalt {
			get {
				return _json.Object("password").String("salt");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").String("salt", value);
			}
		}

		public string PasswordHash {
			get {
				return _json.Object("password").String("hash");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").String("hash", value);
			}
		}

		public int PasswordIterations {
			get {
				return _json.Object("password").Int32("iterations");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").Int32("iterations", value);
			}
		}

		public string PasswordAlgorithm {
			get {
				return _json.Object("password").String("algorithm");
			}
			set {
				if (!_json.Has("password")) {
					_json.Object("password", new JsonObject());
				}
				_json.Object("password").String("algorithm", value);
			}
		}

		public string SymmetricAlgorithm {
			get {
				return _json.String("symmetricAlgorithm");
			}
			set {
				_json.String("symmetricAlgorithm", value);
			}
		}

		public string SymmetricKey {
			get {
				return _json.String("symmetricKey");
			}
			set {
				_json.String("symmetricKey", value);
			}
		}

		public string Data {
			get {
				return _json.String("data");
			}
			set {
				_json.String("data", value);
			}
		}

		public Guid Id {
			get {
				return _stableId;
			}
		}

		public long Size {
			get {
				return _size;
			}
		}

		public long NoteCount {
			get {
				return _noteCount;
			}
		}

		public long TypeId {
			get {
				return _typeId;
			}
		}

		public string ServerConfig {
			get {
				return _serverConfig;
			}
		}

		public string ClientConfig {
			get {
				return _clientConfig;
			}
		}

		public string ToJsonString() {
			return _json.ToString();
		}

	}
}
