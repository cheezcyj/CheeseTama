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
        public DailyCareSaveData dailyCare = new DailyCareSaveData();
        public EconomySaveData economy = new EconomySaveData();
        public MilkroomSessionSaveData milkroomSession = new MilkroomSessionSaveData();
        public CollectionSaveData collections = new CollectionSaveData();

        public void EnsureRuntimeDefaults()
        {
            cheeseTama ??= new CheeseTamaModel();
            cheeseTama.EnsureRuntimeDefaults();
            unlocks ??= new UnlockSaveData();
            milkGrowth ??= new List<MilkGrowthSaveEntry>();
            careHistory ??= new CareHistorySaveData();
            dailyCare ??= new DailyCareSaveData();
            economy ??= new EconomySaveData();
            milkroomSession ??= new MilkroomSessionSaveData();
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

    [Serializable]
    public sealed class DailyCareSaveData
    {
        public string dateKey = string.Empty;
        public int milkFeeds;
        public int snacksFed;
        public int playSessions;
        public int cleanings;
        public int rests;
        public int completedRoutineCount;
        public string lastCompletedDateKey = string.Empty;
        public string lastCompletedAtIso = string.Empty;
    }

    [Serializable]
    public sealed class EconomySaveData
    {
        public int milkCoins;
        public int milkDrops;
        public int starDrops;
        public int affectionPoints;
        public int collectionFragments;
    }

    [Serializable]
    public sealed class MilkroomSessionSaveData
    {
        public string dateKey = string.Empty;
        public int todaySeconds;
        public int currentSessionSeconds;
        public int totalSeconds;
        public int sessionsToday;
        public int totalSessions;
        public int highestClaimedSessionMinute;
        public int todayMilkDropCatches;
        public int totalMilkDropCatches;
        public string currentSessionStartedAtIso = string.Empty;
        public string lastRewardAtIso = string.Empty;
    }
}
