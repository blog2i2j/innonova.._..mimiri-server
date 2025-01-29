namespace Mimer.Notes.Model {
	public interface IResponseObject {
		void SetJson(string json);
		string ToJsonString();
	}
}
