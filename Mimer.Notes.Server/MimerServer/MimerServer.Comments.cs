using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Comment operations for MimerServer
	/// </summary>
	public partial class MimerServer {

		public async Task<BasicResponse?> AddComment(AddCommentRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var comment = new Comment {
						Id = Guid.NewGuid(),
						PostId = request.PostId,
						Username = request.DisplayName,
						CommentText = request.Comment,
					};
					await _dataSource.AddComment(comment, user.Id);
					return new BasicResponse();
				}
			}
			return null;
		}

	}
}
