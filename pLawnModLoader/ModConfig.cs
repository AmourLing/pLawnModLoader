using System.IO;
using System.Text.Json;

namespace pLawnModLoader_Shared
{
    public static class ModConfig
    {
        public static T GetConfig<T>(string modName) where T : class, new()
        {
            string configPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                Constants.ModLoaderFolder,
                Constants.ModsFolder,
                modName,
                $"{modName}.config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<T>(json);
                }
                catch { }
            }

            var defaultConfig = new T();
            SaveConfig(modName, defaultConfig);
            return defaultConfig;
        }

        public static void SaveConfig<T>(string modName, T config)
        {
            string dir = Path.Combine(
                Directory.GetCurrentDirectory(),
                Constants.ModLoaderFolder,
                Constants.ModsFolder,
                modName);
            Directory.CreateDirectory(dir);
            string configPath = Path.Combine(dir, $"{modName}.config.json");
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
    }
}