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
        public List<SnackInventorySaveEntry> snackInventory = new List<SnackInventorySaveEntry>();
        public CareHistorySaveData careHistory = new CareHistorySaveData();
        public DailyCareSaveData dailyCare = new DailyCareSaveData();
        public EconomySaveData economy = new EconomySaveData();
        public MilkroomSessionSaveData milkroomSession = new MilkroomSessionSaveData();
        public CollectionSaveData collections = new CollectionSaveData();
        public GameSettingsSaveData settings = new GameSettingsSaveData();
        public string milkroomThemeId = "milkroom_morning";

        public void EnsureRuntimeDefaults()
        {
            cheeseTama ??= new CheeseTamaModel();
            cheeseTama.EnsureRuntimeDefaults();
            unlocks ??= new UnlockSaveData();
            milkGrowth ??= new List<MilkGrowthSaveEntry>();
            snackInventory ??= new List<SnackInventorySaveEntry>();
            careHistory ??= new CareHistorySaveData();
            dailyCare ??= new DailyCareSaveData();
            economy ??= new EconomySaveData();
            milkroomSession ??= new MilkroomSessionSaveData();
            collections ??= new CollectionSaveData();
            collections.EnsureRuntimeDefaults();
            settings ??= new GameSettingsSaveData();
            settings.EnsureRuntimeDefaults();
            if (string.IsNullOrWhiteSpace(milkroomThemeId))
            {
                milkroomThemeId = "milkroom_morning";
            }
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
    public sealed class SnackInventorySaveEntry
    {
        public string snackId;
        public int quantity;
    }

    [Serializable]
    public sealed class CareHistorySaveData
    {
        public int totalCareActions;
        public int milkFeeds;
        public int starMilkFeeds;
        public int snacksFed;
        public int cookings;
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
        public const int EatingGoal = 3;
        public const int CookingGoal = 2;
        public const int PlayGoal = 3;
        public const int CleanGoal = 2;
        public const int RestGoal = 2;

        public string dateKey = string.Empty;
        public int milkFeeds;
        public int snacksFed;
        public int cookings;
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

    [Serializable]
    public sealed class GameSettingsSaveData
    {
        public const float MinUiScale = 0.9f;
        public const float MaxUiScale = 1.1f;

        public float masterVolume = 1f;
        public bool muteAudio;
        public bool fullScreen;
        public int targetFrameRate = 60;
        public float uiScale = 1f;
        public bool showCareTips = true;

        public static GameSettingsSaveData CreateDefault()
        {
            return new GameSettingsSaveData();
        }

        public void EnsureRuntimeDefaults()
        {
            masterVolume = Clamp(masterVolume, 0f, 1f);
            var normalizedUiScale = Clamp(uiScale <= 0f ? 1f : uiScale, MinUiScale, MaxUiScale);
            uiScale = Clamp((float)Math.Round(normalizedUiScale * 10f, MidpointRounding.AwayFromZero) / 10f, MinUiScale, MaxUiScale);
            targetFrameRate = targetFrameRate switch
            {
                30 => 30,
                120 => 120,
                _ => 60
            };
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
