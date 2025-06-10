using System;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		// Blog post-related methods
		public async Task<bool> AddBlogPost(BlogPost blogPost) {
			try {
				using var command = _postgres.CreateCommand();
				command.CommandText = @"INSERT INTO blog_post (id, title, published, file_name) VALUES (@id, @title, @published, @file_name)";
				command.Parameters.AddWithValue("@id", blogPost.Id);
				command.Parameters.AddWithValue("@title", blogPost.Title);
				command.Parameters.AddWithValue("@published", false); // Always start as unpublished for security
				command.Parameters.AddWithValue("@file_name", blogPost.FileName);
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
	}
}
