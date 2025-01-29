namespace Mimer.Framework.Json {
	public class JsonTokenizer {

		public static List<string> Tokenize(string json) {
			List<string> OTokens = new List<string>();
			for (int i = 0; i < json.Length; i++) {
				if (char.IsWhiteSpace(json[i])) {

				}
				else if (json[i] == '{') {
					OTokens.Add("{");
				}
				else if (json[i] == '}') {
					OTokens.Add("}");
				}
				else if (json[i] == '[') {
					OTokens.Add("[");
				}
				else if (json[i] == ']') {
					OTokens.Add("]");
				}
				else if (json[i] == ',') {
					OTokens.Add(",");
				}
				else if (json[i] == ':') {
					OTokens.Add(":");
				}
				else if (json[i] == '"') {
					bool OFound = false;
					for (int j = i + 1; j < json.Length; j++) {
						if (json[j] == '\\') {
							j++;
						}
						else if (json[j] == '"') {
							OFound = true;
							OTokens.Add("\"" + json.Substring(i + 1, j - i - 1));
							i = j;
							break;
						}
					}
					if (!OFound) {
						throw new Exception("Unexpected end of data looking for '\"' (double quote)");
					}
				}
				else if (json[i] == '\'') {
					bool OFound = false;
					for (int j = i + 1; j < json.Length; j++) {
						if (json[j] == '\\') {
							j++;
						}
						else if (json[j] == '\'') {
							OFound = true;
							OTokens.Add("\"" + json.Substring(i + 1, j - i - 1));
							i = j;
							break;
						}
					}
					if (!OFound) {
						throw new Exception("Unexpected end of data looking for '\'' (single quote)");
					}
				}
				else {
					bool OFound = false;
					for (int j = i + 1; j < json.Length; j++) {
						if (char.IsWhiteSpace(json[j]) || json[j] == '}' || json[j] == ']' || json[j] == ',' || json[j] == ':') {
							OFound = true;
							OTokens.Add(json.Substring(i, j - i));
							i = j - 1;
							break;
						}
					}
					if (!OFound) {
						throw new Exception("Unexpected end of data looking for '\'' (single quote)");
					}
				}
			}
			return OTokens;
		}

	}
}
