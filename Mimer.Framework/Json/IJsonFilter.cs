namespace Mimer.Framework.Json {

	public struct JsonItem {
		public readonly string Name;
		public readonly int Index;
		public readonly JsonValue Value;
		public JsonItem(string name, int index, JsonValue value) {
			Name = name;
			Index = index;
			Value = value;
		}
	}

	public interface IJsonFilter {

		JsonValue Filter(string name, JsonValue item, JsonItem[] path, ref bool remove, object? param);
		JsonValue Filter(int index, JsonValue item, JsonItem[] path, ref bool remove, object? param);

	}
}
