using HarmonyLib;
using Lawn;
using Sexy;
using System.Reflection;
using System.Reflection.Emit;
using pLawnModLoader;

namespace LargerImitaterDialog
{
    public class LargerImitaterDialogConfig
    {
        public int ImitaterSeedCount { get; set; } = 14;
    }

    public static class pLMods
    {
        public static int NewImitaterSeedCount = 14;

        public static void Apply()
        {
            var config = ModConfig.GetConfig<LargerImitaterDialogConfig>("LargerImitaterDialog");
            if (config != null)
            {
                NewImitaterSeedCount = config.ImitaterSeedCount;
                Log.Info($"[LargerImitaterDialog] Loaded ImitaterSeedCount = {NewImitaterSeedCount} from config");
            }
            else
            {
                Log.Warning("[LargerImitaterDialog] Config not found, using default ImitaterSeedCount = 14");
            }

            var harmony = new Harmony("net.pvz.largerimitaterdialog");

            Type imitaterDialogType = Type.GetType("Lawn.ImitaterDialog, Lawn");
            if (imitaterDialogType == null)
            {
                Log.Error("[LargerImitaterDialog] Failed to find type Lawn.ImitaterDialog");
                return;
            }

            ConstructorInfo ctor = imitaterDialogType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            if (ctor == null)
            {
                Log.Error("[LargerImitaterDialog] Failed to find constructor for ImitaterDialog");
                return;
            }

            MethodInfo transpiler = typeof(ImitaterDialog_Ctor_Patch).GetMethod(
                "Transpiler", BindingFlags.Static | BindingFlags.Public);
            if (transpiler == null)
            {
                Log.Error("[LargerImitaterDialog] Failed to find Transpiler method");
                return;
            }

            harmony.Patch(ctor, transpiler: new HarmonyMethod(transpiler));
            Log.Info("[LargerImitaterDialog] Patch applied successfully");
        }

        public static class ImitaterDialog_Ctor_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_I4_S && instr.operand is sbyte operand && operand == 10)
                    {
                        instr.operand = (sbyte)NewImitaterSeedCount;
                    }
                    yield return instr;
                }
            }
        }
    }
}