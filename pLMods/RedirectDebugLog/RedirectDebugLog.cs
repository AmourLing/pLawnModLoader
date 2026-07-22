using pLawnModLoader;
using pLawnModLoader_Shared;
using Sexy;
using System.Reflection;

namespace RedirectDebugLog
{
    public static class pLMods
    {
        public static void Apply()
        {
            try
            {
                // 获取 Sexy.Debug 类型
                var debugType = Type.GetType("Sexy.Debug, Lawn");
                if (debugType == null)
                {
                    Log.Warning("[RedirectDebugLog] Sexy.Debug type not found");
                    return;
                }

                // 获取 Logger 静态字段 (Action<string, DebugType>)
                var loggerField = debugType.GetField("Logger", BindingFlags.Public | BindingFlags.Static);
                if (loggerField == null)
                {
                    Log.Warning("[RedirectDebugLog] Logger field not found");
                    return;
                }

                // 获取当前委托（以保留原有行为，也可选择完全替换）
                var originalLogger = loggerField.GetValue(null) as Action<string, DebugType>;

                // 创建新委托，重定向日志
                Action<string, DebugType> newLogger = (msg, type) =>
                {
                    // 调用我们统一的日志系统
                    switch (type)
                    {
                        case DebugType.Info:
                            Log.Info(msg, writeToConsole: false);
                            break;

                        case DebugType.Warn:
                            Log.Warning(msg, writeToConsole: false);
                            break;

                        case DebugType.Error:
                            Log.Error(msg, writeToConsole: false);
                            break;

                        case DebugType.Fatal:
                            Log.Error($"[FATAL] {msg}", writeToConsole: false);
                            break;

                        default:
                            Log.Info(msg, writeToConsole: false);
                            break;
                    }

                    // 可选：仍调用原始委托（保留控制台输出等）
                    originalLogger?.Invoke(msg, type);
                };

                // 替换委托
                loggerField.SetValue(null, newLogger);

                Log.Info("[RedirectDebugLog] Debug logger redirected successfully");
            }
            catch (Exception ex)
            {
                Log.Error("[RedirectDebugLog] Failed to redirect debug log", ex);
            }
        }
    }
}