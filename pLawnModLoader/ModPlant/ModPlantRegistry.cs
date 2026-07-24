using System.Collections.Generic;
using Lawn;

namespace pLawnModLoader
{
    public static class ModPlantRegistry
    {
        private static readonly List<ModPlant> _registeredPlants = new();

        public static IReadOnlyList<ModPlant> RegisteredPlants => _registeredPlants;

        /// <summary>
        /// 注册一个模组植物实例（模组在 pLMods.Apply 中调用）
        /// </summary>
        public static void Register(ModPlant plant)
        {
            if (plant == null) return;
            if (!_registeredPlants.Contains(plant))
                _registeredPlants.Add(plant);
        }

        /// <summary>
        /// 获取所有已注册的 SeedType（用于后置补丁扩容）
        /// </summary>
        public static IEnumerable<SeedType> GetRegisteredSeedTypes()
        {
            foreach (var p in _registeredPlants)
                yield return p.mSeedType;
        }
    }
}