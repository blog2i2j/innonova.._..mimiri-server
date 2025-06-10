using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Blog operations for MimerServer
	/// </summary>
	public partial class MimerServer {

		public async Task<BasicResponse?> AddBlogPost(AddBlogPostRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var blogPost = new BlogPost {
						Id = Guid.NewGuid(),
						Title = request.Title,
						FileName = request.FileName
					};
					if (await _dataSource.AddBlogPost(blogPost)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> PublishBlogPost(PublishBlogPostRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					if (await _dataSource.PublishBlogPost(request.Id)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}
	}
}
