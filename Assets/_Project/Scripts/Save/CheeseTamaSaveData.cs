using System;
using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Environment;

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
        public List<string> claimedMilkGrowthRewardKeys = new List<string>();
        public List<SnackInventorySaveEntry> snackInventory = new List<SnackInventorySaveEntry>();
        public CareHistorySaveData careHistory = new CareHistorySaveData();
        public DailyCareSaveData dailyCare = new DailyCareSaveData();
        public EconomySaveData economy = new EconomySaveData();
        public MilkroomSessionSaveData milkroomSession = new MilkroomSessionSaveData();
        public CollectionSaveData collections = new CollectionSaveData();
        public GameSettingsSaveData settings = new GameSettingsSaveData();
        public string milkroomThemeId = "milkroom_morning";
        public OnboardingSaveData onboarding;
        public GrowthMilestoneSaveData growthMilestone;
        public EvolutionMilestoneSaveData evolutionMilestone;
        public RandomEventSaveData randomEvents = new RandomEventSaveData();
        public DecorationSaveData decorations = new DecorationSaveData();
        public StarRouteSaveData starRoute;
        public PlayMiniGameSaveData playMiniGames = new PlayMiniGameSaveData();
        public NewGameSetupSaveData newGameSetup;
        public FirstDayJourneySaveData firstDayJourney;
        public CheeseStarDeliverySaveData cheeseStarDelivery;
        public MemoryJournalSaveData memoryJournal;
        public FantasyPowderSaveData fantasyPowder;
        public StarLegacySaveData starLegacy;
        public NpcVisitSaveData npcVisits;
        public MilkBlendingSaveData milkBlending;
        public AutonomousLifeSaveData autonomousLife;
        public LateLevelGrowthSaveData lateLevelGrowth;
        public SleepScheduleSaveData sleepSchedule;
        public NpcRelationshipQuestSaveData npcRelationshipQuests;
        public NpcRelationshipEpisodeSaveData npcRelationshipEpisodes;
        public WeeklyCareJourneySaveData weeklyCareJourney;
        public DecorationWorkshopSaveData decorationWorkshop;
        public CollectionSetAlbumSaveData collectionSetAlbum;

        public void EnsureRuntimeDefaults()
        {
            cheeseTama ??= new CheeseTamaModel();
            cheeseTama.EnsureRuntimeDefaults();
            unlocks ??= new UnlockSaveData();
            milkGrowth ??= new List<MilkGrowthSaveEntry>();
            claimedMilkGrowthRewardKeys ??= new List<string>();
            snackInventory ??= new List<SnackInventorySaveEntry>();
            careHistory ??= new CareHistorySaveData();
            dailyCare ??= new DailyCareSaveData();
            economy ??= new EconomySaveData();
            milkroomSession ??= new MilkroomSessionSaveData();
            milkroomSession.EnsureRuntimeDefaults();
            collections ??= new CollectionSaveData();
            collections.EnsureRuntimeDefaults();
            settings ??= new GameSettingsSaveData();
            settings.EnsureRuntimeDefaults();
            onboarding ??= OnboardingSaveData.CreateCompletedForLegacySave();
            onboarding.EnsureRuntimeDefaults();
            growthMilestone ??= GrowthMilestoneSaveData.CreateAcknowledged(
                CheeseTamaGrowthStageCatalog.Resolve(cheeseTama));
            growthMilestone.EnsureRuntimeDefaults();
            evolutionMilestone ??= EvolutionMilestoneSaveData.CreateAcknowledged(cheeseTama.evolutionId);
            evolutionMilestone.EnsureRuntimeDefaults();
            randomEvents ??= new RandomEventSaveData();
            randomEvents.EnsureRuntimeDefaults();
            decorations ??= new DecorationSaveData();
            decorations.EnsureRuntimeDefaults();
            starRoute ??= StarRouteSaveData.CreateAcknowledged(unlocks.starMilkUnlocked);
            starRoute.EnsureRuntimeDefaults();
            playMiniGames ??= new PlayMiniGameSaveData();
            playMiniGames.EnsureRuntimeDefaults();
            newGameSetup ??= NewGameSetupSaveData.CreateCompletedForLegacySave();
            newGameSetup.EnsureRuntimeDefaults();
            firstDayJourney ??= FirstDayJourneySaveData.CreateCompletedForLegacySave();
            firstDayJourney.EnsureRuntimeDefaults();
            cheeseStarDelivery ??= new CheeseStarDeliverySaveData();
            cheeseStarDelivery.EnsureRuntimeDefaults();
            memoryJournal ??= new MemoryJournalSaveData();
            memoryJournal.EnsureRuntimeDefaults();
            fantasyPowder ??= new FantasyPowderSaveData();
            fantasyPowder.EnsureRuntimeDefaults();
            starLegacy ??= new StarLegacySaveData();
            starLegacy.EnsureRuntimeDefaults();
            npcVisits ??= new NpcVisitSaveData();
            npcVisits.EnsureRuntimeDefaults();
            milkBlending ??= new MilkBlendingSaveData();
            milkBlending.EnsureRuntimeDefaults();
            autonomousLife ??= new AutonomousLifeSaveData();
            autonomousLife.EnsureRuntimeDefaults();
            lateLevelGrowth ??= new LateLevelGrowthSaveData();
            sleepSchedule ??= new SleepScheduleSaveData();
            sleepSchedule.EnsureRuntimeDefaults(DateTimeOffset.Now);
            npcRelationshipQuests ??= new NpcRelationshipQuestSaveData();
            npcRelationshipQuests.EnsureRuntimeDefaults();
            npcRelationshipEpisodes ??= new NpcRelationshipEpisodeSaveData();
            npcRelationshipEpisodes.EnsureRuntimeDefaults();
            weeklyCareJourney ??= new WeeklyCareJourneySaveData();
            weeklyCareJourney.EnsureRuntimeDefaults();
            decorationWorkshop ??= new DecorationWorkshopSaveData();
            decorationWorkshop.EnsureRuntimeDefaults();
            collectionSetAlbum ??= new CollectionSetAlbumSaveData();
            collectionSetAlbum.EnsureRuntimeDefaults();
            milkroomThemeId = MilkroomThemeCatalog.Normalize(milkroomThemeId);
            if (!decorations.ContainsOwnedTheme(milkroomThemeId))
            {
                milkroomThemeId = MilkroomThemeController.MorningThemeId;
            }
        }
    }

    [Serializable]
    public sealed class StarRouteSaveData
    {
        public bool unlockAcknowledged;
        public string unlockedAtIso = string.Empty;

        public static StarRouteSaveData CreateAcknowledged(bool unlocked)
        {
            return new StarRouteSaveData
            {
                unlockAcknowledged = unlocked
            };
        }

        public void EnsureRuntimeDefaults()
        {
            unlockedAtIso ??= string.Empty;
        }
    }

    [Serializable]
    public sealed class PlayMiniGameSaveData
    {
        public int highestBouncyJumpScore;
        public int totalBouncyJumpSessions;
        public int totalBouncyJumpSuccesses;

        public void EnsureRuntimeDefaults()
        {
            highestBouncyJumpScore = Math.Max(0, highestBouncyJumpScore);
            totalBouncyJumpSessions = Math.Max(0, totalBouncyJumpSessions);
            totalBouncyJumpSuccesses = Math.Max(0, totalBouncyJumpSuccesses);
        }
    }

    [Serializable]
    public sealed class EvolutionMilestoneSaveData
    {
        public string acknowledgedEvolutionId = string.Empty;

        public static EvolutionMilestoneSaveData CreateAcknowledged(string evolutionId)
        {
            return new EvolutionMilestoneSaveData
            {
                acknowledgedEvolutionId = evolutionId ?? string.Empty
            };
        }

        public void EnsureRuntimeDefaults()
        {
            acknowledgedEvolutionId ??= string.Empty;
        }
    }

    [Serializable]
    public sealed class DecorationSaveData
    {
        public List<string> ownedItemIds = new List<string>();
        public List<string> ownedThemeIds = new List<string>();
        public string equippedWallId = "wall_cream";
        public string equippedFloorId = "floor_cream_rug";
        public string equippedAccentId = "accent_milk_bottle";
        public string equippedWindowId = "window_cream_curtain";
        public string equippedShelfId = "shelf_cheese_clock";
        public string equippedBedsideId = "bedside_milk_cushion";

        public void EnsureRuntimeDefaults()
        {
            ownedItemIds ??= new List<string>();
            ownedThemeIds ??= new List<string>();
            NormalizeOwnedThemes();
            AddOwnedDefault(DecorationSlot.Wall);
            AddOwnedDefault(DecorationSlot.Floor);
            AddOwnedDefault(DecorationSlot.Accent);
            AddOwnedDefault(DecorationSlot.Window);
            AddOwnedDefault(DecorationSlot.Shelf);
            AddOwnedDefault(DecorationSlot.Bedside);
            AddOwnedDefaultThemes();
            equippedWallId = NormalizeEquipped(equippedWallId, DecorationSlot.Wall);
            equippedFloorId = NormalizeEquipped(equippedFloorId, DecorationSlot.Floor);
            equippedAccentId = NormalizeEquipped(equippedAccentId, DecorationSlot.Accent);
            equippedWindowId = NormalizeEquipped(equippedWindowId, DecorationSlot.Window);
            equippedShelfId = NormalizeEquipped(equippedShelfId, DecorationSlot.Shelf);
            equippedBedsideId = NormalizeEquipped(equippedBedsideId, DecorationSlot.Bedside);
        }

        private void AddOwnedDefaultThemes()
        {
            var themes = MilkroomThemeCatalog.All;
            for (var index = 0; index < themes.Count; index += 1)
            {
                var definition = themes[index];
                if (definition.IsOwnedByDefault && !ContainsOwnedTheme(definition.Id))
                {
                    ownedThemeIds.Add(definition.Id);
                }
            }
        }

        private void NormalizeOwnedThemes()
        {
            var normalized = new List<string>(MilkroomThemeCatalog.All.Count);
            for (var index = 0; index < ownedThemeIds.Count; index += 1)
            {
                var definition = MilkroomThemeCatalog.Find(ownedThemeIds[index]);
                if (definition == null || Contains(normalized, definition.Id))
                {
                    continue;
                }

                normalized.Add(definition.Id);
            }

            ownedThemeIds = normalized;
        }

        private static bool Contains(List<string> values, string value)
        {
            for (var index = 0; index < values.Count; index += 1)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsOwnedTheme(string themeId)
        {
            if (string.IsNullOrWhiteSpace(themeId) || ownedThemeIds == null)
            {
                return false;
            }

            for (var index = 0; index < ownedThemeIds.Count; index += 1)
            {
                if (string.Equals(ownedThemeIds[index], themeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddOwnedDefault(DecorationSlot slot)
        {
            var definition = DecorationCatalog.GetDefault(slot);
            if (definition == null || ContainsOwned(definition.id))
            {
                return;
            }

            ownedItemIds.Add(definition.id);
        }

        private string NormalizeEquipped(string itemId, DecorationSlot slot)
        {
            var definition = DecorationCatalog.Find(itemId);
            if (definition != null && definition.slot == slot && ContainsOwned(definition.id))
            {
                return definition.id;
            }

            return DecorationCatalog.GetDefault(slot)?.id ?? string.Empty;
        }

        private bool ContainsOwned(string itemId)
        {
            for (var index = 0; index < ownedItemIds.Count; index += 1)
            {
                if (string.Equals(ownedItemIds[index], itemId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class GrowthMilestoneSaveData
    {
        public CheeseTamaGrowthStage acknowledgedStage = CheeseTamaGrowthStage.Egg;

        public static GrowthMilestoneSaveData CreateAcknowledged(CheeseTamaGrowthStage stage)
        {
            return new GrowthMilestoneSaveData
            {
                acknowledgedStage = stage
            };
        }

        public void EnsureRuntimeDefaults()
        {
            var value = (int)acknowledgedStage;
            if (value < (int)CheeseTamaGrowthStage.Egg || value > (int)CheeseTamaGrowthStage.Final)
            {
                acknowledgedStage = CheeseTamaGrowthStage.Egg;
            }
        }
    }

    [Serializable]
    public sealed class RandomEventSaveData
    {
        public const int MaximumChoiceReceipts = 64;

        public string dateKey = string.Empty;
        public int eventsToday;
        public string lastEventId = string.Empty;
        public string nextAllowedAtIso = string.Empty;
        public List<RandomEventHistorySaveEntry> history = new List<RandomEventHistorySaveEntry>();
        public PendingCareEventSaveData pendingEvent = new PendingCareEventSaveData();
        public List<CareEventChoiceReceiptSaveEntry> choiceReceipts =
            new List<CareEventChoiceReceiptSaveEntry>();

        public void EnsureRuntimeDefaults()
        {
            history ??= new List<RandomEventHistorySaveEntry>();
            dateKey ??= string.Empty;
            lastEventId ??= string.Empty;
            nextAllowedAtIso ??= string.Empty;
            eventsToday = Math.Max(0, eventsToday);
            pendingEvent ??= new PendingCareEventSaveData();
            pendingEvent.EnsureRuntimeDefaults();
            choiceReceipts ??= new List<CareEventChoiceReceiptSaveEntry>();
            for (var index = choiceReceipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = choiceReceipts[index];
                if (receipt == null || string.IsNullOrWhiteSpace(receipt.occurrenceId))
                {
                    choiceReceipts.RemoveAt(index);
                    continue;
                }

                receipt.EnsureRuntimeDefaults();
            }

            while (choiceReceipts.Count > MaximumChoiceReceipts)
            {
                choiceReceipts.RemoveAt(0);
            }
        }
    }

    [Serializable]
    public sealed class PendingCareEventSaveData
    {
        public string occurrenceId = string.Empty;
        public string eventId = string.Empty;
        public string title = string.Empty;
        public string message = string.Empty;
        public bool firstDiscovery;

        public bool HasValue => !string.IsNullOrWhiteSpace(occurrenceId)
            && !string.IsNullOrWhiteSpace(eventId);

        public void Set(CareEventResult result)
        {
            occurrenceId = result.occurrenceId ?? string.Empty;
            eventId = result.eventId ?? string.Empty;
            title = result.title ?? string.Empty;
            message = result.message ?? string.Empty;
            firstDiscovery = result.firstDiscovery;
        }

        public CareEventResult ToResult()
        {
            return HasValue
                ? new CareEventResult(
                    true,
                    occurrenceId,
                    eventId,
                    title,
                    message,
                    firstDiscovery)
                : CareEventResult.None();
        }

        public void Clear()
        {
            occurrenceId = string.Empty;
            eventId = string.Empty;
            title = string.Empty;
            message = string.Empty;
            firstDiscovery = false;
        }

        public void EnsureRuntimeDefaults()
        {
            occurrenceId ??= string.Empty;
            eventId ??= string.Empty;
            title ??= string.Empty;
            message ??= string.Empty;
            if (string.IsNullOrWhiteSpace(occurrenceId)
                || string.IsNullOrWhiteSpace(eventId))
            {
                Clear();
            }
        }
    }

    [Serializable]
    public sealed class CareEventChoiceReceiptSaveEntry
    {
        public string occurrenceId = string.Empty;
        public string eventId = string.Empty;
        public string choiceId = string.Empty;
        public string resolvedAtIso = string.Empty;

        public void EnsureRuntimeDefaults()
        {
            occurrenceId ??= string.Empty;
            eventId ??= string.Empty;
            choiceId ??= string.Empty;
            resolvedAtIso ??= string.Empty;
        }
    }

    [Serializable]
    public sealed class RandomEventHistorySaveEntry
    {
        public string eventId = string.Empty;
        public int totalOccurrences;
        public string lastOccurredAtIso = string.Empty;
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
        public int petSessions;
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

    public enum FirstMeetingOnboardingStep
    {
        Welcome = 0,
        // Reserved for saves created before naming was separated from the tutorial.
        LegacyNaming = 1,
        FeedMilk = 2,
        Care = 3,
        Collection = 4,
        Complete = 5
    }

    [Serializable]
    public sealed class OnboardingSaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public FirstMeetingOnboardingStep currentStep = FirstMeetingOnboardingStep.Welcome;
        public bool completed;
        public bool skipped;
        public bool replaying;
        public bool firstCollectionRewardGranted;

        public static OnboardingSaveData CreateForNewPlayer()
        {
            return new OnboardingSaveData
            {
                currentStep = FirstMeetingOnboardingStep.Welcome
            };
        }

        public static OnboardingSaveData CreateCompletedForLegacySave()
        {
            return new OnboardingSaveData
            {
                currentStep = FirstMeetingOnboardingStep.Complete,
                completed = true
            };
        }

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            if (!completed && currentStep == FirstMeetingOnboardingStep.LegacyNaming)
            {
                currentStep = FirstMeetingOnboardingStep.FeedMilk;
                changed = true;
            }

            if (completed || currentStep == FirstMeetingOnboardingStep.Complete)
            {
                changed |= currentStep != FirstMeetingOnboardingStep.Complete
                    || !completed
                    || replaying;
                currentStep = FirstMeetingOnboardingStep.Complete;
                completed = true;
                replaying = false;
                return changed;
            }

            var stepValue = (int)currentStep;
            if (stepValue < (int)FirstMeetingOnboardingStep.Welcome
                || stepValue > (int)FirstMeetingOnboardingStep.Collection)
            {
                currentStep = FirstMeetingOnboardingStep.Complete;
                completed = true;
                replaying = false;
                changed = true;
            }

            return changed;
        }
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
        public string lastMilkDropMiniGameRewardAtIso = string.Empty;

        public void EnsureRuntimeDefaults()
        {
            dateKey ??= string.Empty;
            currentSessionStartedAtIso ??= string.Empty;
            lastRewardAtIso ??= string.Empty;
            lastMilkDropMiniGameRewardAtIso ??= string.Empty;
            todaySeconds = Math.Max(0, todaySeconds);
            currentSessionSeconds = Math.Max(0, currentSessionSeconds);
            totalSeconds = Math.Max(0, totalSeconds);
            sessionsToday = Math.Max(0, sessionsToday);
            totalSessions = Math.Max(0, totalSessions);
            highestClaimedSessionMinute = Math.Max(0, highestClaimedSessionMinute);
            todayMilkDropCatches = Math.Max(0, todayMilkDropCatches);
            totalMilkDropCatches = Math.Max(0, totalMilkDropCatches);
        }
    }

    [Serializable]
    public sealed class GameSettingsSaveData
    {
        public const float MinUiScale = 0.9f;
        public const float MaxUiScale = 1.1f;
        public const float DefaultTextScale = 1f;
        public const float MediumTextScale = 1.25f;
        public const float LargeTextScale = 1.4f;

        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float effectVolume = 1f;
        public bool muteAudio;
        public bool fullScreen;
        public int targetFrameRate = 60;
        public float uiScale = 1f;
        public bool showCareTips = true;
        public int graphicsQualityPreset = (int)GraphicsQualityPreset.High;
        public float textScale = DefaultTextScale;
        public bool highContrastUi;
        public bool reduceMotion;
        public GameInputBindingSaveData inputBindings = new GameInputBindingSaveData();

        public static GameSettingsSaveData CreateDefault()
        {
            return new GameSettingsSaveData();
        }

        public void EnsureRuntimeDefaults()
        {
            masterVolume = Clamp(masterVolume, 0f, 1f);
            musicVolume = Clamp(musicVolume, 0f, 1f);
            effectVolume = Clamp(effectVolume, 0f, 1f);
            var normalizedUiScale = Clamp(uiScale <= 0f ? 1f : uiScale, MinUiScale, MaxUiScale);
            uiScale = Clamp((float)Math.Round(normalizedUiScale * 10f, MidpointRounding.AwayFromZero) / 10f, MinUiScale, MaxUiScale);
            targetFrameRate = targetFrameRate switch
            {
                30 => 30,
                120 => 120,
                _ => 60
            };
            graphicsQualityPreset = (int)GraphicsQualityCatalog.Normalize(graphicsQualityPreset);
            textScale = NormalizeTextScale(textScale);
            inputBindings ??= new GameInputBindingSaveData();
            CheeseTama.Gameplay.Input.GameInputBindingSystem.EnsureDefaults(inputBindings);
        }

        public static float NormalizeTextScale(float value)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value <= 0f
                || value < (DefaultTextScale + MediumTextScale) * 0.5f)
            {
                return DefaultTextScale;
            }

            return value < (MediumTextScale + LargeTextScale) * 0.5f
                ? MediumTextScale
                : LargeTextScale;
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
