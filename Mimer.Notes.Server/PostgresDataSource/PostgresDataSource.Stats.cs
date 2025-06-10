using System;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		// Statistics-related methods
		public async Task<List<VersionConflict>?> MultiApply(List<NoteAction> actions, UserStats stats) {
			var result = new List<VersionConflict>();
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				try {
					await using var command = new NpgsqlCommand("", connection, transaction);
					foreach (var action in actions) {
						command.Parameters.Clear();
						if (action.Type == "delete") {
							await DoDeleteNote(command, action.Id);
							stats.Deletes++;
							stats.Writes++;
						}
						else if (action.Type == "create") {
							long written = await DoCreateNote(command, action.Id, action.KeyName, action.Items);
							stats.WriteBytes += written;
							stats.Creates++;
							stats.Writes++;
						}
						else if (action.Type == "update") {
							long written = await DoUpdateNote(command, action.Id, action.KeyName, action.OldKeyName, action.Items, result);
							stats.WriteBytes += written;
							stats.Writes++;
						}
					}
					if (result.Count > 0) {
						await transaction.RollbackAsync();
						return result;
					}
					await RelcalcUserUsage(stats.UserId);
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

		public async Task UpdateUserStats(IEnumerable<UserStats> userStats) {
			for (int retry = 0; retry < 10; retry++) {
				try {
					await using var connection = await _postgres.OpenConnectionAsync();
					await using var transaction = await connection.BeginTransactionAsync();
					try {
						await using var command = new NpgsqlCommand("", connection, transaction);
						command.CommandText = @"
UPDATE user_stats SET
	last_activity = current_timestamp,
	logins = logins + @logins,
	reads = reads + @reads,
	writes = writes + @writes,
	read_bytes = read_bytes + @read_bytes,
	write_bytes = write_bytes + @write_bytes,
	creates = creates + @creates,
	deletes = deletes + @deletes,
	notifications = notifications + @notifications
WHERE
	user_id = @user_id
";
						foreach (var stats in userStats) {
							command.Parameters.Clear();
							command.Parameters.AddWithValue("@logins", stats.Logins);
							command.Parameters.AddWithValue("@reads", stats.Reads);
							command.Parameters.AddWithValue("@writes", stats.Writes);
							command.Parameters.AddWithValue("@read_bytes", stats.ReadBytes);
							command.Parameters.AddWithValue("@write_bytes", stats.WriteBytes);
							command.Parameters.AddWithValue("@creates", stats.Creates);
							command.Parameters.AddWithValue("@deletes", stats.Deletes);
							command.Parameters.AddWithValue("@notifications", stats.Notifications);
							command.Parameters.AddWithValue("@user_id", stats.UserId);
							var affected = await command.ExecuteNonQueryAsync();
						}
						await transaction.CommitAsync();
						return;
					}
					catch (Exception ex) {
						await transaction.RollbackAsync();
						Dev.Log(ex);
					}
				}
				catch (Exception ex) {
					Dev.Log(ex);
				}
			}
		}

		public async Task UpdateGlobalStats(IEnumerable<GlobalStatistic> globalStats) {
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();
				try {
					await using var updateCommand = new NpgsqlCommand("", connection, transaction);
					await using var insertCommand = new NpgsqlCommand("", connection, transaction);
					updateCommand.CommandText = @"
UPDATE global_stats SET
	value = value + @value,
	last_activity = @last_activity
WHERE
	id = @id
";

					insertCommand.CommandText = @"
INSERT INTO
global_stats (
	id,
	value_type,
	action,
	key,
	value,
	last_activity
) VALUES (
	@id,
	@value_type,
	@action,
	@key,
	@value,
	@last_activity
)
";
					foreach (var stats in globalStats) {
						updateCommand.Parameters.Clear();
						updateCommand.Parameters.AddWithValue("@id", stats.Id);
						updateCommand.Parameters.AddWithValue("@value", stats.Value);
						updateCommand.Parameters.AddWithValue("@last_activity", stats.LastActivity);
						if (await updateCommand.ExecuteNonQueryAsync() == 0) {
							insertCommand.Parameters.Clear();
							insertCommand.Parameters.AddWithValue("@id", stats.Id);
							insertCommand.Parameters.AddWithValue("@value_type", stats.Type);
							insertCommand.Parameters.AddWithValue("@action", stats.Action);
							insertCommand.Parameters.AddWithValue("@key", stats.Key);
							insertCommand.Parameters.AddWithValue("@value", stats.Value);
							insertCommand.Parameters.AddWithValue("@last_activity", stats.LastActivity);
							await insertCommand.ExecuteNonQueryAsync();
						}
					}
					await transaction.CommitAsync();
					return;
				}
				catch (Exception ex) {
					await transaction.RollbackAsync();
					Dev.Log(ex);
				}
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}

		public async Task RelcalcUserUsage(Guid userId) {
			for (int retry = 0; retry < 10; retry++) {
				try {
					await using var connection = await _postgres.OpenConnectionAsync();
					await using var transaction = await connection.BeginTransactionAsync();
					try {
						await using var command = new NpgsqlCommand("", connection, transaction);
						command.Parameters.AddWithValue("@user_id", userId);
						command.CommandText = @"
UPDATE mimer_key as k SET
size = COALESCE((SELECT sum(size) FROM mimer_note as n where n.key_name = k.key_name), 0),
note_count = COALESCE((SELECT count(*) FROM mimer_note as n where n.key_name = k.key_name), 0)
WHERE k.user_id = @user_id
";
						command.ExecuteNonQuery();
						command.CommandText = @"
update user_stats as s SET
size = COALESCE((SELECT sum(size) FROM mimer_key as k where k.user_id = s.user_id), 0),
notes = COALESCE((SELECT sum(note_count) FROM mimer_key as k where k.user_id = s.user_id), 0)
WHERE s.user_id = @user_id
";
						command.ExecuteNonQuery();
						await transaction.CommitAsync();
						return;
					}
					catch (Exception ex) {
						await transaction.RollbackAsync();
						Dev.Log(ex);
					}
				}
				catch (Exception ex) {
					Dev.Log(ex);
				}
			}
		}

		public async Task RelcalcAllUsage() {
			for (int retry = 0; retry < 10; retry++) {
				try {
					await using var connection = await _postgres.OpenConnectionAsync();
					await using var transaction = await connection.BeginTransactionAsync();
					try {
						await using var command = new NpgsqlCommand("", connection, transaction);
						command.CommandText = @"
UPDATE mimer_key as k SET
size = COALESCE((SELECT sum(size) FROM mimer_note as n where n.key_name = k.key_name), 0),
note_count = COALESCE((SELECT count(*) FROM mimer_note as n where n.key_name = k.key_name), 0)
";
						command.ExecuteNonQuery();
						command.CommandText = @"
update user_stats as s SET
size = COALESCE((SELECT sum(size) FROM mimer_key as k where k.user_id = s.user_id), 0),
notes = COALESCE((SELECT sum(note_count) FROM mimer_key as k where k.user_id = s.user_id), 0)
";
						command.ExecuteNonQuery();
						await transaction.CommitAsync();
						return;
					}
					catch (Exception ex) {
						await transaction.RollbackAsync();
						Dev.Log(ex);
					}
				}
				catch (Exception ex) {
					Dev.Log(ex);
				}
			}
		}
	}
}
