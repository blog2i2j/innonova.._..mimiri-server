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

					List<SyncNoteInfo>? notes;
					List<SyncKeyInfo>? keys;
					List<Guid>? deletedNotes;
					using (await TakeSyncReaderLock(user.Id)) {
						(notes, keys, deletedNotes) = await _dataSource.GetChangedDataSince(user.Id, request.NoteSince, request.KeySince);
					}

					if (notes == null || keys == null || deletedNotes == null) {
						return null;
					}

					var userSize = await _dataSource.GetUserSize(user.Id);

					response.NoteCount = (int)userSize.NoteCount;
					response.Size = userSize.Size;

					var userType = GetUserType(user.TypeId);
					response.MaxTotalBytes = userType.MaxTotalBytes;
					response.MaxNoteBytes = userType.MaxNoteBytes;
					response.MaxNoteCount = userType.MaxNoteCount;
					response.MaxHistoryEntries = userType.MaxHistoryEntries;

					foreach (var note in notes) {
						response.AddNote(note);
					}

					foreach (var key in keys) {
						response.AddKey(key);
					}

					foreach (var deletedNote in deletedNotes) {
						response.AddDeletedNote(deletedNote);
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
					var response = new SyncPushResponse();
					if (request.Notes.Count == 0 && request.Keys.Count == 0) {
						response.Status = "no-changes";
						return response;
					}
					var userType = GetUserType(user.TypeId);

					var keyNames = request.Notes.Select(n => n.KeyName).Concat(request.Keys.Select(k => k.Name).ToList()).Distinct().ToArray();

					using (await TakeSyncWriterLock(user.Id, keyNames)) {
						var status = await _dataSource.ApplyChanges(user.Id, request.Notes, request.Keys, (userType.MaxNoteCount, userType.MaxTotalBytes, userType.MaxNoteBytes));
						if (status != "success") {
							response.Status = status;
							return response;
						}
					}

					var affectedKeyNames = new HashSet<Guid>();
					foreach (var key in request.Keys) {
						affectedKeyNames.Add(key.Name);
					}
					foreach (var note in request.Notes) {
						affectedKeyNames.Add(note.KeyName);
					}

					await NotifySync(request.SyncId, user.Id, await _dataSource.getOwningUsers(affectedKeyNames.ToList()));

					response.Status = "success";
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
