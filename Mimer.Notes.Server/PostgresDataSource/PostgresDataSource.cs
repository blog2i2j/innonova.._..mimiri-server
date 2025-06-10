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

CREATE TABLE IF NOT EXISTS public."blog_post" (
  id uuid NOT NULL PRIMARY KEY,
  title character varying(50) NOT NULL,
  file_name character varying(50) NOT NULL,
	published boolean NOT NULL DEFAULT false,
  created timestamp without time zone NOT NULL DEFAULT current_timestamp
);

CREATE TABLE IF NOT EXISTS public."comment" (
  id uuid NOT NULL PRIMARY KEY,
	post_id uuid NOT NULL,
	user_id uuid NOT NULL,
	username character varying(50) NOT NULL,
	comment text NOT NULL,
	moderation_state character varying(20) NOT NULL DEFAULT 'pending',
	created timestamp without time zone NOT NULL DEFAULT current_timestamp,
	modified timestamp without time zone NOT NULL DEFAULT current_timestamp
);

CREATE INDEX IF NOT EXISTS idx_comment_post_id_moderation_state ON public."comment" (post_id, moderation_state);

DO
$$BEGIN
CREATE TRIGGER update_comment_modified BEFORE UPDATE ON public."comment"  FOR EACH ROW EXECUTE PROCEDURE update_modified_column();
EXCEPTION
   WHEN duplicate_object THEN
      NULL;
END;$$;
""";
				command.ExecuteNonQuery();
			}
			catch (Exception ex) {
				Dev.Log(ex);
			}
		}

	}
}
