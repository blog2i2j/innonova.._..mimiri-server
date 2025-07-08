using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Note operations for MimerServer
	/// </summary>
	public partial class MimerServer {

		public async Task<UpdateNoteResponse?> MultiNote(MultiNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				Dev.Log("MultiNote request not valid");
				return null;
			}

			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var userType = GetUserType(user.TypeId);
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					foreach (var keyName in request.KeyNames) {
						var key = await _dataSource.GetKeyByName(keyName);
						if (key == null) {
							Dev.Log($"MultiNote request failed, key {keyName} not found");
							return null;
						}
						var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
						if (!keySigner.VerifySignature(keyName.ToString(), request)) {
							Dev.Log($"MultiNote request failed, key {keyName} signature invalid");
							return null;
						}
					}
					long createCount = 0;
					long totalSizeAdded = 0;
					foreach (var action in request.Actions) {
						if (action.Type == "create") {
							if (++createCount + user.NoteCount > userType.MaxNoteCount + SYSTEM_NOTE_COUNT) {
								Dev.Log("create declined, too many notes");
								return null;
							}
							long size = 0;
							foreach (var item in action.Items) {
								size += item.Data.Length;
							}
							if (size > userType.MaxNoteBytes) {
								Dev.Log("create declined, note too big");
								return null;
							}
							totalSizeAdded += size;
						}
						if (action.Type == "update") {
							// if we are just moving notes around or renaming them take the cheap way out
							if (!action.Items.Any(item => item.Type != "metadata")) {
								if (action.Items[0].Data.Length > userType.MaxNoteBytes / 2) {
									Dev.Log("update declined, meta data larger then 50% of total allowed note size");
									return null;
								}
							}
							else {
								var current = await _dataSource.GetNote(action.Id);
								long delta;
								var size = CalcNewSize(current!.Items, action.Items, out delta);
								if (size > userType.MaxNoteBytes) {
									Dev.Log("update declined, note too big");
									return null;
								}
								totalSizeAdded += delta;
							}
						}
					}
					// Make it easier on the client to get the math right by allowing overshooting total by one max size note
					if (totalSizeAdded > 0 && user.Size + totalSizeAdded > userType.MaxTotalBytes + userType.MaxNoteBytes) {
						Dev.Log("multi action declined, would exceed max total bytes, and action causes growth");
						return null;
					}

					var stats = new UserStats();
					var conflicts = await _dataSource.MultiApply(request.Actions, stats);
					if (conflicts == null) {
						return null;
					}
					else if (conflicts.Count == 0) {
						_userStatsManager.AddStats(user.Id, stats);
						foreach (var action in request.Actions) {
							if (action.Type == "update") {
								_ = SendNoteUpdate(user.Id, action.KeyName, action.Id);
							}
						}
						var response = new UpdateNoteResponse();
						var userSize = await _dataSource.GetUserSize(user.Id);
						response.Size = userSize.Size;
						response.NoteCount = userSize.NoteCount;
						response.Success = true;
						return response;
					}
					else {
						var response = new UpdateNoteResponse();
						response.Success = false;
						foreach (var item in conflicts) {
							response.AddVersionConflict(item);
						}
						return response;
					}
				}
			}
			Dev.Log("MultiNote failed, user not found or signature invalid");
			return null;
		}

		public async Task<BasicResponse?> CreateNote(WriteNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var userType = GetUserType(user.TypeId);
				if (user.Size > userType.MaxTotalBytes) {
					Dev.Log("create declined, user has exceeded limit");
					return null;
				}
				if (user.NoteCount > userType.MaxNoteCount + SYSTEM_NOTE_COUNT) {
					Dev.Log("create declined, too many notes");
					return null;
				}
				var key = await _dataSource.GetKeyByName(request.KeyName);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						var note = new DbNote();
						note.Id = request.Id;
						note.KeyName = request.KeyName;
						var size = 0;
						foreach (var item in request.Items) {
							var data = item.Data;
							size += data.Length;
							note.Items.Add(new DbNoteItem(item.Version, item.Type, data));
						}
						if (size > userType.MaxNoteBytes) {
							Dev.Log("create declined, note too big");
							return null;
						}
						if (await _dataSource.CreateNote(note)) {
							_userStatsManager.RegisterCreateNote(user.Id, size);
							return new BasicResponse();
						}
					}
				}
			}
			return null;
		}

		private long CalcNewSize(List<INoteItem> current, List<INoteItem> update, out long delta) {
			delta = 0;
			long newSize = 0;
			foreach (var item in current) {
				var reqItem = update.FirstOrDefault(r => r.Type == item.Type);
				if (reqItem != null) {
					newSize += reqItem.Data.Length;
					delta += reqItem.Data.Length - item.Data.Length;
				}
				else {
					newSize += item.Data.Length;
				}
			}
			foreach (var item in update) {
				if (!current.Any(existing => existing.Type == item.Type)) {
					newSize += item.Data.Length;
					delta += item.Data.Length;
				}
			}
			return newSize;
		}

		public async Task<UpdateNoteResponse?> UpdateNote(WriteNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var userType = GetUserType(user.TypeId);
				var key = await _dataSource.GetKeyByName(request.KeyName);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						var note = await _dataSource.GetNote(request.Id);
						if (note != null) {
							if (request.OldKeyName != Guid.Empty) {
								if (note.KeyName != request.OldKeyName) {
									return null;
								}
								var oldKey = await _dataSource.GetKeyByName(request.OldKeyName);
								if (oldKey == null) {
									return null;
								}
								var oldKeySigner = new CryptSignature(oldKey.AsymmetricAlgorithm, oldKey.PublicKey);
								if (!oldKeySigner.VerifySignature("old-key", request)) {
									return null;
								}
							}
							else if (note.KeyName != request.KeyName) {
								return null;
							}
							note.KeyName = request.KeyName;

							long delta;
							if (CalcNewSize(note.Items, request.Items, out delta) > GetUserType(user.TypeId).MaxNoteBytes) {
								Dev.Log("update declined, resulting note too big");
								return null;
							}
							if (delta > 0 && user.Size > userType.MaxTotalBytes) {
								Dev.Log("create declined, user has exceeded limit, and note would grow");
								return null;
							}
							note.Items.Clear();
							var size = 0;
							foreach (var item in request.Items) {
								var data = item.Data;
								size += data.Length;
								note.Items.Add(new DbNoteItem(item.Version, item.Type, data));
							}
							var conflicts = await _dataSource.UpdateNote(note, request.OldKeyName);
							if (conflicts == null) {
								return null;
							}
							if (conflicts.Count == 0) {
								_ = SendNoteUpdate(user.Id, request.KeyName, note.Id);
								_userStatsManager.RegisterWrite(user.Id, size);
								var response = new UpdateNoteResponse();
								var userSize = await _dataSource.GetUserSize(user.Id);
								response.Size = userSize.Size;
								response.NoteCount = userSize.NoteCount;
								response.Success = true;
								return response;
							}
							else {
								var response = new UpdateNoteResponse();
								response.Success = false;
								foreach (var item in conflicts) {
									response.AddVersionConflict(item);
								}
								return response;
							}
						}
					}
				}
			}
			return null;
		}

		public async Task<ReadNoteResponse?> ReadNote(ReadNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var note = await _dataSource.GetNote(request.Id);
					if (note != null) {
						var response = new ReadNoteResponse();
						response.Id = note.Id;
						response.KeyName = note.KeyName;
						var size = 0;
						foreach (var item in note.Items) {
							if (request.Include != "*") {
								if (!request.Include.Contains(item.Type)) {
									continue;
								}
							}
							if (!request.isNewer(item.Type, item.Version)) {
								response.AddItem(item.Version, item.Type);
								continue;
							}
							size += item.Data.Length;
							response.AddItem(item.Version, item.Type, item.Data);
						}
						_userStatsManager.RegisterRead(user.Id, size);
						return response;
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteNote(DeleteNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var note = await _dataSource.GetNote(request.Id);
				if (note != null) {
					var key = await _dataSource.GetKeyByName(note.KeyName);
					if (key != null) {
						var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
						var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
						if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
							if (await _dataSource.DeleteNote(note.Id)) {
								_userStatsManager.RegisterDeleteNote(user.Id);
								return new BasicResponse();
							}
						}
					}
				}
			}
			return null;
		}
	}
}
