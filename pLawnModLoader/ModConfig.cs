using System.IO;
using System.Text.Json;
using pLawnModLoader_Shared;

namespace pLawnModLoader
{
    public static class ModConfig
    {
        private static readonly object _lock = new object();

        public static T? GetConfig<T>(string modName) where T : class, new()
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
                catch
                {
                    // 解析失败，返回默认值
                }
            }

            // 不存在或解析失败，创建默认并保存
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