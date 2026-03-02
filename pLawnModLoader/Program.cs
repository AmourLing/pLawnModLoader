using System.Reflection;
using pLawnModLoader;

namespace pLawnModLoader
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string gameDir = Directory.GetCurrentDirectory();
                Log.Info($"游戏目录: {gameDir}");
                Log.Info($"日志文件: {Path.GetFullPath(Log.FilePath)}");

                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    string assemblyName = new AssemblyName(e.Name).Name;
                    string path = Path.Combine(gameDir, assemblyName + ".dll");
                    return File.Exists(path) ? Assembly.LoadFrom(path) : null;
                };

                string modsDir = Path.Combine(gameDir, "pLMods");
                if (!Directory.Exists(modsDir))
                    Directory.CreateDirectory(modsDir);

                int total = 0, loaded = 0, skipped = 0, failed = 0;

                foreach (string dllPath in Directory.GetFiles(modsDir, "*.dll"))
                {
                    total++;
                    try
                    {
                        Assembly modAssembly = Assembly.LoadFrom(dllPath);
                        Type patchesType = modAssembly.GetTypes().FirstOrDefault(t => t.Name == "pLMods");
                        if (patchesType != null)
                        {
                            MethodInfo applyMethod = patchesType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static);
                            if (applyMethod != null)
                            {
                                Log.Info($"应用补丁: {Path.GetFileName(dllPath)}");
                                applyMethod.Invoke(null, null);
                                loaded++;
                            }
                            else
                            {
                                Log.Warning($"补丁类 pLMods 缺少 Apply 方法: {Path.GetFileName(dllPath)}");
                                skipped++;
                            }
                        }
                        else
                        {
                            Log.Warning($"忽略非补丁 DLL: {Path.GetFileName(dllPath)}");
                            skipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"加载补丁 {Path.GetFileName(dllPath)} 失败", ex);
                        failed++;
                    }
                }

                Log.Info($"补丁加载完成：总计 {total} 个，成功 {loaded} 个，跳过 {skipped} 个，失败 {failed} 个。");

                string gameAssemblyPath = Path.Combine(gameDir, "Lawn.dll");
                if (!File.Exists(gameAssemblyPath))
                    throw new FileNotFoundException("找不到游戏主程序集 Lawn.dll");

                Assembly gameAssembly = Assembly.LoadFrom(gameAssemblyPath);

                Type entryType = gameAssembly.GetType("LAWN.PlantsVsZombies")
                                 ?? gameAssembly.GetType("Lawn.PlantsVsZombies")
                                 ?? gameAssembly.GetType("PlantsVsZombies");
                if (entryType == null)
                    throw new Exception("找不到入口类型，请检查命名空间");

                MethodInfo entryMethod = entryType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (entryMethod == null)
                    throw new Exception("找不到入口方法 Main");

                var parameters = entryMethod.GetParameters();
                object[] invokeArgs = parameters.Length == 0 ? null : new object[] { args ?? Array.Empty<string>() };

                entryMethod.Invoke(null, invokeArgs);
            }
            catch (Exception ex)
            {
                Log.Error($"启动失败", ex);
                Console.ReadLine();
            }
        }
    }
}