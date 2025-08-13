using System;
using System.Data;
using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		private void CreateKeyTables() {
			using var command = _postgres.CreateCommand();
			command.CommandText = """
				CREATE TABLE IF NOT EXISTS public."mimer_key" (
				  id uuid NOT NULL PRIMARY KEY,
				  user_id uuid NOT NULL,
				  key_name uuid NOT NULL,
				  data text NOT NULL,
				  size bigint NOT NULL DEFAULT 0,
				  note_count bigint NOT NULL DEFAULT 0,
				  created timestamp without time zone NOT NULL DEFAULT current_timestamp,
				  modified timestamp without time zone NOT NULL DEFAULT current_timestamp,
					sync bigint NOT NULL DEFAULT nextval('sync_sequence')
				);

				CREATE INDEX IF NOT EXISTS idx_mimer_key_user_sync ON mimer_key (user_id, sync);
				CREATE INDEX IF NOT EXISTS idx_mimer_key_keyname_userid ON mimer_key (key_name, user_id);

				CREATE OR REPLACE FUNCTION update_key_stats_after_insert()
				RETURNS TRIGGER AS $$
				BEGIN
					UPDATE mimer_key as k SET
					size = COALESCE((SELECT sum(size) FROM mimer_note as n where n.key_name = k.key_name), 0),
					note_count = COALESCE((SELECT count(*) FROM mimer_note as n where n.key_name = k.key_name), 0)
					WHERE k.id = NEW.id;
					RETURN NULL;
				END;
				$$ language 'plpgsql';

				DO
				$$BEGIN
				CREATE TRIGGER insert_mimer_key AFTER INSERT ON public."mimer_key" FOR EACH ROW EXECUTE PROCEDURE update_key_stats_after_insert();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				DO
				$$BEGIN
				CREATE TRIGGER update_mimer_key_modified BEFORE UPDATE ON public."mimer_key" FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				DO
				$$BEGIN
				CREATE TRIGGER update_mimer_key_sync BEFORE UPDATE ON public."mimer_key" FOR EACH ROW EXECUTE PROCEDURE update_sync_column();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				DO
				$$BEGIN
				CREATE TRIGGER update_key_size
				AFTER INSERT OR UPDATE OR DELETE ON mimer_key
				    FOR EACH ROW EXECUTE FUNCTION update_user_stats_size();
				EXCEPTION
				   WHEN duplicate_object THEN
				      NULL;
				END;$$;

				CREATE OR REPLACE FUNCTION update_key_size()
				RETURNS TRIGGER AS $$
				BEGIN
				 	IF (TG_OP = 'DELETE') THEN
						UPDATE mimer_key SET size = size - OLD.size, note_count = note_count - 1 WHERE key_name = OLD.key_name;
					ELSIF (TG_OP = 'UPDATE') THEN
						IF (NEW.key_name != OLD.key_name) THEN
							UPDATE mimer_key SET size = size - OLD.size, note_count = note_count - 1 WHERE key_name = OLD.key_name;
							UPDATE mimer_key SET size = size + NEW.size, note_count = note_count + 1 WHERE key_name = NEW.key_name;
						ELSIF (NEW.size != OLD.size) THEN
							UPDATE mimer_key SET size = size + NEW.size - OLD.size WHERE key_name = OLD.key_name;
						END IF;
					ELSIF (TG_OP = 'INSERT') THEN
						UPDATE mimer_key SET size = size + NEW.size, note_count = note_count + 1 WHERE key_name = NEW.key_name;
					END IF;
				    RETURN NULL;
				END;
				$$ language 'plpgsql';
				""";
			command.ExecuteNonQuery();
		}

		// Key-related methods
		public async Task<UserSize> GetUserSize(Guid userId) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT size, notes FROM user_stats  WHERE user_id = @user_id";
				command.Parameters.AddWithValue("@user_id", userId);
				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync()) {
					return new UserSize(reader.GetInt64(0), reader.GetInt64(1));
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return new UserSize(0, 0);
		}

		public async Task<bool> CreateKey(MimerKey data, Guid userId, (long MaxNoteCount, long MaxTotalBytes, long MaxNoteSize) stats) {
			await using var connection = await _postgres.OpenConnectionAsync();
			await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

			try {
				await using var command = new NpgsqlCommand("", connection, transaction);
				command.CommandText = "SELECT COUNT(*) FROM mimer_key WHERE user_id = @user_id";
				command.Parameters.AddWithValue("@user_id", data.UserId);
				using (var reader = await command.ExecuteReaderAsync()) {
					if (await reader.ReadAsync()) {
						// Limit for DoS considerations
						if (reader.GetInt64(0) > 1000) {
							return false;
						}
					}
				}
				command.CommandText = @"INSERT INTO mimer_key (id, user_id, key_name, data) VALUES (@id, @user_id, @key_name, @data)";
				command.Parameters.AddWithValue("@id", data.Id);
				command.Parameters.AddWithValue("@key_name", data.Name);
				command.Parameters.AddWithValue("@data", data.ToJsonString());
				await command.ExecuteNonQueryAsync();

				var (success, maxCount, maxSize, count, size) = await CheckLimits(userId, stats, command);

				if (!success) {
					throw new LimitException("User has exceeded their storage limits.", (maxCount, maxSize, count, size));
				}

				await transaction.CommitAsync();

				// command.CommandText = "SELECT COUNT(*) FROM mimer_key WHERE key_name = @key_name";
				// bool isSharedKey = false;
				// using (var reader = await command.ExecuteReaderAsync()) {
				// 	if (await reader.ReadAsync() && reader.GetInt64(0) > 1) {
				// 		isSharedKey = true;
				// 	}
				// }
				// if (isSharedKey) {
				// 	await RelcalcUserUsage(data.UserId);
				// }
				return true;
			}
			catch (LimitException) {
				try {
					await transaction.RollbackAsync();
				}
				catch {
					// Ignore rollback failures - transaction might already be aborted
				}
				throw;
			}
			catch (Exception ex) {
				try {
					await transaction.RollbackAsync();
				}
				catch {
					// Ignore rollback failures - transaction might already be aborted
				}
				Dev.Log(_connectionString, ex);
			}
			return false;
		}

		public async Task UpdateKeyStats(Guid id) {
			try {
				using var command = _postgres.CreateCommand();
				command.Parameters.AddWithValue("@id", id);
				command.CommandText = @"
UPDATE mimer_key as k SET
size = COALESCE((SELECT sum(size) FROM mimer_note as n where n.key_name = k.key_name), 0),
note_count = COALESCE((SELECT count(*) FROM mimer_note as n where n.key_name = k.key_name), 0)
WHERE k.id = @id
";
				await command.ExecuteNonQueryAsync();
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
		}

		public async Task<MimerKey?> GetKey(Guid id, Guid userId) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT user_id, data FROM mimer_key WHERE id = @id";
				command.Parameters.AddWithValue("@id", id);
				Guid userIdDb = Guid.Empty;
				string? data = null;
				using (var reader = await command.ExecuteReaderAsync()) {
					if (await reader.ReadAsync()) {
						userIdDb = reader.GetGuid(0);
						data = reader.GetString(1);
					}
				}
				if (data != null) {
					if (userIdDb == Guid.Empty) {
						command.CommandText = @"UPDATE mimer_key SET user_id = @user_id WHERE id = @id";
						command.Parameters.AddWithValue("@user_id", userId);
						await command.ExecuteNonQueryAsync();
					}
					else if (userIdDb != userId) {
						return null;
					}
					var json = new JsonObject(data);
					json.Guid("userId", userIdDb);
					return new MimerKey(json);
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		public async Task<List<MimerKey>> GetAllKeys(Guid userId) {
			var result = new List<MimerKey>();
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT data FROM mimer_key WHERE user_id = @user_id";
				command.Parameters.AddWithValue("@user_id", userId);
				using (var reader = await command.ExecuteReaderAsync()) {
					while (await reader.ReadAsync()) {
						var json = new JsonObject(reader.GetString(0));
						json.Guid("userId", userId);
						result.Add(new MimerKey(json));
					}
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return result;
		}

		public async Task<MimerKey?> GetKeyByName(Guid keyName) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT data FROM mimer_key WHERE key_name = @key_name LIMIT 1";
				command.Parameters.AddWithValue("@key_name", keyName);
				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync()) {
					return new MimerKey(new JsonObject(reader.GetString(0)));
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		public async Task<List<Guid>> GetUserIdsByKeyName(Guid keyName) {
			List<Guid> result = new List<Guid>();
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT user_id FROM mimer_key WHERE key_name = @key_name";
				command.Parameters.AddWithValue("@key_name", keyName);
				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					result.Add(reader.GetGuid(0));
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return result;
		}

		public async Task<bool> DeleteKey(Guid id) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"DELETE FROM mimer_key WHERE id = @id";
				command.Parameters.AddWithValue("@id", id);
				return await command.ExecuteNonQueryAsync() > 0;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return false;
		}

		public async Task<List<Guid>> getOwningUsers(List<Guid> keyNames) {
			var result = new List<Guid>();
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT DISTINCT user_id FROM mimer_key WHERE key_name = ANY(@key_names)";
				command.Parameters.AddWithValue("@key_names", keyNames.ToArray());
				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					result.Add(reader.GetGuid(0));
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return result;
		}

	}
}
