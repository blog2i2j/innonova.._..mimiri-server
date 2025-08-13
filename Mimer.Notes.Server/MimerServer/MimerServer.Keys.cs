using Mimer.Framework;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using Mimer.Notes.Model.Requests;
using Mimer.Notes.Model.Responses;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Key management operations for MimerServer
	/// </summary>
	public partial class MimerServer {

		public async Task<CreateKeyResponse?> CreateKey(CreateKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				var keySigner = new CryptSignature(user.AsymmetricAlgorithm, request.PublicKey);
				if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
					var existingKeyWithName = await _dataSource.GetKeyByName(request.Name);
					if (existingKeyWithName != null) {
						var existingKeySigner = new CryptSignature(user.AsymmetricAlgorithm, existingKeyWithName.PublicKey);
						if (!existingKeySigner.VerifySignature("key", request)) {
							// If you are creating a key with the same name (sharing) you must prove that you know the contents of the key to prevent denial of service
							return null;
						}
					}
					var key = new MimerKey();
					key.Id = request.Id;
					key.UserId = user.Id;
					key.Name = request.Name;
					key.Algorithm = request.Algorithm;
					key.AsymmetricAlgorithm = user.AsymmetricAlgorithm;
					key.PublicKey = request.PublicKey;
					key.PrivateKey = request.PrivateKey;
					key.KeyData = request.KeyData;
					key.Metadata = request.Metadata;

					var response = new CreateKeyResponse();
					var userType = GetUserType(user.TypeId);
					try {
						if (await _dataSource.CreateKey(key, user.Id, (userType.MaxNoteCount, userType.MaxTotalBytes, userType.MaxNoteBytes))) {
							response.Success = true;
						}
						else {
							return null;
						}
					}
					catch (LimitException ex) {
						response.Success = false;
						response.MaxCount = ex.Limits.MaxCount;
						response.MaxSize = ex.Limits.MaxSize;
						response.Count = ex.Limits.Count;
						response.Size = ex.Limits.Size;
					}
					return response;
				}
			}
			return null;
		}

		public async Task<AllKeysResponse?> ReadAllKeys(BasicRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var response = new AllKeysResponse();
					var keys = await _dataSource.GetAllKeys(user.Id);
					foreach (var key in keys) {
						var info = new KeyInfo();
						info.Id = key.Id;
						info.Name = key.Name;
						info.Algorithm = key.Algorithm;
						info.KeyData = key.KeyData;
						info.AsymmetricAlgorithm = key.AsymmetricAlgorithm;
						info.PublicKey = key.PublicKey;
						info.PrivateKey = key.PrivateKey;
						info.Metadata = key.Metadata;
						response.AddKeyInfo(info);
					}
					return response;
				}
			}
			return null;
		}

		public async Task<KeyResponse?> ReadKey(ReadKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var response = new KeyResponse();
					var key = await _dataSource.GetKey(request.Id, user.Id);
					if (key != null) {
						response.Id = key.Id;
						response.Name = key.Name;
						response.Algorithm = key.Algorithm;
						response.KeyData = key.KeyData;
						response.AsymmetricAlgorithm = key.AsymmetricAlgorithm;
						response.PublicKey = key.PublicKey;
						response.PrivateKey = key.PrivateKey;
						response.Metadata = key.Metadata;
						return response;
					}
				}
			}
			return null;
		}

		public async Task<BasicResponse?> DeleteKey(DeleteKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var key = await _dataSource.GetKey(request.Id, user.Id);
				if (key != null) {
					var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
					var keySigner = new CryptSignature(key.AsymmetricAlgorithm, key.PublicKey);
					if (signer.VerifySignature("user", request) && keySigner.VerifySignature("key", request)) {
						if (await _dataSource.DeleteKey(request.Id)) {
							return new BasicResponse();
						}
					}
				}
			}
			return null;
		}

		public async Task<PublicKeyResponse?> GetPublicKey(PublicKeyRequest request) {
			if (!_requestValidator.ValidateRequest(request)) {
				return null;
			}
			var response = new PublicKeyResponse();
			response.BitsExpected = 15;
			response.ProofAccepted = true;
			if (request.HasPow) { // TODO remove check when clients are updated
				response.ProofAccepted = ValidatePoW(request.KeyOwnerName, request.Pow, response.BitsExpected);
				if (!response.ProofAccepted) {
					return response;
				}
			}
			var user = await _dataSource.GetUser(request.Username);
			if (user != null) {
				var signer = new CryptSignature(user.AsymmetricAlgorithm, user.PublicKey);
				if (signer.VerifySignature("user", request)) {
					var targetUser = await _dataSource.GetUser(request.KeyOwnerName);
					if (targetUser != null) {
						response.AsymmetricAlgorithm = targetUser.AsymmetricAlgorithm;
						response.PublicKey = targetUser.PublicKey;
						return response;
					}
				}
			}
			return null;
		}



	}



}
