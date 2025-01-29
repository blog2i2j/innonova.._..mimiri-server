
using Mimer.Framework.Json;

namespace Mimer.Notes.Model.Cryptography {
	public class KeySet {
		public Guid Id { get; }
		public Guid Name { get; }
		public SymmetricCrypt Symmetric { get; }
		public CryptSignature Signature { get; }
		public JsonObject Metadata { get; }
		public KeySet(Guid id, Guid name, SymmetricCrypt symmetric, CryptSignature signature, JsonObject metadata) {
			Id = id;
			Name = name;
			Symmetric = symmetric;
			Signature = signature;
			Metadata = metadata;
		}
	}
}
