using System.Reflection;
using System.Text;

namespace Mimer.Framework {
	public class Dev {
		private static ILogListener? FListener;
		private static string FDebugPath = string.Empty;
		public static bool LogToConsole = false;

		public static void SetLogListener(ILogListener listener) {
			FListener = listener;
		}

		public static void SetDebugPath(string path) {
			FDebugPath = path;
		}

		private static string DebugPath {
			get {
				if (!string.IsNullOrEmpty(FDebugPath)) {
					return FDebugPath;
				}
				var parentDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
				return Path.Combine(parentDir!.FullName, "DebugLog.txt");
			}
		}

		public static void Log(params object?[] items) {
			try {
				StringBuilder OLogData = new StringBuilder();
				OLogData.Append(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff# "));
				bool OFirst = true;
				foreach (object? OItem in items) {
					if (!OFirst) {
						OLogData.Append(", ");
					}
					OLogData.Append(OItem);
					OFirst = false;
				}
				OLogData.AppendLine();
				if (LogToConsole) {
					Console.Write(OLogData);
				}
				if (FListener != null) {
					FListener.WriteLogData(OLogData.ToString());
				}
				else {
					using (StreamWriter OStream = new StreamWriter(DebugPath, true, Encoding.UTF8)) {
						OStream.Write(OLogData.ToString());
					}
				}
			}
			catch (Exception) {
				//Console.WriteLine(e);
			}
		}

	}

	public interface ILogListener {
		void WriteLogData(string data);
	}
}
