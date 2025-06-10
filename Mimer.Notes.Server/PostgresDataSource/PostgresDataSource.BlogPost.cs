using System;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		// Database creation methods
		private void CreateBlogTables() {
			using var command = _postgres.CreateCommand();
			command.CommandText = """
				CREATE TABLE IF NOT EXISTS public."blog_post" (
				  id uuid NOT NULL PRIMARY KEY,
				  title character varying(50) NOT NULL,
				  content text NOT NULL,
				  published boolean NOT NULL DEFAULT false,
				  created timestamp without time zone NOT NULL DEFAULT current_timestamp
				);
				""";
			command.ExecuteNonQuery();
		}

		public async Task<bool> AddBlogPost(BlogPost blogPost) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"INSERT INTO blog_post (id, title, published, content) VALUES (@id, @title, @published, @content)";
				command.Parameters.AddWithValue("@id", blogPost.Id);
				command.Parameters.AddWithValue("@title", blogPost.Title);
				command.Parameters.AddWithValue("@published", false); // Always start as unpublished for security
				command.Parameters.AddWithValue("@content", blogPost.Content);
				await command.ExecuteNonQueryAsync();
				return true;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return false;
			}
		}

		public async Task<bool> PublishBlogPost(Guid id) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"UPDATE blog_post SET published = true WHERE id = @id";
				command.Parameters.AddWithValue("@id", id);
				var rowsAffected = await command.ExecuteNonQueryAsync();
				return rowsAffected > 0;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return false;
			}
		}

		public async Task<List<BlogPost>> GetLatestBlogPosts(int count) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"
					SELECT id, title, content, published, created
					FROM blog_post
					WHERE published = true
					ORDER BY created DESC
					LIMIT @count";
				command.Parameters.AddWithValue("@count", count);
				using var reader = await command.ExecuteReaderAsync();
				var posts = new List<BlogPost>();
				while (await reader.ReadAsync()) {
					posts.Add(new BlogPost {
						Id = reader.GetGuid(0),
						Title = reader.GetString(1),
						Content = reader.GetString(2),
						Published = reader.GetBoolean(3),
						Created = reader.GetDateTime(4)
					});
				}
				return posts;
			}
			catch (Exception ex) {
				Dev.Log(_connectionString, ex);
				return new List<BlogPost>();
			}
		}

	}
}
