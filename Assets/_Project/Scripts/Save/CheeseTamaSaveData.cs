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
        public CareHistorySaveData careHistory = new CareHistorySaveData();
        public CollectionSaveData collections = new CollectionSaveData();

        public void EnsureRuntimeDefaults()
        {
            cheeseTama ??= new CheeseTamaModel();
            cheeseTama.EnsureRuntimeDefaults();
            unlocks ??= new UnlockSaveData();
            milkGrowth ??= new List<MilkGrowthSaveEntry>();
            careHistory ??= new CareHistorySaveData();
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

    [Serializable]
    public sealed class CareHistorySaveData
    {
        public int totalCareActions;
        public int milkFeeds;
        public int starMilkFeeds;
        public int snacksFed;
        public int playSessions;
        public int cleanings;
        public int rests;
        public int waitHours;
        public string lastCareActionId = string.Empty;
        public string lastCareActionAtIso = string.Empty;
    }
}
