using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	public partial class MimerServer {
		public async Task<SyncResponse?> Sync(SyncRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var response = new SyncResponse();
					var notes = await _dataSource.GetNotesModifiedSince(user.Id, request.Since);
					foreach (var note in notes) {
						response.AddNote(note);
					}
					var keys = await _dataSource.GetKeysModifiedSince(user.Id, request.Since);
					foreach (var key in keys) {
						response.AddKey(key);
					}

					return response;
				}
			}
			return null;
		}
	}
}
