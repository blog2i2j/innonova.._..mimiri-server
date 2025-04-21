
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Responses {
	public class LoginResponse : IResponseObject {
		private JsonObject _json;

		public LoginResponse() {
			_json = new JsonObject();
		}

		public LoginResponse(JsonObject json) {
			_json = json;
		}

		public void SetJson(string json) {
			_json = new JsonObject(json);
		}

		public Guid UserId {
			get {
				return _json.Guid("userId");
			}
			set {
				_json.Guid("userId", value);
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

		public string Config {
			get {
				return _json.String("config");
			}
			set {
				_json.String("config", value);
			}
		}

		public long Size {
			get {
				return _json.Int64("size");
			}
			set {
				_json.Int64("size", value);
			}
		}

		public long NoteCount {
			get {
				return _json.Int64("noteCount");
			}
			set {
				_json.Int64("noteCount", value);
			}
		}

		public long MaxTotalBytes {
			get {
				return _json.Int64("maxTotalBytes");
			}
			set {
				_json.Int64("maxTotalBytes", value);
			}
		}

		public long MaxNoteBytes {
			get {
				return _json.Int64("maxNoteBytes");
			}
			set {
				_json.Int64("maxNoteBytes", value);
			}
		}

		public long MaxNoteCount {
			get {
				return _json.Int64("maxNoteCount");
			}
			set {
				_json.Int64("maxNoteCount", value);
			}
		}

		public long MaxHistoryEntries {
			get {
				return _json.Int64("maxHistoryEntries");
			}
			set {
				_json.Int64("maxHistoryEntries", value);
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
