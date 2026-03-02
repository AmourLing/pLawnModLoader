using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Text.Json;

namespace pLawnModLoaderLauncher.Config
{
    public class PatchScheme
    {
        public string SchemeName { get; set; } = string.Empty;
        public string GamePath { get; set; } = string.Empty;
        public Dictionary<string, bool> PatchStates { get; set; } = new();
    }

    public class AppConfig
    {
        public List<PatchScheme> Schemes { get; set; } = new();
        public string CurrentSchemeName { get; set; } = "默认方案";

        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            "config.json");

        public static AppConfig Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        if (config.Schemes.Count == 0)
                            config.Schemes.Add(new PatchScheme { SchemeName = "默认方案" });
                        return config;
                    }
                }
                catch { }
            }

            var defaultConfig = new AppConfig();
            defaultConfig.Schemes.Add(new PatchScheme { SchemeName = "默认方案" });
            return defaultConfig;
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        public PatchScheme CurrentScheme =>
            Schemes.FirstOrDefault(s => s.SchemeName == CurrentSchemeName) ?? Schemes.First();

        public void SwitchScheme(string schemeName)
        {
            if (Schemes.Any(s => s.SchemeName == schemeName))
                CurrentSchemeName = schemeName;
        }

        public void AddScheme(string schemeName)
        {
            if (!Schemes.Any(s => s.SchemeName == schemeName))
            {
                Schemes.Add(new PatchScheme { SchemeName = schemeName });
                CurrentSchemeName = schemeName;
            }
        }
    }
}