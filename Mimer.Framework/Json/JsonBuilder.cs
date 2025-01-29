using System.Globalization;
using System.Text;

namespace Mimer.Framework.Json {
	public class JsonBuilder {
		private StringBuilder FBuilder = new StringBuilder();

		public static string EncodeStringLiteral(string str) {
			StringBuilder? OResult = null;
			char OPrevChar = ' ';
			for (int i = 0; i < str.Length; i++) {
				char OChar = str[i];
				switch (OChar) {
					case '\\':
						if (OResult == null) {
							OResult = new StringBuilder();
							if (i > 0) {
								OResult.Append(str.Substring(0, i));
							}
						}
						OResult.Append("\\\\");
						break;
					case '"':
						if (OResult == null) {
							OResult = new StringBuilder();
							if (i > 0) {
								OResult.Append(str.Substring(0, i));
							}
						}
						OResult.Append("\\\"");
						break;
					case '/':
						if (OPrevChar == '<') {
							if (OResult == null) {
								OResult = new StringBuilder();
								if (i > 0) {
									OResult.Append(str.Substring(0, i));
								}
							}
							OResult.Append("\\");
						}
						if (OResult != null) {
							OResult.Append("/");
						}
						break;
					case '\b':
						if (OResult == null) {
							OResult = new StringBuilder();
							if (i > 0) {
								OResult.Append(str.Substring(0, i));
							}
						}
						OResult.Append("\\b");
						break;
					case '\t':
						if (OResult == null) {
							OResult = new StringBuilder();
							if (i > 0) {
								OResult.Append(str.Substring(0, i));
							}
						}
						OResult.Append("\\t");
						break;
					case '\n':
						if (OResult == null) {
							OResult = new StringBuilder();
							if (i > 0) {
								OResult.Append(str.Substring(0, i));
							}
						}
						OResult.Append("\\n");
						break;
					case '\f':
						if (OResult == null) {
							OResult = new StringBuilder();
							if (i > 0) {
								OResult.Append(str.Substring(0, i));
							}
						}
						OResult.Append("\\f");
						break;
					case '\r':
						if (OResult == null) {
							OResult = new StringBuilder();
							if (i > 0) {
								OResult.Append(str.Substring(0, i));
							}
						}
						OResult.Append("\\r");
						break;
					default:
						if (OChar < ' ') {
							if (OResult == null) {
								OResult = new StringBuilder();
								if (i > 0) {
									OResult.Append(str.Substring(0, i));
								}
							}
							OResult.Append("\\u");
							OResult.Append(((int)OChar).ToString("x").PadLeft(4, '0'));
						}
						else if (OResult != null) {
							OResult.Append(OChar);
						}
						break;
				}
				OPrevChar = OChar;
			}
			if (OResult != null) {
				return OResult.ToString();
			}
			return str;
		}

		public static string DecodeStringLiteral(string str) {
			StringBuilder? OResult = null;
			for (int i = 0; i < str.Length; i++) {
				char OChar = str[i];
				if (OChar == '\\') {
					if (OResult == null) {
						OResult = new StringBuilder();
						if (i > 0) {
							OResult.Append(str.Substring(0, i));
						}
					}
					i++;
					OChar = str[i];
					switch (OChar) {
						case '\\':
							OResult.Append("\\");
							break;
						case '"':
							OResult.Append("\"");
							break;
						case '/':
							OResult.Append("/");
							break;
						case 'b':
							OResult.Append("\b");
							break;
						case 't':
							OResult.Append("\t");
							break;
						case 'n':
							OResult.Append("\n");
							break;
						case 'f':
							OResult.Append("\f");
							break;
						case 'r':
							OResult.Append("\r");
							break;
						case 'u':
							OResult.Append((char)int.Parse(str.Substring(i + 1, 4), NumberStyles.HexNumber));
							i += 4;
							break;
						default:
							OResult.Append(OChar);
							break;
					}
				}
				else if (OResult != null) {
					OResult.Append(OChar);
				}
			}
			if (OResult != null) {
				return OResult.ToString();
			}
			return str;
		}

		public void BeginObject() {
			if (FBuilder.Length > 0) {
				char OEnd = FBuilder[FBuilder.Length - 1];
				if (OEnd != '[' && OEnd != '{') {
					FBuilder.Append(",");
				}
			}
			FBuilder.Append("{");
		}

		public void BeginObject(string name) {
			char OEnd = FBuilder[FBuilder.Length - 1];
			if (OEnd != '[' && OEnd != '{') {
				FBuilder.Append(",");
			}
			FBuilder.Append("\"" + name + "\":{");
		}

		public void EndObject() {
			FBuilder.Append("}");
		}

		public void BeginArray() {
			if (FBuilder.Length > 0) {
				char OEnd = FBuilder[FBuilder.Length - 1];
				if (OEnd != '[' && OEnd != '{') {
					FBuilder.Append(",");
				}
			}
			FBuilder.Append("[");
		}

		public void BeginArray(string name) {
			char OEnd = FBuilder[FBuilder.Length - 1];
			if (OEnd != '[' && OEnd != '{') {
				FBuilder.Append(",");
			}
			FBuilder.Append("\"" + name + "\":[");
		}

		public void EndArray() {
			FBuilder.Append("]");
		}

		public void Add(string name, string value) {
			char OEnd = FBuilder[FBuilder.Length - 1];
			if (OEnd != '[' && OEnd != '{') {
				FBuilder.Append(",");
			}
			FBuilder.Append("\"");
			FBuilder.Append(name);
			FBuilder.Append("\":");
			FBuilder.Append("\"");
			FBuilder.Append(value);
			FBuilder.Append("\"");
		}

		public void Add(string value) {
			char OEnd = FBuilder[FBuilder.Length - 1];
			if (OEnd != '[' && OEnd != '{') {
				FBuilder.Append(",");
			}
			FBuilder.Append("\"");
			FBuilder.Append(value);
			FBuilder.Append("\"");
		}

		public void AddObject(string name, string value) {
			char OEnd = FBuilder[FBuilder.Length - 1];
			if (OEnd != '[' && OEnd != '{') {
				FBuilder.Append(",");
			}
			FBuilder.Append("\"");
			FBuilder.Append(name);
			FBuilder.Append("\":");
			FBuilder.Append(value);
		}

		public void AddObject(string value) {
			if (FBuilder.Length > 0) {
				char OEnd = FBuilder[FBuilder.Length - 1];
				if (OEnd != '[' && OEnd != '{') {
					FBuilder.Append(",");
				}
			}
			FBuilder.Append(value);
		}

		public void NewLine() {
			FBuilder.AppendLine();
		}

		public override string ToString() {
			return FBuilder.ToString();
		}

	}
}
