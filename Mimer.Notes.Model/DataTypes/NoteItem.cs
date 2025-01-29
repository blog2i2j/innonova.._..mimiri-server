using Mimer.Framework.Json;

namespace Mimer.Notes.Model.DataTypes {
	public class NoteItem {
		private JsonObject _json;

		public NoteItem() {
			_json = new JsonObject();
		}

		public NoteItem(JsonObject json) {
			_json = json;
		}

		public long Version {
			get {
				return _json.Int64("version");
			}
		}

		public string Type {
			get {
				return _json.String("type");
			}
		}

		public string String(string name) {
			return _json.Object("data").String(name);
		}

		public NoteItem String(string name, string value) {
			_json.Object("data").String(name, value);
			MarkChanged();
			return this;
		}

		public string StringOrDefault(string name, string defaultValue) {
			return _json.Object("data").StringOrDefault(name, defaultValue);
		}

		public DateTime DateTime(string name) {
			return _json.Object("data").DateTime(name);
		}

		public NoteItem DateTime(string name, DateTime value) {
			_json.Object("data").DateTime(name, value);
			MarkChanged();
			return this;
		}

		public bool Has(string name) {
			return _json.Object("data").Has(name);
		}

		public NoteItemArray Array(string name) {
			return new NoteItemArray(this, _json.Object("data").Array(name));
		}

		public NoteItem Array(string name, JsonArray value) {
			_json.Object("data").Array(name, value);
			MarkChanged();
			return this;
		}

		public void MarkChanged() {
			_json.Boolean("changed", true);
		}

		public bool Changed {
			get {
				return _json.Has("changed") && _json.Boolean("changed");
			}
		}

		public override string ToString() {
			return _json.ToString(true);
		}
	}
}
