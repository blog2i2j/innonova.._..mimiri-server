
using Mimer.Notes.Model.Requests;

namespace Mimer.Notes.Server {

	public class DbNoteItem : INoteItem {
		public long Version { get; set; }
		public string Type { get; set; }
		public string Data { get; set; }

		public DbNoteItem() {
			Version = 0;
			Type = "";
			Data = "";
		}

		public DbNoteItem(long version, string type, string data) {
			Version = version;
			Type = type;
			Data = data;
		}
	}
}
