using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mimer.Framework;
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

	}
}
