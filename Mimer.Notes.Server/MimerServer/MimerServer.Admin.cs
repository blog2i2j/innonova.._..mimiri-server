using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	public partial class MimerServer {

		public async Task<BasicResponse?> PromoteUserToAdmin(PromoteUserRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var requestingUser = await _dataSource.GetUser(request.Username);
			if (requestingUser != null) {
				var signer = new CryptSignature(requestingUser.AsymmetricAlgorithm, requestingUser.PublicKey);
				if (signer.VerifySignature("user", request)) {
					if (!await _dataSource.IsUserAdmin(requestingUser.Id)) {
						return null;
					}

					var targetUser = await _dataSource.GetUser(request.TargetUsername);
					if (targetUser != null) {
						if (await _dataSource.SetUserRole(targetUser.Id, UserRole.Admin)) {
							return new BasicResponse();
						}
					}
				}
			}
			return null;
		}
	}
}
