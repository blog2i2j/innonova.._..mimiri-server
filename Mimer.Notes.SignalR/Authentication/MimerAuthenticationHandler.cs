using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Mimer.Framework.Json;
using Mimer.Notes.Model.Cryptography;
using Mimer.Notes.Model.DataTypes;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Mimer.Notes.SignalR.Authentication {

	public class MimerAuthenticationSchemeOptions : AuthenticationSchemeOptions {

	}

	public class MimerAuthenticationHandler : AuthenticationHandler<MimerAuthenticationSchemeOptions> {
		private static AuthenticationTicket Anonymous = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new Claim[0], "none")), "MimerAuth");
		private static CryptSignature _signature;

		static MimerAuthenticationHandler() {
			_signature = new CryptSignature("RSA;3072", File.ReadAllText(Path.Combine(NotificationServer.CertPath!, "server.pub")));
		}

		public MimerAuthenticationHandler(
			 IOptionsMonitor<MimerAuthenticationSchemeOptions> options,
			 ILoggerFactory logger,
			 UrlEncoder encoder) : base(options, logger, encoder) {
		}

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
			await Task.Delay(1);
			string? auth = Context.Request.Headers["Authorization"];
			if (string.IsNullOrEmpty(auth)) {
				auth = Context.Request.Query["access_token"];
			}
			if (auth?.StartsWith("Bearer ") ?? false) {
				auth = auth.Substring("Bearer ".Length);
			}
			//Console.WriteLine($"HandleAuthenticateAsync {auth}");
			if (string.IsNullOrEmpty(auth)) {
				return AuthenticateResult.Success(Anonymous);
			}
			else {
				var token = new MimerNotificationToken(new JsonObject(Encoding.UTF8.GetString(Convert.FromBase64String(auth))));
				if (_signature.VerifySignature("mimer", token)) {
					//Console.WriteLine("signature verified for " + token.UserId);
					var claims = new[] {
						new Claim(ClaimTypes.Name, token.Username),
						new Claim(ClaimTypes.NameIdentifier, token.UserId),
						new Claim(ClaimTypes.Role, "User")
					};
					var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "MimerAuth"));
					var ticket = new AuthenticationTicket(principal, Scheme.Name);
					return AuthenticateResult.Success(ticket);
				}
				return AuthenticateResult.NoResult();
			}
		}
	}
}
