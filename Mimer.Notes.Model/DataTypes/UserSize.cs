namespace Mimer.Notes.Model.DataTypes {
	public class UserSize {
		public long Size { get; private set; }
		public long NoteCount { get; private set; }

		public UserSize(long size, long noteCount) {
			Size = size;
			NoteCount = noteCount;
		}
	}
}
