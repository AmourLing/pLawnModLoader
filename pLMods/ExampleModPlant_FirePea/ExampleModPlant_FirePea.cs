using Lawn;
using pLawnModLoader;
using Sexy;
using Sexy.TodLib;
using System;

namespace ExampleModPlant_FirePea
{
    // ==================== 配置类 ====================
    public class FirePeaConfig
    {
        public int Cost { get; set; } = 150;
        public int RefreshTime { get; set; } = 750;
        public int LaunchRate { get; set; } = 150;
        public string DisplayName { get; set; } = "火焰豌豆";
        public string ReanimFile { get; set; } = null; // 可自行设置动画文件
    }

    public class ModFirePea : ModPlant
    {
        private readonly FirePeaConfig _config;

        public ModFirePea(FirePeaConfig config) : base()
        {
            _config = config ?? new FirePeaConfig();
            this.Register();
        }

        public override string mPlantName => _config.DisplayName;
        public override int mSeedCost => _config.Cost;
        public override int mRefreshTime => _config.RefreshTime;
        public override int mLaunchRate => _config.LaunchRate;
        public override string mReanimationFileName => _config.ReanimFile;

        public override Image[] mPlantImage => new Image[0];

        public override void OnPlantInitialize(Plant plant, Board board, int gridX, int gridY, SeedType imitaterType)
        {
            // 初始化状态与冷却
            StartPacketCooldown(board, mSeedType);
            PlantStateHelper.AlignBody(plant);
        }

        public override void Fire(Plant plant, Board board, Zombie target, int row, PlantWeapon weapon)
        {
            try
            {
                int peaKind = 6; // 自定义豌豆类型标识（在 ModProjectile 中记录）
                ProjectileType projType = ProjectileType.Pea;
                Projectile proj = board.AddProjectile(
                    plant.mX + 45,
                    (int)(plant.mY + 10),
                    plant.mRenderOrder - 1,
                    row,
                    projType
                );
                proj.mDamageRangeFlags = plant.GetDamageRangeFlags(PlantWeapon.Primary);
                proj.mFromPlant = plant.mSeedType;
                ModProjectile.MarkCustom(proj, peaKind);
            }
            catch (Exception) { }
        }

        public override void OnDie(Plant plant, Board board)
        {
            PlantStateHelper.RemoveState<object>(plant);
        }
    }
}