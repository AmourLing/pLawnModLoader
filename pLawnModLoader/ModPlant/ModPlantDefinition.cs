using Lawn;
using Sexy.TodLib;
using System;
using System.Collections.Generic;
using System.Text;
using Sexy;

namespace pLawnModLoader
{
    public class PlantDefinition
    {
        public PlantDefinition(SeedType aSeedType, Image[] aPlantImage, ReanimationType aReanimationType, int aPacketIndex, int aSeedCost, int aRefreshTime, PlantSubClass aSubClass, int aLaunchRate, string aPlantName)
        {
            mSeedType = aSeedType;
            mPlantImage = aPlantImage;
            mReanimationType = aReanimationType;
            mPacketIndex = aPacketIndex;
            mSeedCost = aSeedCost;
            mRefreshTime = aRefreshTime;
            mSubClass = aSubClass;
            mLaunchRate = aLaunchRate;
            mPlantName = aPlantName;
        }

        public SeedType mSeedType;

        public Image[] mPlantImage;

        public ReanimationType mReanimationType;

        public int mPacketIndex;

        public int mSeedCost;

        public int mRefreshTime;

        public PlantSubClass mSubClass;

        public int mLaunchRate;

        public string mPlantName;
    }
}