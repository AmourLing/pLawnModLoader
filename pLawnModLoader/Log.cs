using System.IO;

namespace pLawnModLoader
{
    public static class Log
    {
        private static readonly string LogFilePath;
        private static readonly object _lock = new object();

        public static string FilePath => LogFilePath;

        static Log()
        {
            string logDir = Path.Combine(Directory.GetCurrentDirectory(), "pLMods", "log");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            string fileName = $"p_{timestamp}.log";
            LogFilePath = Path.Combine(logDir, fileName);

            lock (_lock)
            {
                File.WriteAllText(LogFilePath, $"=== pLawnModLoader Log Started at {DateTime.Now} ===\n");
            }
        }

        public static void Info(string message) => WriteLine("INFO", message);
        public static void Warning(string message) => WriteLine("WARN", message);
        public static void Error(string message) => WriteLine("ERROR", message);
        public static void Error(string message, Exception ex) => WriteLine("ERROR", $"{message}\n{ex}");

        private static void WriteLine(string level, string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            Console.WriteLine(line);
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, line + "\n");
            }
        }
    }
}