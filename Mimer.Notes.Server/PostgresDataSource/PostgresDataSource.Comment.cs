using System;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		// Database creation methods
		private void CreateCommentTables() {
			using var command = _postgres.CreateCommand();
			command.CommandText = """
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

		// Comment-related methods
		public async Task<bool> AddComment(Comment comment, Guid userId) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"INSERT INTO comment (id, post_id, user_id, username, comment) VALUES (@id, @post_id, @user_id, @username, @comment)";
				command.Parameters.AddWithValue("@id", comment.Id);
				command.Parameters.AddWithValue("@post_id", comment.PostId);
				command.Parameters.AddWithValue("@user_id", userId);
				command.Parameters.AddWithValue("@username", comment.Username);
				command.Parameters.AddWithValue("@comment", comment.CommentText);
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return false;
			}
		}

		public async Task<List<Comment>> GetCommentsByPostId(Guid postId) {
			var comments = new List<Comment>();
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT id, post_id, user_id, username, comment, moderation_state, created, modified FROM comment WHERE post_id = @post_id AND moderation_state != 'blocked' ORDER BY created ASC";
				command.Parameters.AddWithValue("@post_id", postId);
				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					comments.Add(new Comment {
						Id = reader.GetGuid(0),
						PostId = reader.GetGuid(1),
						Username = reader.GetString(3),
						CommentText = reader.GetString(4),
						Created = reader.GetDateTime(6),
						Modified = reader.GetDateTime(7)
					});
				}
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
			}
			return comments;
		}
	}
}
