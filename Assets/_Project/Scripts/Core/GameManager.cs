using System;
using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Data;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Stats;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        private const string BasicMilkId = "basic_milk";
        private const string StarMilkId = "star_milk";
        private const string DailyRoutineCompleteEventId = "daily_routine_complete";
        private const string DailyRoutineThreeEventId = "daily_routine_3";
        private const string SessionFiveMinuteEventId = "session_5m";
        private const string SessionTenMinuteEventId = "session_10m";
        private const string SessionTwentyMinuteEventId = "session_20m";
        private const string SessionThirtyMinuteEventId = "session_30m";
        private const string MilkDropCatchEventId = "milk_drop_catch";

        [SerializeField] private DataRegistry dataRegistry;
        [SerializeField] private SaveManager saveManager;

        private readonly TimeProgressionSystem timeProgressionSystem = new TimeProgressionSystem();
        private readonly CollectionSystem collectionSystem = new CollectionSystem();
        private readonly HiddenCollectionSystem hiddenCollectionSystem = new HiddenCollectionSystem();
        private readonly MilkGrowthSystem milkGrowthSystem = new MilkGrowthSystem();
        private readonly RandomEventSystem randomEventSystem = new RandomEventSystem();
        private bool presenceSessionStarted;

        public static GameManager Instance { get; private set; }
        public DataRegistry DataRegistry => dataRegistry;
        public CheeseTamaSaveData CurrentSave { get; private set; }
        public CheeseTamaModel CurrentTama => CurrentSave?.cheeseTama;
        public TimeProgressionResult LastTimeProgression { get; private set; }
        public string SaveFilePath => saveManager != null ? saveManager.SaveFilePath : string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyGameObjectSafely(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            dataRegistry = dataRegistry != null ? dataRegistry : GetComponent<DataRegistry>();
            saveManager = saveManager != null ? saveManager : GetComponent<SaveManager>();
            LoadOrCreateGame();
        }

        public void LoadOrCreateGame()
        {
            dataRegistry = dataRegistry != null ? dataRegistry : GetComponent<DataRegistry>();
            saveManager = saveManager != null ? saveManager : GetComponent<SaveManager>();

            if (saveManager == null)
            {
                Debug.LogWarning("저장 관리자가 없습니다. 런타임 저장 데이터를 불러오지 못했습니다.");
                return;
            }

            CurrentSave = saveManager.LoadOrCreate();
            var dailyCareChanged = EnsureDailyCareDate();
            var sessionDateChanged = EnsureMilkroomSessionDate();
            presenceSessionStarted = false;
            LastTimeProgression = timeProgressionSystem.ApplyOfflineProgress(CurrentTama, DateTimeOffset.Now);
            if (LastTimeProgression.applied || dailyCareChanged || sessionDateChanged)
            {
                saveManager.Save(CurrentSave);
            }
        }

        public void ReloadGame()
        {
            LoadOrCreateGame();
        }

        public void ResetGame()
        {
            if (saveManager == null)
            {
                CurrentSave = SaveManager.CreateDefaultSave();
                return;
            }

            saveManager.DeleteSave();
            CurrentSave = saveManager.LoadOrCreate();
            LastTimeProgression = TimeProgressionResult.None();
            presenceSessionStarted = false;
        }

        public void SaveGame()
        {
            if (saveManager == null || CurrentSave == null)
            {
                return;
            }

            saveManager.Save(CurrentSave);
        }

        public TimeProgressionResult ApplyTimeSkipHours(int hours)
        {
            if (CurrentTama == null || hours <= 0)
            {
                LastTimeProgression = TimeProgressionResult.None();
                return LastTimeProgression;
            }

            LastTimeProgression = timeProgressionSystem.ApplyCareTicks(CurrentTama, hours);
            SaveGame();
            return LastTimeProgression;
        }

        public string TickMilkroomPresence(int seconds)
        {
            if (CurrentSave == null || seconds <= 0)
            {
                return string.Empty;
            }

            CurrentSave.EnsureRuntimeDefaults();
            EnsureMilkroomPresenceSession();

            var safeSeconds = Math.Max(1, seconds);
            var session = CurrentSave.milkroomSession;
            var previousMinute = session.currentSessionSeconds / 60;
            session.currentSessionSeconds += safeSeconds;
            session.todaySeconds += safeSeconds;
            session.totalSeconds += safeSeconds;
            var currentMinute = session.currentSessionSeconds / 60;

            var rewardMessage = GrantPresenceRewards(previousMinute, currentMinute);
            if (!string.IsNullOrWhiteSpace(rewardMessage) || currentMinute > previousMinute)
            {
                RefreshDerivedCollectionRecords();
                SaveGame();
            }

            return rewardMessage;
        }

        public string PlayMilkDropCatch()
        {
            if (CurrentSave == null)
            {
                return "치즈타마 저장 데이터를 불러오지 못했습니다.";
            }

            CurrentSave.EnsureRuntimeDefaults();
            EnsureMilkroomPresenceSession();

            var session = CurrentSave.milkroomSession;
            var stayBonus = Math.Min(3, session.currentSessionSeconds / 600);
            var coinGain = 4 + stayBonus;
            var dropGain = stayBonus >= 2 ? 2 : 1;

            CurrentSave.economy.milkCoins += coinGain;
            CurrentSave.economy.milkDrops += dropGain;
            session.todayMilkDropCatches += 1;
            session.totalMilkDropCatches += 1;

            if (CurrentTama != null && CurrentTama.stats != null)
            {
                CurrentTama.stats.mood += 5;
                CurrentTama.stats.affection += 2;
                CurrentTama.stats.sleepiness += 2;
                CurrentTama.stats.ClampAll();
            }

            AddUniqueRecord(CurrentSave.collections.events, MilkDropCatchEventId);
            return $"우유 방울을 모았습니다. 코인 +{coinGain}, 방울 +{dropGain}.";
        }

        public void RegisterMilkDiscovery(string milkId)
        {
            if (CurrentSave == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            collectionSystem.RegisterMilk(CurrentSave.collections, milkId);
            SaveGame();
        }

        public void RegisterCurrentEvolutionDiscovery()
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var evolutionId = string.IsNullOrWhiteSpace(CurrentTama.evolutionId)
                ? CurrentTama.form
                : CurrentTama.evolutionId;
            collectionSystem.RegisterEvolution(CurrentSave.collections, evolutionId);
            SaveGame();
        }

        public bool RegisterEventDiscovery(string eventId)
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var added = AddUniqueRecord(CurrentSave.collections.events, eventId);
            if (added)
            {
                SaveGame();
            }

            return added;
        }

        public bool IsMilkUnlocked(string milkId)
        {
            if (milkId == BasicMilkId)
            {
                return true;
            }

            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            return milkId == StarMilkId && CurrentSave.unlocks.starMilkUnlocked;
        }

        public bool UnlockStarMilk()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (CurrentSave.unlocks.starMilkUnlocked)
            {
                return false;
            }

            CurrentSave.unlocks.starMilkUnlocked = true;
            SaveGame();
            return true;
        }

        public MilkGrowthSaveEntry RegisterMilkGrowth(string milkId, int points)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return null;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var entry = milkGrowthSystem.AddGrowthPoints(CurrentSave.milkGrowth, milkId, points);
            if (entry != null)
            {
                CurrentTama.growthHistory.lastFedMilkId = milkId;
                CurrentTama.growthHistory.mostUsedMilkId = milkId;
                SaveGame();
            }

            return entry;
        }

        public MilkGrowthSaveEntry FindMilkGrowth(string milkId)
        {
            if (CurrentSave == null)
            {
                return null;
            }

            CurrentSave.EnsureRuntimeDefaults();
            return milkGrowthSystem.FindEntry(CurrentSave.milkGrowth, milkId);
        }

        public void RegisterCareAction(string actionId, int amount = 1)
        {
            if (CurrentSave == null || string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var safeAmount = Math.Max(1, amount);
            var history = CurrentSave.careHistory;
            history.totalCareActions += 1;
            history.lastCareActionId = actionId;
            history.lastCareActionAtIso = DateTimeOffset.Now.ToString("O");

            switch (actionId)
            {
                case "feed_milk":
                    history.milkFeeds += safeAmount;
                    break;
                case "feed_star_milk":
                    history.starMilkFeeds += safeAmount;
                    break;
                case "feed_snack":
                    history.snacksFed += safeAmount;
                    break;
                case "play":
                    history.playSessions += safeAmount;
                    break;
                case "clean":
                    history.cleanings += safeAmount;
                    break;
                case "rest":
                    history.rests += safeAmount;
                    break;
                case "wait_hour":
                    history.waitHours += safeAmount;
                    break;
            }
        }

        public bool RegisterDailyCareAction(string actionId)
        {
            if (CurrentSave == null || string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            EnsureDailyCareDate();
            var daily = CurrentSave.dailyCare;

            switch (actionId)
            {
                case "feed_milk":
                case "feed_star_milk":
                    daily.milkFeeds += 1;
                    break;
                case "feed_snack":
                    daily.snacksFed += 1;
                    break;
                case "play":
                    daily.playSessions += 1;
                    break;
                case "clean":
                    daily.cleanings += 1;
                    break;
                case "rest":
                    daily.rests += 1;
                    break;
            }

            if (!IsDailyRoutineComplete(daily) || daily.lastCompletedDateKey == daily.dateKey)
            {
                return false;
            }

            daily.completedRoutineCount += 1;
            daily.lastCompletedDateKey = daily.dateKey;
            daily.lastCompletedAtIso = DateTimeOffset.Now.ToString("O");
            AddUniqueRecord(CurrentSave.collections.events, DailyRoutineCompleteEventId);
            return true;
        }

        public void RefreshDerivedCollectionRecords()
        {
            if (CurrentSave == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var changed = false;
            changed |= EnsureDailyCareDate();
            changed |= EnsureMilkroomSessionDate();
            foreach (var entry in CurrentSave.milkGrowth)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.milkId))
                {
                    continue;
                }

                changed |= AddUniqueRecord(CurrentSave.collections.milk, entry.milkId);
                var normalizedLevel = milkGrowthSystem.FindEntry(CurrentSave.milkGrowth, entry.milkId)?.growthLevel ?? entry.growthLevel;
                if (entry.growthLevel != normalizedLevel)
                {
                    entry.growthLevel = normalizedLevel;
                    changed = true;
                }

                changed |= AddMilkGrowthMilestoneRecords(entry.milkId, entry.growthLevel);
            }

            changed |= AddCareMilestoneRecords(CurrentSave.careHistory);
            changed |= AddDailyCareMilestoneRecords(CurrentSave.dailyCare);
            changed |= AddPresenceMilestoneRecords(CurrentSave.milkroomSession);

            if (CurrentSave.unlocks.starMilkUnlocked)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.milk, StarMilkId);
                changed |= AddUniqueRecord(CurrentSave.collections.events, "star_milk_unlocked");
            }

            if (CurrentTama != null && CurrentTama.isHatched)
            {
                var evolutionId = string.IsNullOrWhiteSpace(CurrentTama.evolutionId)
                    ? CurrentTama.form
                    : CurrentTama.evolutionId;
                changed |= AddUniqueRecord(CurrentSave.collections.evolution, evolutionId);
            }

            changed |= UnlockHiddenCollectionRecords();

            if (changed)
            {
                SaveGame();
            }
        }

        public CareEventResult TryRollCareEvent()
        {
            return randomEventSystem.RollCareEvent(CurrentTama);
        }

        public CareEventResult ForceCareEvent()
        {
            return randomEventSystem.RollCareEvent(CurrentTama, true);
        }

        private bool UnlockHiddenCollectionRecords()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            var changed = false;
            var now = DateTimeOffset.Now;
            var collections = CurrentSave.collections;

            if (CurrentTama != null && CurrentTama.isHatched)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "first_soft_hatch", now);
            }

            if (CurrentSave.unlocks.starMilkUnlocked)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "star_milk_keeper", now);
            }

            if (collections.events != null && collections.events.Count >= 3)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "milkroom_listener", now);
            }

            if (collections.events != null && collections.events.Contains("cheese_snack_fed"))
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "first_snack_bite", now);
            }

            if (CurrentSave.careHistory != null && CurrentSave.careHistory.totalCareActions >= 10)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "gentle_caretaker", now);
            }

            if (CurrentSave.careHistory != null && CurrentSave.careHistory.cleanings >= 3)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "tidy_keeper", now);
            }

            if (CurrentSave.careHistory != null && CurrentSave.careHistory.playSessions >= 3)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "playful_friend", now);
            }

            if (CurrentSave.dailyCare != null && CurrentSave.dailyCare.completedRoutineCount >= 3)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "daily_regular", now);
            }

            if (CurrentSave.milkroomSession != null && CurrentSave.milkroomSession.totalSeconds >= 1800)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "patient_guest", now);
            }

            if (CurrentSave.milkroomSession != null && CurrentSave.milkroomSession.totalMilkDropCatches >= 5)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "drop_listener", now);
            }

            if (CurrentTama != null
                && CurrentTama.isHatched
                && CurrentTama.stats != null
                && CurrentTama.stats.hunger >= 70
                && CurrentTama.stats.mood >= 70
                && CurrentTama.stats.cleanliness >= 70
                && CurrentTama.stats.sleepiness <= 35
                && CurrentTama.stats.health >= 80)
            {
                changed |= hiddenCollectionSystem.Unlock(collections, "warm_balance", now);
            }

            return changed;
        }

        private bool AddCareMilestoneRecords(CareHistorySaveData history)
        {
            if (history == null)
            {
                return false;
            }

            var changed = false;
            changed |= AddThresholdRecord(history.totalCareActions, 5, "care_total_5");
            changed |= AddThresholdRecord(history.totalCareActions, 15, "care_total_15");
            changed |= AddThresholdRecord(history.milkFeeds, 5, "milk_feeds_5");
            changed |= AddThresholdRecord(history.starMilkFeeds, 3, "star_milk_feeds_3");
            changed |= AddThresholdRecord(history.snacksFed, 3, "snacks_fed_3");
            changed |= AddThresholdRecord(history.playSessions, 3, "play_sessions_3");
            changed |= AddThresholdRecord(history.cleanings, 3, "cleanings_3");
            changed |= AddThresholdRecord(history.rests, 3, "rests_3");
            changed |= AddThresholdRecord(history.waitHours, 3, "wait_hours_3");
            return changed;
        }

        private bool AddDailyCareMilestoneRecords(DailyCareSaveData daily)
        {
            if (daily == null)
            {
                return false;
            }

            var changed = false;
            if (daily.completedRoutineCount >= 1)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, DailyRoutineCompleteEventId);
            }

            if (daily.completedRoutineCount >= 3)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, DailyRoutineThreeEventId);
            }

            return changed;
        }

        private bool AddPresenceMilestoneRecords(MilkroomSessionSaveData session)
        {
            if (session == null)
            {
                return false;
            }

            var changed = false;
            changed |= AddThresholdRecord(session.highestClaimedSessionMinute, 5, SessionFiveMinuteEventId);
            changed |= AddThresholdRecord(session.highestClaimedSessionMinute, 10, SessionTenMinuteEventId);
            changed |= AddThresholdRecord(session.highestClaimedSessionMinute, 20, SessionTwentyMinuteEventId);
            changed |= AddThresholdRecord(session.highestClaimedSessionMinute, 30, SessionThirtyMinuteEventId);
            changed |= AddThresholdRecord(session.todaySeconds, 600, "daily_presence_10m");
            changed |= AddThresholdRecord(session.todaySeconds, 1800, "daily_presence_30m");
            changed |= AddThresholdRecord(session.totalMilkDropCatches, 1, MilkDropCatchEventId);
            changed |= AddThresholdRecord(session.totalMilkDropCatches, 5, "milk_drop_catch_5");
            changed |= AddThresholdRecord(session.totalMilkDropCatches, 10, "milk_drop_catch_10");
            return changed;
        }

        private string GrantPresenceRewards(int previousMinute, int currentMinute)
        {
            var message = string.Empty;
            message = CombineMessages(message, TryGrantPresenceReward(previousMinute, currentMinute, 5, 5, 2, 0, "5분 체류 보상"));
            message = CombineMessages(message, TryGrantPresenceReward(previousMinute, currentMinute, 10, 10, 4, 0, "10분 체류 보상"));
            message = CombineMessages(message, TryGrantPresenceReward(previousMinute, currentMinute, 20, 20, 8, 1, "20분 체류 보상"));
            message = CombineMessages(message, TryGrantPresenceReward(previousMinute, currentMinute, 30, 33, 12, 2, "30분 체류 보상"));
            return message;
        }

        private string TryGrantPresenceReward(
            int previousMinute,
            int currentMinute,
            int thresholdMinute,
            int milkCoins,
            int milkDrops,
            int collectionFragments,
            string message)
        {
            var session = CurrentSave?.milkroomSession;
            if (session == null
                || previousMinute >= thresholdMinute
                || currentMinute < thresholdMinute
                || session.highestClaimedSessionMinute >= thresholdMinute)
            {
                return string.Empty;
            }

            CurrentSave.economy.milkCoins += milkCoins;
            CurrentSave.economy.milkDrops += milkDrops;
            CurrentSave.economy.collectionFragments += collectionFragments;
            session.highestClaimedSessionMinute = thresholdMinute;
            session.lastRewardAtIso = DateTimeOffset.Now.ToString("O");

            var fragmentMessage = collectionFragments > 0
                ? $", 도감 조각 +{collectionFragments}"
                : string.Empty;
            return $"{message}: 코인 +{milkCoins}, 우유 방울 +{milkDrops}{fragmentMessage}.";
        }

        private bool EnsureDailyCareDate()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var daily = CurrentSave.dailyCare;
            var todayKey = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            if (daily.dateKey == todayKey)
            {
                return false;
            }

            daily.dateKey = todayKey;
            daily.milkFeeds = 0;
            daily.snacksFed = 0;
            daily.playSessions = 0;
            daily.cleanings = 0;
            daily.rests = 0;
            return true;
        }

        private bool EnsureMilkroomSessionDate()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var session = CurrentSave.milkroomSession;
            var todayKey = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            if (session.dateKey == todayKey)
            {
                return false;
            }

            session.dateKey = todayKey;
            session.todaySeconds = 0;
            session.currentSessionSeconds = 0;
            session.sessionsToday = 0;
            session.highestClaimedSessionMinute = 0;
            session.todayMilkDropCatches = 0;
            session.currentSessionStartedAtIso = string.Empty;
            presenceSessionStarted = false;
            return true;
        }

        private void EnsureMilkroomPresenceSession()
        {
            EnsureMilkroomSessionDate();
            if (presenceSessionStarted || CurrentSave == null)
            {
                return;
            }

            var session = CurrentSave.milkroomSession;
            session.currentSessionSeconds = 0;
            session.highestClaimedSessionMinute = 0;
            session.currentSessionStartedAtIso = DateTimeOffset.Now.ToString("O");
            session.sessionsToday += 1;
            session.totalSessions += 1;
            presenceSessionStarted = true;
        }

        private static bool IsDailyRoutineComplete(DailyCareSaveData daily)
        {
            return daily != null
                && daily.milkFeeds >= 1
                && daily.playSessions >= 1
                && daily.cleanings >= 1
                && daily.rests >= 1;
        }

        private static string CombineMessages(string primary, string secondary)
        {
            if (string.IsNullOrWhiteSpace(primary))
            {
                return secondary ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(secondary))
            {
                return primary;
            }

            return $"{primary} {secondary}";
        }

        private bool AddThresholdRecord(int value, int threshold, string eventId)
        {
            return value >= threshold && AddUniqueRecord(CurrentSave.collections.events, eventId);
        }

        private bool AddMilkGrowthMilestoneRecords(string milkId, int growthLevel)
        {
            var changed = false;
            for (var level = 1; level <= growthLevel; level++)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, $"{milkId}_growth_lv_{level}");
            }

            return changed;
        }

        private static bool AddUniqueRecord(ICollection<string> records, string id)
        {
            if (records == null || string.IsNullOrWhiteSpace(id) || records.Contains(id))
            {
                return false;
            }

            records.Add(id);
            return true;
        }

        private static void DestroyGameObjectSafely(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }
    }
}
