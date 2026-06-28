using System;
using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Gameplay;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class CheeseTamaSaveData
    {
        public string version = "0.1.0";
        public string playerId = "local_player";
        public CheeseTamaModel cheeseTama = new CheeseTamaModel();
        public UnlockSaveData unlocks = new UnlockSaveData();
        public List<MilkGrowthSaveEntry> milkGrowth = new List<MilkGrowthSaveEntry>();
        public CollectionSaveData collections = new CollectionSaveData();

        public void EnsureRuntimeDefaults()
        {
            cheeseTama ??= new CheeseTamaModel();
            unlocks ??= new UnlockSaveData();
            milkGrowth ??= new List<MilkGrowthSaveEntry>();
            collections ??= new CollectionSaveData();
            collections.EnsureRuntimeDefaults();
        }
    }

    [Serializable]
    public sealed class MilkGrowthSaveEntry
    {
        public string milkId;
        public int growthLevel;
        public int growthPoints;
    }
}
