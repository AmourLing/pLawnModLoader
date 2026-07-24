using Lawn;
using Sexy;
using Sexy.TodLib;
using System.Threading;

namespace pLawnModLoader
{
    public abstract class ModPlant
    {
        private static int _nextModIndex = (int)SeedType.ZombieImp;
        private static int _nextReanimIndex = (int)ReanimationType.NumReanims;

        public virtual SeedType mSeedType { get; protected set; }
        public virtual ReanimationType mReanimationType { get; protected set; }

        protected ModPlant(int? fixedSeedType = null, int? fixedReanimType = null)
        {
            if (fixedSeedType.HasValue)
                mSeedType = (SeedType)fixedSeedType.Value;
            else
                mSeedType = (SeedType)Interlocked.Increment(ref _nextModIndex);

            if (fixedReanimType.HasValue)
                mReanimationType = (ReanimationType)fixedReanimType.Value;
            else
                mReanimationType = (ReanimationType)Interlocked.Increment(ref _nextReanimIndex);
        }

        protected void StartPacketCooldown(Board board, SeedType seedType)
        {
            var bank = board.mSeedBank;
            if (bank == null) return;
            for (int i = 0; i < bank.mNumPackets; i++)
            {
                var packet = bank.mSeedPackets[i];
                if ((int)packet.mPacketType == (int)seedType)
                {
                    packet.mRefreshing = true;
                    packet.mRefreshTime = 750;
                    packet.mRefreshCounter = 0;
                    packet.mActive = false;
                    break;
                }
            }
        }

        protected ModPlant()
        {
            int index = Interlocked.Increment(ref _nextModIndex);
            mSeedType = (SeedType)index;
            mReanimationType = (ReanimationType)Interlocked.Increment(ref _nextReanimIndex);
        }

        public virtual PlantDefinition GetPlantDefinition()
        {
            return new PlantDefinition(
                mSeedType,
                mPlantImage,
                mReanimationType,
                mPacketIndex,
                mSeedCost,
                mRefreshTime,
                mSubClass,
                mLaunchRate,
                mPlantName
            );
        }

        public virtual string mReanimationFileName => null;
        public virtual Image[] mPlantImage => null;
        public virtual int mPacketIndex => 0;
        public virtual int mSeedCost => 0;
        public virtual int mRefreshTime => 0;
        public virtual PlantSubClass mSubClass => PlantSubClass.Normal;
        public virtual int mLaunchRate => 0;
        public virtual string mPlantName => "CustomPlant";

        public virtual void Update(Plant plant, Board board)
        {
        }

        public virtual void Fire(Plant plant, Board board, Zombie target, int row, PlantWeapon weapon)
        {
        }

        public virtual void OnPlantInitialize(Plant plant, Board board, int gridX, int gridY, SeedType imitaterType)
        {
        }

        public virtual void OnDie(Plant plant, Board board)
        {
        }

        public static bool IsModSeedType(SeedType type)
        {
            return (int)type > (int)SeedType.ZombieImp;
        }
    }
}