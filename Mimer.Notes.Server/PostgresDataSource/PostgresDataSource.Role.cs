using System;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		private void CreateRoleTables() {
			using var command = _postgres.CreateCommand();
			command.CommandText = """
				CREATE TABLE IF NOT EXISTS public."admin_users" (
				  user_id uuid NOT NULL PRIMARY KEY,
				  created timestamp without time zone NOT NULL DEFAULT current_timestamp,
				  CONSTRAINT fk_admin_user FOREIGN KEY (user_id) REFERENCES mimer_user(id) ON DELETE CASCADE
				);
				""";
			command.ExecuteNonQuery();
		}

		public async Task<UserRole> GetUserRole(Guid userId) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT COUNT(*) FROM admin_users WHERE user_id = @user_id";
				command.Parameters.AddWithValue("@user_id", userId);
				var result = await command.ExecuteScalarAsync();
				var count = result != null ? (long)result : 0L;
				return count > 0 ? UserRole.Admin : UserRole.User;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return UserRole.User;
			}
		}

		public async Task<bool> SetUserRole(Guid userId, UserRole role) {
			try {
				using var command = _postgres.CreateCommand();

				if (role == UserRole.Admin) {
					command.CommandText = @"INSERT INTO admin_users (user_id) VALUES (@user_id) ON CONFLICT (user_id) DO NOTHING";
					command.Parameters.AddWithValue("@user_id", userId);
				}
				else {
					command.CommandText = @"DELETE FROM admin_users WHERE user_id = @user_id";
					command.Parameters.AddWithValue("@user_id", userId);
				}

				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return false;
			}
		}

		public async Task<bool> IsUserAdmin(Guid userId) {
			var role = await GetUserRole(userId);
			return role == UserRole.Admin;
		}
	}
}
