using HarmonyLib;
using pLawnModLoader;
using Sexy;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using pLawnModLoader_Shared;

namespace LargerImitaterDialog
{
    public class LargerImitaterDialogConfig
    {
        public int ImitaterSeedCount { get; set; } = 16;
    }

    public static class pLMods
    {
        public static int NewImitaterSeedCount = 16;

        public static void Apply()
        {
            // 1. 加载配置并校验范围
            var config = ModConfig.GetConfig<LargerImitaterDialogConfig>("LargerImitaterDialog");
            if (config != null)
            {
                int count = config.ImitaterSeedCount;
                if (count < 1) count = 1;
                if (count > 50) count = 50;
                NewImitaterSeedCount = count;
                Log.Info($"[LargerImitaterDialog] Loaded ImitaterSeedCount = {NewImitaterSeedCount} from config");
            }
            else
            {
                Log.Warning("[LargerImitaterDialog] Config not found, using default ImitaterSeedCount = 14");
                NewImitaterSeedCount = 14;
            }

            var harmony = new Harmony("net.pvz.largerimitaterdialog");

            // 2. 获取目标类型（使用程序集限定名）
            var dialogType = Type.GetType("Lawn.ImitaterDialog, Lawn");
            if (dialogType == null)
            {
                // 备选：尝试不带程序集名（以防类型在主程序集）
                dialogType = Type.GetType("Lawn.ImitaterDialog");
                if (dialogType == null)
                {
                    Log.Warning("[LargerImitaterDialog] Lawn.ImitaterDialog type not found");
                    return;
                }
            }

            // 3. 构造函数补丁（Transpiler）
            var ctor = AccessTools.Constructor(dialogType, Type.EmptyTypes);
            if (ctor != null)
            {
                var transpiler = new HarmonyMethod(typeof(ImitaterDialog_Ctor_Patch).GetMethod(nameof(ImitaterDialog_Ctor_Patch.Transpiler)));
                harmony.Patch(ctor, transpiler: transpiler);
                Log.Info("[LargerImitaterDialog] Constructor patched");
            }
            else
            {
                Log.Warning("[LargerImitaterDialog] Constructor not found");
            }

            // 4. Draw 方法补丁（Postfix）
            var graphicsType = Type.GetType("Sexy.Graphics, Lawn");
            if (graphicsType == null)
                graphicsType = Type.GetType("Sexy.Graphics");
            if (graphicsType != null)
            {
                var drawMethod = AccessTools.Method(dialogType, "Draw", new Type[] { graphicsType });
                if (drawMethod != null)
                {
                    var postfix = new HarmonyMethod(typeof(ImitaterDialog_Draw_Patch).GetMethod(nameof(ImitaterDialog_Draw_Patch.Postfix)));
                    harmony.Patch(drawMethod, postfix: postfix);
                    Log.Info("[LargerImitaterDialog] Draw method patched");
                }
                else
                {
                    Log.Warning("[LargerImitaterDialog] Draw method not found");
                }
            }
            else
            {
                Log.Warning("[LargerImitaterDialog] Sexy.Graphics type not found");
            }

            Log.Info("[LargerImitaterDialog] All patches applied");
        }

        // ================= 补丁类 =================

        // 构造函数 Transpiler
        public static class ImitaterDialog_Ctor_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool patched = false;
                foreach (var instr in instructions)
                {
                    if (!patched && instr.opcode == OpCodes.Ldc_I4_S && instr.operand is sbyte sb && sb == 10)
                    {
                        patched = true;
                        int newCount = NewImitaterSeedCount;
                        if (newCount >= sbyte.MinValue && newCount <= sbyte.MaxValue)
                        {
                            instr.operand = (sbyte)newCount;
                            yield return instr;
                        }
                        else
                        {
                            yield return new CodeInstruction(OpCodes.Ldc_I4, newCount);
                            continue;
                        }
                    }
                    else
                    {
                        yield return instr;
                    }
                }
            }
        }

        // Draw 方法 Postfix（验证用）
        public static class ImitaterDialog_Draw_Patch
        {
            public static void Postfix(object __instance)
            {
                var widgetField = AccessTools.Field(__instance.GetType(), "mSeedPacketsWidget");
                if (widgetField == null) return;
                var widget = widgetField.GetValue(__instance);
                if (widget == null) return;

                var rowsField = AccessTools.Field(widget.GetType(), "mRows");
                if (rowsField == null) return;

                int rows = (int)rowsField.GetValue(widget);
                //Log.Info($"[LargerImitaterDialog] SeedPacketsWidget.mRows = {rows}");
            }
        }
    }
}