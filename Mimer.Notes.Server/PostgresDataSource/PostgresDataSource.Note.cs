using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		// Note-related methods
		private async Task<long> DoCreateNote(NpgsqlCommand command, Guid id, Guid keyName, List<INoteItem> items) {
			long writtenBytes = 0;
			command.CommandText = @"INSERT INTO mimer_note (id, key_name) VALUES (@id, @key_name)";
			command.Parameters.AddWithValue("@id", id);
			command.Parameters.AddWithValue("@key_name", keyName);
			await command.ExecuteNonQueryAsync();
			foreach (var item in items) {
				writtenBytes += item.Data.Length;
				command.CommandText = @"INSERT INTO mimer_note_item (note_id, version, item_type, data, size) VALUES (@note_id, 1, @item_type, @data, @size)";
				command.Parameters.Clear();
				command.Parameters.AddWithValue("@note_id", id);
				command.Parameters.AddWithValue("@item_type", item.Type);
				command.Parameters.AddWithValue("@data", item.Data);
				command.Parameters.AddWithValue("@size", item.Data.Length);
				await command.ExecuteNonQueryAsync();
			}
			return writtenBytes;
		}

		public async Task<bool> CreateNote(DbNote note) {
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				try {
					await using var command = new NpgsqlCommand("", connection, transaction);
					await DoCreateNote(command, note.Id, note.KeyName, note.Items);
					await transaction.CommitAsync();
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return false;
		}

		private async Task<long> DoUpdateNote(NpgsqlCommand command, Guid id, Guid keyName, Guid oldKeyName, List<INoteItem> items, List<VersionConflict> conflicts) {
			long writtenBytes = 0;
			command.CommandText = @"SELECT key_name FROM mimer_note WHERE id = @id";
			command.Parameters.Clear();
			command.Parameters.AddWithValue("@id", id);
			Guid currentKeyName;
			using (var reader = await command.ExecuteReaderAsync()) {
				await reader.ReadAsync();
				currentKeyName = reader.GetGuid(0);
				if (currentKeyName != keyName && currentKeyName != oldKeyName) {
					throw new Exception($"KeyName does not match ${currentKeyName} matches neither ${keyName} nor ${oldKeyName}");
				}
			}
			if (currentKeyName != keyName) {
				command.CommandText = @"UPDATE mimer_note SET key_name = @key_name WHERE id = @id";
				command.Parameters.Clear();
				command.Parameters.AddWithValue("@key_name", keyName);
				command.Parameters.AddWithValue("@id", id);
				await command.ExecuteNonQueryAsync();
			}

			foreach (var item in items) {
				if (item.Type == "created" && item.Version > 1) {
					command.CommandText = @"DELETE FROM mimer_note_item WHERE note_id = @note_id AND item_type = @item_type AND version = @version";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("@note_id", id);
					command.Parameters.AddWithValue("@item_type", item.Type);
					command.Parameters.AddWithValue("@version", item.Version);
					await command.ExecuteNonQueryAsync();
				}
				else if (item.Version > 0) {
					command.CommandText = @"UPDATE mimer_note_item SET data = @data, size = @size, version = @version + 1 WHERE note_id = @note_id AND item_type = @item_type AND version = @version";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("@data", item.Data);
					command.Parameters.AddWithValue("@size", item.Data.Length);
					command.Parameters.AddWithValue("@note_id", id);
					command.Parameters.AddWithValue("@item_type", item.Type);
					command.Parameters.AddWithValue("@version", item.Version);
					if (await command.ExecuteNonQueryAsync() == 0) {
						command.CommandText = "SELECT version FROM mimer_note_item WHERE note_id = @note_id AND item_type = @item_type";
						using var reader = await command.ExecuteReaderAsync();
						long actual = 0;
						if (await reader.ReadAsync()) {
							actual = reader.GetInt64(0);
						}
						conflicts.Add(new VersionConflict(item.Type, item.Version, actual));
					}
					writtenBytes += item.Data.Length;
				}
				else {
					command.CommandText = @"INSERT INTO mimer_note_item (note_id, version, item_type, data, size) VALUES (@note_id, 1, @item_type, @data, @size)";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("@note_id", id);
					command.Parameters.AddWithValue("@item_type", item.Type);
					command.Parameters.AddWithValue("@data", item.Data);
					command.Parameters.AddWithValue("@size", item.Data.Length);
					writtenBytes += item.Data.Length;
					await command.ExecuteNonQueryAsync();
				}
			}
			return writtenBytes;
		}

		public async Task<List<VersionConflict>?> UpdateNote(DbNote note, Guid oldKeyName) {
			var conflicts = new List<VersionConflict>();
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				try {
					await using var command = new NpgsqlCommand("", connection, transaction);
					await DoUpdateNote(command, note.Id, note.KeyName, oldKeyName, note.Items, conflicts);

					if (conflicts.Count > 0) {
						await transaction.RollbackAsync();
						return conflicts;
					}

					await transaction.CommitAsync();
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
				return conflicts;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		public async Task<DbNote?> GetNote(Guid id) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"
SELECT mimer_note.key_name, mimer_note_item.version, mimer_note_item.item_type, mimer_note_item.data
FROM mimer_note
INNER JOIN mimer_note_item ON mimer_note_item.note_id = mimer_note.id
WHERE mimer_note.id = @id";
				command.Parameters.AddWithValue("@id", id);
				using var reader = await command.ExecuteReaderAsync();
				var note = new DbNote();
				note.Id = id;
				bool found = false;
				while (await reader.ReadAsync()) {
					found = true;
					note.KeyName = reader.GetGuid(0);
					note.Items.Add(new DbNoteItem(reader.GetInt64(1), reader.GetString(2), reader.GetString(3)));
				}
				if (found) {
					return note;
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		private async Task DoDeleteNote(NpgsqlCommand command, Guid id) {
			command.Parameters.AddWithValue("@note_id", id);
			command.CommandText = @"DELETE FROM mimer_note_item WHERE note_id = @note_id";
			await command.ExecuteNonQueryAsync();
			command.CommandText = @"DELETE FROM mimer_note WHERE id = @note_id";
			await command.ExecuteNonQueryAsync();
		}

		public async Task<bool> DeleteNote(Guid id) {
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				try {
					await using var command = new NpgsqlCommand("", connection, transaction);
					await DoDeleteNote(command, id);
					await transaction.CommitAsync();
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return false;
		}
	}
}
