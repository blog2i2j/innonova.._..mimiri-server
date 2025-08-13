
using Mimer.Notes.Model.Requests;

namespace Mimer.Notes.Server {
	public class DbNote {
		public Guid Id { get; set; }
		public Guid KeyName { get; set; }
		public DateTime Modified { get; set; }
		public DateTime Created { get; set; }
		public long Sync { get; set; }
		public int Size { get; set; }
		public List<INoteItem> Items { get; } = new List<INoteItem>();
	}
}
