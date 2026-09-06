using System;
using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Story;
using CheeseTama.Environment;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class CheeseTamaSaveData
    {
        public const int CurrentSchemaRevision = SaveSchemaMigrationHub.CurrentRevision;

        private static readonly LifeChapterSystem LifeChapterNormalizer =
            new LifeChapterSystem();
        private static DreamStorySeasonSystem dreamStoryNormalizer;
        private static MilkroomInvestigationSystem investigationNormalizer;

        public string version = "0.1.0";
        public int schemaRevision = CurrentSchemaRevision;
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
        public DecorationPlacementSaveData decorationPlacement =
            new DecorationPlacementSaveData();
        public StarRouteSaveData starRoute;
        public PlayMiniGameSaveData playMiniGames = new PlayMiniGameSaveData();
        public MiniGameMasterySaveData miniGameMastery = new MiniGameMasterySaveData();
        public NewGameSetupSaveData newGameSetup;
        public FirstDayJourneySaveData firstDayJourney;
        public CheeseStarDeliverySaveData cheeseStarDelivery;
        public MemoryJournalSaveData memoryJournal;
        public FantasyPowderSaveData fantasyPowder;
        public StarLegacySaveData starLegacy;
        public StarLineageSaveData starLineage;
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
        public PostgameResearchSaveData postgameResearch;
        public MilkroomMysterySaveData milkroomMystery;
        public NpcAfterstorySaveData npcAfterstory;
        public DreamStorySeasonSaveData dreamStorySeason;
        public LifeChapterSaveData lifeChapters;
        public MilkroomInvestigationSaveData milkroomInvestigation;

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
            decorationPlacement ??= DecorationPlacementSaveData.MigrateLegacySlots(
                decorations.equippedAccentId,
                decorations.equippedShelfId,
                decorations.equippedBedsideId);
            decorationPlacement.EnsureRuntimeDefaults(
                decorations.equippedAccentId,
                decorations.equippedShelfId,
                decorations.equippedBedsideId);
            starRoute ??= StarRouteSaveData.CreateAcknowledged(unlocks.starMilkUnlocked);
            starRoute.EnsureRuntimeDefaults();
            playMiniGames ??= new PlayMiniGameSaveData();
            playMiniGames.EnsureRuntimeDefaults();
            miniGameMastery ??= new MiniGameMasterySaveData();
            miniGameMastery.EnsureRuntimeDefaults();
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
            starLineage ??= new StarLineageSaveData();
            starLineage.EnsureRuntimeDefaults();
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
            postgameResearch ??= new PostgameResearchSaveData();
            postgameResearch.EnsureRuntimeDefaults();
            milkroomMystery ??= new MilkroomMysterySaveData();
            milkroomMystery.EnsureRuntimeDefaults();
            npcAfterstory ??= new NpcAfterstorySaveData();
            npcAfterstory.EnsureRuntimeDefaults();
            dreamStorySeason ??= new DreamStorySeasonSaveData();
            GetDreamStoryNormalizer().NormalizeState(dreamStorySeason);
            lifeChapters ??= LifeChapterSaveData.CreateForLegacyPlayer();
            LifeChapterNormalizer.NormalizeState(lifeChapters);
            milkroomInvestigation ??= new MilkroomInvestigationSaveData();
            GetInvestigationNormalizer().NormalizeState(milkroomInvestigation);
            milkroomThemeId = MilkroomThemeCatalog.Normalize(milkroomThemeId);
            if (!decorations.ContainsOwnedTheme(milkroomThemeId))
            {
                milkroomThemeId = MilkroomThemeController.MorningThemeId;
            }
        }

        private static DreamStorySeasonSystem GetDreamStoryNormalizer()
        {
            if (dreamStoryNormalizer == null || !dreamStoryNormalizer.ContentAvailable)
            {
                dreamStoryNormalizer = new DreamStorySeasonSystem();
            }

            return dreamStoryNormalizer;
        }

        private static MilkroomInvestigationSystem GetInvestigationNormalizer()
        {
            if (investigationNormalizer == null || !investigationNormalizer.ContentAvailable)
            {
                investigationNormalizer = new MilkroomInvestigationSystem();
            }

            return investigationNormalizer;
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
    public sealed class MiniGameSessionSaveEntry
    {
        public string sessionId = string.Empty;
        public string gameId = string.Empty;
        public int score;
        public int successes;
        public string completedAtIso = string.Empty;

        public void EnsureRuntimeDefaults()
        {
            sessionId = Normalize(sessionId);
            gameId = Normalize(gameId);
            completedAtIso = Normalize(completedAtIso);
            score = Clamp(score, 0, PlayMiniGameSaveData.MaximumRecordedScore);
            successes = Clamp(successes, 0, PlayMiniGameSaveData.MaximumRecordedSuccesses);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }

    [Serializable]
    public sealed class PlayMiniGameSaveData
    {
        public const int MaximumRecentSessions = 5;
        public const int MaximumRecordedScore = 1000000000;
        public const int MaximumRecordedSuccesses = 1000000000;
        public const int MaximumRecordedSessions = 1000000000;

        public int highestBouncyJumpScore;
        public int totalBouncyJumpSessions;
        public int totalBouncyJumpSuccesses;
        public int highestMilkDropScore;
        public int totalMilkDropSessions;
        public int totalMilkDropSuccesses;
        public int highestCleaningScore;
        public int totalCleaningSessions;
        public int totalCleaningSuccesses;
        public int highestBlueBallScore;
        public int totalBlueBallSessions;
        public int totalBlueBallSuccesses;
        public List<MiniGameSessionSaveEntry> recentSessions = new List<MiniGameSessionSaveEntry>();

        public void EnsureRuntimeDefaults()
        {
            highestBouncyJumpScore = ClampScore(highestBouncyJumpScore);
            totalBouncyJumpSessions = ClampSessions(totalBouncyJumpSessions);
            totalBouncyJumpSuccesses = ClampSuccesses(totalBouncyJumpSuccesses);
            highestMilkDropScore = ClampScore(highestMilkDropScore);
            totalMilkDropSessions = ClampSessions(totalMilkDropSessions);
            totalMilkDropSuccesses = ClampSuccesses(totalMilkDropSuccesses);
            highestCleaningScore = ClampScore(highestCleaningScore);
            totalCleaningSessions = ClampSessions(totalCleaningSessions);
            totalCleaningSuccesses = ClampSuccesses(totalCleaningSuccesses);
            highestBlueBallScore = ClampScore(highestBlueBallScore);
            totalBlueBallSessions = ClampSessions(totalBlueBallSessions);
            totalBlueBallSuccesses = ClampSuccesses(totalBlueBallSuccesses);
            NormalizeRecentSessions();
        }

        public bool RecordSession(
            string sessionId,
            string gameId,
            int score,
            int successes,
            string completedAtIso)
        {
            EnsureRuntimeDefaults();
            var normalizedSessionId = Normalize(sessionId);
            var normalizedGameId = Normalize(gameId);
            if (string.IsNullOrEmpty(normalizedSessionId)
                || !IsKnownGameId(normalizedGameId)
                || ContainsSession(normalizedSessionId))
            {
                return false;
            }

            var safeScore = ClampScore(score);
            var safeSuccesses = ClampSuccesses(successes);
            switch (normalizedGameId)
            {
                case "milk_drop":
                    highestMilkDropScore = Math.Max(highestMilkDropScore, safeScore);
                    totalMilkDropSessions = SaturatingIncrement(totalMilkDropSessions);
                    totalMilkDropSuccesses = SaturatingAdd(totalMilkDropSuccesses, safeSuccesses);
                    break;
                case "cleaning":
                    highestCleaningScore = Math.Max(highestCleaningScore, safeScore);
                    totalCleaningSessions = SaturatingIncrement(totalCleaningSessions);
                    totalCleaningSuccesses = SaturatingAdd(totalCleaningSuccesses, safeSuccesses);
                    break;
                case "blue_ball":
                    highestBlueBallScore = Math.Max(highestBlueBallScore, safeScore);
                    totalBlueBallSessions = SaturatingIncrement(totalBlueBallSessions);
                    totalBlueBallSuccesses = SaturatingAdd(totalBlueBallSuccesses, safeSuccesses);
                    break;
                default:
                    highestBouncyJumpScore = Math.Max(highestBouncyJumpScore, safeScore);
                    totalBouncyJumpSessions = SaturatingIncrement(totalBouncyJumpSessions);
                    totalBouncyJumpSuccesses = SaturatingAdd(totalBouncyJumpSuccesses, safeSuccesses);
                    break;
            }

            recentSessions.Insert(0, new MiniGameSessionSaveEntry
            {
                sessionId = normalizedSessionId,
                gameId = normalizedGameId,
                score = safeScore,
                successes = safeSuccesses,
                completedAtIso = Normalize(completedAtIso)
            });
            TrimRecentSessions();
            return true;
        }

        private void NormalizeRecentSessions()
        {
            var source = recentSessions ?? new List<MiniGameSessionSaveEntry>();
            var normalized = new List<MiniGameSessionSaveEntry>(
                Math.Min(source.Count, MaximumRecentSessions));
            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                index < source.Count && normalized.Count < MaximumRecentSessions;
                index += 1)
            {
                var entry = source[index];
                entry?.EnsureRuntimeDefaults();
                if (entry == null
                    || string.IsNullOrEmpty(entry.sessionId)
                    || !IsKnownGameId(entry.gameId)
                    || !knownIds.Add(entry.sessionId))
                {
                    continue;
                }

                normalized.Add(entry);
            }

            recentSessions = normalized;
        }

        private void TrimRecentSessions()
        {
            if (recentSessions.Count > MaximumRecentSessions)
            {
                recentSessions.RemoveRange(
                    MaximumRecentSessions,
                    recentSessions.Count - MaximumRecentSessions);
            }
        }

        private bool ContainsSession(string sessionId)
        {
            for (var index = 0; index < recentSessions.Count; index += 1)
            {
                if (string.Equals(recentSessions[index]?.sessionId, sessionId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsKnownGameId(string gameId)
        {
            return string.Equals(gameId, "milk_drop", StringComparison.Ordinal)
                || string.Equals(gameId, "cleaning", StringComparison.Ordinal)
                || string.Equals(gameId, "bouncy_jump", StringComparison.Ordinal)
                || string.Equals(gameId, "blue_ball", StringComparison.Ordinal);
        }

        private static int ClampScore(int value)
        {
            return Math.Max(0, Math.Min(MaximumRecordedScore, value));
        }

        private static int ClampSuccesses(int value)
        {
            return Math.Max(0, Math.Min(MaximumRecordedSuccesses, value));
        }

        private static int ClampSessions(int value)
        {
            return Math.Max(0, Math.Min(MaximumRecordedSessions, value));
        }

        private static int SaturatingIncrement(int value)
        {
            return value >= MaximumRecordedSessions ? MaximumRecordedSessions : value + 1;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            var result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result >= MaximumRecordedSuccesses ? MaximumRecordedSuccesses : (int)result;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
        public string equippedFloorId = string.Empty;
        public string equippedAccentId = string.Empty;
        public string equippedWindowId = string.Empty;
        public string equippedShelfId = string.Empty;
        public string equippedBedsideId = string.Empty;

        public void EnsureRuntimeDefaults()
        {
            ownedItemIds ??= new List<string>();
            ownedThemeIds ??= new List<string>();
            NormalizeOwnedThemes();
            AddOwnedDefault(DecorationSlot.Wall);
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
