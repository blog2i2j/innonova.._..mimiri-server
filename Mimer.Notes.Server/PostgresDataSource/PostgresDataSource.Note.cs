using System;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		// Database creation methods
		private void CreateNoteTables() {
			using var command = _postgres.CreateCommand();
			command.CommandText = """
				CREATE TABLE IF NOT EXISTS public."mimer_note" (
				  id uuid NOT NULL PRIMARY KEY,
				  key_name uuid NOT NULL,
				  size bigint NOT NULL DEFAULT 0,
				  created timestamp without time zone NOT NULL DEFAULT current_timestamp,
				  modified timestamp without time zone NOT NULL DEFAULT current_timestamp,
					sync bigint NOT NULL DEFAULT nextval('sync_sequence')
				);

				CREATE INDEX IF NOT EXISTS idx_mimer_note_keyname_sync ON mimer_note (key_name, sync);

				DO
				$$BEGIN
				CREATE TRIGGER update_mimer_note_modified BEFORE UPDATE ON public."mimer_note"  FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				DO
				$$BEGIN
				CREATE TRIGGER update_mimer_note_sync BEFORE UPDATE ON public."mimer_note"  FOR EACH ROW EXECUTE PROCEDURE update_sync_column();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				CREATE TABLE IF NOT EXISTS public."mimer_note_item" (
				  note_id uuid NOT NULL,
				  item_type character varying(50) NOT NULL,
				  version bigint NOT NULL,
				  data text NOT NULL,
				  size bigint NOT NULL,
				  created timestamp without time zone NOT NULL DEFAULT current_timestamp,
				  modified timestamp without time zone NOT NULL DEFAULT current_timestamp,
				  PRIMARY KEY (note_id, item_type)
				);

				CREATE INDEX IF NOT EXISTS idx_mimer_note_item_note_id ON mimer_note_item (note_id);

				DO
				$$BEGIN
				CREATE TRIGGER update_mimer_note_item_modified BEFORE UPDATE ON public."mimer_note_item"  FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				CREATE TABLE IF NOT EXISTS public."deleted_mimer_note" (
				  note_id uuid NOT NULL PRIMARY KEY,
				  key_name uuid NOT NULL,
				  created timestamp without time zone NOT NULL DEFAULT current_timestamp,
				  sync bigint NOT NULL DEFAULT nextval('sync_sequence')
				);

				CREATE OR REPLACE FUNCTION log_note_deletion()
				RETURNS TRIGGER AS $$
				BEGIN
					 INSERT INTO deleted_mimer_note (note_id, key_name) VALUES (OLD.id, OLD.key_name);
					 RETURN NULL;
				END;
				$$ language 'plpgsql';

				DO
				$$BEGIN
				CREATE TRIGGER note_deletion_trigger
				AFTER DELETE ON public."mimer_note"
				FOR EACH ROW EXECUTE PROCEDURE log_note_deletion();
				EXCEPTION
					WHEN duplicate_object THEN
						NULL;
				END;$$;

				CREATE OR REPLACE FUNCTION remove_from_deleted_notes()
				RETURNS TRIGGER AS $$
				BEGIN
					DELETE FROM deleted_mimer_note WHERE note_id = NEW.id;
					RETURN NEW;
				END;
				$$ language 'plpgsql';

				DO
				$$BEGIN
				CREATE TRIGGER remove_deleted_note_trigger
				AFTER INSERT ON public."mimer_note"
				FOR EACH ROW EXECUTE PROCEDURE remove_from_deleted_notes();
				EXCEPTION
					 WHEN duplicate_object THEN
						NULL;
				END;$$;

				CREATE OR REPLACE FUNCTION update_note_size()
				RETURNS TRIGGER AS $$
				BEGIN
				 	IF (TG_OP = 'DELETE') THEN
						UPDATE mimer_note SET size = size - OLD.size WHERE id = OLD.note_id;
					ELSIF (TG_OP = 'UPDATE') THEN
						IF (NEW.size != OLD.size) THEN
							UPDATE mimer_note SET size = size + NEW.size - OLD.size WHERE id = NEW.note_id;
						ELSE
							UPDATE mimer_note SET size = size + 1 WHERE id = NEW.note_id;
							UPDATE mimer_note SET size = size - 1 WHERE id = NEW.note_id;
						END IF;
					ELSIF (TG_OP = 'INSERT') THEN
						UPDATE mimer_note SET size = size + NEW.size WHERE id = NEW.note_id;
					END IF;
				    RETURN NULL;
				END;
				$$ language 'plpgsql';

				DO
				$$BEGIN
				CREATE TRIGGER update_note_item_size
				AFTER INSERT OR UPDATE OR DELETE ON mimer_note_item
				    FOR EACH ROW EXECUTE FUNCTION update_note_size();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				DO
				$$BEGIN
				CREATE TRIGGER update_note_size
				AFTER INSERT OR UPDATE OR DELETE ON mimer_note
				    FOR EACH ROW EXECUTE FUNCTION update_key_size();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;
				""";
			command.ExecuteNonQuery();
		}

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
SELECT mimer_note.key_name, mimer_note.created, mimer_note.modified, mimer_note.sync, mimer_note.size,
			 mimer_note_item.version, mimer_note_item.item_type, mimer_note_item.data, mimer_note_item.created, mimer_note_item.modified, mimer_note_item.size
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
					note.Created = reader.GetDateTime(1);
					note.Modified = reader.GetDateTime(2);
					note.Sync = reader.GetInt64(3);
					note.Size = reader.GetInt32(4);
					note.Items.Add(new DbNoteItem(reader.GetInt64(5), reader.GetString(6), reader.GetString(7), reader.GetDateTime(8), reader.GetDateTime(9), reader.GetInt32(10)));
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
