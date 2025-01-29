using System.Security.Cryptography;

namespace Mimer.Notes.Model.Cryptography {
	public class PasswordHasher {
		public static readonly PasswordHasher Instance = new PasswordHasher();

		public string HashPassword(string password, string salt, string algorithm, int iterations) {
			var algorithmParts = algorithm.Split(';');
			if (algorithmParts[0] != "PBKDF2") {
				throw new ArgumentException($"Algorithm not supported {algorithm}");
			}
			if (algorithmParts[1] != "SHA512") {
				throw new ArgumentException($"Algorithm not supported {algorithm}");
			}
			var size = int.Parse(algorithmParts[2]);
			using Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(password, Convert.FromHexString(salt), iterations, HashAlgorithmName.SHA512);
			return Convert.ToHexString(deriver.GetBytes(size));
		}

		public string ComputeResponse(string passwordHash, string challenge) {
			using var hmac = new HMACSHA512(Convert.FromHexString(passwordHash));
			return Convert.ToHexString(hmac.ComputeHash(Convert.FromHexString(challenge)));
		}

		public string CreateSalt(int size) {
			var salt = new byte[size];
			using var random = RandomNumberGenerator.Create();
			random.GetBytes(salt);
			return Convert.ToHexString(salt);
		}

	}
}
