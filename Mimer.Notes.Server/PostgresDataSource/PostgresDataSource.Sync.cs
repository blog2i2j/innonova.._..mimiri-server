using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;
using Npgsql;

namespace Mimer.Notes.Server {

	public class SyncException : Exception {
		public SyncException(string message) : base(message) { }
		public SyncException(string message, Exception innerException) : base(message, innerException) { }
	}

	public partial class PostgresDataSource {
		private const long SYSTEM_NOTE_COUNT = 3;

		public async Task<(List<SyncNoteInfo>? notes, List<SyncKeyInfo>? keys, List<Guid>? deletedNotes)> GetChangedDataSince(Guid userId, long noteSince, long keySince) {
			const int maxRetries = 3;
			const int baseDelayMs = 100;

			for (int attempt = 0; attempt <= maxRetries; attempt++) {
				try {
					return await GetChangedDataSinceInternal(userId, noteSince, keySince);
				}
				catch (PostgresException ex) when (IsSerializationError(ex) && attempt < maxRetries) {
					var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt) + Random.Shared.Next(0, 50));
					Dev.Log($"Serialization error (GetChangedDataSince) on attempt {attempt + 1}, retrying after {delay.TotalMilliseconds}ms: {ex.Message}");
					await Task.Delay(delay);
				}
				catch (Exception ex) {
					Dev.Log(ex);
					return (null, null, null);
				}
			}

			return (null, null, null);
		}


		private async Task<(List<SyncNoteInfo>? notes, List<SyncKeyInfo>? keys, List<Guid>? deletedNotes)> GetChangedDataSinceInternal(Guid userId, long noteSince, long keySince) {
			var notes = new List<SyncNoteInfo>();
			var keys = new List<SyncKeyInfo>();
			var deletedNotes = new List<Guid>();

			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

				try {
					await using var command = new NpgsqlCommand("", connection, transaction);
					var notesMap = new Dictionary<Guid, SyncNoteInfo>();
					command.CommandText = @"
			SELECT n.id, n.key_name, n.modified, n.created, n.sync, n.size, ni.item_type, ni.version, ni.data, ni.modified as item_modified, ni.created as item_created, ni.size as item_size
			FROM mimer_note n
			INNER JOIN mimer_key k ON k.key_name = n.key_name
			LEFT JOIN mimer_note_item ni ON ni.note_id = n.id
			WHERE k.user_id = @user_id AND n.sync > @since
			ORDER BY n.sync ASC LIMIT 250";
					command.Parameters.AddWithValue("@user_id", userId);
					command.Parameters.AddWithValue("@since", noteSince);

					using (var reader = await command.ExecuteReaderAsync()) {
						while (await reader.ReadAsync()) {
							var noteId = reader.GetGuid(0);

							if (!notesMap.ContainsKey(noteId)) {
								notesMap[noteId] = new SyncNoteInfo {
									Id = noteId,
									KeyName = reader.GetGuid(1),
									Modified = reader.GetDateTime(2),
									Created = reader.GetDateTime(3),
									Sync = reader.GetInt64(4),
									Size = (int)reader.GetInt64(5)
								};
							}

							if (!reader.IsDBNull(6)) {
								var noteItem = new SyncNoteItemInfo {
									NoteId = noteId,
									ItemType = reader.GetString(6),
									Version = reader.GetInt64(7),
									Data = reader.GetString(8),
									Modified = reader.GetDateTime(9),
									Created = reader.GetDateTime(10),
									Size = (int)reader.GetInt64(11)
								};
								notesMap[noteId].AddItem(noteItem);
							}
						}
					}
					notes.AddRange(notesMap.Values);

					command.CommandText = @"
			SELECT id, key_name, data, created, modified, sync
			FROM mimer_key
			WHERE user_id = @user_id AND sync > @since
			ORDER BY sync ASC LIMIT 250";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("@user_id", userId);
					command.Parameters.AddWithValue("@since", keySince);

					using (var reader = await command.ExecuteReaderAsync()) {
						while (await reader.ReadAsync()) {
							keys.Add(new SyncKeyInfo {
								Id = reader.GetGuid(0),
								Name = reader.GetGuid(1),
								Data = reader.GetString(2),
								Created = reader.GetDateTime(3),
								Modified = reader.GetDateTime(4),
								Sync = reader.GetInt64(5)
							});
						}
					}

					command.CommandText = @"
			SELECT n.note_id FROM deleted_mimer_note n
			INNER JOIN mimer_key k ON k.key_name = n.key_name
			WHERE k.user_id = @user_id AND n.sync > @since
			ORDER BY n.sync ASC LIMIT 250";
					command.Parameters.AddWithValue("@user_id", userId);
					command.Parameters.AddWithValue("@since", noteSince);

					using (var reader = await command.ExecuteReaderAsync()) {
						while (await reader.ReadAsync()) {
							deletedNotes.Add(reader.GetGuid(0));
						}
					}

					await transaction.CommitAsync();
					return (notes, keys, deletedNotes);
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex) {
				Dev.Log(ex);
				return (null, null, null);
			}
		}

		public async Task<string> ApplyChanges(Guid userId, List<NoteSyncAction> noteActions, List<KeySyncAction> keyActions, (long MaxNoteCount, long MaxTotalBytes, long MaxNoteSize) stats) {
			// Dev.Log($"Applying changes for user {userId}: {noteActions.Count} notes, {keyActions.Count} keys");
			const int maxRetries = 3;
			const int baseDelayMs = 100;

			for (int attempt = 0; attempt <= maxRetries; attempt++) {
				try {
					await ApplyChangesCore(userId, noteActions, keyActions, stats);
					return "success";
				}
				catch (PostgresException ex) when (IsSerializationError(ex) && attempt < maxRetries) {
					var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt) + Random.Shared.Next(0, 50));
					Dev.Log($"Serialization error (ApplyChanges) on attempt {attempt + 1}, retrying after {delay.TotalMilliseconds}ms: {ex.Message}");
					await Task.Delay(delay);
				}
				catch (SyncException) {
					return "conflict";
				}
				catch (Exception ex) {
					Dev.Log(ex);
					return "error";
				}
			}
			return "no-changes";
		}

		private static bool IsSerializationError(PostgresException ex) {
			return ex.SqlState == "40001" || // serialization_failure
					 ex.SqlState == "40P01";   // deadlock_detected
		}

		private async Task ApplyChangesCore(Guid userId, List<NoteSyncAction> noteActions, List<KeySyncAction> keyActions, (long MaxNoteCount, long MaxTotalBytes, long MaxNoteSize) stats) {
			await using var connection = await _postgres.OpenConnectionAsync();
			await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

			try {
				await using var command = new NpgsqlCommand("", connection, transaction);

				await ProcessKeyActions(command, keyActions, userId);
				await ProcessNoteActions(command, noteActions, userId, stats.MaxNoteSize);
				var limits = await CheckLimits(userId, stats, command);
				if (!limits.Success) {
					throw new LimitException("User has exceeded their storage limits.", (limits.MaxCount, limits.MaxSize, limits.Count, limits.Size));
				}

				await transaction.CommitAsync();
			}
			catch (PostgresException ex) when (IsSerializationError(ex)) {
				throw;
			}
			catch {
				try {
					await transaction.RollbackAsync();
				}
				catch {
					// Ignore rollback failures - transaction might already be aborted
				}
				throw;
			}
		}

		private async Task ProcessKeyActions(NpgsqlCommand command, List<KeySyncAction> keyActions, Guid userId) {
			foreach (var keyAction in keyActions) {
				command.Parameters.Clear();

				if (keyAction.Type == "create") {
					command.CommandText = @"
						INSERT INTO mimer_key (id, user_id, key_name, data)
						SELECT @id, @user_id, @key_name, @data
						WHERE NOT EXISTS (
							SELECT 1 FROM mimer_key WHERE id = @id
						)";

					var keyData = new JsonObject(keyAction.Data);
					command.Parameters.AddWithValue("@id", keyAction.Id);
					command.Parameters.AddWithValue("@user_id", keyData.Guid("userId"));
					command.Parameters.AddWithValue("@key_name", keyAction.Name);
					command.Parameters.AddWithValue("@data", keyAction.Data);

					if (await command.ExecuteNonQueryAsync() == 0) {
						throw new SyncException($"Failed to create key {keyAction.Id} for user {userId}. Key already exists.");
					}
				}
				else if (keyAction.Type == "delete") {
					command.CommandText = @"DELETE FROM mimer_key WHERE id = @id AND user_id = @user_id";
					command.Parameters.AddWithValue("@id", keyAction.Id);
					command.Parameters.AddWithValue("@user_id", userId);

					if (await command.ExecuteNonQueryAsync() == 0) {
						Dev.Log($"Failed to delete key {keyAction.Id} for user {userId}. Key does not exist or is not owned by the user.");
					}
				}
			}
		}

		private async Task ProcessNoteActions(NpgsqlCommand command, List<NoteSyncAction> noteActions, Guid userId, long MaxNoteSize) {
			foreach (var noteAction in noteActions) {
				command.Parameters.Clear();

				switch (noteAction.Type) {
					case "create":
						await ProcessNoteCreate(command, noteAction, MaxNoteSize);
						break;
					case "update":
						await ProcessNoteUpdate(command, noteAction, userId, MaxNoteSize);
						break;
					case "delete":
						await ProcessNoteDelete(command, noteAction, userId);
						break;
				}
			}
		}

		private async Task ProcessNoteCreate(NpgsqlCommand command, NoteSyncAction noteAction, long MaxNoteSize) {
			if (noteAction.Items.Sum(item => item.Data.Length) > MaxNoteSize) {
				throw new SyncException($"Note {noteAction.Id} exceeds maximum size of {MaxNoteSize} bytes.");
			}

			command.CommandText = @"
				INSERT INTO mimer_note (id, key_name)
				SELECT @id, @key_name
				WHERE NOT EXISTS (
					SELECT 1 FROM mimer_note WHERE id = @id
				)";

			command.Parameters.AddWithValue("@id", noteAction.Id);
			command.Parameters.AddWithValue("@key_name", noteAction.KeyName);

			int noteCreated = await command.ExecuteNonQueryAsync();

			if (noteCreated > 0) {
				foreach (var item in noteAction.Items) {
					command.CommandText = @"
						INSERT INTO mimer_note_item (note_id, version, item_type, data, size)
						VALUES (@note_id, 1, @item_type, @data, @size)";

					command.Parameters.Clear();
					command.Parameters.AddWithValue("@note_id", noteAction.Id);
					command.Parameters.AddWithValue("@item_type", item.Type);
					command.Parameters.AddWithValue("@data", item.Data);
					command.Parameters.AddWithValue("@size", item.Data.Length);
					await command.ExecuteNonQueryAsync();
				}
			}
			else {
				throw new SyncException($"Failed to create note {noteAction.Id}. Note already exists.");
			}
		}

		private async Task ProcessNoteUpdate(NpgsqlCommand command, NoteSyncAction noteAction, Guid userId, long MaxNoteSize) {
			if (noteAction.Items.Sum(item => item.Data.Length) > MaxNoteSize) {
				throw new SyncException($"Note {noteAction.Id} exceeds maximum size of {MaxNoteSize} bytes.");
			}

			command.CommandText = @"SELECT 1 FROM mimer_note n INNER JOIN mimer_key k ON k.key_name = n.key_name WHERE n.id = @note_id AND k.user_id = @user_id";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("@note_id", noteAction.Id);
			command.Parameters.AddWithValue("@user_id", userId);

			using (var reader = await command.ExecuteReaderAsync()) {
				if (!await reader.ReadAsync()) {
					throw new SyncException($"Failed to update note {noteAction.Id}. Note does not exist or is not accessible by the user.");
				}
			}

			command.CommandText = @"
				UPDATE mimer_note
				SET key_name = @key_name
				WHERE id = @id AND key_name != @key_name";

			command.Parameters.Clear();
			command.Parameters.AddWithValue("@id", noteAction.Id);
			command.Parameters.AddWithValue("@key_name", noteAction.KeyName);
			await command.ExecuteNonQueryAsync();

			foreach (var item in noteAction.Items) {
				if (item.Version > 0) {
					await ProcessNoteItemUpdate(command, noteAction, item);
				}
				else {
					await ProcessNoteItemInsert(command, noteAction, item);
				}
			}
		}

		private async Task ProcessNoteItemUpdate(NpgsqlCommand command, NoteSyncAction noteAction, dynamic item) {
			command.CommandText = @"UPDATE mimer_note_item SET data = @data, size = @size, version = @version + 1 WHERE note_id = @note_id AND item_type = @item_type AND version = @version";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("@data", item.Data);
			command.Parameters.AddWithValue("@size", item.Data.Length);
			command.Parameters.AddWithValue("@note_id", noteAction.Id);
			command.Parameters.AddWithValue("@item_type", item.Type);
			command.Parameters.AddWithValue("@version", item.Version);

			if (await command.ExecuteNonQueryAsync() == 0) {
				throw new SyncException($"Failed to update note item {item.Type} for note {noteAction.Id}. Item does not exist or version mismatch.");
			}
		}

		private async Task ProcessNoteItemInsert(NpgsqlCommand command, NoteSyncAction noteAction, dynamic item) {
			command.CommandText = @"
				INSERT INTO mimer_note_item (note_id, version, item_type, data, size)
				VALUES (@note_id, 1, @item_type, @data, @size)
				ON CONFLICT (note_id, item_type) DO NOTHING";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("@note_id", noteAction.Id);
			command.Parameters.AddWithValue("@item_type", item.Type);
			command.Parameters.AddWithValue("@data", item.Data);
			command.Parameters.AddWithValue("@size", item.Data.Length);

			if (await command.ExecuteNonQueryAsync() == 0) {
				throw new SyncException($"Failed to insert note item {item.Type} for note {noteAction.Id}. Item already exists.");
			}
		}

		private async Task ProcessNoteDelete(NpgsqlCommand command, NoteSyncAction noteAction, Guid userId) {
			command.CommandText = @"SELECT 1 FROM mimer_note n INNER JOIN mimer_key k ON k.key_name = n.key_name WHERE n.id = @note_id AND k.user_id = @user_id";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("@note_id", noteAction.Id);
			command.Parameters.AddWithValue("@user_id", userId);

			using (var reader = await command.ExecuteReaderAsync()) {
				if (!await reader.ReadAsync()) {
					throw new SyncException($"Failed to delete note {noteAction.Id}. Note does not exist or is not accessible by the user.");
				}
			}

			command.Parameters.Clear();
			command.CommandText = @"DELETE FROM mimer_note_item WHERE note_id = @note_id";
			command.Parameters.AddWithValue("@note_id", noteAction.Id);

			await command.ExecuteNonQueryAsync();

			command.CommandText = @"DELETE FROM mimer_note WHERE id = @note_id";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("@note_id", noteAction.Id);

			if (await command.ExecuteNonQueryAsync() == 0) {
				throw new SyncException($"Failed to delete note {noteAction.Id}. Unexpected error");
			}
		}

	}
}
