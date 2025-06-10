using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	public partial class MimerServer {
		public async Task<BasicResponse?> AddBlogPost(AddBlogPostRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					if (!await _dataSource.IsUserAdmin(user.Id)) {
						return null;
					}
					var blogPost = new BlogPost {
						Id = request.Id,
						Title = request.Title,
						Content = request.Content
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
					if (!await _dataSource.IsUserAdmin(user.Id)) {
						return null;
					}
					if (await _dataSource.PublishBlogPost(request.Id)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<BlogPostsResponse?> GetLatestBlogPosts(GetBlogPostsRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					BlogPostsResponse response = new BlogPostsResponse();
					foreach (var blogPost in await _dataSource.GetLatestBlogPosts(request.Count)) {
						response.AddBlogPost(blogPost);
					}
					return response;
				}
			}
			return null;
		}
	}
}
