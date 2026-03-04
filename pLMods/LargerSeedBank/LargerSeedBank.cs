using HarmonyLib;
using Lawn;
using Sexy;
using System.Reflection;
using System.Reflection.Emit;
using pLawnModLoader;

namespace LargerSeedBank
{
    public class LargerSeedBankConfig
    {
        public int SeedBankMax { get; set; } = 12;
    }

    public static class pLMods
    {
        public static int NewSeedBankMax = 12;

        public static void Apply()
        {
            var config = ModConfig.GetConfig<LargerSeedBankConfig>("LargerSeedBank");
            if (config != null)
            {
                NewSeedBankMax = config.SeedBankMax;
                Log.Info($"[LargerSeedBank] Loaded SeedBankMax = {NewSeedBankMax} from config");
            }
            else
            {
                Log.Warning("[LargerSeedBank] Config not found, using default SeedBankMax=12");
            }

            var harmony = new Harmony("net.pvz.largerseedbank");
            harmony.PatchAll(typeof(pLMods).Assembly);
            Log.Info("[LargerSeedBank] Patch applied successfully");
        }

        [HarmonyPatch(typeof(SeedBank), MethodType.Constructor)]
        public static class SeedBank_Ctor_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_I4_S && instr.operand is sbyte operand && operand == 10)
                        instr.operand = (sbyte)NewSeedBankMax;
                    yield return instr;
                }
            }
        }

        [HarmonyPatch(typeof(Board), "InitLevel")]
        public static class Board_InitLevel_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_I4_S && instr.operand is sbyte operand && operand == 10)
                        instr.operand = (sbyte)NewSeedBankMax;
                    yield return instr;
                }
            }
        }

        [HarmonyPatch(typeof(Board), "InitSurvivalStage")]
        public static class Board_InitSurvivalStage_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_I4_S && instr.operand is sbyte operand && operand == 10)
                        instr.operand = (sbyte)NewSeedBankMax;
                    yield return instr;
                }
            }
        }

        [HarmonyPatch(typeof(Board), "GetNumSeedsInBank")]
        public static class Board_GetNumSeedsInBank_Patch
        {
            static void Postfix(ref int __result)
            {
                if (__result > NewSeedBankMax || NewSeedBankMax > 10)
                    __result = NewSeedBankMax;
            }
        }

        [HarmonyPatch(typeof(Board), "GetSeedPacketPositionY")]
        public static class Board_GetSeedPacketPositionY_Patch
        {
            static bool Prefix(Board __instance, int theIndex, ref int __result)
            {
                try
                {
                    MethodInfo getNumMethod = typeof(Board).GetMethod("GetNumSeedsInBank", BindingFlags.Public | BindingFlags.Instance);
                    if (getNumMethod == null)
                    {
                        Log.Error("[LargerSeedBank] GetNumSeedsInBank method not found");
                        return true;
                    }
                    int aNumPackets = (int)getNumMethod.Invoke(__instance, null);

                    if (aNumPackets == 0)
                    {
                        Log.Warning("[LargerSeedBank] aNumPackets is 0, fallback");
                        return true;
                    }

                    int num = Constants.SMALL_SEEDPACKET_HEIGHT * 88 / 10 / aNumPackets;

                    if (!__instance.HasConveyorBeltSeedBank() && !Constants.Is500pMode)
                    {
                        num += 10;
                    }

                    int offset = (!__instance.HasConveyorBeltSeedBank() && !Constants.Is500pMode) ? 20 : 0;
                    __result = theIndex * num + offset;

                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error($"[LargerSeedBank] Prefix exception: {ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(SeedBank), "LoadFromFile")]
        public static class SeedBank_LoadFromFile_Patch
        {
            static bool Prefix(SeedBank __instance, Sexy.Buffer b, ref bool __result)
            {
                try
                {
                    var baseMethod = typeof(GameObject).GetMethod("LoadFromFile", BindingFlags.Public | BindingFlags.Instance);
                    if (baseMethod == null) return true;
                    baseMethod.Invoke(__instance, new object[] { b });

                    var mConveyorBeltCounter = typeof(SeedBank).GetField("mConveyorBeltCounter", BindingFlags.NonPublic | BindingFlags.Instance);
                    var mCutSceneDarken = typeof(SeedBank).GetField("mCutSceneDarken", BindingFlags.NonPublic | BindingFlags.Instance);
                    var mNumPackets = typeof(SeedBank).GetField("mNumPackets", BindingFlags.Public | BindingFlags.Instance);
                    var mSeedPacketsField = typeof(SeedBank).GetField("mSeedPackets", BindingFlags.Public | BindingFlags.Instance);
                    if (mConveyorBeltCounter == null || mCutSceneDarken == null || mNumPackets == null || mSeedPacketsField == null)
                        return true;

                    mConveyorBeltCounter.SetValue(__instance, b.ReadLong());
                    mCutSceneDarken.SetValue(__instance, b.ReadLong());
                    mNumPackets.SetValue(__instance, b.ReadLong());

                    SeedPacket[] seedPackets = (SeedPacket[])mSeedPacketsField.GetValue(__instance);
                    for (int i = 0; i < seedPackets.Length; i++)
                    {
                        bool hasPacket = b.ReadBoolean();
                        if (hasPacket)
                        {
                            seedPackets[i] = new SeedPacket();
                            seedPackets[i].LoadFromFile(b);
                        }
                        else
                        {
                            seedPackets[i] = null;
                        }
                    }

                    __result = true;
                    return false;
                }
                catch
                {
                    __result = false;
                    return false;
                }
            }
        }

        [HarmonyPatch(typeof(SeedBank), "SaveToFile")]
        public static class SeedBank_SaveToFile_Patch
        {
            static bool Prefix(SeedBank __instance, Sexy.Buffer b, ref bool __result)
            {
                try
                {
                    var baseMethod = typeof(GameObject).GetMethod("SaveToFile", BindingFlags.Public | BindingFlags.Instance);
                    if (baseMethod == null) return true;
                    baseMethod.Invoke(__instance, new object[] { b });

                    var mConveyorBeltCounter = typeof(SeedBank).GetField("mConveyorBeltCounter", BindingFlags.NonPublic | BindingFlags.Instance);
                    var mCutSceneDarken = typeof(SeedBank).GetField("mCutSceneDarken", BindingFlags.NonPublic | BindingFlags.Instance);
                    var mNumPackets = typeof(SeedBank).GetField("mNumPackets", BindingFlags.Public | BindingFlags.Instance);
                    var mSeedPacketsField = typeof(SeedBank).GetField("mSeedPackets", BindingFlags.Public | BindingFlags.Instance);
                    if (mConveyorBeltCounter == null || mCutSceneDarken == null || mNumPackets == null || mSeedPacketsField == null)
                        return true;

                    b.WriteLong((int)mConveyorBeltCounter.GetValue(__instance));
                    b.WriteLong((int)mCutSceneDarken.GetValue(__instance));
                    b.WriteLong((int)mNumPackets.GetValue(__instance));

                    SeedPacket[] seedPackets = (SeedPacket[])mSeedPacketsField.GetValue(__instance);
                    foreach (var packet in seedPackets)
                    {
                        b.WriteBoolean(packet != null);
                        if (packet != null)
                        {
                            packet.SaveToFile(b);
                        }
                    }

                    __result = true;
                    return false;
                }
                catch
                {
                    __result = false;
                    return false;
                }
            }
        }

        [HarmonyPatch(typeof(SeedBank), "Sync")]
        public static class SeedBank_Sync_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = new List<CodeInstruction>(instructions);
                var mSeedPacketsField = typeof(SeedBank).GetField("mSeedPackets", BindingFlags.Public | BindingFlags.Instance);
                if (mSeedPacketsField == null)
                    return instructions;

                var lengthLoadInstructions = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, mSeedPacketsField),
                    new CodeInstruction(OpCodes.Ldlen),
                    new CodeInstruction(OpCodes.Conv_I4)
                };

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].opcode == OpCodes.Ldc_I4_S && list[i].operand is sbyte operand && operand == 10)
                    {
                        list.RemoveAt(i);
                        list.InsertRange(i, lengthLoadInstructions);
                        break;
                    }
                }
                return list.AsEnumerable();
            }
        }
    }
}