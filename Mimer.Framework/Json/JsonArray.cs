using System.Text;

namespace Mimer.Framework.Json {
	public class JsonArray {
		private List<JsonValue> FItems = new List<JsonValue>();

		public JsonArray() {

		}

		public JsonArray(string json) {
			if (!json.StartsWith("[")) {
				json = json.Trim();
				if (!json.StartsWith("[")) {
					throw new Exception("Not a json array '" + json + "'");
				}
			}
			List<string> OTokens = JsonTokenizer.Tokenize(json);
			int OIndex = 0;
			ReadTokens(OTokens, ref OIndex);
		}

		public JsonArray(List<string> tokens, ref int index) {
			ReadTokens(tokens, ref index);
		}

		private void ReadTokens(List<string> tokens, ref int index) {
			for (; index < tokens.Count; index++) {
				if (tokens[index] == "]") {
					break;
				}
				index++;
				string OValue = tokens[index];
				if (OValue == "]") {
					break;
				}

				if (OValue == "{") {
					FItems.Add(new JsonValue(new JsonObject(tokens, ref index)));
				}
				else if (OValue == "[") {
					FItems.Add(new JsonValue(new JsonArray(tokens, ref index)));
				}
				else if (OValue.StartsWith("\"", StringComparison.Ordinal)) {
					FItems.Add(new JsonValue(OValue.Substring(1), JsonValueType.String));
				}
				else {
					FItems.Add(new JsonValue(OValue, JsonValueType.Simple));
				}
			}
		}

		public void Add(JsonObject item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(JsonArray item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(string item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(Guid item) {
			FItems.Add(new JsonValue(item.ToString()));
		}

		public void Add(sbyte item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(byte item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(char item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(short item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(ushort item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(int item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(uint item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(long item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(ulong item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(float item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(double item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(decimal item) {
			FItems.Add(new JsonValue(item));
		}

		public void Add(DateTime item) {
			FItems.Add(new JsonValue(item));
		}

		public void Insert(int index, JsonObject item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, JsonArray item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, string item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, Guid item) {
			FItems.Insert(index, new JsonValue(item.ToString()));
		}

		public void Insert(int index, sbyte item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, byte item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, char item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, short item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, ushort item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, int item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, uint item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, long item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, ulong item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, float item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, double item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, decimal item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public void Insert(int index, DateTime item) {
			FItems.Insert(index, new JsonValue(item));
		}

		public JsonArray Delete(int index) {
			FItems.RemoveAt(index);
			return this;
		}

		public JsonArray Delete(JsonObject item) {
			for (int i = 0; i < FItems.Count; i++) {
				if (FItems[i].Value == item) {
					FItems.RemoveAt(i);
					break;
				}
			}
			return this;
		}

		public int IndexOf(JsonObject item) {
			for (int i = 0; i < FItems.Count; i++) {
				if (FItems[i].Value == item) {
					return i;
				}
			}
			return -1;
		}

		public JsonObject Object(int index) {
			return FItems[index].Object();
		}

		public JsonArray Object(int index, JsonObject item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public JsonArray Array(int index) {
			return FItems[index].Array();
		}

		public JsonArray Array(int index, JsonArray item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public string String(int index) {
			return FItems[index].String();
		}

		public JsonArray String(int index, string item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public Guid Guid(int index) {
			return new Guid(FItems[index].String());
		}

		public JsonArray Guid(int index, Guid item) {
			FItems[index] = new JsonValue(item.ToString());
			return this;
		}

		public bool Boolean(int index) {
			return FItems[index].Boolean();
		}

		public JsonArray Boolean(int index, bool item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public sbyte SByte(int index) {
			return FItems[index].SByte();
		}

		public JsonArray SByte(int index, sbyte item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public byte Byte(int index) {
			return FItems[index].Byte();
		}

		public JsonArray Byte(int index, byte item) {
			FItems[index] = new JsonValue(item);
			return this;
		}


		public short Int16(int index) {
			return FItems[index].Int16();
		}

		public JsonArray Int16(int index, short item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public ushort UInt16(int index) {
			return FItems[index].UInt16();
		}

		public JsonArray UInt16(int index, ushort item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public int Int32(int index) {
			return FItems[index].Int32();
		}

		public JsonArray Int32(int index, int item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public uint UInt32(int index) {
			return FItems[index].UInt32();
		}

		public JsonArray UInt32(int index, uint item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public long Int64(int index) {
			return FItems[index].Int64();
		}

		public JsonArray Int64(int index, long item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public ulong UInt64(int index) {
			return FItems[index].UInt64();
		}

		public JsonArray UInt64(int index, ulong item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public float Single(int index) {
			return FItems[index].Single();
		}

		public JsonArray Single(int index, float item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public double Double(int index) {
			return FItems[index].Double();
		}

		public JsonArray Double(int index, double item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public decimal Decimal(int index) {
			return FItems[index].Decimal();
		}

		public JsonArray Decimal(int index, decimal item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		public DateTime DateTime(int index) {
			return FItems[index].DateTime();
		}

		public JsonArray DateTime(int index, DateTime item) {
			FItems[index] = new JsonValue(item);
			return this;
		}

		internal string ToString(int depth) {
			StringBuilder OBuilder = new StringBuilder();
			OBuilder.Append("[");
			bool OFirst = true;
			foreach (JsonValue OValue in FItems) {
				if (!OFirst) {
					OBuilder.Append(",");
				}
				if (depth >= 0) {
					OBuilder.AppendLine();
					for (int i = 0; i < depth; i++) {
						OBuilder.Append("\t");
					}
				}
				if (OValue.Type == JsonValueType.Object) {
					OBuilder.Append(OValue.Object().ToString(depth >= 0 ? depth + 1 : -1));
				}
				else if (OValue.Type == JsonValueType.Array) {
					OBuilder.Append(OValue.Array().ToString(depth >= 0 ? depth + 1 : -1));
				}
				else {
					OValue.ToJson(OBuilder);
				}
				OFirst = false;
			}
			if (depth >= 0) {
				OBuilder.AppendLine();
				for (int i = 0; i < depth - 1; i++) {
					OBuilder.Append("\t");
				}
			}
			OBuilder.Append("]");
			return OBuilder.ToString();
		}

		public override string ToString() {
			return ToString(-1);
		}

		public T Get<T>(int index) {
			if (typeof(T) == typeof(JsonObject)) return (T)(object)Object(index);
			if (typeof(T) == typeof(JsonArray)) return (T)(object)Array(index);
			if (typeof(T) == typeof(string)) return (T)(object)String(index);
			if (typeof(T) == typeof(Guid)) return (T)(object)Guid(index);
			if (typeof(T) == typeof(bool)) return (T)(object)Boolean(index);
			if (typeof(T) == typeof(sbyte)) return (T)(object)SByte(index);
			if (typeof(T) == typeof(byte)) return (T)(object)Byte(index);
			if (typeof(T) == typeof(short)) return (T)(object)Int16(index);
			if (typeof(T) == typeof(ushort)) return (T)(object)UInt16(index);
			if (typeof(T) == typeof(int)) return (T)(object)Int32(index);
			if (typeof(T) == typeof(uint)) return (T)(object)UInt32(index);
			if (typeof(T) == typeof(long)) return (T)(object)Int64(index);
			if (typeof(T) == typeof(ulong)) return (T)(object)UInt64(index);
			if (typeof(T) == typeof(float)) return (T)(object)Single(index);
			if (typeof(T) == typeof(double)) return (T)(object)Double(index);
			if (typeof(T) == typeof(decimal)) return (T)(object)Decimal(index);
			if (typeof(T) == typeof(DateTime)) return (T)(object)DateTime(index);
			throw new Exception("Unexpected type: " + typeof(T));
		}

		public T[] ToArray<T>() {
			T[] OResult = new T[Count];
			for (int i = 0; i < Count; i++) {
				OResult[i] = Get<T>(i);
			}
			return OResult;
		}

		public string ToString(bool indent) {
			return ToString(indent ? 1 : -1);
		}

		private JsonItem[] ExtendPath(JsonItem[] path, int index, JsonValue item) {
			JsonItem[] OPath = new JsonItem[path.Length + 1];
			System.Array.Copy(path, OPath, path.Length);
			OPath[path.Length] = new JsonItem(null!, index, item);
			return OPath;
		}

		internal string ToFilteredString(IJsonFilter filter, JsonItem[] path, object param) {
			StringBuilder OBuilder = new StringBuilder();
			OBuilder.Append("[");
			bool OFirst = true;
			for (int i = 0; i < FItems.Count; i++) {
				bool ORemove = false;
				JsonValue OValue = filter.Filter(i, FItems[i], path, ref ORemove, param);
				if (!ORemove) {
					if (!OFirst) {
						OBuilder.Append(",");
					}
					if (OValue.Type == JsonValueType.Object) {
						OBuilder.Append(OValue.Object().ToFilteredString(filter, ExtendPath(path, i, FItems[i]), param));
					}
					else if (OValue.Type == JsonValueType.Array) {
						OBuilder.Append(OValue.Array().ToFilteredString(filter, ExtendPath(path, i, FItems[i]), param));
					}
					else {
						FItems[i].ToJson(OBuilder);
					}
					OFirst = false;
				}
			}
			OBuilder.Append("]");
			return OBuilder.ToString();
		}

		public string ToFilteredString(IJsonFilter filter, object param) {
			return ToFilteredString(filter, new JsonItem[0], param);
		}

		public IEnumerable<JsonObject> AsObjects() {
			return this.FItems.Select(item => item.Object());
		}


		public int Count {
			get {
				return FItems.Count;
			}
		}

	}
}
