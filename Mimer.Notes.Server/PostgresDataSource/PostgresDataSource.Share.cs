using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mimer.Framework;
using Mimer.Notes.Model.DataTypes;
using Npgsql;

namespace Mimer.Notes.Server {
	public partial class PostgresDataSource {
		// Share offer and participant methods
		public async Task<string?> CreateNoteShareOffer(Guid senderId, string recipient, Guid keyName, string data) {
			try {
				var recipientId = Guid.Empty;
				using var command = _postgres.CreateCommand();
				command.CommandText = @"SELECT id FROM mimer_user WHERE username_upper = upper(@recipient)";
				command.Parameters.AddWithValue("@recipient", recipient);
				using (var reader = await command.ExecuteReaderAsync()) {
					if (await reader.ReadAsync()) {
						recipientId = reader.GetGuid(0);
					}
				}
				if (recipientId == Guid.Empty) {
					throw new Exception("Recipient not found");
				}

				// keep the number of share requests down to limit DoS options
				command.CommandText = @"SELECT id FROM note_share_offer WHERE recipient = @recipient_id ORDER BY created DESC";
				command.Parameters.Clear();
				command.Parameters.AddWithValue("@recipient_id", recipientId);
				List<Guid> ids = new List<Guid>();
				using (var reader = await command.ExecuteReaderAsync()) {
					int count = 0;
					while (await reader.ReadAsync()) {
						if (count++ >= 5) {
							ids.Add(reader.GetGuid(0));
						}
					}
				}
				command.CommandText = @"DELETE FROM note_share_offer WHERE id = @id";
				foreach (Guid id in ids) {
					command.Parameters.Clear();
					command.Parameters.AddWithValue("@id", id);
					await command.ExecuteNonQueryAsync();
				}

				for (int i = 0; i < 10; i++) { // if we can't do it in 10 tries something is wrong
					var code = new Random().Next(1000, 10000).ToString();
					command.CommandText = @"INSERT INTO note_share_offer (id, sender, recipient, key_name, code, data) VALUES (@id, @sender_id, @recipient_id, @key_name, @code, @data)";
					command.Parameters.Clear();
					command.Parameters.AddWithValue("@id", Guid.NewGuid());
					command.Parameters.AddWithValue("@sender_id", senderId);
					command.Parameters.AddWithValue("@recipient_id", recipientId);
					command.Parameters.AddWithValue("@key_name", keyName);
					command.Parameters.AddWithValue("@code", code);
					command.Parameters.AddWithValue("@data", data);
					try {
						await command.ExecuteNonQueryAsync();
						return code;
					}
					catch (Exception ex) {
						Dev.Log(ex);
						command.CommandText = "SELECT code FROM note_share_offer WHERE sender = @sender_id AND recipient = @recipient_id AND key_name = @key_name";
						using var reader = await command.ExecuteReaderAsync();
						if (await reader.ReadAsync()) {
							return reader.GetString(0);
						}
					}
				}
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
	}
}
