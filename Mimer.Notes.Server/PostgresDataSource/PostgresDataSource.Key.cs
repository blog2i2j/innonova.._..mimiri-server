using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
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

		public async Task<bool> CreateKey(MimerKey data) {
			try {
				using var command = _postgres.CreateCommand();
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
				command.CommandText = "SELECT COUNT(*) FROM mimer_key WHERE key_name = @key_name";
				bool isSharedKey = false;
				using (var reader = await command.ExecuteReaderAsync()) {
					if (await reader.ReadAsync() && reader.GetInt64(0) > 1) {
						isSharedKey = true;
					}
				}
				if (isSharedKey) {
					await RelcalcUserUsage(data.UserId);
				}
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return false;
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
	}
}
