namespace Mimer.Notes.Model {
	public interface INonRepeatableRequest {
		DateTime TimeStamp { get; }
		Guid RequestId { get; }
		bool IsValid { get; }
	}
}
