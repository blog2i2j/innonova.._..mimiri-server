using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mimer.Framework.Json {
	public enum JsonValueType { Object, Array, String, Simple }
	public class JsonValue {
		public static JsonValue Empty = new JsonValue(null!, JsonValueType.Simple);
		public static JsonValue EmptyString = new JsonValue("", JsonValueType.String);
		public static JsonValue EmptyArray = new JsonValue(new JsonArray());
		public static JsonValue EmptyObject = new JsonValue(new JsonObject());
		public static Regex EnglishTimeFormat = new Regex(@"T(\d+)\.(\d+)\.(\d+)\.(\d+)Z", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public object Value;
		public JsonValueType Type;

		public JsonValue(string value, JsonValueType type) {
			Value = value;
			Type = type;
		}

		public JsonValue(JsonValue value) {
			Value = value.Value;
			Type = value.Type;
		}

		public JsonValue(JsonObject value) {
			Value = value;
			Type = JsonValueType.Object;
		}

		public JsonValue(JsonArray value) {
			Value = value;
			Type = JsonValueType.Array;
		}

		public JsonValue(string value) {
			Value = JsonBuilder.EncodeStringLiteral(value);
			Type = JsonValueType.String;
		}

		public JsonValue(bool value) {
			Value = value ? "true" : "false";
			Type = JsonValueType.Simple;
		}

		public JsonValue(sbyte value) {
			Value = value.ToString();
			Type = JsonValueType.Simple;
		}

		public JsonValue(byte value) {
			Value = value.ToString();
			Type = JsonValueType.Simple;
		}

		public JsonValue(short value) {
			Value = value.ToString();
			Type = JsonValueType.Simple;
		}

		public JsonValue(ushort value) {
			Value = value.ToString();
			Type = JsonValueType.Simple;
		}

		public JsonValue(int value) {
			Value = value.ToString();
			Type = JsonValueType.Simple;
		}

		public JsonValue(uint value) {
			Value = value.ToString();
			Type = JsonValueType.Simple;
		}

		public JsonValue(long value) {
			Value = value.ToString();
			if (value >= 9007199254740991 || value <= -9007199254740991) {
				Type = JsonValueType.String;
			}
			else {
				Type = JsonValueType.Simple;
			}
		}

		public JsonValue(ulong value) {
			Value = value.ToString();
			Type = JsonValueType.String;
		}

		public JsonValue(float value) {
			Value = value.ToString(CultureInfo.InvariantCulture);
			Type = JsonValueType.Simple;
		}

		public JsonValue(double value) {
			Value = value.ToString(CultureInfo.InvariantCulture);
			Type = JsonValueType.Simple;
		}

		public JsonValue(decimal value) {
			Value = value.ToString(CultureInfo.InvariantCulture);
			Type = JsonValueType.Simple;
		}

		public JsonValue(DateTime value) {
			Value = value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
			Type = JsonValueType.String;
		}

		public string String() {
			if (Type == JsonValueType.String) {
				return JsonBuilder.DecodeStringLiteral((string)Value);
			}
			return Value.ToString()!;
		}

		public bool Boolean() {
			return (string)Value == "true";
		}

		public sbyte SByte() {
			return sbyte.Parse((string)Value);
		}

		public byte Byte() {
			return byte.Parse((string)Value);
		}

		public short Int16() {
			return short.Parse((string)Value);
		}

		public ushort UInt16() {
			return ushort.Parse((string)Value);
		}

		public int Int32() {
			return int.Parse((string)Value);
		}

		public uint UInt32() {
			return uint.Parse((string)Value);
		}

		public long Int64() {
			return long.Parse((string)Value);
		}

		public ulong UInt64() {
			return ulong.Parse((string)Value);
		}

		public float Single() {
			return float.Parse((string)Value, CultureInfo.InvariantCulture);
		}

		public double Double() {
			return double.Parse((string)Value, CultureInfo.InvariantCulture);
		}

		public decimal Decimal() {
			return decimal.Parse((string)Value, CultureInfo.InvariantCulture);
		}

		public DateTime DateTime() {
			string stringValue = (string)Value;
			if (EnglishTimeFormat.IsMatch(stringValue)) {
				stringValue = EnglishTimeFormat.Replace(stringValue, "T$1:$2:$3.$4Z");
			}
			return System.DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
		}

		public JsonObject Object() {
			return (JsonObject)Value;
		}

		public JsonArray Array() {
			return (JsonArray)Value;
		}

		public string ToJson() {
			if (Type == JsonValueType.String) {
				return "\"" + Value + "\"";
			}
			return Value.ToString()!;
		}

		public void ToJson(StringBuilder builder) {
			if (Type == JsonValueType.String) {
				builder.Append("\"");
				builder.Append(Value.ToString());
				builder.Append("\"");
			}
			else {
				builder.Append(Value.ToString());
			}
		}

	}
}
