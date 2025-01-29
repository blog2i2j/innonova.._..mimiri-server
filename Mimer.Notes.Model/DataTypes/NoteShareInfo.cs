using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class NoteShareInfo {
		private JsonObject _json;

		public NoteShareInfo() {
			_json = new JsonObject();
		}

		public NoteShareInfo(JsonObject json) {
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

		public DateTime Created {
			get {
				if (!_json.Has("created")) {
					return DateTime.MinValue;
				}
				return _json.DateTime("created");
			}
			set {
				_json.DateTime("created", value);
			}
		}

		public string Sender {
			get {
				return _json.String("sender");
			}
			set {
				_json.String("sender", value);
			}
		}

		public string Name {
			get {
				return _json.StringOrDefault("name", "unknown node");
			}
			set {
				_json.String("name", value);
			}
		}

		public Guid NoteId {
			get {
				return _json.Guid("noteId");
			}
			set {
				_json.Guid("noteId", value);
			}
		}

		public Guid KeyName {
			get {
				return _json.Guid("keyName");
			}
			set {
				_json.Guid("keyName", value);
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

		public override string ToString() {
			return _json.ToString(true);
		}

		public string ToJsonString() {
			return _json.ToString();
		}
	}
}
