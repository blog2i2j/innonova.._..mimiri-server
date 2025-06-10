using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Sharing operations for MimerServer
	/// </summary>
	public partial class MimerServer {

		public async Task<ShareResponse?> ShareNote(ShareNoteRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var key = await _dataSource.GetKeyByName(request.KeyName);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						string? code = await _dataSource.CreateNoteShareOffer(user.Id, request.Recipient, request.KeyName, request.Data);
						if (code != null) {
							var response = new ShareResponse();
							response.Code = code;
							return response;
						}
					}
				}
			}
			return null;
		}

		public async Task<ShareOffersResponse?> ReadShareOffers(BasicRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var offers = await _dataSource.GetShareOffers(user.Username);
					var response = new ShareOffersResponse();
					foreach (var offer in offers) {
						response.AddOffer(offer.Id, offer.Sender, offer.Data);
					}
					return response;
				}
			}
			return null;
		}

		public async Task<ShareOffersResponse?> GetShareOffer(ShareOfferRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var offer = await _dataSource.GetShareOffer(user.Username, request.Code);
					if (offer != null) {
						var response = new ShareOffersResponse();
						response.AddOffer(offer.Id, offer.Sender, offer.Data);
						return response;
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteShare(DeleteShareRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					if (await _dataSource.DeleteNoteShareOffer(request.Id)) {
						return new BasicResponse();
					}
				}
			}
			return null;
		}

		public async Task<ShareParticipantsResponse?> GetSharedWith(ShareParticipantsRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var participants = await _dataSource.GetShareParticipants(request.Id);
					if (participants.Any(item => item.id == user.Id)) {
						var result = new ShareParticipantsResponse();
						foreach (var item in participants) {
							result.AddParticipant(item.username, item.since);
						}
						return result;
					}
				}
			}
			return null;
		}
	}
}
