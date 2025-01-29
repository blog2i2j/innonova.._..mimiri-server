using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class NoteItemArray {
		private NoteItem _owner;
		private JsonArray _json;

		public NoteItemArray(NoteItem owner, JsonArray json) {
			_owner = owner;
			_json = json;
		}

		public void Add(JsonObject item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(JsonArray item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(string item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(Guid item) {
			_json.Add(item.ToString());
			_owner.MarkChanged();
		}

		public void Add(sbyte item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(byte item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(char item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(short item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(ushort item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(int item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(uint item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(long item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(ulong item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(float item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(double item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(decimal item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Add(DateTime item) {
			_json.Add(item);
			_owner.MarkChanged();
		}

		public void Insert(int index, JsonObject item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, JsonArray item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, string item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, Guid item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, sbyte item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, byte item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, char item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, short item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, ushort item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, int item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, uint item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, long item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, ulong item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, float item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, double item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, decimal item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Insert(int index, DateTime item) {
			_json.Insert(index, item);
			_owner.MarkChanged();
		}

		public void Delete(int index) {
			_json.Delete(index);
			_owner.MarkChanged();
		}

		public JsonObject Object(int index) {
			return _json.Object(index);
		}

		public NoteItemArray Object(int index, JsonObject item) {
			_json.Object(index, item);
			_owner.MarkChanged();
			return this;
		}

		public JsonArray Array(int index) {
			return _json.Array(index);
		}

		public NoteItemArray Array(int index, JsonArray item) {
			_json.Array(index, item);
			_owner.MarkChanged();
			return this;
		}

		public string String(int index) {
			return _json.String(index);
		}

		public NoteItemArray String(int index, string item) {
			_json.String(index, item);
			_owner.MarkChanged();
			return this;
		}

		public Guid Guid(int index) {
			return _json.Guid(index);
		}

		public NoteItemArray Guid(int index, Guid item) {
			_json.Guid(index, item);
			_owner.MarkChanged();
			return this;
		}

		public bool Boolean(int index) {
			return _json.Boolean(index);
		}

		public NoteItemArray Boolean(int index, bool item) {
			_json.Boolean(index, item);
			_owner.MarkChanged();
			return this;
		}

		public sbyte SByte(int index) {
			return _json.SByte(index);
		}

		public NoteItemArray SByte(int index, sbyte item) {
			_json.SByte(index, item);
			_owner.MarkChanged();
			return this;
		}

		public byte Byte(int index) {
			return _json.Byte(index);
		}

		public NoteItemArray Byte(int index, byte item) {
			_json.Byte(index, item);
			_owner.MarkChanged();
			return this;
		}

		public short Int16(int index) {
			return _json.Int16(index);
		}

		public NoteItemArray Int16(int index, short item) {
			_json.Int16(index, item);
			_owner.MarkChanged();
			return this;
		}

		public ushort UInt16(int index) {
			return _json.UInt16(index);
		}

		public NoteItemArray UInt16(int index, ushort item) {
			_json.UInt16(index, item);
			_owner.MarkChanged();
			return this;
		}

		public int Int32(int index) {
			return _json.Int32(index);
		}

		public NoteItemArray Int32(int index, int item) {
			_json.Int32(index, item);
			_owner.MarkChanged();
			return this;
		}

		public uint UInt32(int index) {
			return _json.UInt32(index);
		}

		public NoteItemArray UInt32(int index, uint item) {
			_json.UInt32(index, item);
			_owner.MarkChanged();
			return this;
		}

		public long Int64(int index) {
			return _json.Int64(index);
		}

		public NoteItemArray Int64(int index, long item) {
			_json.Int64(index, item);
			_owner.MarkChanged();
			return this;
		}

		public ulong UInt64(int index) {
			return _json.UInt64(index);
		}

		public NoteItemArray UInt64(int index, ulong item) {
			_json.UInt64(index, item);
			_owner.MarkChanged();
			return this;
		}

		public float Single(int index) {
			return _json.Single(index);
		}

		public NoteItemArray Single(int index, float item) {
			_json.Single(index, item);
			_owner.MarkChanged();
			return this;
		}

		public double Double(int index) {
			return _json.Double(index);
		}

		public NoteItemArray Double(int index, double item) {
			_json.Double(index, item);
			_owner.MarkChanged();
			return this;
		}

		public decimal Decimal(int index) {
			return _json.Decimal(index);
		}

		public NoteItemArray Decimal(int index, decimal item) {
			_json.Decimal(index, item);
			_owner.MarkChanged();
			return this;
		}

		public DateTime DateTime(int index) {
			return _json.DateTime(index);
		}

		public NoteItemArray DateTime(int index, DateTime item) {
			_json.DateTime(index, item);
			_owner.MarkChanged();
			return this;
		}

		public T[] ToArray<T>() {
			return _json.ToArray<T>();
		}

		public IEnumerable<JsonObject> AsObjects() {
			return this._json.AsObjects();
		}

		public int Count {
			get {
				return _json.Count;
			}
		}

		public override string ToString() {
			return _json.ToString(true);
		}
	}
}
