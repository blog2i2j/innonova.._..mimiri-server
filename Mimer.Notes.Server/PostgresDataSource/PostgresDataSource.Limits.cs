using System;
using Mimer.Framework;
using Npgsql;

namespace Mimer.Notes.Server {

	public class LimitException : Exception {
		public (long MaxCount, long MaxSize, long Count, long Size) Limits { get; }
		public LimitException(string message, (long MaxCount, long MaxSize, long Count, long Size) limits) : base(message) {
			Limits = limits;
		}
		public LimitException(string message, Exception innerException, (long MaxCount, long MaxSize, long Count, long Size) limits) : base(message, innerException) {
			Limits = limits;
		}
	}

	public partial class PostgresDataSource {

		private async Task<(bool Success, long MaxCount, long MaxSize, long Count, long Size)> CheckLimits(Guid userId, (long MaxNoteCount, long MaxTotalBytes, long MaxNoteSize) stats, NpgsqlCommand command) {
			command.CommandText = @"SELECT size, notes FROM  user_stats WHERE user_id = @user_id";
			command.Parameters.AddWithValue("@user_id", userId);
			using (var reader = await command.ExecuteReaderAsync()) {
				if (await reader.ReadAsync()) {
					var size = reader.GetInt64(0);
					var noteCount = reader.GetInt64(1);
					if (size > stats.MaxTotalBytes || noteCount - SYSTEM_NOTE_COUNT > stats.MaxNoteCount) {
						Dev.Log($"User {userId} exceeded storage limits: size={size}, noteCount={noteCount}, maxSize={stats.MaxTotalBytes}, maxNotes={stats.MaxNoteCount}");
						return (false, stats.MaxNoteCount, stats.MaxTotalBytes, noteCount, size);
					}
					return (true, stats.MaxNoteCount, stats.MaxTotalBytes, noteCount, size);
				}
			}
			return (true, stats.MaxNoteCount, stats.MaxTotalBytes, 0, 0);
		}

	}
}