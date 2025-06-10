using Mimer.Framework;
using Mimer.Framework.Json;
using System.Security.Cryptography;
using System.Text;

namespace Mimer.Notes.Server {
	/// <summary>
	/// Utility and helper methods for MimerServer
	/// </summary>
	public partial class MimerServer {

		private bool ValidatePoW(string username, string powData, int bitsExpected) {
			var pow = powData.Split("::");
			var hash = pow[0];
			var data = pow[1];
			var dataAry = data.Split(':');
			var timestamp = dataAry[0];
			var value = dataAry[3];

			if (value != username) {
				return false;
			}

			if (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(timestamp)) > TimeSpan.FromMinutes(100)) {
				return false;
			}

			if (hash != BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower()) {
				return false;
			}

			ulong hashULong = ulong.Parse(hash.Substring(0, 16), System.Globalization.NumberStyles.HexNumber);
			if ((hashULong & (0xFFFFFFFFFFFFFFFF << (64 - bitsExpected))) != 0) {
				return false;
			}
			return true;
		}

		private bool IsValidUserName(string name) {
			if (_invalidChars.IsMatch(name)) {
				return false;
			}
			string upperName = name.Trim().ToUpper();
			if (_anonUserPattern.IsMatch(upperName)) {
				return true;
			}
			if (upperName.StartsWith("INNONOVA")) {
				return false;
			}
			if (InvalidUsernames.Any(entry => {
				string entryUpper = entry.ToUpper();
				if (entryUpper == upperName) {
					return true;
				}
				if (upperName.StartsWith(entryUpper + "_")) {
					return true;
				}
				if (upperName.StartsWith(entryUpper)) {
					if (char.IsWhiteSpace(upperName[entryUpper.Length])) {
						return true;
					}
				}
				return false;
			})) {
				return false;
			}
			return true;
		}

		public JsonObject DecryptRequest(JsonObject json) {
			try {
				return new JsonObject(_signature.Decrypt(json.String("data")));
			}
			catch (Exception ex) {
				Dev.Log(ex);
				throw;
			}
		}
	}
}
