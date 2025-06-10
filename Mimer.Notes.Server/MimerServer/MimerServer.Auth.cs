using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;
using System.Security.Cryptography;
using System.Text;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Authentication and user management operations for MimerServer
	/// </summary>
	public partial class MimerServer {

		public async Task<PreLoginResponse> PreLogin(string username) {
			var user = await _dataSource.GetUser(username);
			if (user != null) {
				var response = new PreLoginResponse();
				response.Salt = user.PasswordSalt;
				response.Iterations = user.PasswordIterations;
				response.Algorithm = user.PasswordAlgorithm;
				response.Challenge = _challengeManager.IssueChallenge(user.Username);
				return response;
			}
			else {
				// Generate fake response to prevent scanning for usernames
				using var random = RandomNumberGenerator.Create();
				var saltBytes = new byte[32];
				var challengeBytes = new byte[32];
				random.GetBytes(saltBytes);
				random.GetBytes(challengeBytes);

				var response = new PreLoginResponse();
				response.Salt = Convert.ToHexString(saltBytes);
				response.Iterations = 1000000;
				response.Algorithm = "PBKDF2;SHA512;1024";
				response.Challenge = Convert.ToHexString(challengeBytes);
				return response;
			}
		}

		public async Task<LoginResponse?> Login(LoginRequest request) {
			if (!request.IsValid) { // challenge response ensures non repeatability here
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if ((user != null)) {
				if (_challengeManager.ValidateChallenge(user.Username, user.PasswordHash, request.Response, request.HashLength)) {
					var userType = GetUserType(user.TypeId);
					var response = new LoginResponse();
					response.UserId = user.Id;
					response.PublicKey = user.PublicKey;
					response.PrivateKey = user.PrivateKey;
					response.AsymmetricAlgorithm = user.AsymmetricAlgorithm;
					response.Salt = user.Salt;
					response.Iterations = user.Iterations;
					response.Algorithm = user.Algorithm;
					response.SymmetricAlgorithm = user.SymmetricAlgorithm;
					response.SymmetricKey = user.SymmetricKey;
					response.Data = user.Data;
					response.Config = user.ClientConfig;
					response.Size = user.Size;
					response.NoteCount = user.NoteCount;
					response.MaxTotalBytes = userType.MaxTotalBytes;
					response.MaxNoteBytes = userType.MaxNoteBytes;
					response.MaxNoteCount = userType.MaxNoteCount;
					response.MaxHistoryEntries = userType.MaxHistoryEntries;
					_userStatsManager.RegisterLogin(user.Id);
					return response;
				}
			}
			return null;
		}

		public async Task<UserDataResponse?> GetUserData(BasicRequest request) {
			if (!request.IsValid) { // challenge response ensures non repeatability here
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var userType = GetUserType(user.TypeId);
					var response = new UserDataResponse();
					response.Data = user.Data;
					response.Config = user.ClientConfig;
					response.Size = user.Size;
					response.NoteCount = user.NoteCount;
					response.MaxTotalBytes = userType.MaxTotalBytes;
					response.MaxNoteBytes = userType.MaxNoteBytes;
					response.MaxNoteCount = userType.MaxNoteCount;
					response.MaxHistoryEntries = userType.MaxHistoryEntries;
					return response;
				}
			}
			return null;
		}

		public async Task<CheckUsernameResponse?> UsernameAvailable(CheckUsernameRequest request) {
			if (!request.IsValid) { // Not worth prevent repeats here
				return null;
			}
			var response = new CheckUsernameResponse();
			response.BitsExpected = 15;
			response.Username = request.Username;
			response.Available = false;

			response.ProofAccepted = ValidatePoW(request.Username, request.Pow, response.BitsExpected);
			if (!response.ProofAccepted) {
				return response;
			}

			if (!IsValidUserName(request.Username)) {
				response.Available = false;
			}
			else {
				response.Available = null == await _dataSource.GetUser(request.Username);
			}
			return response;
		}

		public async Task<BasicResponse?> CreateUser(CreateUserRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var signer = new CryptSignature(request.AsymmetricAlgorithm, request.PublicKey);
			// Do not create user that will never work
			if (!signer.VerifySignature("user", request)) {
				return null;
			}
			if (!IsValidUserName(request.Username)) {
				return null;
			}
			var user = new MimerUser();
			user.Username = request.Username.Trim();
			user.PublicKey = request.PublicKey;
			user.PrivateKey = request.PrivateKey;
			user.AsymmetricAlgorithm = request.AsymmetricAlgorithm;
			user.Salt = request.Salt;
			user.Iterations = request.Iterations;
			user.Algorithm = request.Algorithm;
			user.PasswordSalt = request.PasswordSalt;
			user.PasswordHash = request.PasswordHash;
			user.PasswordIterations = request.PasswordIterations;
			user.PasswordAlgorithm = request.PasswordAlgorithm;
			user.SymmetricAlgorithm = request.SymmetricAlgorithm;
			user.SymmetricKey = request.SymmetricKey;
			user.Data = request.Data;
			if (await _dataSource.CreateUser(user)) {
				return new BasicResponse();
			}
			return null;
		}

		public async Task<BasicResponse?> UpdateUser(UpdateUserRequest request, ClientInfo clientInfo) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var signer = new CryptSignature(request.AsymmetricAlgorithm, request.PublicKey);
			// Do not create user that will never work
			if (!signer.VerifySignature("user", request)) {
				return null;
			}
			var oldUser = await _dataSource.GetUser(request.OldUsername);
			if (oldUser != null) {
				if (request.OldUsername != request.Username) {
					if (!IsValidUserName(request.Username)) {
						return null;
					}
				}

				// TODO remove response length 0 check at some point in the future (backwards compatability)
				if (request.Response.Length == 0 || _challengeManager.ValidateChallenge(oldUser.Username, oldUser.PasswordHash, request.Response, request.HashLength)) {
					var oldSigner = new CryptSignature(oldUser.AsymmetricAlgorithm, oldUser.PublicKey);
					if (!oldSigner.VerifySignature("old-user", request)) {
						return null;
					}
					if (_anonUserPattern.IsMatch(oldUser.Username.ToUpper())) {
						this.RegisterAction(clientInfo, "promote-account");
					}
					var user = new MimerUser();
					user.Username = request.Username;
					user.PublicKey = request.PublicKey;
					user.PrivateKey = request.PrivateKey;
					user.AsymmetricAlgorithm = request.AsymmetricAlgorithm;
					user.Salt = request.Salt;
					user.Iterations = request.Iterations;
					user.Algorithm = request.Algorithm;
					user.PasswordSalt = request.PasswordSalt;
					user.PasswordHash = request.PasswordHash;
					user.PasswordIterations = request.PasswordIterations;
					user.PasswordAlgorithm = request.PasswordAlgorithm;
					user.SymmetricAlgorithm = request.SymmetricAlgorithm;
					user.SymmetricKey = request.SymmetricKey;
					user.Data = request.Data;
					if (await _dataSource.UpdateUser(request.OldUsername, user)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> UpdateUserData(UpdateUserDataRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					user.Data = request.Data;
					if (await _dataSource.UpdateUser(user.Username, user)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteUser(DeleteAccountRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				// TODO remove response length 0 check at some point in the future (backwards compatability)
				if (request.Response.Length == 0 || _challengeManager.ValidateChallenge(user.Username, user.PasswordHash, request.Response, request.HashLength)) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					if (signer.VerifySignature("user", request)) {
						if (await _dataSource.DeleteUser(user.Id)) {
							return new BasicResponse();
						}
					}
				}
			}
			return null;
		}
	}
}
