namespace Mimer.Notes.Model {
	public interface ISignable {
		string PayloadToSign { get; }
		void AddSignature(string name, string signature);
		string? GetSignature(string name);
	}
}
