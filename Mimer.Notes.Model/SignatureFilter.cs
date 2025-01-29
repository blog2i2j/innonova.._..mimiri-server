using Mimer.Framework.Json;

namespace Mimer.Notes.Model {
	internal class SignatureFilter : IJsonFilter {
		public static SignatureFilter Default = new SignatureFilter();

		public JsonValue Filter(string name, JsonValue item, JsonItem[] path, ref bool remove, object? param) {
			if (name == "signatures") {
				remove = true;
			}
			return item;
		}

		public JsonValue Filter(int index, JsonValue item, JsonItem[] path, ref bool remove, object? param) {
			return item;
		}
	}
}
