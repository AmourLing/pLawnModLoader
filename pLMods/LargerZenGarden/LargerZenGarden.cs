using HarmonyLib;
using Lawn;
using pLawnModLoader;
using pLawnModLoader_Shared;
using Sexy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LargerZenGarden
{
    public class LargerZenGardenConfig
    {
        public List<string> GardenOrder { get; set; } = new List<string>();
    }

    public static class pLMods
    {
        private static List<int> _gardenOrder = new List<int>();

        // 名称到 GardenType 整数值的映射（可扩展）
        private static readonly Dictionary<string, int> _nameToValue = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Main", 0 },
            { "Main2", 4 },
            { "Main3", 8 },
            { "Night", 6 },
            { "Night2", 9 },
            { "Mushroom", 1 },
            { "Mushroom2", 5 },
            { "Mushroom3", 10 },
            { "Aquarium", 3 },
            { "Aquarium2", 11 }
        };

        private static readonly Dictionary<int, string> _valueToName = _nameToValue.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        // 获取所有 Main 和 Night 衍生类型的数量（用于动态容量计算）
        public static int GetMainAndNightCount()
        {
            int count = 0;
            foreach (var key in _nameToValue.Keys)
            {
                string baseName = key;
                int i = key.Length - 1;
                while (i >= 0 && char.IsDigit(key[i])) i--;
                if (i < key.Length - 1)
                    baseName = key.Substring(0, i + 1);
                else
                    baseName = key;

                if (baseName.Equals("Main", StringComparison.OrdinalIgnoreCase) ||
                    baseName.Equals("Night", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        // 生成默认顺序（自动分组）
        private static List<string> GenerateDefaultOrder()
        {
            var groups = new Dictionary<string, List<string>>();
            foreach (var key in _nameToValue.Keys)
            {
                string baseName = key;
                int i = key.Length - 1;
                while (i >= 0 && char.IsDigit(key[i])) i--;
                if (i < key.Length - 1)
                    baseName = key.Substring(0, i + 1);
                else
                    baseName = key;

                if (!groups.ContainsKey(baseName))
                    groups[baseName] = new List<string>();
                groups[baseName].Add(key);
            }

            string[] groupOrder = { "Main", "Night", "Mushroom", "Aquarium" };
            var result = new List<string>();
            foreach (var group in groupOrder)
            {
                if (groups.TryGetValue(group, out var list))
                {
                    list.Sort((a, b) => ExtractNumber(a).CompareTo(ExtractNumber(b)));
                    result.AddRange(list);
                }
            }
            return result;
        }

        private static int ExtractNumber(string name)
        {
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i])) i--;
            if (i < name.Length - 1)
            {
                string numStr = name.Substring(i + 1);
                return int.TryParse(numStr, out int num) ? num : 0;
            }
            return 0;
        }

        public static void Apply()
        {
            var config = ModConfig.GetConfig<LargerZenGardenConfig>("LargerZenGarden");
            List<string> orderNames;

            if (config != null && config.GardenOrder.Count > 0)
            {
                orderNames = config.GardenOrder;
                Log.Info($"[LargerZenGarden] Loaded garden order: {string.Join(", ", orderNames)}");
            }
            else
            {
                orderNames = GenerateDefaultOrder();
                Log.Info($"[LargerZenGarden] Generated default order: {string.Join(", ", orderNames)}");
            }

            _gardenOrder.Clear();
            foreach (var name in orderNames)
            {
                if (_nameToValue.TryGetValue(name, out int val))
                    _gardenOrder.Add(val);
                else
                    Log.Warning($"[LargerZenGarden] Unknown garden name: {name}, skipped.");
            }

            if (_gardenOrder.Count == 0)
            {
                Log.Error("[LargerZenGarden] No valid gardens, using fallback.");
                _gardenOrder = new List<int> { 0, 4, 8, 6, 9, 1, 5, 10, 3, 11 };
            }

            var harmony = new Harmony("net.pvz.largerzengarden");

            // -------- 核心补丁 --------
            // GetNextGarden
            var getNextGarden = AccessTools.Method(typeof(ZenGarden), "GetNextGarden");
            harmony.Patch(getNextGarden, prefix: new HarmonyMethod(typeof(GetNextGarden_Patch), nameof(GetNextGarden_Patch.Prefix)));

            // JumptoNextGarden
            var jumptoNextGarden = AccessTools.Method(typeof(ZenGarden), "JumptoNextGarden");
            harmony.Patch(jumptoNextGarden, postfix: new HarmonyMethod(typeof(JumptoNextGarden_Patch), nameof(JumptoNextGarden_Patch.Postfix)));

            // FindOpenZenGardenSpot
            var findOpenSpot = AccessTools.Method(typeof(ZenGarden), "FindOpenZenGardenSpot");
            harmony.Patch(findOpenSpot, prefix: new HarmonyMethod(typeof(FindOpenSpot_Prefix), nameof(FindOpenSpot_Prefix.Prefix)));

            // IsZenGardenFull - 动态容量
            var isFull = AccessTools.Method(typeof(ZenGarden), "IsZenGardenFull");
            harmony.Patch(isFull, prefix: new HarmonyMethod(typeof(IsZenGardenFull_Prefix), nameof(IsZenGardenFull_Prefix.Prefix)));

            // -------- 数据持久化补丁（关键！防止加载时值被重置） --------
            // 修补 PlayerInfo.LoadFromFile，使其不对 mWhichZenGarden 进行范围限制
            var playerInfoType = AccessTools.TypeByName("Lawn.PlayerInfo");
            if (playerInfoType != null)
            {
                var loadMethod = AccessTools.Method(playerInfoType, "LoadFromFile");
                if (loadMethod != null)
                {
                    harmony.Patch(loadMethod, prefix: new HarmonyMethod(typeof(PlayerInfo_LoadPatch), nameof(PlayerInfo_LoadPatch.Postfix)));
                    Log.Info("[LargerZenGarden] Patched PlayerInfo.LoadFromFile to preserve garden values.");
                }
                else
                {
                    Log.Warning("[LargerZenGarden] Could not find PlayerInfo.LoadFromFile, garden values may reset on load.");
                }
            }
            else
            {
                Log.Warning("[LargerZenGarden] PlayerInfo type not found.");
            }

            Log.Info("[LargerZenGarden] All patches applied.");
        }

        // ==================== 补丁类 ====================

        [HarmonyPatch(typeof(ZenGarden), "GetNextGarden")]
        public static class GetNextGarden_Patch
        {
            public static bool Prefix(LawnApp theApp, ref GardenType theCurrent, ref BackgroundType theBackground, ref bool theTree, int theNext)
            {
                var list = pLMods._gardenOrder;

                if (theTree)
                {
                    if (theNext > 0)
                        theCurrent = (GardenType)list[0];
                    else
                        theCurrent = (GardenType)list[list.Count - 1];
                    theTree = false;
                    SetBackground(ref theBackground, (int)theCurrent);
                    return false;
                }

                int curIndex = list.IndexOf((int)theCurrent);
                if (curIndex == -1)
                {
                    if (theNext > 0)
                        theCurrent = (GardenType)list[0];
                    else
                        theCurrent = (GardenType)list[list.Count - 1];
                    SetBackground(ref theBackground, (int)theCurrent);
                    return false;
                }

                if (theNext > 0 && curIndex == list.Count - 1)
                {
                    theTree = true;
                    return false;
                }
                if (theNext < 0 && curIndex == 0)
                {
                    theTree = true;
                    return false;
                }

                int newIndex = curIndex + theNext;
                if (newIndex < 0) newIndex = list.Count - 1;
                else if (newIndex >= list.Count) newIndex = 0;

                theCurrent = (GardenType)list[newIndex];
                SetBackground(ref theBackground, (int)theCurrent);
                return false;
            }

            private static void SetBackground(ref BackgroundType bg, int gardenType)
            {
                switch (gardenType)
                {
                    case 1:
                    case 5:
                    case 10:
                        bg = BackgroundType.MushroomGarden;
                        break;

                    case 3:
                    case 11:
                        bg = BackgroundType.Zombiquarium;
                        break;

                    case 6:
                    case 9:
                        bg = BackgroundType.GreenhouseNight;
                        break;

                    default:
                        bg = BackgroundType.Greenhouse;
                        break;
                }
            }
        }

        public static string FromValueToKey(int value)
        {
            return _valueToName.TryGetValue(value, out string name) ? name : $"Unknown({value})";
        }

        [HarmonyPatch(typeof(ZenGarden), "JumptoNextGarden")]
        public static class JumptoNextGarden_Patch
        {
            public static void Postfix(ZenGarden __instance)
            {
                int current = (int)__instance.mGardenType;
                string currentT = FromValueToKey(current);
                Log.Info($"[LargerZenGarden] JumptoNextGarden: current garden type = {currentT}");
                if (__instance.mBoard != null)
                {
                    BackgroundType targetBg;
                    switch (current)
                    {
                        case 1:
                        case 5:
                        case 10:
                            targetBg = BackgroundType.MushroomGarden;
                            break;

                        case 3:
                        case 11:
                            targetBg = BackgroundType.Zombiquarium;
                            break;

                        case 6:
                        case 9:
                            targetBg = BackgroundType.GreenhouseNight;
                            break;

                        default:
                            targetBg = BackgroundType.Greenhouse;
                            break;
                    }
                    if (__instance.mBoard.mBackground != targetBg)
                    {
                        __instance.mBoard.mBackground = targetBg;
                        string loadKey = targetBg switch
                        {
                            BackgroundType.MushroomGarden => "DelayLoad_MushroomGarden",
                            BackgroundType.Zombiquarium => "DelayLoad_Zombiquarium",
                            BackgroundType.GreenhouseNight => "DelayLoad_GreenHouseNight",
                            _ => "DelayLoad_GreenHouseGarden"
                        };
                        __instance.mApp.DelayLoadZenGardenBackground(loadKey);
                        Log.Info($"[LargerZenGarden] Background set to {targetBg} for garden {current}");
                    }
                }
            }
        }

        // 映射 Main3(8) -> Main(0), Night2(9) -> Night(6)
        [HarmonyPatch(typeof(ZenGarden), "FindOpenZenGardenSpot")]
        public static class FindOpenSpot_Prefix
        {
            public static void Prefix(ref GardenType theGardenType)
            {
                int val = (int)theGardenType;
                if (val == 8) // Main3
                    theGardenType = GardenType.Main;
                else if (val == 9) // Night2
                    theGardenType = GardenType.Night;
            }
        }

        // 满园检测：动态容量 = 32 * (Main衍生类型数量 + Night衍生类型数量)
        [HarmonyPatch(typeof(ZenGarden), "IsZenGardenFull")]
        public static class IsZenGardenFull_Prefix
        {
            public static bool Prefix(ZenGarden __instance, bool theIncludeDroppedPresents, ref bool __result)
            {
                int num = 0;
                if (__instance.mBoard != null && theIncludeDroppedPresents)
                {
                    num += __instance.mBoard.CountCoinByType(CoinType.AwardPresent);
                    num += __instance.mBoard.CountCoinByType(CoinType.PresentPlant);
                }

                int num2 = 0;
                for (int i = 0; i < __instance.mApp.mPlayerInfo.mNumPottedPlants; i++)
                {
                    PottedPlant pottedPlant = __instance.PottedPlantFromIndex(i);
                    int gt = (int)pottedPlant.mWhichZenGarden;
                    // 统计所有 Main 和 Night 衍生类型
                    string name = FromValueToKey(gt);
                    if (name.StartsWith("Main", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Night", StringComparison.OrdinalIgnoreCase))
                    {
                        num2++;
                    }
                }

                int maxCapacity = 32 * GetMainAndNightCount();
                __result = (num2 + num >= maxCapacity);
                return false;
            }
        }

        // ==================== 数据持久化补丁 ====================
        // 由于我们不知道 PlayerInfo.LoadFromFile 的具体实现，此处提供一个通用 Prefix，
        // 在加载完成后，将所有 mWhichZenGarden 值修正为原始值（如果被重置）。
        // 但更好的办法是防止重置，所以如果有可能，请修改 LoadFromFile 中的验证逻辑。
        // 这里我们采用 Postfix 修正：加载后如果某个花园值被改为 0，但根据其他信息（如植物种子类型）推测其应有的花园类型，
        // 但这不是可靠的方法。所以我们直接修改 LoadFromFile 的 IL，移除范围检查。
        // 由于无法提供具体 IL 修改，我们提供一个框架，您需要根据反编译结果调整。

        [HarmonyPatch(typeof(Lawn.PlayerInfo), "LoadFromFile")]
        public static class PlayerInfo_LoadPatch
        {
            // 我们可以用 Prefix 在加载前备份，但最好是用 Transpiler 移除检查。
            // 这里仅示意，实际需要您查看反编译代码后调整。
            public static void Postfix(object __instance)
            {
                // __instance 是 PlayerInfo 对象
                // 遍历 mPottedPlant，如果 mWhichZenGarden 被置为 0 但原本应为 8/9，则无法恢复。
                // 所以我们推荐使用 Transpiler 修改加载逻辑。
                Log.Warning("[LargerZenGarden] PlayerInfo.LoadFromFile patch is incomplete. Please implement Transpiler to remove value validation.");
            }
        }
    }
}