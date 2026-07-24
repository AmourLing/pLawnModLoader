using HarmonyLib;
using Lawn;
using System.Reflection;
using System.Reflection.Emit;

namespace pLawnModLoader
{
    /// <summary>
    /// 所有在模组加载之前必须应用的 Harmony 补丁
    /// </summary>
    public static class PreModPatches
    {
        public static int ChosenSeedsSize = 100;

        [HarmonyPatch(typeof(SeedChooserScreen), MethodType.Constructor)]
        public static class SeedChooserScreen_Ctor_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = new List<CodeInstruction>(instructions);
                for (int i = 0; i < list.Count; i++)
                {
                    var instr = list[i];
                    // 替换 newarr 前的 Ldc_I4_S(64) 为动态大小
                    if (instr.opcode == OpCodes.Ldc_I4_S && instr.operand is sbyte operand && operand == 64)
                    {
                        if (i + 1 < list.Count && list[i + 1].opcode == OpCodes.Newarr)
                        {
                            instr.opcode = OpCodes.Ldc_I4;
                            instr.operand = ChosenSeedsSize;
                        }
                    }
                    yield return instr;
                }
            }

            static void Postfix(SeedChooserScreen __instance)
            {
                try
                {
                    var fi = typeof(SeedChooserScreen).GetField("mChosenSeeds", BindingFlags.Public | BindingFlags.Instance);
                    if (fi == null) return;
                    var arr = fi.GetValue(__instance) as Array;
                    if (arr == null)
                    {
                        int size = Math.Max(ChosenSeedsSize, 65);
                        arr = Array.CreateInstance(fi.FieldType.GetElementType() ?? typeof(object), size);
                        fi.SetValue(__instance, arr);
                    }
                    // 确保每个元素非空
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr.GetValue(i) == null)
                            arr.SetValue(Activator.CreateInstance(fi.FieldType.GetElementType()), i);
                    }
                }
                catch { /* 容错 */ }
            }
        }
    }
}