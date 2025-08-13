
using Mimer.Notes.Model.Requests;

namespace Mimer.Notes.Server {

	public class DbNoteItem : INoteItem {
		public long Version { get; set; }
		public string Type { get; set; }
		public string Data { get; set; }
		public DateTime Created { get; set; }
		public DateTime Modified { get; set; }
		public int Size { get; set; }

		public DbNoteItem() {
			Version = 0;
			Type = "";
			Data = "";
		}

		public DbNoteItem(long version, string type, string data, DateTime created, DateTime modified, int size) {
			Version = version;
			Type = type;
			Data = data;
			Created = created;
			Modified = modified;
			Size = size;
		}

		public DbNoteItem(long version, string type, string data) {
			Version = version;
			Type = type;
			Data = data;
		}
	}
}
