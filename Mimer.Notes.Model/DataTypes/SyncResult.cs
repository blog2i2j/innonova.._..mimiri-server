namespace Mimer.Notes.Model.DataTypes {
	public class SyncResult {
		public string ItemType { get; }
		public string Action { get; }
		public Guid Id { get; }
		public string Type { get; }
		public long Expected { get; }
		public long Actual { get; }
		public SyncResult(string itemType, string action, Guid id, string type, long expected, long actual) {
			ItemType = itemType;
			Action = action;
			Id = id;
			Type = type;
			Expected = expected;
			Actual = actual;
		}
	}
}
