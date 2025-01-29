using System.Security.Principal;

namespace Mimer.Notes.SignalR.Authentication {
	public class MimerIdentity : IIdentity {
		private readonly string _name;

		public MimerIdentity(string name) {
			_name = name;
		}

		public string? AuthenticationType => "mime";
		public bool IsAuthenticated => true;
		public string? Name => _name;
	}
}
