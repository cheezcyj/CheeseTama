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

        [SerializeField] private DataRegistry dataRegistry;
        [SerializeField] private SaveManager saveManager;

        private readonly TimeProgressionSystem timeProgressionSystem = new TimeProgressionSystem();
        private readonly CollectionSystem collectionSystem = new CollectionSystem();
        private readonly HiddenCollectionSystem hiddenCollectionSystem = new HiddenCollectionSystem();
        private readonly MilkGrowthSystem milkGrowthSystem = new MilkGrowthSystem();
        private readonly RandomEventSystem randomEventSystem = new RandomEventSystem();

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
                Destroy(gameObject);
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
            if (saveManager == null)
            {
                Debug.LogWarning("SaveManager is missing. Runtime save data was not loaded.");
                return;
            }

            CurrentSave = saveManager.LoadOrCreate();
            LastTimeProgression = timeProgressionSystem.ApplyOfflineProgress(CurrentTama, DateTimeOffset.Now);
            if (LastTimeProgression.applied)
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

        public void RefreshDerivedCollectionRecords()
        {
            if (CurrentSave == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var changed = false;
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
    }
}
