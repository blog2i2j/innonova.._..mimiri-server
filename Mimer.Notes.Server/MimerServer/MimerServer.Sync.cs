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

					var (notes, keys) = await _dataSource.GetChangedDataSince(user.Id, request.NoteSince, request.KeySince);
					var userSize = await _dataSource.GetUserSize(user.Id);

					response.NoteCount = (int)userSize.NoteCount;
					response.Size = userSize.Size;

					if (notes == null || keys == null) {
						return null;
					}

					foreach (var note in notes) {
						response.AddNote(note);
					}

					foreach (var key in keys) {
						response.AddKey(key);
					}

					return response;
				}
			}
			return null;
		}
		public async Task<SyncPushResponse?> PushSync(SyncPushRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var results = await _dataSource.ApplyChanges(user.Id, request.Notes, request.Keys);
					if (results == null) {
						return null;
					}
					// TODO check success of results
					if (results.Count > 0) {
						await NotifySync();
					}

					var response = new SyncPushResponse();

					foreach (var result in results) {
						response.AddSyncResult(result);
					}

					return response;
				}
			}
			return null;
		}

		// private string CompressToBase64(string data) {
		// 	byte[] dataBytes = Encoding.UTF8.GetBytes(data);
		// 	using (var outputStream = new MemoryStream()) {
		// 		using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress)) {
		// 			gzipStream.Write(dataBytes, 0, dataBytes.Length);
		// 		}
		// 		byte[] compressedBytes = outputStream.ToArray();
		// 		return Convert.ToBase64String(compressedBytes);
		// 	}
		// }
	}
}
