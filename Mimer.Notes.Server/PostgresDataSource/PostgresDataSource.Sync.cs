using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mimer.Framework;
using Mimer.Notes.Model.Responses;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {   // Sync-related methods
		public async Task<List<SyncNoteInfo>> GetNotesModifiedSince(Guid userId, DateTime since) {
			var result = new List<SyncNoteInfo>();
			var noteItemsMap = new Dictionary<Guid, List<SyncNoteItemInfo>>();

			try {
				using var command = _postgres.CreateCommand();

				// First get all note items modified since the date
				command.CommandText = @"
SELECT ni.note_id, ni.item_type, ni.version, ni.data, ni.modified
FROM mimer_note_item ni
INNER JOIN mimer_note n ON n.id = ni.note_id
INNER JOIN mimer_key k ON k.key_name = n.key_name
WHERE k.user_id = @user_id AND ni.modified >= @since
ORDER BY ni.note_id, ni.modified DESC";
				command.Parameters.AddWithValue("@user_id", userId);
				command.Parameters.AddWithValue("@since", since);

				using (var reader = await command.ExecuteReaderAsync()) {
					while (await reader.ReadAsync()) {
						var noteId = reader.GetGuid(0);
						var noteItem = new SyncNoteItemInfo {
							NoteId = noteId,
							ItemType = reader.GetString(1),
							Version = reader.GetInt64(2),
							Data = reader.GetString(3),
							Modified = reader.GetDateTime(4)
						};

						if (!noteItemsMap.ContainsKey(noteId)) {
							noteItemsMap[noteId] = new List<SyncNoteItemInfo>();
						}
						noteItemsMap[noteId].Add(noteItem);
					}
				}

				// Then get the notes that have been modified (either the note itself or its items)
				command.CommandText = @"
SELECT DISTINCT n.id, n.key_name, GREATEST(n.modified, COALESCE(MAX(ni.modified), n.modified)) as latest_modified
FROM mimer_note n
INNER JOIN mimer_key k ON k.key_name = n.key_name
LEFT JOIN mimer_note_item ni ON ni.note_id = n.id
WHERE k.user_id = @user_id AND (n.modified >= @since OR ni.modified >= @since)
GROUP BY n.id, n.key_name, n.modified
ORDER BY latest_modified DESC";
				command.Parameters.Clear();
				command.Parameters.AddWithValue("@user_id", userId);
				command.Parameters.AddWithValue("@since", since);

				using (var reader = await command.ExecuteReaderAsync()) {
					while (await reader.ReadAsync()) {
						var noteId = reader.GetGuid(0);
						var note = new SyncNoteInfo {
							Id = noteId,
							KeyName = reader.GetGuid(1),
							Modified = reader.GetDateTime(2)
						};

						// Add any note items that belong to this note
						if (noteItemsMap.ContainsKey(noteId)) {
							foreach (var noteItem in noteItemsMap[noteId]) {
								note.AddItem(noteItem);
							}
						}

						result.Add(note);
					}
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return result;
		}

		public async Task<List<SyncKeyInfo>> GetKeysModifiedSince(Guid userId, DateTime since) {
			var result = new List<SyncKeyInfo>();
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"
SELECT id, key_name, created, modified
FROM mimer_key
WHERE user_id = @user_id AND (created >= @since OR modified >= @since)
ORDER BY modified DESC";
				command.Parameters.AddWithValue("@user_id", userId);
				command.Parameters.AddWithValue("@since", since);

				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					result.Add(new SyncKeyInfo {
						Id = reader.GetGuid(0),
						Name = reader.GetGuid(1),
						Created = reader.GetDateTime(2),
						Modified = reader.GetDateTime(3)
					});
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return result;
		}
	}
}
