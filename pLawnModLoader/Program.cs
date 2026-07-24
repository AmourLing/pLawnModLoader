using System.Reflection;
using HarmonyLib;
using pLawnModLoader_Shared;
using System.Text.Json;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;

namespace pLawnModLoader
{
    public class ModLoaderConfig
    {
        public int ChosenSeedsSize { get; set; } = 100;
    }

    public class Program
    {
        private static Harmony _harmony;
        private static Assembly _gameAssembly;
        private static string _gameDir;
        private static string[] _args;

        static void Main(string[] args)
        {
            try
            {
                _args = args;

                // 1. 寻找 Lawn.dll
                _gameDir = FindGameDirectory();
                _gameAssembly = LoadGameAssembly();

                // 2. 加载配置 (此时 _harmony 还为 null，这是正常的，只要 LoadModLoaderConfig 不使用 _harmony)
                LoadModLoaderConfig();

                // 3. 初始化 Harmony 并应用前置补丁
                // 【关键】必须先初始化 _harmony
                ApplyPreModPatches();

                // 4. 应用外部模组
                LoadExternalMods();

                // 5. 应用后置补丁
                // 【关键】此时 _harmony 已经初始化，可以安全调用
                LoadInternalMods(ModTypeEnum.Post);

                // 6. 启动游戏
                StartGame();
            }
            catch (Exception ex)
            {
                Log.Error("启动失败", ex);
                Console.ReadLine();
            }
        }

        // ---------- 步骤 1 ----------
        private static string FindGameDirectory()
        {
            string dir = Directory.GetCurrentDirectory();
            Log.Info($"游戏目录: {dir}");
            Log.Info($"日志文件: {Log.FilePath}");
            return dir;
        }

        private static Assembly LoadGameAssembly()
        {
            string path = Path.Combine(_gameDir, "Lawn.dll");
            if (!File.Exists(path))
                throw new FileNotFoundException("找不到 Lawn.dll");

            Assembly assembly = Assembly.LoadFrom(path);
            Log.Info("Lawn.dll 加载成功");
            return assembly;
        }

        // ---------- 步骤 2 ----------
        private static void LoadModLoaderConfig()
        {
            string configPath = Path.Combine(_gameDir, "pLawnModLoader.config.json");
            int chosenSeedsSize = 100;

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<ModLoaderConfig>(json);
                    if (config != null && config.ChosenSeedsSize > 0)
                        chosenSeedsSize = config.ChosenSeedsSize;
                }
                catch (Exception ex)
                {
                    Log.Warning($"读取配置文件失败，使用默认值: {ex.Message}");
                }
            }

            // 同步配置到静态字段
            // 注意：这里不要访问 _harmony
            if (typeof(PreModPatches).GetField("ChosenSeedsSize") != null)
            {
                typeof(PreModPatches).GetField("ChosenSeedsSize").SetValue(null, chosenSeedsSize);
            }

            if (typeof(ModSeedChooserScreen).GetField("ChosenSeedsSize") != null)
            {
                typeof(ModSeedChooserScreen).GetField("ChosenSeedsSize").SetValue(null, chosenSeedsSize);
            }

            Log.Info($"种子选择器数组大小设为 {chosenSeedsSize}");
        }

        // ---------- 步骤 3: 初始化 Harmony 并应用前置补丁 ----------
        private static void ApplyPreModPatches()
        {
            // 【关键】在这里初始化 _harmony
            if (_harmony == null)
            {
                _harmony = new Harmony("pLawnModLoader");
                Log.Info("Harmony 实例已创建");
            }

            // 加载前置内置补丁
            LoadInternalMods(ModTypeEnum.Pre);

            Log.Info("前置补丁阶段完成");
        }

        // ---------- 通用内部模组加载器 ----------
        private static void LoadInternalMods(ModTypeEnum type)
        {
            if (_harmony == null)
            {
                Log.Error("严重错误: Harmony 实例未初始化，无法加载内置补丁。请检查 ApplyPreModPatches 是否已执行。");
                return;
            }

            string typeName = type == ModTypeEnum.Pre ? "前置" : "后置";
            Log.Info($"--- 开始加载内置{typeName}补丁 ---");

            var assembly = Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsGenericType &&
                    t.GetCustomAttribute<ModPatchAttribute>() != null
                )
                .ToList();

            var targetTypes = types.Where(t =>
                t.GetCustomAttribute<ModPatchAttribute>().Type == type
            ).ToList();

            int total = targetTypes.Count;
            int loaded = 0;

            if (total == 0)
            {
                Log.Info($"未找到任何内置{typeName}补丁。");
                return;
            }

            foreach (var patchType in targetTypes)
            {
                var attr = patchType.GetCustomAttribute<ModPatchAttribute>();
                string name = attr?.Name ?? patchType.Name;

                try
                {
                    Log.Info($"[{loaded + 1}/{total}] 正在应用: {name}");

                    _harmony.CreateClassProcessor(patchType).Patch();

                    loaded++;
                    Log.Info($"[{loaded}/{total}] 成功应用: {name}", color: ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    Log.Error($"[{loaded + 1}/{total}] 应用失败: {name}", ex);
                }
            }

            Log.Info($"内置{typeName}补丁加载完成: {loaded}/{total}");
        }

        // ---------- 步骤 4: 外部模组加载 ----------
        private static void LoadExternalMods()
        {
            Log.Info("--- 开始加载外部模组 ---");

            string modsDir = Path.Combine(_gameDir, Constants.ModLoaderFolder, Constants.ModsFolder);
            if (!Directory.Exists(modsDir))
                Directory.CreateDirectory(modsDir);

            var dllPaths = Directory.GetFiles(modsDir, "*.dll", SearchOption.AllDirectories);
            int total = dllPaths.Length;
            int loaded = 0;

            if (total == 0)
            {
                Log.Info("未找到外部模组 DLL。");
                return;
            }

            for (int i = 0; i < total; i++)
            {
                string dllPath = dllPaths[i];
                string modName = Path.GetFileNameWithoutExtension(dllPath);

                try
                {
                    Log.Info($"[{i + 1}/{total}] 正在加载: {modName}");
                    Assembly modAssembly = Assembly.LoadFrom(dllPath);

                    try
                    {
                        _harmony.PatchAll(modAssembly);
                    }
                    catch (Exception patchEx)
                    {
                        Log.Warning($"Harmony Patch 警告: {modName} - {patchEx.Message}");
                    }

                    Type patchesType = modAssembly.GetTypes().FirstOrDefault(t => t.Name == "pLMods");
                    if (patchesType != null)
                    {
                        MethodInfo applyMethod = patchesType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static);
                        if (applyMethod != null)
                        {
                            applyMethod.Invoke(null, null);
                        }
                    }

                    loaded++;
                    Log.Info($"[{loaded}/{total}] 成功加载: {modName}", color: ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    Log.Error($"[{i + 1}/{total}] 加载失败: {modName}", ex);
                }
            }

            Log.Info($"外部模组加载完成: {loaded}/{total}");
        }

        // ---------- 步骤 6 ----------
        private static void StartGame()
        {
            Log.Info("--- 启动游戏 ---");
            Type? entryType = _gameAssembly.GetType("LAWN.PlantsVsZombies");
            if (entryType == null)
                throw new Exception("找不到入口类型 LAWN.PlantsVsZombies");

            MethodInfo? entryMethod = entryType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (entryMethod == null)
                throw new Exception("找不到入口方法 Main");

            var parameters = entryMethod.GetParameters();
            object[]? invokeArgs = null;
            if (parameters.Length != 0)
            {
                invokeArgs = new object[] { _args ?? Array.Empty<string>() };
            }

            entryMethod.Invoke(null, invokeArgs);
        }
    }
}
