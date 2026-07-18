using System;
using System.IO;
using System.Reflection;

namespace pLawnModLoader_Shared
{
    public static class Log
    {
        private static readonly object _lock = new object();
        private static readonly string LogFilePath;
        private static StreamWriter? _writer;
        private static bool _initialized = false;
        public static string FilePath => LogFilePath;

        static Log()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(baseDir, Constants.ModLoaderFolder, Constants.LogFolder);
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string fileName = $"log_{timestamp}.txt";
            LogFilePath = Path.Combine(logDirectory, fileName);

            try
            {
                _writer = new StreamWriter(LogFilePath, append: true) { AutoFlush = true };
                _initialized = true;

                AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
                TrySubscribeWpfExit();

                WriteRaw($"=== pLawnModLoader Log Started at {DateTime.Now} ===", writeToConsole: true, writeToFile: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法初始化日志文件: {ex.Message}");
            }
        }

        private static void TrySubscribeWpfExit()
        {
            try
            {
                Type? appType = Type.GetType("System.Windows.Application, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                if (appType == null)
                    appType = Type.GetType("System.Windows.Application, PresentationFramework");
                if (appType == null)
                    return;

                PropertyInfo? currentProp = appType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
                if (currentProp == null)
                    return;

                object? currentApp = currentProp.GetValue(null);
                if (currentApp == null)
                    return;

                EventInfo? exitEvent = appType.GetEvent("Exit");
                if (exitEvent == null)
                    return;

                Delegate handler = Delegate.CreateDelegate(exitEvent.EventHandlerType, typeof(Log).GetMethod(nameof(Shutdown), BindingFlags.Static | BindingFlags.Public));
                exitEvent.AddEventHandler(currentApp, handler);
            }
            catch { }
        }

        public static void Info(string message, bool writeToConsole = true, bool writeToFile = true, ConsoleColor color = ConsoleColor.Gray)
            => Write("INFO", message, writeToConsole, writeToFile, color);

        public static void Warning(string message, bool writeToConsole = true, bool writeToFile = true, ConsoleColor color = ConsoleColor.Yellow)
            => Write("WARN", message, writeToConsole, writeToFile, color);

        public static void Error(string message, bool writeToConsole = true, bool writeToFile = true, ConsoleColor color = ConsoleColor.Red)
            => Write("ERROR", message, writeToConsole, writeToFile, color);

        public static void Error(string message, Exception ex, bool writeToConsole = true, bool writeToFile = true, ConsoleColor color = ConsoleColor.Red)
            => Write("ERROR", $"{message}\n{ex}", writeToConsole, writeToFile, color);

        public static void Raw(string message, bool writeToConsole = true, bool writeToFile = true, ConsoleColor color = ConsoleColor.Gray)
        {
            if (!_initialized) return;
            WriteRaw(message, writeToConsole, writeToFile, color);
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                if (_writer != null)
                {
                    try
                    {
                        _writer.Flush();
                        _writer.Close();
                        _writer.Dispose();
                    }
                    catch { }
                    finally
                    {
                        _writer = null;
                        _initialized = false;
                    }
                }
            }
        }

        private static void Write(string level, string message, bool writeToConsole, bool writeToFile, ConsoleColor color)
        {
            if (!_initialized) return;
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logLine = $"[{timeStamp}] [{level}] {message}";
            WriteRaw(logLine, writeToConsole, writeToFile, color);
        }

        private static void WriteRaw(string message, bool writeToConsole, bool writeToFile, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (_lock)
            {
                if (writeToConsole)
                {
                    var originalColor = Console.ForegroundColor;
                    try
                    {
                        Console.ForegroundColor = color;
                        Console.WriteLine(message);
                    }
                    finally
                    {
                        Console.ForegroundColor = originalColor;
                    }
                }

                if (writeToFile)
                {
                    try { _writer?.WriteLine(message); }
                    catch { }
                }
            }
        }
    }
}