using HarmonyLib;
using Lawn;
using pLawnModLoader_Shared;
using Sexy;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace pLawnModLoader
{
    /// <summary>
    /// 后置补丁：添加类似模仿者的独立自定义种子按钮
    /// </summary>
    [ModPatch(ModTypeEnum.Post, "内置选卡扩展")]
    public static class ModSeedChooserScreen
    {
        // 自定义植物 ID
        public const int CUSTOM_SEED_ID = 65;
        // 自定义按钮 ID (避免与原游戏冲突，原游戏最大用到 111)
        public const int CUSTOM_BUTTON_ID = 112;

        public static int ChosenSeedsSize = 100;

        // 存储每个实例对应的自定义按钮，以便在 Patch 中访问
        private static readonly Dictionary<SeedChooserScreen, GameButton> _customButtons = new Dictionary<SeedChooserScreen, GameButton>();

        /// <summary>
        /// Patch 1: 扩展 mChosenSeeds 数组大小
        /// </summary>
        [HarmonyPatch(typeof(SeedChooserScreen), MethodType.Constructor)]
        public static class SeedChooserScreen_Ctor_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = new List<CodeInstruction>(instructions);
                for (int i = 0; i < list.Count; i++)
                {
                    var instr = list[i];
                    // 匹配 new ChosenSeed[65]
                    if ((instr.opcode == OpCodes.Ldc_I4_S && instr.operand is sbyte sb && sb == 65) ||
                        (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int iv && iv == 65))
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

            /// <summary>
            /// Postfix: 初始化自定义按钮和数据对象
            /// </summary>
            static void Postfix(SeedChooserScreen __instance)
            {
                try
                {
                    if (__instance == null) return;

                    // 1. 初始化 ChosenSeed 数据对象 (用于逻辑状态管理)
                    FieldInfo fi = typeof(SeedChooserScreen).GetField("mChosenSeeds", BindingFlags.Public | BindingFlags.Instance);
                    if (fi != null)
                    {
                        var arrObj = fi.GetValue(__instance) as Array;
                        if (arrObj != null && arrObj.Length > CUSTOM_SEED_ID)
                        {
                            Type elemType = fi.FieldType.GetElementType();
                            object existingSeed = arrObj.GetValue(CUSTOM_SEED_ID);

                            if (existingSeed == null && elemType != null)
                            {
                                object newSeedObj = Activator.CreateInstance(elemType);
                                SetField(newSeedObj, "mSeedType", (SeedType)CUSTOM_SEED_ID);
                                SetField(newSeedObj, "mImitaterType", SeedType.None);
                                SetField(newSeedObj, "mSeedState", ChosenSeedState.SEED_IN_CHOOSER);
                                SetField(newSeedObj, "mRefreshCounter", 0);
                                SetField(newSeedObj, "mRefreshing", false);
                                SetField(newSeedObj, "mCrazyDavePicked", false);
                                SetField(newSeedObj, "mTimeStartMotion", 0);
                                SetField(newSeedObj, "mTimeEndMotion", 0);
                                SetField(newSeedObj, "mSeedIndexInBank", 0);

                                // 初始位置设为屏幕外，实际显示由按钮控制
                                SetField(newSeedObj, "mX", -100f);
                                SetField(newSeedObj, "mY", -100f);

                                arrObj.SetValue(newSeedObj, CUSTOM_SEED_ID);
                            }
                        }
                    }

                    // 2. 创建自定义 GameButton (完全模仿 mImitaterButton 的创建方式)
                    GameButton customBtn = new GameButton(CUSTOM_BUTTON_ID, __instance);

                    // 关键：不设置背景图片，因为我们后面会手动绘制种子包
                    customBtn.mButtonImage = null;
                    customBtn.mOverImage = null;
                    customBtn.mDownImage = null;
                    customBtn.mDisabledImage = null;

                    // 设置位置：放在模仿者按钮 (248, 27) 的正下方，间距 50 像素
                    // 你可以根据需要调整这里的坐标
                    int btnX = (int)Sexy.Constants.InvertAndScale(248f);
                    int btnY = (int)Sexy.Constants.InvertAndScale(77f);

                    customBtn.Resize(btnX, btnY, Sexy.Constants.SMALL_SEEDPACKET_WIDTH, Sexy.Constants.SMALL_SEEDPACKET_HEIGHT);
                    customBtn.mParentWidget = __instance;

                    // 初始状态检查：玩家是否拥有该植物？
                    bool hasSeed = __instance.mApp.HasSeedType((SeedType)CUSTOM_SEED_ID);
                    customBtn.SetDisabled(!hasSeed);
                    customBtn.mBtnNoDraw = !hasSeed; // 如果没有解锁，直接隐藏

                    // 保存引用
                    _customButtons[__instance] = customBtn;

                }
                catch (Exception ex)
                {
                    Log.Warning($"ModSeedChooserScreen Init Error: {ex.Message}");
                }
            }

            private static void SetField(object obj, string fieldName, object value)
            {
                FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null) field.SetValue(obj, value);
            }
        }

        /// <summary>
        /// Patch 2: 在 Draw 方法中手动绘制种子包图标
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.Draw))]
        public static void SeedChooserScreen_Draw_Postfix(SeedChooserScreen __instance, Graphics g)
        {
            if (_customButtons.TryGetValue(__instance, out GameButton btn))
            {
                // 如果按钮被禁用或标记为不绘制，则跳过
                if (btn.mBtnNoDraw || btn.mDisabled) return;

                // 获取对应的 ChosenSeed 以获取 ImitaterType 等信息
                ChosenSeed seed = GetChosenSeed(__instance, CUSTOM_SEED_ID);
                if (seed == null) return;

                SeedType seedType = (SeedType)CUSTOM_SEED_ID;

                // 使用 SeedPacket.DrawSmallSeedPacket 绘制标准的种子包外观
                // 参数：g, x, y, seedType, imitaterType, percentDark, grayness, drawCost, useCurrentCost, drawBackground, drawCostBackground
                SeedPacket.DrawSmallSeedPacket(
                    g,
                    btn.mX,
                    btn.mY,
                    seedType,
                    seed.mImitaterType,
                    0f,
                    255,          // 正常亮度
                    theDrawCost: true,       // 显示阳光消耗
                    theUseCurrentCost: false,
                    theDrawBackground: true, // 显示种子包背景图
                    theDrawCostBackground: true // 显示消耗标签背景
                );
            }
        }

        /// <summary>
        /// Patch 3: 处理鼠标点击事件
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.MouseDown))]
        public static bool SeedChooserScreen_MouseDown_Prefix(SeedChooserScreen __instance, int x, int y, int theClickCount)
        {
            if (_customButtons.TryGetValue(__instance, out GameButton btn))
            {
                if (!btn.mDisabled && !btn.mBtnNoDraw)
                {
                    // 检查鼠标是否点击在按钮区域内
                    if (x >= btn.mX && x < btn.mX + btn.mWidth &&
                        y >= btn.mY && y < btn.mY + btn.mHeight)
                    {
                        // 播放点击音效
                        __instance.mApp.PlaySample(Resources.SOUND_TAP);

                        // 获取种子对象并触发选择逻辑
                        ChosenSeed seed = GetChosenSeed(__instance, CUSTOM_SEED_ID);
                        if (seed != null && seed.mSeedState == ChosenSeedState.SEED_IN_CHOOSER)
                        {
                            __instance.ClickedSeedInChooser(ref seed);
                        }

                        // 返回 false 阻止基类继续处理此点击，避免冲突
                        return false;
                    }
                }
            }
            return true; // 其他区域正常处理
        }

        /// <summary>
        /// Patch 4: 在购买后更新按钮状态 (例如解锁了新植物)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.UpdateAfterPurchase))]
        public static void SeedChooserScreen_UpdateAfterPurchase_Postfix(SeedChooserScreen __instance)
        {
            if (_customButtons.TryGetValue(__instance, out GameButton btn))
            {
                bool hasSeed = __instance.mApp.HasSeedType((SeedType)CUSTOM_SEED_ID);
                btn.SetDisabled(!hasSeed);
                btn.mBtnNoDraw = !hasSeed;
            }
        }

        private static ChosenSeed GetChosenSeed(SeedChooserScreen instance, int index)
        {
            FieldInfo fi = typeof(SeedChooserScreen).GetField("mChosenSeeds", BindingFlags.Public | BindingFlags.Instance);
            if (fi != null)
            {
                var arr = fi.GetValue(instance) as Array;
                if (arr != null && index < arr.Length)
                {
                    return arr.GetValue(index) as ChosenSeed;
                }
            }
            return null;
        }
    }
}
