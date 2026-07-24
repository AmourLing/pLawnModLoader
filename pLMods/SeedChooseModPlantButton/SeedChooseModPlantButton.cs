using HarmonyLib;
using Lawn;
using Sexy;
using System.Reflection;
using System.Reflection.Emit;
using pLawnModLoader;
using pLawnModLoader_Shared;

namespace SeedChooseModPlantButton
{
    public class SeedChooseModPlantButtonConfig
    {
        public bool EnableModPlantButton { get; set; } = true;
        public int ModPlantSeedType { get; set; } = 0; // 0 = Peashooter
    }

    public static class pLMods
    {
        public static bool IsEnabled = true;
        public static int ModPlantSeedType = 0;

        public static void Apply()
        {
            var config = ModConfig.GetConfig<SeedChooseModPlantButtonConfig>("SeedChooseModPlantButton");
            if (config != null)
            {
                IsEnabled = config.EnableModPlantButton;
                ModPlantSeedType = config.ModPlantSeedType;
                Log.Info($"[SeedChooseModPlantButton] Loaded EnableModPlantButton = {IsEnabled}, ModPlantSeedType = {ModPlantSeedType}");
            }
            else
            {
                Log.Warning("[SeedChooseModPlantButton] Config not found, using defaults");
                IsEnabled = true;
                ModPlantSeedType = 0;
            }

            var harmony = new Harmony("net.pvz.seedchoosemodplantbutton");
            harmony.PatchAll(typeof(pLMods).Assembly);
            Log.Info("[SeedChooseModPlantButton] Patch applied successfully");
        }

        [HarmonyPatch(typeof(SeedChooseScreen), "AddButton")]
        [HarmonyPostfix]
        public static class SeedChooseScreen_AddButton_Patch
        {
            public static void Postfix(SeedChooseScreen __instance)
            {
                if (!IsEnabled) return;

                try
                {
                    var mAppField = typeof(SeedChooseScreen).GetField("mApp", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mAppField == null)
                    {
                        Log.Warning("[SeedChooseModPlantButton] mApp field not found");
                        return;
                    }
                    var mApp = (LawnApp)mAppField.GetValue(__instance);

                    var imitaterButtonField = typeof(SeedChooseScreen).GetField("mImitaterButton", BindingFlags.Public | BindingFlags.Instance);
                    if (imitaterButtonField == null)
                    {
                        Log.Warning("[SeedChooseModPlantButton] mImitaterButton field not found");
                        return;
                    }
                    var imitaterButton = (GameButton)imitaterButtonField.GetValue(__instance);
                    if (imitaterButton == null)
                    {
                        Log.Warning("[SeedChooseModPlantButton] mImitaterButton is null");
                        return;
                    }

                    var modPlantButton = new GameButton();
                    modPlantButton.mApp = mApp;
                    modPlantButton.mId = 1000;
                    modPlantButton.mLabel = "";
                    
                    int btnX = imitaterButton.mX;
                    int btnY = imitaterButton.mY + imitaterButton.mHeight + 5;
                    int btnWidth = imitaterButton.mWidth;
                    int btnHeight = imitaterButton.mHeight;

                    modPlantButton.Resize(btnX, btnY, btnWidth, btnHeight);
                    modPlantButton.SetFont(mApp.mFontHolder.GetFont(TRes.FONT_BRIANNETOD12));
                    modPlantButton.mColors[0] = new Color(255, 255, 255, 255);
                    modPlantButton.mColors[1] = new Color(200, 200, 200, 255);
                    modPlantButton.mColors[2] = new Color(150, 150, 150, 255);
                    modPlantButton.mColors[3] = new Color(100, 100, 100, 255);
                    modPlantButton.mColors[4] = new Color(255, 255, 255, 255);

                    modPlantButton.mParentWidget = __instance;
                    modPlantButton.mVisible = true;
                    modPlantButton.mDisabled = false;

                    var buttonListField = typeof(SeedChooseScreen).GetField("mButtonList", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (buttonListField != null)
                    {
                        var buttonList = buttonListField.GetValue(__instance) as System.Collections.Generic.List<GameButton>;
                        if (buttonList != null)
                        {
                            buttonList.Add(modPlantButton);
                        }
                    }

                    var modPlantButtonField = typeof(SeedChooseScreen).GetField("mModPlantButton", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (modPlantButtonField == null)
                    {
                        typeof(SeedChooseScreen).GetField("mModPlantButton", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance) 
                            ?? typeof(SeedChooseScreen).GetField("mModPlantButton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        var extraButtonsField = typeof(SeedChooseScreen).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                        Log.Info($"[SeedChooseModPlantButton] Created mod plant button at ({btnX}, {btnY})");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[SeedChooseModPlantButton] AddButton postfix exception: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(SeedChooseScreen), "Draw")]
        [HarmonyPostfix]
        public static class SeedChooseScreen_Draw_Patch
        {
            public static void Postfix(SeedChooseScreen __instance, Graphics g)
            {
                if (!IsEnabled) return;

                try
                {
                    var imitaterButtonField = typeof(SeedChooseScreen).GetField("mImitaterButton", BindingFlags.Public | BindingFlags.Instance);
                    if (imitaterButtonField == null) return;
                    
                    var imitaterButton = (GameButton)imitaterButtonField.GetValue(__instance);
                    if (imitaterButton == null || !imitaterButton.mVisible) return;

                    var mAppField = typeof(SeedChooseScreen).GetField("mApp", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mAppField == null) return;
                    var mApp = (LawnApp)mAppField.GetValue(__instance);

                    int btnX = imitaterButton.mX;
                    int btnY = imitaterButton.mY + imitaterButton.mHeight + 5;
                    int btnWidth = imitaterButton.mWidth;
                    int btnHeight = imitaterButton.mHeight;

                    g.DrawImage(Resource.IMAGE_IMITATER_BUTTON, btnX, btnY, btnWidth, btnHeight);

                    SeedType seedType = (SeedType)ModPlantSeedType;
                    Image seedImage = mApp.GetSeedPacketImage(seedType);
                    if (seedImage != null)
                    {
                        int imgSize = btnHeight - 10;
                        int imgX = btnX + (btnWidth - imgSize) / 2;
                        int imgY = btnY + (btnHeight - imgSize) / 2;
                        g.DrawImage(seedImage, imgX, imgY, imgSize, imgSize);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[SeedChooseModPlantButton] Draw postfix exception: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(SeedChooseScreen), "MouseUp")]
        [HarmonyPostfix]
        public static class SeedChooseScreen_MouseUp_Patch
        {
            public static void Postfix(SeedChooseScreen __instance, int x, int y, int theClickTime)
            {
                if (!IsEnabled) return;

                try
                {
                    var imitaterButtonField = typeof(SeedChooseScreen).GetField("mImitaterButton", BindingFlags.Public | BindingFlags.Instance);
                    if (imitaterButtonField == null) return;
                    
                    var imitaterButton = (GameButton)imitaterButtonField.GetValue(__instance);
                    if (imitaterButton == null || !imitaterButton.mVisible) return;

                    int btnX = imitaterButton.mX;
                    int btnY = imitaterButton.mY + imitaterButton.mHeight + 5;
                    int btnWidth = imitaterButton.mWidth;
                    int btnHeight = imitaterButton.mHeight;

                    if (x >= btnX && x <= btnX + btnWidth && y >= btnY && y <= btnY + btnHeight)
                    {
                        var mAppField = typeof(SeedChooseScreen).GetField("mApp", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mAppField == null) return;
                        var mApp = (LawnApp)mAppField.GetValue(__instance);

                        var boardField = typeof(LawnApp).GetField("mBoard", BindingFlags.Public | BindingFlags.Instance);
                        if (boardField == null) return;
                        var board = (Board)boardField.GetValue(mApp);
                        if (board == null) return;

                        var modPlantDialogField = typeof(Board).GetField("mModPlantDialog", BindingFlags.Public | BindingFlags.Instance);
                        if (modPlantDialogField != null)
                        {
                            var dialog = modPlantDialogField.GetValue(board);
                            if (dialog != null)
                            {
                                var showDialogMethod = dialog.GetType().GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
                                if (showDialogMethod != null)
                                {
                                    showDialogMethod.Invoke(dialog, null);
                                    Log.Info("[SeedChooseModPlantButton] Opened ModPlantDialog");
                                    return;
                                }
                            }
                        }

                        Log.Warning("[SeedChooseModPlantButton] ModPlantDialog not found");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[SeedChooseModPlantButton] MouseUp postfix exception: {ex}");
                }
            }
        }
    }
}
