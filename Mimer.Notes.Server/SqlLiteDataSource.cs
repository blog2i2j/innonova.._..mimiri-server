using Microsoft.Data.Sqlite;
using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;

namespace Mimer.Notes.Server {
	public class SqlLiteDataSource : IMimerDataSource {
		private bool _isTest = false;
		private string? _testId;
		private string _rootPath = @"P:\projects.innonova\mimer\server.3.0";
		private string _connectionString = @"Data Source=P:\projects.innonova\mimer\server.3.0\database.db";

		public SqlLiteDataSource(string? testId = null) {
			_testId = testId;
			if (!string.IsNullOrEmpty(testId)) {
				_isTest = true;
				_rootPath = Path.Combine(_rootPath, "tests", testId);
				Dev.Log("Create", _rootPath);
				var dataBasePath = Path.Combine(_rootPath, "database.db");
				if (!Directory.Exists(_rootPath)) {
					Directory.CreateDirectory(_rootPath);
				}
				_connectionString = $"Data Source={dataBasePath}";
				Dev.Log(_connectionString);
			}
		}

		public void TearDown(bool keepLogs) {
			Dev.Log("Tear Down", keepLogs, _rootPath);
			if (_isTest) {
				if (Directory.Exists(_rootPath)) {
					SqliteConnection.ClearAllPools();
					string newPath = Path.Combine(@"P:\projects.innonova\mimer\server.3.0\Tests", $"{_testId!.Split('-').Last()}-last-run.db");
					if (!keepLogs) {
						File.Move(Path.Combine(_rootPath, "database.db"), newPath, true);
						Directory.Delete(_rootPath);
					}
					else {
						File.Copy(Path.Combine(_rootPath, "database.db"), newPath, true);
					}
				}
			}
		}

		public void CreateDatabase() {
			Dev.Log("CreateDatabase");
			try {
				using var connection = new SqliteConnection(_connectionString);
				connection.Open();

				using var command = connection.CreateCommand();
				command.CommandText =
@"
    DROP TABLE IF EXISTS mimer_user;
    CREATE TABLE mimer_user (
        id TEXT NOT NULL PRIMARY KEY,
        username TEXT NOT NULL UNIQUE,
				data TEXT NOT NULL
    );

    DROP TABLE IF EXISTS mimer_key;
    CREATE TABLE mimer_key (
				id TEXT NOT NULL PRIMARY KEY,
				user_id TEXT NOT NULL,
				key_name TEXT NOT NULL,
        data TEXT NOT NULL
    );

    DROP TABLE IF EXISTS mimer_note;
    CREATE TABLE mimer_note (
        id TEXT NOT NULL PRIMARY KEY,
				key_name TEXT NOT NULL
    );

    DROP TABLE IF EXISTS mimer_note_item;
    CREATE TABLE mimer_note_item (
        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
				note_id TEXT NOT NULL,
				version INTEGER NOT NULL,
				item_type TEXT NOT NULL,
        data TEXT NOT NULL,
				UNIQUE(note_id, item_type) ON CONFLICT ABORT
    );

    DROP TABLE IF EXISTS note_share_offer;
    CREATE TABLE note_share_offer (
        id TEXT NOT NULL PRIMARY KEY,
				sender TEXT NOT NULL,
				recipient TEXT NOT NULL,
				key_name TEXT NOT NULL,
        data TEXT NOT NULL,
				UNIQUE(sender, recipient, key_name) ON CONFLICT ABORT
    );

";
				command.ExecuteNonQuery();

			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}

		public async Task<bool> CreateUser(MimerUser user) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"INSERT INTO mimer_user (id, username, data) VALUES ($id, $username, $data)";

				command.Parameters.AddWithValue("$id", Guid.NewGuid());
				command.Parameters.AddWithValue("$username", user.Username);
				command.Parameters.AddWithValue("$data", user.ToJsonString());
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
				return false;
			}
		}

		public async Task<bool> UpdateUser(string oldUsername, MimerUser user) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				if (oldUsername != user.Username) {
					command.CommandText = @"UPDATE mimer_user SET username = $username, data = $data WHERE username = $old_username";
				}
				else {
					command.CommandText = @"UPDATE mimer_user SET data = $data WHERE username = $old_username";
				}
				command.Parameters.AddWithValue("$username", user.Username);
				command.Parameters.AddWithValue("$data", user.ToJsonString());
				command.Parameters.AddWithValue("$old_username", oldUsername);
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
				return false;
			}
		}

		public async Task<MimerUser?> GetUser(string username) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"SELECT data, id FROM mimer_user WHERE username = $username";
				command.Parameters.AddWithValue("$username", username);
				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync()) {
					return new MimerUser(new JsonObject(reader.GetString(0)), reader.GetGuid(1), 0, 0, 0, "{}", "{}");
				}
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return null;
		}


		public Task<bool> DeleteUser(Guid userId) {
			return Task.FromResult(false);
		}

		public Task<UserSize> GetUserSize(Guid userId) {
			return Task.FromResult(new UserSize(0, 0));
		}

		public async Task<bool> CreateKey(MimerKey data) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"INSERT INTO mimer_key (id, user_id, key_name, data) VALUES ($id, $user_id, $key_name, $data)";
				command.Parameters.AddWithValue("$id", data.Id);
				command.Parameters.AddWithValue("$user_id", data.UserId);
				command.Parameters.AddWithValue("$key_name", data.Name);
				command.Parameters.AddWithValue("$data", data.ToJsonString());
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return false;
		}

		public async Task<MimerKey?> GetKey(Guid id, Guid userId) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"SELECT data FROM mimer_key WHERE id = $id";
				command.Parameters.AddWithValue("$id", id);
				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync()) {
					return new MimerKey(new JsonObject(reader.GetString(0)));
				}
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return null;
		}

		public async Task<List<MimerKey>> GetAllKeys(Guid userId) {
			var result = new List<MimerKey>();
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"SELECT data FROM mimer_key WHERE user_id = $user_id";
				command.Parameters.AddWithValue("$user_id", userId);
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
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"SELECT data FROM mimer_key WHERE key_name = $key_name LIMIT 1";
				command.Parameters.AddWithValue("$key_name", keyName);
				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync()) {
					return new MimerKey(new JsonObject(reader.GetString(0)));
				}
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return null;
		}


		public async Task<List<Guid>> GetUserIdsByKeyName(Guid keyName) {
			List<Guid> result = new List<Guid>();
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
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
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"DELETE FROM mimer_key WHERE id = $id";
				command.Parameters.AddWithValue("$id", id);
				return await command.ExecuteNonQueryAsync() > 0;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return false;
		}

		public async Task<bool> CreateNote(DbNote note) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();
				try {
					using var command = connection.CreateCommand();
					command.CommandText = @"INSERT INTO mimer_note (id, key_name) VALUES ($id, $key_name)";
					command.Parameters.AddWithValue("$id", note.Id);
					command.Parameters.AddWithValue("$key_name", note.KeyName);
					await command.ExecuteNonQueryAsync();
					foreach (var item in note.Items) {
						command.CommandText = @"INSERT INTO mimer_note_item (note_id, version, item_type, data) VALUES ($note_id, 1, $item_type, $data)";
						command.Parameters.Clear();
						command.Parameters.AddWithValue("$note_id", note.Id);
						command.Parameters.AddWithValue("$item_type", item.Type);
						command.Parameters.AddWithValue("$data", item.Data);
						await command.ExecuteNonQueryAsync();
					}
					await transaction.CommitAsync();
				}
				catch {
					transaction.Rollback();
					throw;
				}
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return false;
		}

		public async Task<List<VersionConflict>?> UpdateNote(DbNote note, Guid oldKeyName) {
			var result = new List<VersionConflict>();
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();
				try {
					using var command = connection.CreateCommand();
					command.CommandText = @"SELECT key_name FROM mimer_note WHERE id = $id";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("$id", note.Id);
					Guid currentKeyName;
					using (var reader = await command.ExecuteReaderAsync()) {
						await reader.ReadAsync();
						currentKeyName = reader.GetGuid(0);
						if (currentKeyName != note.KeyName && currentKeyName != oldKeyName) {
							throw new Exception($"KeyName does not match ${currentKeyName} matches neither ${note.KeyName} nor ${oldKeyName}");
						}
					}
					if (currentKeyName != note.KeyName) {
						command.CommandText = @"UPDATE mimer_note SET key_name = $key_name WHERE id = $id";
						command.Parameters.Clear();
						command.Parameters.AddWithValue("$key_name", note.KeyName);
						command.Parameters.AddWithValue("$id", note.Id);
						await command.ExecuteNonQueryAsync();
					}
					foreach (var item in note.Items) {
						if (item.Version > 0) {
							command.CommandText = @"UPDATE mimer_note_item SET data = $data, version = $version + 1 WHERE note_id = $note_id AND item_type = $item_type AND version = $version";
							command.Parameters.Clear();
							command.Parameters.AddWithValue("$data", item.Data);
							command.Parameters.AddWithValue("$note_id", note.Id);
							command.Parameters.AddWithValue("$item_type", item.Type);
							command.Parameters.AddWithValue("$version", item.Version);
							if (await command.ExecuteNonQueryAsync() == 0) {
								command.CommandText = "SELECT version FROM mimer_note_item WHERE note_id = $note_id AND item_type = $item_type";
								using var reader = await command.ExecuteReaderAsync();
								long actual = 0;
								if (await reader.ReadAsync()) {
									actual = reader.GetInt64(0);
								}
								result.Add(new VersionConflict(item.Type, item.Version, actual));
							}
						}
						else {
							command.CommandText = @"INSERT INTO mimer_note_item (note_id, version, item_type, data) VALUES ($note_id, 1, $item_type, $data)";
							command.Parameters.Clear();
							command.Parameters.AddWithValue("$note_id", note.Id);
							command.Parameters.AddWithValue("$item_type", item.Type);
							command.Parameters.AddWithValue("$data", item.Data);
							await command.ExecuteNonQueryAsync();
						}
					}
					if (result.Count > 0) {
						await transaction.RollbackAsync();
						return result;
					}
					command.CommandText = @"UPDATE mimer_note SET key_name = $key_name WHERE id = $id";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("$key_name", note.KeyName);
					command.Parameters.AddWithValue("$id", note.Id);
					await command.ExecuteNonQueryAsync();

					await transaction.CommitAsync();
				}
				catch {
					transaction.Rollback();
					throw;
				}
				return result;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return null;
		}

		public async Task<DbNote?> GetNote(Guid id) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var command = connection.CreateCommand();
				command.CommandText = @"
SELECT mimer_note.key_name, mimer_note_item.version, mimer_note_item.item_type, mimer_note_item.data
FROM mimer_note
INNER JOIN mimer_note_item ON mimer_note_item.note_id = mimer_note.id
WHERE mimer_note.id = $id";
				command.Parameters.AddWithValue("$id", id);
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
				Dev.Log(_isTest, _connectionString, ex);
			}
			return null;
		}

		public async Task<bool> DeleteNote(Guid id) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();
				try {
					using var command = connection.CreateCommand();
					command.Parameters.AddWithValue("$note_id", id);
					command.CommandText = @"DELETE FROM mimer_note_item WHERE note_id = $note_id";
					await command.ExecuteNonQueryAsync();
					command.CommandText = @"DELETE FROM mimer_note WHERE id = $note_id";
					await command.ExecuteNonQueryAsync();
					await transaction.CommitAsync();
				}
				catch {
					transaction.Rollback();
					throw;
				}
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return false;
		}

		public async Task<string?> CreateNoteShareOffer(string sender, string recipient, Guid keyName, string code, string data) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();
				using var command = connection.CreateCommand();
				command.CommandText = @"INSERT INTO note_share_offer (id, sender, recipient, key_name, data) VALUES ($id, (SELECT id FROM mimer_user WHERE username = $sender), (SELECT id FROM mimer_user WHERE username = $recipient), $key_name, $data)";
				command.Parameters.AddWithValue("$id", Guid.NewGuid());
				command.Parameters.AddWithValue("$sender", sender);
				command.Parameters.AddWithValue("$recipient", recipient);
				command.Parameters.AddWithValue("$key_name", keyName);
				command.Parameters.AddWithValue("$data", data);
				await command.ExecuteNonQueryAsync();
				return code;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return null;
		}

		public async Task<List<DbShareOffer>> GetShareOffers(string username) {
			var result = new List<DbShareOffer>();
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();
				using var command = connection.CreateCommand();
				command.CommandText = @"SELECT note_share_offer.id, mimer_user.username, note_share_offer.data FROM note_share_offer INNER JOIN mimer_user ON mimer_user.id = sender WHERE recipient = (SELECT id FROM mimer_user WHERE username = $username)";
				command.Parameters.AddWithValue("$username", username);
				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					result.Add(new DbShareOffer {
						Id = reader.GetGuid(0),
						Sender = reader.GetString(1),
						Data = reader.GetString(2)
					});
				}
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return result;
		}

		public Task<DbShareOffer?> GetShareOffer(string username, string code) {
			return Task.FromResult<DbShareOffer?>(null);
		}

		public async Task<bool> DeleteNoteShareOffer(Guid id) {
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();
				using var command = connection.CreateCommand();
				command.CommandText = @"DELETE FROM note_share_offer WHERE id = $id";
				command.Parameters.AddWithValue("$id", id);
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_isTest, _connectionString, ex);
			}
			return false;
		}

		public async Task<List<VersionConflict>?> MultiApply(List<NoteAction> actions, UserStats stats) {
			var result = new List<VersionConflict>();
			try {
				using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();

				try {
					using var command = connection.CreateCommand();
					foreach (var action in actions) {
						command.Parameters.Clear();
						if (action.Type == "delete") {
							command.CommandText = @"DELETE FROM note_share_offer WHERE id = $id";
							command.Parameters.AddWithValue("$id", action.Id);
							await command.ExecuteNonQueryAsync();
						}
						else if (action.Type == "create") {
							command.CommandText = @"INSERT INTO mimer_note (id, key_name) VALUES ($id, $key_name)";
							command.Parameters.AddWithValue("$id", action.Id);
							command.Parameters.AddWithValue("$key_name", action.KeyName);
							await command.ExecuteNonQueryAsync();
							foreach (var item in action.Items) {
								command.CommandText = @"INSERT INTO mimer_note_item (note_id, version, item_type, data) VALUES ($note_id, 1, $item_type, $data)";
								command.Parameters.Clear();
								command.Parameters.AddWithValue("$note_id", action.Id);
								command.Parameters.AddWithValue("$item_type", item.Type);
								command.Parameters.AddWithValue("$data", item.Data);
								await command.ExecuteNonQueryAsync();
							}
						}
						else if (action.Type == "update") {
							command.CommandText = @"SELECT key_name FROM mimer_note WHERE id = $id";
							command.Parameters.Clear();
							command.Parameters.AddWithValue("$id", action.Id);
							Guid currentKeyName;
							using (var reader = await command.ExecuteReaderAsync()) {
								await reader.ReadAsync();
								currentKeyName = reader.GetGuid(0);
								if (currentKeyName != action.KeyName && currentKeyName != action.OldKeyName) {
									throw new Exception($"KeyName does not match ${currentKeyName} matches neither ${action.KeyName} nor ${action.OldKeyName}");
								}
							}
							if (currentKeyName != action.KeyName) {
								command.CommandText = @"UPDATE mimer_note SET key_name = $key_name WHERE id = $id";
								command.Parameters.Clear();
								command.Parameters.AddWithValue("$key_name", action.KeyName);
								command.Parameters.AddWithValue("$id", action.Id);
								await command.ExecuteNonQueryAsync();
							}

							foreach (var item in action.Items) {
								if (item.Type == "created" && item.Version > 1) {
									command.CommandText = @"DELETE FROM mimer_note_item WHERE note_id = $note_id AND item_type = $item_type AND version = $version";
									command.Parameters.Clear();
									command.Parameters.AddWithValue("$note_id", action.Id);
									command.Parameters.AddWithValue("$item_type", item.Type);
									command.Parameters.AddWithValue("$version", item.Version);
									await command.ExecuteNonQueryAsync();
								}
								else if (item.Version > 0) {
									command.CommandText = @"UPDATE mimer_note_item SET data = $data, version = $version + 1 WHERE note_id = $note_id AND item_type = $item_type AND version = $version";
									command.Parameters.Clear();
									command.Parameters.AddWithValue("$data", item.Data);
									command.Parameters.AddWithValue("$note_id", action.Id);
									command.Parameters.AddWithValue("$item_type", item.Type);
									command.Parameters.AddWithValue("$version", item.Version);
									if (await command.ExecuteNonQueryAsync() == 0) {
										command.CommandText = "SELECT version FROM mimer_note_item WHERE note_id = $note_id AND item_type = $item_type";
										using var reader = await command.ExecuteReaderAsync();
										long actual = 0;
										if (await reader.ReadAsync()) {
											actual = reader.GetInt64(0);
										}
										result.Add(new VersionConflict(item.Type, item.Version, actual));
									}
								}
								else {
									command.CommandText = @"INSERT INTO mimer_note_item (note_id, version, item_type, data) VALUES ($note_id, 1, $item_type, $data)";
									command.Parameters.Clear();
									command.Parameters.AddWithValue("$note_id", action.Id);
									command.Parameters.AddWithValue("$item_type", item.Type);
									command.Parameters.AddWithValue("$data", item.Data);
									await command.ExecuteNonQueryAsync();
								}
							}
						}
					}
					if (result.Count > 0) {
						await transaction.RollbackAsync();
						return result;
					}
					await transaction.CommitAsync();
					return result;
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		public Task UpdateUserStats(IEnumerable<UserStats> userStats) {
			return Task.CompletedTask;
		}

		public Task UpdateGlobalStats(IEnumerable<GlobalStatistic> globalStats) {
			return Task.CompletedTask;
		}

		public List<UserType> GetUserTypes() {
			return new List<UserType>() { new UserType(0, "", long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue) };
		}
	}
}
