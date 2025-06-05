using Mimer.Framework;
using Mimer.Framework.Json;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace Mimer.Notes.Server {
	public class PostgresDataSource : IMimerDataSource {
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
				using var command = _postgres.CreateCommand();
				command.CommandText =
"""
CREATE OR REPLACE FUNCTION update_modified_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.modified = current_timestamp;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TABLE IF NOT EXISTS public."user_type" (
  id bigint NOT NULL PRIMARY KEY,
  name character varying(50) NOT NULL,
  max_total_bytes bigint NOT NULL,
  max_note_bytes bigint NOT NULL,
  max_note_count bigint NOT NULL,
	max_history_entries bigint NOT NULL,
  CONSTRAINT "user_type_name" UNIQUE (name)
);

CREATE TABLE IF NOT EXISTS public."mimer_user" (
  id uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
  username character varying(50) NOT NULL,
  username_upper character varying(50) COLLATE pg_catalog."default" GENERATED ALWAYS AS (upper((username)::text)) STORED,
	user_type bigint NOT NULL DEFAULT 1,
  data text NOT NULL,
	server_config text NOT NULL DEFAULT '{}',
	client_config text NOT NULL DEFAULT '{}',
	created timestamp without time zone NOT NULL DEFAULT current_timestamp,
	modified timestamp without time zone NOT NULL DEFAULT current_timestamp,
  CONSTRAINT "mimer_user_username" UNIQUE (username),
  CONSTRAINT "mimer_user_username_upper" UNIQUE (username_upper)
);

DO
$$BEGIN
CREATE TRIGGER update_mimer_user_modified BEFORE UPDATE ON public."mimer_user" FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
EXCEPTION
   WHEN duplicate_object THEN
      NULL;
END;$$;

CREATE TABLE IF NOT EXISTS public."mimer_key" (
  id uuid NOT NULL PRIMARY KEY,
	user_id uuid NOT NULL,
	key_name uuid NOT NULL,
  data text NOT NULL,
	size bigint NOT NULL,
	created timestamp without time zone NOT NULL DEFAULT current_timestamp,
	modified timestamp without time zone NOT NULL DEFAULT current_timestamp
);

DO
$$BEGIN
CREATE TRIGGER update_mimer_key_modified BEFORE UPDATE ON public."mimer_key" FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
EXCEPTION
   WHEN duplicate_object THEN
      NULL;
END;$$;

CREATE TABLE IF NOT EXISTS public."mimer_note" (
  id uuid NOT NULL PRIMARY KEY,
	key_name uuid NOT NULL,
	size bigint NOT NULL,
	created timestamp without time zone NOT NULL DEFAULT current_timestamp,
	modified timestamp without time zone NOT NULL DEFAULT current_timestamp
);

DO
$$BEGIN
CREATE TRIGGER update_mimer_note_modified BEFORE UPDATE ON public."mimer_note"  FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
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

DO
$$BEGIN
CREATE TRIGGER update_mimer_note_item_modified BEFORE UPDATE ON public."mimer_note_item"  FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
EXCEPTION
   WHEN duplicate_object THEN
      NULL;
END;$$;

CREATE TABLE IF NOT EXISTS public."note_share_offer" (
  id uuid NOT NULL PRIMARY KEY,
  sender uuid NOT NULL,
  recipient uuid NOT NULL,
  code character varying(10),
	key_name uuid NOT NULL,
  data text NOT NULL,
	created timestamp without time zone NOT NULL DEFAULT current_timestamp,
	CONSTRAINT "note_share_offer_sender_recipient_key_name" UNIQUE (sender, recipient, key_name),
	CONSTRAINT "note_share_offer_recipient_code" UNIQUE (recipient, code)
);

CREATE TABLE IF NOT EXISTS public."user_stats" (
  user_id uuid NOT NULL PRIMARY KEY,
	created timestamp without time zone NOT NULL DEFAULT current_timestamp,
	last_activity timestamp without time zone NOT NULL DEFAULT current_timestamp,
	size bigint NOT NULL DEFAULT 0,
	logins bigint NOT NULL DEFAULT 0,
	reads bigint NOT NULL DEFAULT 0,
	writes bigint NOT NULL DEFAULT 0,
	read_bytes bigint NOT NULL DEFAULT 0,
	write_bytes bigint NOT NULL DEFAULT 0,
	creates bigint NOT NULL DEFAULT 0,
	deletes bigint NOT NULL DEFAULT 0,
	notifications bigint NOT NULL DEFAULT 0
);

CREATE OR REPLACE FUNCTION update_note_size()
RETURNS TRIGGER AS $$
BEGIN
 	IF (TG_OP = 'DELETE') THEN
		UPDATE mimer_note SET size = size - OLD.size WHERE id = OLD.note_id;
	ELSIF (TG_OP = 'UPDATE') THEN
		IF (NEW.size <> OLD.size) THEN
			UPDATE mimer_note SET size = size + NEW.size - OLD.size WHERE id = NEW.note_id;
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

CREATE OR REPLACE FUNCTION update_key_size()
RETURNS TRIGGER AS $$
BEGIN
 	IF (TG_OP = 'DELETE') THEN
		UPDATE mimer_key SET size = size - OLD.size, note_count = note_count - 1 WHERE key_name = OLD.key_name;
	ELSIF (TG_OP = 'UPDATE') THEN
		IF (NEW.key_name <> OLD.key_name) THEN
			UPDATE mimer_key SET size = size - OLD.size WHERE key_name = OLD.key_name;
			UPDATE mimer_key SET size = size + NEW.size WHERE key_name = NEW.key_name;
		ELSIF (NEW.size <> OLD.size) THEN
			UPDATE mimer_key SET size = size + NEW.size - OLD.size WHERE key_name = OLD.key_name;
		END IF;
	ELSIF (TG_OP = 'INSERT') THEN
		UPDATE mimer_key SET size = size + NEW.size, note_count = note_count + 1 WHERE key_name = NEW.key_name;
	END IF;
    RETURN NULL;
END;
$$ language 'plpgsql';

DO
$$BEGIN
CREATE TRIGGER update_note_size
AFTER INSERT OR UPDATE OR DELETE ON mimer_note
    FOR EACH ROW EXECUTE FUNCTION update_key_size();
EXCEPTION
   WHEN duplicate_object THEN
      NULL;
END;$$;

CREATE OR REPLACE FUNCTION update_user_stats_size()
RETURNS TRIGGER AS $$
BEGIN
 	IF (TG_OP = 'DELETE') THEN
		UPDATE user_stats SET size = size - OLD.size, notes = notes - OLD.note_count WHERE user_id = OLD.user_id;
	ELSIF (TG_OP = 'UPDATE') THEN
		IF (NEW.size <> OLD.size OR NEW.note_count <> OLD.note_count) THEN
			UPDATE user_stats SET size = size + NEW.size - OLD.size, notes = notes + NEW.note_count - OLD.note_count WHERE user_id = NEW.user_id;
		END IF;
	ELSIF (TG_OP = 'INSERT') THEN
		UPDATE user_stats SET size = size + NEW.size WHERE user_id = NEW.user_id;
	END IF;
    RETURN NULL;
END;
$$ language 'plpgsql';

DO
$$BEGIN
CREATE TRIGGER update_key_size
AFTER INSERT OR UPDATE OR DELETE ON mimer_key
    FOR EACH ROW EXECUTE FUNCTION update_user_stats_size();
EXCEPTION
   WHEN duplicate_object THEN
      NULL;
END;$$;

CREATE TABLE IF NOT EXISTS public."global_stats" (
  id character(32) NOT NULL PRIMARY KEY,
  value_type character varying(30) NOT NULL,
	action character varying(30) NOT NULL,
	key character varying(250) NOT NULL,
	value bigint NOT NULL,
	last_activity timestamp without time zone NOT NULL,
	created timestamp without time zone NOT NULL DEFAULT current_timestamp
);
""";
				command.ExecuteNonQuery();
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}

		private byte[] ReadToEnd(Stream stream) {
			using (MemoryStream OStream = new MemoryStream()) {
				byte[] OBuffer = new byte[25600];
				while (true) {
					int ORead = stream.Read(OBuffer, 0, OBuffer.Length);
					if (ORead == 0) {
						return OStream.ToArray();
					}
					OStream.Write(OBuffer, 0, ORead);
				}
			}
		}

		private string EncryptUser(MimerUser user) {
			var aes = Aes.Create();
			aes.KeySize = 256;
			aes.Key = _userEncryptionKey;
			aes.GenerateIV();
			byte[] data = Encoding.UTF8.GetBytes(user.ToJsonString());
			JsonObject json = new JsonObject();
			json.String("iv", Convert.ToBase64String(aes.IV));

			using (ICryptoTransform transform = aes.CreateEncryptor()) {
				using (MemoryStream stream = new MemoryStream()) {
					using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write)) {
						cryptoStream.Write(data, 0, data.Length);
					}
					json.String("data", Convert.ToBase64String(stream.ToArray()));
				}
			}
			return json.ToString();
		}

		private MimerUser DecryptUser(string data, Guid stableId, long size, long noteCount, long typeId, string serverConfig, string clientConfig) {
			var json = new JsonObject(data);
			if (json.Has("publicKey")) {
				return new MimerUser(json, stableId, size, noteCount, typeId, serverConfig, clientConfig);
			}
			var aes = Aes.Create();
			aes.KeySize = 256;
			aes.Key = _userEncryptionKey;
			aes.IV = Convert.FromBase64String(json.String("iv"));
			var cipherText = Convert.FromBase64String(json.String("data"));
			using (ICryptoTransform transform = aes.CreateDecryptor()) {
				using (MemoryStream stream = new MemoryStream(cipherText)) {
					using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read)) {
						return new MimerUser(new JsonObject(Encoding.UTF8.GetString(ReadToEnd(cryptoStream))), stableId, size, noteCount, typeId, serverConfig, clientConfig);
					}
				}
			}
		}

		public List<UserType> GetUserTypes() {
			try {
				var result = new List<UserType>();
				using var command = _postgres.CreateCommand();
				command.CommandText = "SELECT id, name, max_total_bytes, max_note_bytes, max_note_count, max_history_entries FROM user_type";
				var reader = command.ExecuteReader();
				while (reader.Read()) {
					result.Add(new UserType(reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5)));
				}
				return result;
			}
			catch (Exception ex) {
				Dev.Log(ex);
				throw;
			}
		}

		public async Task<bool> CreateUser(MimerUser user) {
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();

				try {
					await using var command = new NpgsqlCommand("", connection, transaction);
					command.CommandText = @"INSERT INTO mimer_user (username, data) VALUES (@username, @data)";
					command.Parameters.AddWithValue("@username", user.Username);
					command.Parameters.AddWithValue("@data", EncryptUser(user));
					await command.ExecuteNonQueryAsync();

					command.CommandText = @"INSERT INTO user_stats (user_id) VALUES ((SELECT id FROM mimer_user WHERE username_upper = upper(@username)))";
					await command.ExecuteNonQueryAsync();
					await transaction.CommitAsync();
					return true;
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return false;
			}
		}

		public async Task<bool> UpdateUser(string oldUsername, MimerUser user) {
			try {
				using var command = _postgres.CreateCommand();
				if (oldUsername != user.Username) {
					command.CommandText = @"UPDATE mimer_user SET username = @username, data = @data WHERE username = @old_username";
				}
				else {
					command.CommandText = @"UPDATE mimer_user SET data = @data WHERE username = @old_username";
				}
				command.Parameters.AddWithValue("@username", user.Username);
				command.Parameters.AddWithValue("@data", EncryptUser(user));
				command.Parameters.AddWithValue("@old_username", oldUsername);
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return false;
			}
		}

		public async Task<MimerUser?> GetUser(string username) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT u.data, u.id, s.size, s.notes, u.user_type, u.server_config, u.client_config FROM mimer_user as u INNER JOIN user_stats as s ON s.user_id = u.id WHERE u.username_upper = upper(@username)";
				command.Parameters.AddWithValue("@username", username);
				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync()) {
					return DecryptUser(reader.GetString(0), reader.GetGuid(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetString(5), reader.GetString(6));
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		public async Task<bool> DeleteUser(Guid userId) {
			try {
				await using var connection = await _postgres.OpenConnectionAsync();
				await using var transaction = await connection.BeginTransactionAsync();
				int count = 0;
				try {
					await using var command = new NpgsqlCommand("", connection, transaction);

					command.CommandText = @"select k.key_name from mimer_key as k WHERE user_id = @user_id AND (SELECT count(*) FROM mimer_key as k2 WHERE k2.key_name = k.key_name) < 2";
					command.Parameters.AddWithValue("@user_id", userId);
					var keyNames = new List<Guid>();
					using (var reader = await command.ExecuteReaderAsync()) {
						while (await reader.ReadAsync()) {
							keyNames.Add(reader.GetGuid(0));
						}
					}
					foreach (var keyName in keyNames) {
						command.Parameters.Clear();
						command.Parameters.AddWithValue("@key_name", keyName);

						command.CommandText = @"DELETE FROM mimer_note_item WHERE note_id in (SELECT note.id FROM mimer_note as note WHERE key_name = @key_name)";
						count = await command.ExecuteNonQueryAsync();

						command.CommandText = @"DELETE FROM mimer_note as note WHERE key_name = @key_name";
						count = await command.ExecuteNonQueryAsync();
					}

					command.CommandText = @"DELETE FROM mimer_key WHERE user_id = @user_id";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("@user_id", userId);
					count = await command.ExecuteNonQueryAsync();

					command.CommandText = @"DELETE FROM user_stats WHERE user_id = @user_id";
					count = await command.ExecuteNonQueryAsync();

					command.CommandText = @"DELETE FROM mimer_user WHERE id = @user_id";
					count = await command.ExecuteNonQueryAsync();

					await transaction.CommitAsync();
					return true;
				}
				catch {
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return false;
		}

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

		public async Task<string?> CreateNoteShareOffer(string sender, string recipient, Guid keyName, string code, string data) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"INSERT INTO note_share_offer (id, sender, recipient, key_name, code, data) VALUES (@id, (SELECT id FROM mimer_user WHERE username_upper = upper(@sender)), (SELECT id FROM mimer_user WHERE username_upper = upper(@recipient)), @key_name, @code, @data)";
				command.Parameters.AddWithValue("@id", Guid.NewGuid());
				command.Parameters.AddWithValue("@sender", sender);
				command.Parameters.AddWithValue("@recipient", recipient);
				command.Parameters.AddWithValue("@key_name", keyName);
				command.Parameters.AddWithValue("@code", code);
				command.Parameters.AddWithValue("@data", data);
				try {
					await command.ExecuteNonQueryAsync();
				}
				catch {
					command.CommandText = "SELECT code FROM note_share_offer WHERE sender = (SELECT id FROM mimer_user WHERE username_upper = upper(@sender)) AND recipient = (SELECT id FROM mimer_user WHERE username_upper = upper(@recipient)) AND key_name = @key_name";
					using var reader = await command.ExecuteReaderAsync();
					if (await reader.ReadAsync()) {
						return reader.GetString(0);
					}
				}
				return code;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		public async Task<List<DbShareOffer>> GetShareOffers(string username) {
			var result = new List<DbShareOffer>();
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT note_share_offer.id, mimer_user.username, note_share_offer.code, note_share_offer.data FROM note_share_offer INNER JOIN mimer_user ON mimer_user.id = sender WHERE recipient = (SELECT id FROM mimer_user WHERE username_upper = upper(@username))";
				command.Parameters.AddWithValue("@username", username);
				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					result.Add(new DbShareOffer {
						Id = reader.GetGuid(0),
						Sender = reader.GetString(1),
						Code = reader.GetString(2),
						Data = reader.GetString(3)
					});
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return result;
		}

		public async Task<DbShareOffer?> GetShareOffer(string username, string code) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT note_share_offer.id, mimer_user.username, note_share_offer.code, note_share_offer.data FROM note_share_offer INNER JOIN mimer_user ON mimer_user.id = sender WHERE recipient = (SELECT id FROM mimer_user WHERE username_upper = upper(@username)) AND code = @code ORDER BY note_share_offer.created DESC";
				command.Parameters.AddWithValue("@username", username);
				command.Parameters.AddWithValue("@code", code);
				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync()) {
					return new DbShareOffer {
						Id = reader.GetGuid(0),
						Sender = reader.GetString(1),
						Code = reader.GetString(2),
						Data = reader.GetString(3)
					};
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return null;
		}

		public async Task<bool> DeleteNoteShareOffer(Guid id) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"DELETE FROM note_share_offer WHERE id = @id";
				command.Parameters.AddWithValue("@id", id);
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return false;
		}

		public async Task<List<(Guid id, string username, DateTime since)>> GetShareParticipants(Guid noteId) {
			var result = new List<(Guid id, string username, DateTime since)>();
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"select u.id, u.username, k.created from mimer_user u inner join mimer_key k on k.user_id = u.id inner join mimer_note n on k.key_name = n.key_name where n.id = @id";
				command.Parameters.AddWithValue("@id", noteId);
				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					result.Add((reader.GetGuid(0), reader.GetString(1), reader.GetDateTime(2)));
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return result;
		}

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
						command.CommandText = """
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
""";
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
					updateCommand.CommandText = """
UPDATE global_stats SET
	value = value + @value,
	last_activity = @last_activity
WHERE
	id = @id
""";

					insertCommand.CommandText = """
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
""";
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
						command.CommandText = """
UPDATE mimer_key as k SET
size = COALESCE((SELECT sum(size) FROM mimer_note as n where n.key_name = k.key_name), 0),
note_count = COALESCE((SELECT count(*) FROM mimer_note as n where n.key_name = k.key_name), 0)
WHERE k.user_id = @user_id
""";
						command.ExecuteNonQuery();
						command.CommandText = """
update user_stats as s SET
size = COALESCE((SELECT sum(size) FROM mimer_key as k where k.user_id = s.user_id), 0),
notes = COALESCE((SELECT sum(note_count) FROM mimer_key as k where k.user_id = s.user_id), 0)
WHERE s.user_id = @user_id
""";
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
						command.CommandText = """
UPDATE mimer_key as k SET
size = COALESCE((SELECT sum(size) FROM mimer_note as n where n.key_name = k.key_name), 0),
note_count = COALESCE((SELECT count(*) FROM mimer_note as n where n.key_name = k.key_name), 0)
""";
						command.ExecuteNonQuery();
						command.CommandText = """
update user_stats as s SET
size = COALESCE((SELECT sum(size) FROM mimer_key as k where k.user_id = s.user_id), 0),
notes = COALESCE((SELECT sum(note_count) FROM mimer_key as k where k.user_id = s.user_id), 0)
""";
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
