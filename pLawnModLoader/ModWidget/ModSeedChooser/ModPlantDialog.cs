using HarmonyLib;
using Lawn;
using Sexy;
using Sexy.TodLib;
using System;
using System.Reflection;

namespace pLawnModLoader
{
    /// <summary>
    /// 模组使用的植物选择对话框。
    /// 特点：
    /// 1. 从 GatlingPea 开始显示可选植物。
    /// 2. 图标固定显示为 Peashooter。
    /// 3. 独立于主卡槽和模仿者，可重复选择同一植物。
    /// 4. 点击即选中并关闭。
    /// </summary>
    public class ModPlantDialog : LawnDialog, SeedPacketsWidgetListener
    {
        private readonly Action<SeedType> onSelected;
        public SeedPacketsWidget mSeedPacketsWidget;
        public ScrollWidget mScrollWidget;

        // 定义起始种子类型
        private const SeedType START_SEED_TYPE = SeedType.Gatlingpea;

        // 定义结束种子类型 (通常到 ExplodeONut 之前，或者根据游戏版本调整)
        private const SeedType END_SEED_TYPE = SeedType.ExplodeONut;

        public ModPlantDialog(Action<SeedType> onSelectedCallback)
            : base(GlobalStaticVars.gLawnApp, null, 50, true,
                   "[CHOOSE_A_PLANT]", "",
                   "[DIALOG_BUTTON_OK]", 3)
        {
            this.onSelected = onSelectedCallback;

            // 计算对话框大小，使用与 ImitaterDialog 类似的尺寸
            CalcSize(Constants.ImitaterDialog_Size.X, Constants.ImitaterDialog_Size.Y);

            // 初始化种子包组件
            // 参数: theApp, theNumberOfRows, theIsImitaters, theListener
            // 注意：这里 theIsImitaters 设为 false，因为我们要显示普通植物图标，但逻辑上我们手动控制显示范围
            mSeedPacketsWidget = new SeedPacketsWidget(mApp, 10, false, this);

            mScrollWidget = new ScrollWidget();
            AddWidget(mScrollWidget);
            mScrollWidget.AddWidget(mSeedPacketsWidget);

            // 调整滚动区域大小
            mScrollWidget.Resize(
                mWidth / 2 - mSeedPacketsWidget.mWidth / 2 - Constants.ImitaterDialog_ScrollWidget_Offset_X,
                Constants.ImitaterDialog_ScrollWidget_Y,
                mSeedPacketsWidget.mWidth + Constants.ImitaterDialog_ScrollWidget_ExtraWidth,
                Constants.ImitaterDialog_Height
            );

            mScrollWidget.EnableIndicators(AtlasResources.IMAGE_SCROLL_INDICATOR);
            mSeedPacketsWidget.Move(0, 0);
            mClip = false;

            Type type = Type.GetType("Sexy.TodLib.TodStringFile");
            MethodInfo MethodInfo = type.GetMethod("TodStringTranslate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            // 将“确定”按钮改为“取消”，因为点击种子即确认
            mLawnYesButton.mLabel = MethodInfo.Invoke(null, new object[] { "[DIALOG_BUTTON_CANCEL]" }) as string;

            // 默认滚动位置：如果拥有起始种子，则滚动到它
            if (mApp.HasSeedType(START_SEED_TYPE))
            {
                int x = 0, y = 0;
                mSeedPacketsWidget.GetSeedPosition(START_SEED_TYPE, ref x, ref y);
                mScrollWidget.SetScrollOffset(0, y);
            }
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, true);
            base.Dispose();
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            DeferOverlay();
        }

        public override void DrawOverlay(Graphics g)
        {
            g.SetColor(new SexyColor(16, 16, 33));
            g.SetColorizeImages(true);

            // 绘制顶部渐变遮罩
            if (mSeedPacketsWidget.mY < 0)
            {
                g.DrawImage(AtlasResources.IMAGE_ALMANAC_PLANTS_TOPGRADIENT,
                    mScrollWidget.mX, mScrollWidget.mY + (int)Constants.InvertAndScale(-2f),
                    (int)Constants.InvertAndScale(178f), (int)Constants.InvertAndScale(12f));
            }

            // 绘制底部渐变遮罩
            if (mSeedPacketsWidget.mY + mSeedPacketsWidget.mHeight > mScrollWidget.mHeight)
            {
                g.DrawImage(AtlasResources.IMAGE_ALMANAC_PLANTS_BOTTOMGRADIENT,
                    mScrollWidget.mX + (int)Constants.InvertAndScale(-2f),
                    mScrollWidget.mY + Constants.ImitaterDialog_BottomGradient_Y,
                    (int)Constants.InvertAndScale(180f), (int)Constants.InvertAndScale(12f));
            }

            g.SetColorizeImages(false);

            // 【关键修改】：强制在对话框左上角或标题附近绘制 Peashooter 的图标
            // 这里模仿 Almanac 或 Dialog 常见的做法，在标题栏左侧画一个小图标
            // 坐标需要根据实际对话框布局微调，这里假设在标题文字左侧
            float iconX = mX + 15;
            float iconY = mY + 15;

            // 绘制豌豆射手图标
            SeedPacket.DrawSmallSeedPacket(g, iconX, iconY, SeedType.Peashooter, SeedType.None, 0f, 255, false, false, true, false);
        }

        /// <summary>
        /// 当用户点击种子时触发
        /// </summary>
        public virtual void SeedSelected(SeedType theSeedType)
        {
            // 检查种子是否有效
            if (theSeedType != SeedType.None)
            {
                // 【关键修改】：移除 SeedNotAllowedToPick 检查，允许重复选择
                // 仅检查玩家是否拥有该种子（已解锁）
                if (mApp.HasSeedType(theSeedType))
                {
                    // 触发回调
                    onSelected?.Invoke(theSeedType);

                    // 关闭对话框
                    mApp.KillDialog(mId);
                }
                else
                {
                    // 如果没解锁，播放错误音效
                    mApp.PlaySample(Resources.SOUND_BUZZER);
                }
            }
        }

        /// <summary>
        /// 静态方法：显示对话框
        /// </summary>
        public static void ShowDialog(Action<SeedType> onSelected)
        {
            var dialog = new ModPlantDialog(onSelected);
            LawnApp app = GlobalStaticVars.gLawnApp;

            // 添加对话框
            app.AddDialog(dialog.mId, dialog);

            // 居中显示
            dialog.Resize(
                (app.mWidth - dialog.mWidth) / 2,
                (app.mHeight - dialog.mHeight) / 2,
                dialog.mWidth,
                dialog.mHeight
            );

            // 设置焦点
            app.mWidgetManager.SetFocus(dialog);
        }
    }
}