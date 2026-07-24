using Lawn;
using System.Collections.Generic;

namespace pLawnModLoader
{
    public static class ModProjectile
    {
        private static Dictionary<int, int> _peaKinds = new();   // 弹丸ID -> 豌豆类型(0~5)
        private static Dictionary<int, float> _fanVelY = new();  // 弹丸ID -> 垂直速度(扇形)
        private static Dictionary<int, int> _electricHits = new(); // 电击已命中僵尸集合(用位标记或HashSet)

        public static void MarkCustom(Projectile proj, int peaKind)
            => _peaKinds[proj.GetHashCode()] = peaKind;

        public static void MarkFan(Projectile proj, float velY)
            => _fanVelY[proj.GetHashCode()] = velY;

        public static int GetPeaKind(Projectile proj)
            => _peaKinds.TryGetValue(proj.GetHashCode(), out var kind) ? kind : -1;

        public static bool IsFan(Projectile proj) => _fanVelY.ContainsKey(proj.GetHashCode());

        public static float GetFanVelY(Projectile proj)
            => _fanVelY.TryGetValue(proj.GetHashCode(), out var v) ? v : 0;

        public static void Unregister(Projectile proj)
        {
            int key = proj.GetHashCode();
            _peaKinds.Remove(key);
            _fanVelY.Remove(key);
            _electricHits.Remove(key);
        }

        // 用于电击记录
        public static HashSet<int> GetElectricHitSet(Projectile proj)
        {
            int key = proj.GetHashCode();
            if (!_electricHits.ContainsKey(key))
                _electricHits[key] = 0; // 使用位标记或另行设计
            return null; // 实际应返回一个集合，可改用 Dictionary<int, HashSet<int>>
        }
    }
}