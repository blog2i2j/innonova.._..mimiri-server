using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class MimerKey {
		private JsonObject _json;

		public MimerKey() {
			_json = new JsonObject();
		}

		public MimerKey(JsonObject json) {
			_json = json;
		}

		public Guid Id {
			get {
				return _json.Guid("id");
			}
			set {
				_json.Guid("id", value);
			}
		}

		public Guid UserId {
			get {
				return _json.Guid("userId");
			}
			set {
				_json.Guid("userId", value);
			}
		}

		public Guid Name {
			get {
				return _json.Guid("name");
			}
			set {
				_json.Guid("name", value);
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

		public string KeyData {
			get {
				return _json.String("keyData");
			}
			set {
				_json.String("keyData", value);
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

		public string Metadata {
			get {
				return _json.String("metadata");
			}
			set {
				_json.String("metadata", value);
			}
		}


		public JsonObject Json() {
			return _json;
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
