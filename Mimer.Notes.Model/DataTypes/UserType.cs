namespace Mimer.Notes.Model.DataTypes {
	public class UserType {
		public long Id { get; private set; }
		public string Name { get; private set; }
		public long MaxTotalBytes { get; private set; }
		public long MaxNoteBytes { get; private set; }
		public long MaxNoteCount { get; private set; }
		public long MaxHistoryEntries { get; private set; }

		public UserType(long id, string name, long maxTotalBytes, long maxNoteBytes, long maxNoteCount, long maxHistoryEntries) {
			Id = id;
			Name = name;
			MaxTotalBytes = maxTotalBytes;
			MaxNoteBytes = maxNoteBytes;
			MaxNoteCount = maxNoteCount;
			MaxHistoryEntries = maxHistoryEntries;
		}
	}
}
