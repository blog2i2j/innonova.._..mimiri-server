using System.Text;

namespace Mimer.Framework.Json {
	public class JsonObject {
		private Dictionary<string, JsonValue> FItems = new Dictionary<string, JsonValue>();

		public JsonObject() {

		}

		public JsonObject(JsonObject obj) {
			FItems = obj.FItems;
		}

		public JsonObject(string json) {
			//Console.WriteLine("JsonObject.ctor1");
			if (!json.StartsWith("{")) {
				json = json.Trim();
				if (!json.StartsWith("{")) {
					throw new Exception("Not a json object '" + json + "'");
				}
			}
			List<string> OTokens = JsonTokenizer.Tokenize(json);
			int OIndex = 0;
			ReadTokens(OTokens, ref OIndex);
		}

		public JsonObject(List<string> tokens, ref int index) {
			//Console.WriteLine("JsonObject.ctor2");
			ReadTokens(tokens, ref index);
		}

		private void ReadTokens(List<string> tokens, ref int index) {
			//Console.WriteLine("JsonObject.ReadTokens");
			for (; index < tokens.Count; index++) {
				//if (index > 0 && index < tokens.Count - 1) {
				//	Console.WriteLine("     "+ tokens[index - 1] + "  --  " + tokens[index] + "  --  " + tokens[index + 1]);
				//}
				if (tokens[index] == "}") {
					break;
				}
				index++;
				string OName = tokens[index++];
				if (OName.StartsWith("\"", StringComparison.Ordinal)) {
					OName = OName.Substring(1);
				}
				if (OName == "}") {
					index--;
					break;
				}
				index++; // :
				string OValue = tokens[index];
				if (OValue == "{") {
					//Console.WriteLine("Add Object: " + OName);
					FItems.Add(OName, new JsonValue(new JsonObject(tokens, ref index)));
				}
				else if (OValue == "[") {
					//Console.WriteLine("Add Array: " + OName);
					FItems.Add(OName, new JsonValue(new JsonArray(tokens, ref index)));
				}
				else if (OValue.StartsWith("\"", StringComparison.Ordinal)) {
					//Console.WriteLine("Add ValueS: " + OName);
					FItems.Add(OName, new JsonValue(OValue.Substring(1), JsonValueType.String));
				}
				else {
					//Console.WriteLine("Add ValueN: " + OName);
					FItems.Add(OName, new JsonValue(OValue, JsonValueType.Simple));
				}
			}
		}

		public List<string> Keys {
			get {
				return FItems.Keys.ToList();
			}
		}

		public bool Has(string name) {
			return FItems.ContainsKey(name);
		}

		public JsonValueType Type(string name) {
			return FItems[name].Type;
		}

		public JsonObject Delete(string name) {
			FItems.Remove(name);
			return this;
		}

		public JsonObject Object(string name) {
			return FItems[name].Object();
		}

		public JsonObject Object(string name, JsonObject item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public JsonArray Array(string name) {
			return FItems[name].Array();
		}

		public JsonObject Array(string name, JsonArray item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public string String(string name) {
			return FItems[name].String();
		}

		public string StringOrDefault(string name, string def) {
			if (!FItems.ContainsKey(name)) {
				return def;
			}
			return FItems[name].String();
		}

		public JsonObject String(string name, string item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public Guid Guid(string name) {
			return new Guid(FItems[name].String());
		}

		public JsonObject Guid(string name, Guid item) {
			FItems[name] = new JsonValue(item.ToString());
			return this;
		}

		public bool Boolean(string name) {
			return FItems[name].Boolean();
		}

		public JsonObject Boolean(string name, bool item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public sbyte SByte(string name) {
			return FItems[name].SByte();
		}

		public JsonObject SByte(string name, sbyte item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public byte Byte(string name) {
			return FItems[name].Byte();
		}

		public JsonObject Byte(string name, byte item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public short Int16(string name) {
			return FItems[name].Int16();
		}

		public JsonObject Int16(string name, short item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public ushort UInt16(string name) {
			return FItems[name].UInt16();
		}

		public JsonObject UInt16(string name, ushort item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public int Int32(string name) {
			return FItems[name].Int32();
		}

		public JsonObject Int32(string name, int item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public uint UInt32(string name) {
			return FItems[name].UInt32();
		}

		public JsonObject UInt32(string name, uint item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public long Int64(string name) {
			return FItems[name].Int64();
		}

		public JsonObject Int64(string name, long item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public ulong UInt64(string name) {
			return FItems[name].UInt64();
		}

		public JsonObject UInt64(string name, ulong item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public float Single(string name) {
			return FItems[name].Single();
		}

		public JsonObject Single(string name, float item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public double Double(string name) {
			return FItems[name].Double();
		}

		public JsonObject Double(string name, double item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public decimal Decimal(string name) {
			return FItems[name].Decimal();
		}

		public JsonObject Decimal(string name, decimal item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public DateTime DateTime(string name) {
			return FItems[name].DateTime();
		}

		public JsonObject DateTime(string name, DateTime item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		public JsonValue Value(string name) {
			return FItems[name];
		}

		public JsonObject Value(string name, JsonValue item) {
			FItems[name] = new JsonValue(item);
			return this;
		}

		internal string ToString(int depth) {
			StringBuilder OBuilder = new StringBuilder();
			OBuilder.Append("{");
			bool OFirst = true;
			foreach (var OPair in FItems) {
				if (!OFirst) {
					OBuilder.Append(",");
				}
				if (depth >= 0) {
					OBuilder.AppendLine();
					for (int i = 0; i < depth; i++) {
						OBuilder.Append("\t");
					}
				}
				OBuilder.Append("\"");
				OBuilder.Append(OPair.Key);
				OBuilder.Append("\":");
				if (depth >= 0) {
					OBuilder.Append(" ");
				}
				if (OPair.Value.Type == JsonValueType.Object) {
					OBuilder.Append(OPair.Value.Object().ToString(depth >= 0 ? depth + 1 : -1));
				}
				else if (OPair.Value.Type == JsonValueType.Array) {
					OBuilder.Append(OPair.Value.Array().ToString(depth >= 0 ? depth + 1 : -1));
				}
				else {
					OPair.Value.ToJson(OBuilder);
				}
				OFirst = false;
			}
			if (depth >= 0) {
				OBuilder.AppendLine();
				for (int i = 0; i < depth - 1; i++) {
					OBuilder.Append("\t");
				}
			}
			OBuilder.Append("}");
			return OBuilder.ToString();
		}

		public override string ToString() {
			return ToString(-1);
		}

		public string ToString(bool indent) {
			return ToString(indent ? 1 : -1);
		}

		private JsonItem[] ExtendPath(JsonItem[] path, string name, JsonValue item) {
			JsonItem[] OPath = new JsonItem[path.Length + 1];
			System.Array.Copy(path, OPath, path.Length);
			OPath[path.Length] = new JsonItem(name, -1, item);
			return OPath;
		}

		internal string ToFilteredString(IJsonFilter filter, JsonItem[] path, object? param) {
			StringBuilder OBuilder = new StringBuilder();
			OBuilder.Append("{");
			bool OFirst = true;
			foreach (var OPair in FItems) {
				bool ORemove = false;
				JsonValue OValue = filter.Filter(OPair.Key, OPair.Value, path, ref ORemove, param);
				if (!ORemove) {
					if (!OFirst) {
						OBuilder.Append(",");
					}
					OBuilder.Append("\"");
					OBuilder.Append(OPair.Key);
					OBuilder.Append("\":");
					if (OValue.Type == JsonValueType.Object) {
						OBuilder.Append(OValue.Object().ToFilteredString(filter, ExtendPath(path, OPair.Key, OPair.Value), param));
					}
					else if (OValue.Type == JsonValueType.Array) {
						OBuilder.Append(OValue.Array().ToFilteredString(filter, ExtendPath(path, OPair.Key, OPair.Value), param!));
					}
					else {
						OValue.ToJson(OBuilder);
					}
					OFirst = false;
				}
			}
			OBuilder.Append("}");
			return OBuilder.ToString();
		}

		public string ToFilteredString(IJsonFilter filter, object? param = null) {
			return ToFilteredString(filter, new JsonItem[0], param);
		}

		public int Count {
			get {
				return FItems.Count;
			}
		}


	}
}
