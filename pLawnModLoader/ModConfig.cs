using System.IO;
using System.Text.Json;

namespace pLawnModLoader
{
    public static class ModConfig
    {
        private static readonly string ConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "pLMods", "config", "pLawnModLoaderConfig.json");
        private static JsonDocument? _configDocument;
        private static readonly object _lock = new object();

        static ModConfig()
        {
            string configDir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            LoadConfig();
        }

        private static void LoadConfig()
        {
            lock (_lock)
            {
                if (File.Exists(ConfigPath))
                {
                    try
                    {
                        string json = File.ReadAllText(ConfigPath);
                        _configDocument = JsonDocument.Parse(json);
                    }
                    catch
                    {
                        _configDocument = null;
                    }
                }
                else
                {
                    _configDocument = JsonDocument.Parse("{}");
                    SaveDefaultConfig();
                }
            }
        }

        private static void SaveDefaultConfig()
        {
            File.WriteAllText(ConfigPath, "{}");
        }

        public static T? GetConfig<T>(string modName) where T : class, new()
        {
            lock (_lock)
            {
                if (_configDocument == null) return null;
                if (_configDocument.RootElement.TryGetProperty(modName, out var modElement))
                {
                    return JsonSerializer.Deserialize<T>(modElement.GetRawText());
                }
                else
                {
                    var defaultConfig = new T();
                    SetConfig(modName, defaultConfig);
                    return defaultConfig;
                }
            }
        }

        public static void SetConfig<T>(string modName, T config)
        {
            lock (_lock)
            {
                Dictionary<string, object>? root;
                if (_configDocument != null)
                {
                    var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(_configDocument.RootElement.GetRawText());
                    root = rootDict ?? new Dictionary<string, object>();
                }
                else
                {
                    root = new Dictionary<string, object>();
                }

                root[modName] = config!;

                string newJson = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, newJson);
                LoadConfig();
            }
        }
    }
}