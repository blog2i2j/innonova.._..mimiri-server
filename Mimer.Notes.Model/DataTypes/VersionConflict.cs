namespace Mimer.Notes.Model.DataTypes {
	public class VersionConflict {
		public string Type { get; }
		public long Expected { get; }
		public long Actual { get; }
		public VersionConflict(string type, long expected, long actual) {
			Type = type;
			Expected = expected;
			Actual = actual;
		}
	}
}
