using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		public async Task<(List<SyncNoteInfo> notes, List<SyncKeyInfo> keys)> GetChangedDataSince(Guid userId, long noteSince, long keySince) {
			var notes = new List<SyncNoteInfo>();
			var keys = new List<SyncKeyInfo>();
			try {
				using var command = _postgres.CreateCommand();
				var notesMap = new Dictionary<Guid, SyncNoteInfo>();
				command.CommandText = @"
			SELECT n.id, n.key_name, n.modified, n.created, n.sync, ni.item_type, ni.version, ni.data, ni.modified as item_modified, ni.created as item_created
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
								Sync = reader.GetInt64(4)
							};
						}

						if (!reader.IsDBNull(4)) {
							var noteItem = new SyncNoteItemInfo {
								NoteId = noteId,
								ItemType = reader.GetString(5),
								Version = reader.GetInt64(6),
								Data = reader.GetString(7),
								Modified = reader.GetDateTime(8),
								Created = reader.GetDateTime(9)
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
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return (notes, keys);
		}

		public async Task<List<SyncResult>?> ApplyChanges(Guid userId, List<NoteSyncAction> noteActions, List<KeySyncAction> keyActions) {
			List<SyncResult> results = new List<SyncResult>();
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				try {
					await using var command = new NpgsqlCommand("", connection, transaction);

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

							if (await command.ExecuteNonQueryAsync() > 0) {
								results.Add(new SyncResult("key", "create", keyAction.Id, "", 0, 0));
							}
							else {
								results.Add(new SyncResult("key", "create", keyAction.Id, "", 0, 1));
							}
						}
						else if (keyAction.Type == "delete") {
							command.CommandText = @"DELETE FROM mimer_key WHERE id = @id AND user_id = @user_id";
							command.Parameters.AddWithValue("@id", keyAction.Id);
							command.Parameters.AddWithValue("@user_id", userId);

							if (await command.ExecuteNonQueryAsync() > 0) {
								results.Add(new SyncResult("key", "delete", keyAction.Id, "", 0, 0));
							}
							else {
								results.Add(new SyncResult("key", "delete", keyAction.Id, "", 0, 1));
							}
						}
					}

					foreach (var noteAction in noteActions) {
						command.Parameters.Clear();

						if (noteAction.Type == "create") {
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
									results.Add(new SyncResult("note-item", "create", noteAction.Id, item.Type, 0, 0));
								}
								results.Add(new SyncResult("note", "create", noteAction.Id, "", 0, 0));
							}
							else {
								results.Add(new SyncResult("note", "create", noteAction.Id, "", 0, 1));
							}
						}
						else if (noteAction.Type == "update") {
							command.CommandText = @"SELECT 1 FROM mimer_note n INNER JOIN mimer_key k ON k.key_name = n.key_name WHERE n.id = @note_id AND k.user_id = @user_id";
							command.Parameters.Clear();
							command.Parameters.AddWithValue("@note_id", noteAction.Id);
							command.Parameters.AddWithValue("@user_id", userId);
							using (var reader = await command.ExecuteReaderAsync()) {
								if (!await reader.ReadAsync()) {
									results.Add(new SyncResult("note", "update", noteAction.Id, "", 1, 0));
									continue;
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
							results.Add(new SyncResult("note", "update", noteAction.Id, "", 1, 1));

							foreach (var item in noteAction.Items) {
								if (item.Version > 0) {
									command.CommandText = @"UPDATE mimer_note_item SET data = @data, size = @size, version = @version + 1 WHERE note_id = @note_id AND item_type = @item_type AND version = @version";
									command.Parameters.Clear();
									command.Parameters.AddWithValue("@data", item.Data);
									command.Parameters.AddWithValue("@size", item.Data.Length);
									command.Parameters.AddWithValue("@note_id", noteAction.Id);
									command.Parameters.AddWithValue("@item_type", item.Type);
									command.Parameters.AddWithValue("@version", item.Version);
									if (await command.ExecuteNonQueryAsync() == 0) {
										command.CommandText = "SELECT version FROM mimer_note_item WHERE note_id = @note_id AND item_type = @item_type";
										using var reader = await command.ExecuteReaderAsync();
										long actual = 0;
										if (await reader.ReadAsync()) {
											actual = reader.GetInt64(0);
										}
										results.Add(new SyncResult("note-item", "update", noteAction.Id, item.Type, item.Version, actual));
									}
									else {
										results.Add(new SyncResult("note-item", "update", noteAction.Id, item.Type, item.Version, item.Version));
									}
								}
								else {
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
										command.CommandText = "SELECT version FROM mimer_note_item WHERE note_id = @note_id AND item_type = @item_type";
										using var reader = await command.ExecuteReaderAsync();
										long actual = 0;
										if (await reader.ReadAsync()) {
											actual = reader.GetInt64(0);
										}
										results.Add(new SyncResult("note-item", "insert", noteAction.Id, item.Type, 0, actual));
									}
									else {
										results.Add(new SyncResult("note-item", "insert", noteAction.Id, item.Type, 0, 0));
									}
								}
							}
						}
						else if (noteAction.Type == "delete") {
							command.CommandText = @"SELECT 1 FROM mimer_note n INNER JOIN mimer_key k ON k.key_name = n.key_name WHERE n.id = @note_id AND k.user_id = @user_id";
							command.Parameters.Clear();
							command.Parameters.AddWithValue("@note_id", noteAction.Id);
							command.Parameters.AddWithValue("@user_id", userId);
							using (var reader = await command.ExecuteReaderAsync()) {
								if (!await reader.ReadAsync()) {
									results.Add(new SyncResult("note", "delete", noteAction.Id, "", 0, 2));
									continue;
								}
							}

							command.Parameters.Clear();
							command.CommandText = @"DELETE FROM mimer_note_item WHERE note_id = @note_id";
							command.Parameters.AddWithValue("@note_id", noteAction.Id);

							await command.ExecuteNonQueryAsync();

							command.CommandText = @"DELETE FROM mimer_note WHERE id = @note_id";
							command.Parameters.Clear();
							command.Parameters.AddWithValue("@note_id", noteAction.Id);

							if (await command.ExecuteNonQueryAsync() > 0) {
								results.Add(new SyncResult("note", "delete", noteAction.Id, "", 0, 0));
							}
							else {
								results.Add(new SyncResult("note", "delete", noteAction.Id, "", 0, 1));
							}
						}
					}

					await transaction.CommitAsync();
					return results;
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return null;
			}
		}

	}
}
