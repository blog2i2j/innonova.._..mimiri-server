using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;

namespace Mimer.Notes.Server {
	public interface IMimerDataSource {
		void CreateDatabase();
		List<UserType> GetUserTypes();
		Task<bool> CreateKey(MimerKey data);
		Task<bool> CreateNote(DbNote note);
		Task<bool> CreateNoteShareOffer(string sender, string recipient, Guid keyName, string data);
		Task<bool> CreateUser(MimerUser user);
		Task<bool> DeleteKey(Guid id);
		Task<bool> DeleteNote(Guid id);
		Task<bool> DeleteNoteShareOffer(Guid id);
		Task<List<MimerKey>> GetAllKeys(Guid userId);
		Task<MimerKey?> GetKey(Guid id, Guid userId);
		Task<MimerKey?> GetKeyByName(Guid keyName);
		Task<DbNote?> GetNote(Guid id);
		Task<List<DbShareOffer>> GetShareOffers(string username);
		Task<MimerUser?> GetUser(string username);
		Task<UserSize> GetUserSize(Guid userId);
		void TearDown(bool keepLogs);
		Task<List<VersionConflict>?> UpdateNote(DbNote note, Guid oldKeyName);
		Task<bool> UpdateUser(string oldUsername, MimerUser user);
		Task<List<Guid>> GetUserIdsByKeyName(Guid keyName);
		Task<List<VersionConflict>?> MultiApply(List<NoteAction> actions, UserStats stats);
		Task UpdateUserStats(IEnumerable<UserStats> userStats);
		Task UpdateGlobalStats(IEnumerable<GlobalStatistic> globalStats);
		Task<bool> DeleteUser(Guid userId);
	}
}