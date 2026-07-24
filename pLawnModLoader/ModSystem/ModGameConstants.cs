using Lawn;

namespace pLawnModLoader
{
    public static class ModGameConstants
    {
        // 游戏原始植物种类数量（可根据实际调整）
        public const int ORIGINAL_PLANT_COUNT = 50;

        // 模组植物起始 SeedType 偏移（与 ModPlant 中的自增逻辑配合）
        public const int MOD_SEED_TYPE_OFFSET = (int)SeedType.ZombieImp + 1;
    }
}