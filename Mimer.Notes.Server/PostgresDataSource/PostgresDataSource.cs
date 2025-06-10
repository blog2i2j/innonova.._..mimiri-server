using Mimer.Framework;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		private string _connectionString;
		private NpgsqlDataSource _postgres;
		private byte[] _userEncryptionKey;

		public PostgresDataSource(string connectionString, byte[] userEncryptionKey) {
			_connectionString = connectionString;
			_postgres = NpgsqlDataSource.Create(connectionString);
			_userEncryptionKey = userEncryptionKey;
		}


		public void TearDown(bool keepLogs) { }

		public void CreateDatabase() {
			Dev.Log("CreateDatabase");
			try {
				CreateCommonFunctions();
				CreateRoleTables();
				CreateUserTables();
				CreateKeyTables();
				CreateNoteTables();
				CreateShareTables();
				CreateStatsTables();
				CreateBlogTables();
				CreateCommentTables();
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}

		private void CreateCommonFunctions() {
			using var command = _postgres.CreateCommand();
			command.CommandText = """
			CREATE OR REPLACE FUNCTION update_modified_column()
			RETURNS TRIGGER AS $$
			BEGIN
			    NEW.modified = current_timestamp;
			    RETURN NEW;
			END;
			$$ language 'plpgsql';
			""";
			command.ExecuteNonQuery();
		}

	}
}
