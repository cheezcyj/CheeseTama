using System;
using System.Collections.Generic;
using System.IO;
using CheeseTama.Collections;
using CheeseTama.Data;
using CheeseTama.Environment;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Care;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Feeding;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Gameplay.Journey;
using CheeseTama.Gameplay.Deliveries;
using CheeseTama.Gameplay.Memories;
using CheeseTama.Gameplay.HiddenRecipes;
using CheeseTama.Gameplay.Guidance;
using CheeseTama.Gameplay.Records;
using CheeseTama.Gameplay.Research;
using CheeseTama.Gameplay.Reset;
using CheeseTama.Collections.HiddenCareers;
using CheeseTama.Gameplay.Bond;
using CheeseTama.Gameplay.Stats;
using CheeseTama.Gameplay.Sleep;
using CheeseTama.Gameplay.Story;
using CheeseTama.Gameplay.Weekly;
using CheeseTama.Save;
using CheeseTama.Platform;
using CheeseTama.Platform.Accounts;
using CheeseTama.Utilities;
using CheeseTama.UI;
using UnityEngine;

namespace CheeseTama.Core
{
    public sealed class CloudSaveApplyResult
    {
        private CloudSaveApplyResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static CloudSaveApplyResult Success(string message)
        {
            return new CloudSaveApplyResult(true, message);
        }

        public static CloudSaveApplyResult Failure(string message)
        {
            return new CloudSaveApplyResult(false, message);
        }
    }

    public sealed partial class GameManager : MonoBehaviour
    {
        private const string DailyRoutineCompleteEventId = "daily_routine_complete";
        private const string DailyRoutineThreeEventId = "daily_routine_3";
        private const string SessionFiveMinuteEventId = "session_5m";
        private const string SessionTenMinuteEventId = "session_10m";
        private const string SessionTwentyMinuteEventId = "session_20m";
        private const string SessionThirtyMinuteEventId = "session_30m";
        private const string MilkDropCatchEventId = "milk_drop_catch";
        private const string BouncyJumpEventId = "bouncy_jump";
        private const string BlueBallEventId = "blue_ball_play";
        private const string StarRouteEnteredEventId = "star_route_entered";
        private const int MaximumCareEventCardsPerDay = 5;
        private const int CareEventGlobalCooldownMinutes = 5;
        private const int CareEventPerIdCooldownMinutes = 30;

        public const string CloudSaveApplyConfirmationPhrase = "USE CLOUD";

        public const int DailyRoutineMilkCoinReward = 20;
        public const int DailyRoutineMilkDropReward = 5;
        public const int DailyRoutineCollectionFragmentReward = 1;
        public const string DailyRoutineRewardMessage = "오늘 돌봄 루틴 완료! 코인 +20, 우유방울 +5, 도감조각 +1.";

        [SerializeField] private DataRegistry dataRegistry;
        [SerializeField] private SaveManager saveManager;

        private readonly TimeProgressionSystem timeProgressionSystem = new TimeProgressionSystem();
        private readonly CollectionSystem collectionSystem = new CollectionSystem();
        private readonly HiddenCollectionSystem hiddenCollectionSystem = new HiddenCollectionSystem();
        private readonly MilkGrowthSystem milkGrowthSystem = new MilkGrowthSystem();
        private readonly EvolutionSystem evolutionSystem = new EvolutionSystem();
        private readonly RandomEventSystem randomEventSystem = new RandomEventSystem();
        private readonly SeasonalCareEventSystem seasonalCareEventSystem =
            new SeasonalCareEventSystem();
        private readonly CareEventChoiceSystem careEventChoiceSystem = new CareEventChoiceSystem();
        private readonly MemoryJournalSystem memoryJournalSystem = new MemoryJournalSystem();
        private readonly FantasyPowderHiddenRecipeSystem fantasyPowderSystem =
            new FantasyPowderHiddenRecipeSystem();
        private readonly StarEggEmmentalEvolutionSystem starLegacyEvolutionSystem =
            new StarEggEmmentalEvolutionSystem();
        private readonly StarLineageSystem starLineageSystem = new StarLineageSystem();
        private readonly FinalMaturationCycleSystem finalMaturationCycleSystem =
            new FinalMaturationCycleSystem();
        private readonly HiddenCareerCardSystem hiddenCareerCardSystem =
            new HiddenCareerCardSystem();
        private readonly BondReactionSystem bondReactionSystem = new BondReactionSystem();
        private readonly NpcVisitSystem npcVisitSystem = new NpcVisitSystem();
        private readonly NpcRelationshipQuestSystem npcRelationshipQuestSystem =
            new NpcRelationshipQuestSystem();
        private readonly NpcRelationshipEpisodeSystem npcRelationshipEpisodeSystem =
            new NpcRelationshipEpisodeSystem();
        private readonly NpcAfterstorySystem npcAfterstorySystem = new NpcAfterstorySystem();
        private MilkroomMysterySystem milkroomMysterySystem;
        private DreamStorySeasonSystem dreamStorySeasonSystem;
        private MilkroomInvestigationSystem milkroomInvestigationSystem;
        private readonly MilkBlendingSystem milkBlendingSystem = new MilkBlendingSystem();
        private readonly MilkroomThemeUnlockSystem milkroomThemeUnlockSystem =
            new MilkroomThemeUnlockSystem();
        private readonly SleepScheduleSystem sleepScheduleSystem = new SleepScheduleSystem();
        private readonly LateLevelGrowthSystem lateLevelGrowthSystem = new LateLevelGrowthSystem();
        private readonly WeeklyCareJourneySystem weeklyCareJourneySystem =
            new WeeklyCareJourneySystem();
        private readonly DecorationWorkshopSystem decorationWorkshopSystem =
            new DecorationWorkshopSystem();
        private readonly CollectionSetAlbumSystem collectionSetAlbumSystem =
            new CollectionSetAlbumSystem();
        private readonly LifeRecordsSystem lifeRecordsSystem = new LifeRecordsSystem();
        private readonly MiniGameMasterySystem miniGameMasterySystem = new MiniGameMasterySystem();
        private readonly PostgameResearchSystem postgameResearchSystem =
            new PostgameResearchSystem();
        private readonly PostgameResearchProfileEffectSystem postgameResearchProfileEffectSystem =
            new PostgameResearchProfileEffectSystem();
        private readonly LifeChapterSystem lifeChapterSystem = new LifeChapterSystem();
        private readonly CloudSaveSyncCoordinator cloudSaveSyncCoordinator =
            new CloudSaveSyncCoordinator();
        private bool presenceSessionStarted;
        private bool applicationPaused;
        private bool applicationHasFocus = true;
        private bool applicationSuspended;
        private bool applicationQuitting;
        private ReturnSummaryData pendingReturnSummary;
        private GrowthMilestoneData pendingGrowthMilestone;
        private CareEventResult pendingCareEvent;
        private EvolutionMilestoneData pendingEvolutionMilestone;
        private MilkGrowthMilestoneRewardResult lastMilkGrowthMilestoneReward = MilkGrowthMilestoneRewardResult.None;
        private CloudApplyGuard pendingCloudApplyGuard;

        public static GameManager Instance { get; private set; }
        public event Action SaveDataReplaced;
        public event Action LocalAccountChanged;
        public event Action<string> CareActionRegistered;
        public event Action DailyRoutineCompleted;
        public event Action<ReturnSummaryData> ReturnSummaryAvailable;
        public event Action<GrowthMilestoneData> GrowthMilestoneAvailable;
        public event Action<CareEventResult> CareEventAvailable;
        public event Action<MilkGrowthMilestoneRewardResult> MilkGrowthMilestoneRewardGranted;
        public event Action<EvolutionMilestoneData> EvolutionMilestoneAvailable;
        public event Action DecorationChanged;
        public event Action StarRouteUnlockAvailable;
        public event Action FirstDayJourneyChanged;
        public event Action CheeseStarDeliveryChanged;
        public event Action MemoryJournalChanged;
        public event Action FantasyPowderChanged;
        public event Action StarLegacyChanged;
        public event Action StarLineageChanged;
        public event Action HiddenCareerCardChanged;
        public event Action<NpcVisitOffer> NpcVisitAvailable;
        public event Action<NpcRelationshipEpisodeChoiceResult> NpcRelationshipEpisodeCompleted;
        public event Action<MilkBlendResult> MilkBlendingChanged;
        public event Action SleepScheduleChanged;
        public event Action JourneyHubChanged;
        public event Action<PostgameResearchUnlockResult> PostgameResearchChanged;
        public event Action<NpcAfterstoryChoiceResult> NpcAfterstoryChanged;
        public event Action<NpcKeepsakeDisplaySnapshot> NpcKeepsakeDisplayChanged;
        public event Action<MilkroomMysteryChoiceResult> MilkroomMysteryChanged;
        public event Action<DreamStoryChoiceResult> DreamStorySeasonChanged;
        public event Action<MiniGameMasteryReconcileResult> MiniGameMasteryChanged;
        public event Action LifeChapterChanged;
        public event Action MilkroomInvestigationChanged;

        public DataRegistry DataRegistry => dataRegistry;
        public CheeseTamaSaveData CurrentSave { get; private set; }
        public CheeseTamaModel CurrentTama => CurrentSave?.cheeseTama;
        public TimeProgressionResult LastTimeProgression { get; private set; }
        public string SaveFilePath => saveManager != null ? saveManager.SaveFilePath : string.Empty;
        public LocalAccountSnapshot LocalAccount
        {
            get
            {
                LocalAccountRuntime.EnsureInitialized();
                return LocalAccountRuntime.Snapshot;
            }
        }
        public bool HasPendingReturnSummary => pendingReturnSummary != null;
        public bool HasPendingGrowthMilestone => pendingGrowthMilestone != null;
        public bool HasPendingCareEvent => pendingCareEvent.occurred;
        public MilkGrowthMilestoneRewardResult LastMilkGrowthMilestoneReward => lastMilkGrowthMilestoneReward;
        public bool HasPendingEvolutionMilestone => pendingEvolutionMilestone != null;
        public bool IsSleepScheduleActive => CurrentSave?.sleepSchedule?.HasActiveSession == true;
        public bool HasPendingStarRouteUnlock => CurrentSave?.starRoute != null
            && CurrentSave.unlocks != null
            && CurrentSave.unlocks.starMilkUnlocked
            && !CurrentSave.starRoute.unlockAcknowledged;
        public SaveRecoveryReport LastSaveRecoveryReport => saveManager?.LastRecoveryReport
            ?? SaveRecoveryReport.NoRecovery;

        public LifeRecordsSnapshot GetLifeRecordsSnapshot()
        {
            return lifeRecordsSystem.BuildSnapshot(CurrentSave);
        }

        public MiniGameMasteryBoardSnapshot GetMiniGameMasterySnapshot()
        {
            return miniGameMasterySystem.BuildSnapshot(
                GetLifeRecordsSnapshot(),
                CurrentSave?.miniGameMastery);
        }

        private MiniGameMasteryReconcileResult ReconcileMiniGameMastery(
            DateTimeOffset achievedAt)
        {
            var result = miniGameMasterySystem.Reconcile(
                GetLifeRecordsSnapshot(),
                CurrentSave?.miniGameMastery,
                achievedAt);
            if (CurrentSave == null)
            {
                return result;
            }

            CurrentSave.miniGameMastery = result.State;
            if (result.StateChanged)
            {
                MiniGameMasteryChanged?.Invoke(result);
                JourneyHubChanged?.Invoke();
            }

            return result;
        }

        public LifeChapterSnapshot GetPendingLifeChapterSnapshot()
        {
            return GetPendingLifeChapterSnapshot(DateTimeOffset.Now);
        }

        public LifeChapterSnapshot GetPendingLifeChapterSnapshot(DateTimeOffset now)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return LifeChapterSnapshot.CreateHidden();
            }

            CurrentSave.EnsureRuntimeDefaults();
            var snapshot = lifeChapterSystem.BuildPendingSnapshot(
                CurrentSave.lifeChapters,
                CurrentTama.level,
                now,
                out var stateChanged);
            if (stateChanged)
            {
                SaveGame();
            }

            return snapshot;
        }

        public LifeChapterSnapshot PeekPendingLifeChapterSnapshot()
        {
            return CurrentSave?.lifeChapters == null || CurrentTama == null
                ? LifeChapterSnapshot.CreateHidden()
                : lifeChapterSystem.BuildPendingSnapshotReadOnly(
                    CurrentSave.lifeChapters,
                    CurrentTama.level);
        }

        public LifeChapterChoiceResult TryChooseLifeChapter(
            string chapterId,
            string choiceId)
        {
            return TryChooseLifeChapter(chapterId, choiceId, DateTimeOffset.Now);
        }

        public LifeChapterChoiceResult TryChooseLifeChapter(
            string chapterId,
            string choiceId,
            DateTimeOffset completedAt)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return lifeChapterSystem.TryChoose(
                    null,
                    0,
                    chapterId,
                    choiceId,
                    completedAt,
                    _ => false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var memoryRecorded = false;
            var result = lifeChapterSystem.TryChoose(
                CurrentSave.lifeChapters,
                CurrentTama.level,
                chapterId,
                choiceId,
                completedAt,
                payloads => CommitLifeChapterPayloads(payloads, out memoryRecorded));
            if (!result.Applied)
            {
                if (result.StateChanged)
                {
                    SaveGame();
                    LifeChapterChanged?.Invoke();
                }

                return result;
            }

            SaveGame();
            if (memoryRecorded)
            {
                MemoryJournalChanged?.Invoke();
            }

            LifeChapterChanged?.Invoke();
            JourneyHubChanged?.Invoke();
            return result;
        }

        private bool CommitLifeChapterPayloads(
            LifeChapterChoicePayloadBundle payloads,
            out bool memoryRecorded)
        {
            memoryRecorded = false;
            if (payloads?.Memory == null
                || payloads.Codex == null
                || payloads.Reward == null
                || CurrentSave?.economy == null
                || CurrentSave.collections?.events == null
                || CurrentSave.memoryJournal == null
                || CurrentTama == null
                || string.IsNullOrWhiteSpace(payloads.Memory.RecordId)
                || string.IsNullOrWhiteSpace(payloads.Codex.RecordId))
            {
                return false;
            }

            var reward = payloads.Reward;
            var economy = CurrentSave.economy;
            var codexAlreadyRecorded = ContainsOrdinal(
                CurrentSave.collections.events,
                payloads.Codex.RecordId);
            var memoryAlreadyRecorded = HasMemorySource(payloads.Memory.RecordId);
            var rewardAlreadyRecorded = codexAlreadyRecorded || memoryAlreadyRecorded;
            var validReward = reward.Amount >= 0;
            switch (reward.Kind)
            {
                case LifeChapterRewardKind.MilkCoins:
                    validReward &= rewardAlreadyRecorded
                        || CanAddMysteryReward(economy.milkCoins, reward.Amount);
                    break;
                case LifeChapterRewardKind.MilkDrops:
                    validReward &= rewardAlreadyRecorded
                        || CanAddMysteryReward(economy.milkDrops, reward.Amount);
                    break;
                case LifeChapterRewardKind.CollectionFragments:
                    validReward &= rewardAlreadyRecorded
                        || CanAddMysteryReward(
                            economy.collectionFragments,
                            reward.Amount);
                    break;
                case LifeChapterRewardKind.None:
                    validReward &= reward.Amount == 0;
                    break;
                default:
                    validReward = false;
                    break;
            }

            if (!validReward
                || (!codexAlreadyRecorded && CurrentSave.collections.events.Count >= 512))
            {
                return false;
            }

            if (!memoryAlreadyRecorded)
            {
                memoryRecorded = memoryJournalSystem.TryRecord(
                    CurrentSave.memoryJournal,
                    new MemoryJournalDraft(
                        MemoryJournalKind.Story,
                        payloads.Memory.RecordId,
                        payloads.ReceiptId,
                        payloads.Memory.DetailId,
                        payloads.CompletedAt,
                        CurrentTama.name,
                        CurrentTama.form,
                        payloads.Memory.Title,
                        payloads.Memory.Detail,
                        true),
                    out _);
                if (!memoryRecorded)
                {
                    return false;
                }
            }

            AddUniqueRecord(CurrentSave.collections.events, payloads.Codex.RecordId);
            if (rewardAlreadyRecorded)
            {
                return true;
            }

            switch (reward.Kind)
            {
                case LifeChapterRewardKind.MilkCoins:
                    economy.milkCoins += reward.Amount;
                    break;
                case LifeChapterRewardKind.MilkDrops:
                    economy.milkDrops += reward.Amount;
                    break;
                case LifeChapterRewardKind.CollectionFragments:
                    economy.collectionFragments += reward.Amount;
                    break;
            }

            return true;
        }

        public bool TryGetPendingNpcVisit(out NpcVisitOffer offer)
        {
            offer = null;
            return CurrentSave != null
                && npcVisitSystem.TryGetPending(
                    CurrentSave.npcVisits,
                    BuildNpcVisitContext(),
                    out offer);
        }

        public bool TryQueueNpcVisit()
        {
            return TryQueueNpcVisit(
                DateTimeOffset.Now,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                $"npc_visit_{Guid.NewGuid():N}",
                false,
                out _);
        }

        public bool TryQueueNpcVisit(
            DateTimeOffset now,
            double chanceRoll,
            double visitorRoll,
            string occurrenceId,
            bool force,
            out NpcVisitOffer offer)
        {
            offer = null;
            if (CurrentSave == null || CurrentTama == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!npcVisitSystem.TryQueueVisit(
                    CurrentSave.npcVisits,
                    CurrentTama,
                    CurrentSave.careHistory,
                    now,
                    chanceRoll,
                    visitorRoll,
                    occurrenceId,
                    force,
                    BuildNpcVisitContext(),
                    out offer))
            {
                return false;
            }

            if (offer != null && offer.StateChanged)
            {
                NpcVisitAvailable?.Invoke(offer);
            }

            return offer != null && offer.HasOffer;
        }

        public bool TryResolvePendingNpcVisit(
            string occurrenceId,
            string choiceId,
            out NpcVisitResolutionResult result)
        {
            result = null;
            if (CurrentSave == null || CurrentTama == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!npcVisitSystem.TryResolve(
                    CurrentSave.npcVisits,
                    CurrentTama,
                    CurrentSave.economy,
                    occurrenceId,
                    choiceId,
                    DateTimeOffset.Now,
                    out result))
            {
                return false;
            }

            AddUniqueRecord(CurrentSave.collections.events, $"npc_visit_{result.NpcId}");
            if (result.KeepsakeUnlocked)
            {
                AddUniqueRecord(
                    CurrentSave.collections.events,
                    NpcVisitSystem.StainGuestKeepsakeRecordId);
            }
            if (memoryJournalSystem.TryRecord(
                    CurrentSave.memoryJournal,
                    new MemoryJournalDraft(
                        MemoryJournalKind.Story,
                        $"npc_story_{result.NpcId}_{result.RelationshipLevel}",
                        result.OccurrenceId,
                        result.ChoiceId,
                        DateTimeOffset.Now,
                        CurrentTama.name,
                        CurrentTama.form,
                        $"{npcVisitSystem.Find(result.NpcId)?.DisplayName ?? "손님"}의 방문",
                        result.Message,
                        result.RelationshipLevel >= 2),
                    out _))
            {
                MemoryJournalChanged?.Invoke();
            }

            TryActivateRelationshipQuestForVisit(result);
            RefreshDerivedCollectionRecords();
            SaveGame();
            JourneyHubChanged?.Invoke();
            return true;
        }

        private NpcVisitContext BuildNpcVisitContext()
        {
            var state = CurrentSave?.dreamStorySeason;
            var completion = state?.FindCompletion(NpcVisitSystem.SeasonOneFinalEpisodeId);
            var choiceId = completion?.choiceId;
            if (!string.Equals(choiceId, NpcVisitSystem.SeasonOneOpenChoiceId, StringComparison.Ordinal)
                && !string.Equals(choiceId, NpcVisitSystem.SeasonOneCloseChoiceId, StringComparison.Ordinal))
            {
                choiceId = CurrentSave?.npcVisits?.stainGuestEchoChoiceId;
            }

            var unlocked = string.Equals(
                    choiceId,
                    NpcVisitSystem.SeasonOneOpenChoiceId,
                    StringComparison.Ordinal)
                || string.Equals(
                    choiceId,
                    NpcVisitSystem.SeasonOneCloseChoiceId,
                    StringComparison.Ordinal);
            return new NpcVisitContext(unlocked, choiceId);
        }

        public NpcRelationshipSnapshot GetNpcRelationshipSnapshot(string npcId)
        {
            if (CurrentSave == null)
            {
                return default;
            }

            CurrentSave.EnsureRuntimeDefaults();
            return npcRelationshipQuestSystem.ObserveRelationship(
                CurrentSave.npcVisits,
                npcId);
        }

        public NpcQuestWindowSnapshot GetActiveNpcRelationshipQuest()
        {
            return GetActiveNpcRelationshipQuest(DateTimeOffset.Now);
        }

        public NpcQuestWindowSnapshot GetActiveNpcRelationshipQuest(DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return default;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var wasTerminallyExpired = CurrentSave.npcRelationshipQuests.activeQuest
                ?.terminalExpired ?? false;
            var snapshot = npcRelationshipQuestSystem.ObserveActive(
                CurrentSave.npcRelationshipQuests,
                now);
            if (!wasTerminallyExpired
                && (CurrentSave.npcRelationshipQuests.activeQuest?.terminalExpired ?? false))
            {
                SaveGame();
            }

            return snapshot;
        }

        public NpcQuestDeliveryResult TryDeliverNpcRelationshipQuest(
            string claimReceiptId)
        {
            return TryDeliverNpcRelationshipQuest(DateTimeOffset.Now, claimReceiptId);
        }

        public NpcQuestDeliveryResult TryDeliverNpcRelationshipQuest(
            DateTimeOffset now,
            string claimReceiptId)
        {
            if (CurrentSave == null)
            {
                return new NpcQuestDeliveryResult(
                    NpcQuestDeliveryStatus.MissingState,
                    null,
                    claimReceiptId,
                    false,
                    0,
                    0,
                    NpcRelationshipTier.NewFace,
                    NpcRelationshipTier.NewFace);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var wasTerminallyExpired = CurrentSave.npcRelationshipQuests.activeQuest
                ?.terminalExpired ?? false;
            var result = npcRelationshipQuestSystem.TryDeliver(
                CurrentSave.npcRelationshipQuests,
                CurrentSave.npcVisits,
                CurrentSave.economy,
                CurrentSave.snackInventory,
                now,
                claimReceiptId);
            if (!result.Applied)
            {
                if (!wasTerminallyExpired
                    && (CurrentSave.npcRelationshipQuests.activeQuest?.terminalExpired ?? false))
                {
                    SaveGame();
                    JourneyHubChanged?.Invoke();
                }

                return result;
            }

            AddUniqueRecord(
                CurrentSave.collections.events,
                $"npc_quest_{result.Quest?.Id ?? "completed"}");
            RefreshDerivedCollectionRecords();
            SaveGame();
            JourneyHubChanged?.Invoke();
            return result;
        }

        public NpcRelationshipEpisodeSnapshot GetNpcRelationshipEpisodeSnapshot(string npcId)
        {
            if (CurrentSave == null)
            {
                return npcRelationshipEpisodeSystem.BuildNextEpisodeSnapshot(
                    null,
                    null,
                    npcId);
            }

            CurrentSave.EnsureRuntimeDefaults();
            return npcRelationshipEpisodeSystem.BuildNextEpisodeSnapshot(
                CurrentSave.npcRelationshipEpisodes,
                CurrentSave.npcVisits,
                npcId);
        }

        public IReadOnlyList<NpcRelationshipEpisodeSnapshot> GetNpcRelationshipEpisodeSnapshots()
        {
            if (CurrentSave == null)
            {
                return npcRelationshipEpisodeSystem.BuildNextEpisodeSnapshots(null, null);
            }

            CurrentSave.EnsureRuntimeDefaults();
            return npcRelationshipEpisodeSystem.BuildNextEpisodeSnapshots(
                CurrentSave.npcRelationshipEpisodes,
                CurrentSave.npcVisits);
        }

        public NpcAfterstoryPanelSnapshot GetNpcAfterstoryPanelSnapshot()
        {
            return GetNpcAfterstoryPanelSnapshot(DateTimeOffset.Now);
        }

        public NpcAfterstoryPanelSnapshot GetNpcAfterstoryPanelSnapshot(DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return NpcAfterstoryPanelSnapshot.Empty;
            }

            CurrentSave.EnsureRuntimeDefaults();
            return npcAfterstorySystem.BuildPanelSnapshot(
                CurrentSave.npcAfterstory,
                CurrentSave.npcRelationshipEpisodes,
                now);
        }

        public NpcAfterstoryChoiceResult TryApplyNpcAfterstoryChoice(
            string afterstoryId,
            string choiceId)
        {
            return TryApplyNpcAfterstoryChoice(
                afterstoryId,
                choiceId,
                $"npc-afterstory:{afterstoryId}",
                DateTimeOffset.Now);
        }

        public NpcAfterstoryChoiceResult TryApplyNpcAfterstoryChoice(
            string afterstoryId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt)
        {
            var result = npcAfterstorySystem.TryApplyAfterstoryChoice(
                CurrentSave?.npcAfterstory,
                CurrentSave?.npcRelationshipEpisodes,
                afterstoryId,
                choiceId,
                receiptId,
                completedAt);
            if (result == null || !result.Applied || CurrentSave == null)
            {
                return result;
            }

            AddUniqueRecord(CurrentSave.collections.events, $"npc_afterstory_{result.AfterstoryId}");
            AddUniqueRecord(CurrentSave.collections.events, $"npc_keepsake_{result.KeepsakeId}");
            var memoryRecorded = memoryJournalSystem.TryRecord(
                CurrentSave.memoryJournal,
                new MemoryJournalDraft(
                    MemoryJournalKind.Story,
                    $"npc_afterstory_{result.AfterstoryId}",
                    result.ReceiptId,
                    result.ChoiceId,
                    completedAt,
                    CurrentTama.name,
                    CurrentTama.form,
                    result.MemoryTitle,
                    result.MemoryDetail,
                    true),
                out _);
            SaveGame();
            if (memoryRecorded)
            {
                MemoryJournalChanged?.Invoke();
            }

            JourneyHubChanged?.Invoke();
            NpcAfterstoryChanged?.Invoke(result);
            return result;
        }

        public NpcWeeklyClaimResult TryClaimNpcWeeklyQuest(string npcId)
        {
            var now = DateTimeOffset.Now;
            var weekKey = NpcAfterstorySystem.GetMondayWeekKey(now);
            return TryClaimNpcWeeklyQuest(
                npcId,
                $"npc-weekly:{npcId}:{weekKey}",
                now);
        }

        public NpcWeeklyClaimResult TryClaimNpcWeeklyQuest(
            string npcId,
            string receiptId,
            DateTimeOffset now)
        {
            var result = npcAfterstorySystem.TryClaimWeeklyQuest(
                CurrentSave?.npcAfterstory,
                CurrentSave?.npcRelationshipEpisodes,
                CurrentSave?.economy,
                npcId,
                receiptId,
                now);
            if (result == null || !result.Applied)
            {
                return result;
            }

            SaveGame();
            JourneyHubChanged?.Invoke();
            return result;
        }

        public NpcKeepsakeSelectionResult TrySelectNpcKeepsake(string keepsakeId)
        {
            var result = npcAfterstorySystem.TrySelectDisplayedKeepsake(
                CurrentSave?.npcAfterstory,
                keepsakeId);
            if (!result.Changed)
            {
                return result;
            }

            SaveGame();
            var snapshot = npcAfterstorySystem.BuildDisplayedKeepsakeSnapshot(
                CurrentSave?.npcAfterstory);
            NpcKeepsakeDisplayChanged?.Invoke(snapshot);
            JourneyHubChanged?.Invoke();
            return result;
        }

        public NpcKeepsakeDisplaySnapshot GetNpcKeepsakeDisplaySnapshot()
        {
            return npcAfterstorySystem.BuildDisplayedKeepsakeSnapshot(
                CurrentSave?.npcAfterstory);
        }

        private MilkroomMysterySystem GetMilkroomMysterySystem()
        {
            return milkroomMysterySystem
                ?? (milkroomMysterySystem = new MilkroomMysterySystem());
        }

        public MilkroomMysterySnapshot GetMilkroomMysterySnapshot()
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return MilkroomMysterySnapshot.CreateHidden();
            }

            CurrentSave.EnsureRuntimeDefaults();
            return GetMilkroomMysterySystem().BuildSnapshot(
                CurrentSave.milkroomMystery,
                new MilkroomMysteryProgress(
                    CurrentTama.level,
                    CurrentSave.unlocks?.starMilkUnlocked == true));
        }

        public MilkroomMysteryChoiceResult TryChooseMilkroomMystery(
            string chapterId,
            string choiceId)
        {
            return TryChooseMilkroomMystery(
                chapterId,
                choiceId,
                $"milkroom-mystery:{chapterId}",
                DateTimeOffset.Now);
        }

        public MilkroomMysteryChoiceResult TryChooseMilkroomMystery(
            string chapterId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return GetMilkroomMysterySystem().TryChoose(
                    null,
                    default,
                    chapterId,
                    choiceId,
                    receiptId,
                    completedAt,
                    _ => false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var memoryRecorded = false;
            var decorationChanged = false;
            var result = GetMilkroomMysterySystem().TryChoose(
                CurrentSave.milkroomMystery,
                new MilkroomMysteryProgress(
                    CurrentTama.level,
                    CurrentSave.unlocks?.starMilkUnlocked == true),
                chapterId,
                choiceId,
                receiptId,
                completedAt,
                payloads => CommitMilkroomMysteryPayloads(
                    payloads,
                    completedAt,
                    out memoryRecorded,
                    out decorationChanged));
            if (!result.Applied)
            {
                return result;
            }

            SaveGame();
            if (memoryRecorded)
            {
                MemoryJournalChanged?.Invoke();
            }

            if (decorationChanged)
            {
                DecorationChanged?.Invoke();
            }

            JourneyHubChanged?.Invoke();
            MilkroomMysteryChanged?.Invoke(result);
            return result;
        }

        private bool CommitMilkroomMysteryPayloads(
            MilkroomMysteryChoicePayloadBundle payloads,
            DateTimeOffset completedAt,
            out bool memoryRecorded,
            out bool decorationChanged)
        {
            memoryRecorded = false;
            decorationChanged = false;
            if (payloads?.Memory == null
                || payloads.Codex == null
                || payloads.Reward == null
                || CurrentSave?.economy == null
                || CurrentSave.collections == null
                || CurrentSave.memoryJournal == null
                || CurrentSave.decorations == null)
            {
                return false;
            }

            var reward = payloads.Reward;
            var economy = CurrentSave.economy;
            var validReward = reward.Amount >= 0;
            switch (reward.Kind)
            {
                case MilkroomMysteryRewardKind.Coin:
                    validReward &= CanAddMysteryReward(economy.milkCoins, reward.Amount);
                    break;
                case MilkroomMysteryRewardKind.MilkDrop:
                    validReward &= CanAddMysteryReward(economy.milkDrops, reward.Amount);
                    break;
                case MilkroomMysteryRewardKind.StarDrop:
                    validReward &= CanAddMysteryReward(economy.starDrops, reward.Amount);
                    break;
                case MilkroomMysteryRewardKind.CollectionFragment:
                    validReward &= CanAddMysteryReward(
                        economy.collectionFragments,
                        reward.Amount);
                    break;
                case MilkroomMysteryRewardKind.Decoration:
                    validReward &= !string.IsNullOrWhiteSpace(reward.RewardId)
                        && (ContainsOrdinal(CurrentSave.decorations.ownedItemIds, reward.RewardId)
                            || CurrentSave.decorations.ownedItemIds.Count < 256);
                    break;
                default:
                    validReward = false;
                    break;
            }

            if (!validReward
                || string.IsNullOrWhiteSpace(payloads.Memory.RecordId)
                || string.IsNullOrWhiteSpace(payloads.Codex.RecordId))
            {
                return false;
            }

            if (!HasMemorySource(payloads.Memory.RecordId))
            {
                memoryRecorded = memoryJournalSystem.TryRecord(
                    CurrentSave.memoryJournal,
                    new MemoryJournalDraft(
                        MemoryJournalKind.Story,
                        payloads.Memory.RecordId,
                        payloads.ReceiptId,
                        payloads.Memory.DetailId,
                        completedAt,
                        CurrentTama.name,
                        CurrentTama.form,
                        payloads.Memory.Title,
                        payloads.Memory.Detail,
                        true),
                    out _);
                if (!memoryRecorded)
                {
                    return false;
                }
            }

            AddUniqueRecord(CurrentSave.collections.events, payloads.Codex.RecordId);
            switch (reward.Kind)
            {
                case MilkroomMysteryRewardKind.Coin:
                    economy.milkCoins += reward.Amount;
                    break;
                case MilkroomMysteryRewardKind.MilkDrop:
                    economy.milkDrops += reward.Amount;
                    break;
                case MilkroomMysteryRewardKind.StarDrop:
                    economy.starDrops += reward.Amount;
                    break;
                case MilkroomMysteryRewardKind.CollectionFragment:
                    economy.collectionFragments += reward.Amount;
                    break;
                case MilkroomMysteryRewardKind.Decoration:
                    if (!ContainsOrdinal(CurrentSave.decorations.ownedItemIds, reward.RewardId))
                    {
                        CurrentSave.decorations.ownedItemIds.Add(reward.RewardId);
                        decorationChanged = true;
                    }
                    break;
            }

            return true;
        }

        private bool HasMemorySource(string sourceId)
        {
            var entries = CurrentSave?.memoryJournal?.entries;
            if (entries == null || string.IsNullOrWhiteSpace(sourceId))
            {
                return false;
            }

            for (var index = 0; index < entries.Count; index += 1)
            {
                if (string.Equals(entries[index]?.sourceId, sourceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanAddMysteryReward(int current, int amount)
        {
            return current >= 0 && amount >= 0 && (long)current + amount <= int.MaxValue;
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
        {
            if (values == null || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            for (var index = 0; index < values.Count; index += 1)
            {
                if (string.Equals(values[index], expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private DreamStorySeasonSystem GetDreamStorySeasonSystem()
        {
            return dreamStorySeasonSystem
                ?? (dreamStorySeasonSystem = new DreamStorySeasonSystem());
        }

        private DreamStoryProgress BuildDreamStoryProgress()
        {
            return DreamStoryProgress.FromAuthoritativeSavesAndSeasonState(
                CurrentSave?.sleepSchedule,
                CurrentSave?.npcVisits,
                CurrentSave?.milkroomMystery,
                CurrentSave?.dreamStorySeason);
        }

        public DreamStorySnapshot GetDreamStoryPendingSnapshot()
        {
            return GetDreamStoryPendingSnapshot(DateTimeOffset.Now);
        }

        public DreamStorySnapshot GetDreamStoryPendingSnapshot(DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return DreamStorySnapshot.CreateHidden();
            }

            CurrentSave.EnsureRuntimeDefaults();
            var snapshot = GetDreamStorySeasonSystem().BuildPendingSnapshot(
                CurrentSave.dreamStorySeason,
                BuildDreamStoryProgress(),
                now,
                out var stateChanged);
            if (stateChanged)
            {
                SaveGame();
            }

            return snapshot;
        }

        public IReadOnlyList<DreamStoryRecallEntry> GetDreamStoryRecallIndex()
        {
            return GetDreamStoryRecallIndex(DateTimeOffset.Now);
        }

        public IReadOnlyList<DreamStoryRecallEntry> GetDreamStoryRecallIndex(
            DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return Array.Empty<DreamStoryRecallEntry>();
            }

            CurrentSave.EnsureRuntimeDefaults();
            var entries = GetDreamStorySeasonSystem().BuildRecallIndex(
                CurrentSave.dreamStorySeason,
                now,
                out var stateChanged);
            if (stateChanged)
            {
                SaveGame();
            }

            return entries;
        }

        public DreamStorySnapshot GetDreamStoryRecallSnapshot(string episodeId)
        {
            return GetDreamStoryRecallSnapshot(episodeId, DateTimeOffset.Now);
        }

        public DreamStorySnapshot GetDreamStoryRecallSnapshot(
            string episodeId,
            DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return DreamStorySnapshot.CreateHidden();
            }

            CurrentSave.EnsureRuntimeDefaults();
            var snapshot = GetDreamStorySeasonSystem().BuildRecallSnapshot(
                CurrentSave.dreamStorySeason,
                episodeId,
                now,
                out var stateChanged);
            if (stateChanged)
            {
                SaveGame();
            }

            return snapshot;
        }

        public bool TryDiscoverDreamStorySignal(string signalId)
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!GetDreamStorySeasonSystem().TryDiscoverSignal(
                    CurrentSave.dreamStorySeason,
                    signalId))
            {
                return false;
            }

            SaveGame();
            JourneyHubChanged?.Invoke();
            return true;
        }

        public DreamStoryChoiceResult TryChooseDreamStory(
            string episodeId,
            string choiceId)
        {
            return TryChooseDreamStory(
                episodeId,
                choiceId,
                $"dream-story:{episodeId}",
                DateTimeOffset.Now);
        }

        public DreamStoryChoiceResult TryChooseDreamStory(
            string episodeId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return GetDreamStorySeasonSystem().TryChoose(
                    null,
                    null,
                    episodeId,
                    choiceId,
                    receiptId,
                    completedAt,
                    _ => false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var memoryRecorded = false;
            var result = GetDreamStorySeasonSystem().TryChoose(
                CurrentSave.dreamStorySeason,
                BuildDreamStoryProgress(),
                episodeId,
                choiceId,
                receiptId,
                completedAt,
                payloads => CommitDreamStoryPayloads(
                    payloads,
                    out memoryRecorded));
            if (!result.Applied)
            {
                return result;
            }

            if (string.Equals(
                    result.EpisodeId,
                    "dream_02_reversed_label",
                    StringComparison.Ordinal))
            {
                GetDreamStorySeasonSystem().TryDiscoverSignal(
                    CurrentSave.dreamStorySeason,
                    "label_error_observed");
            }

            SaveGame();
            if (memoryRecorded)
            {
                MemoryJournalChanged?.Invoke();
            }

            JourneyHubChanged?.Invoke();
            DreamStorySeasonChanged?.Invoke(result);
            return result;
        }

        private bool CommitDreamStoryPayloads(
            DreamStoryChoicePayloadBundle payloads,
            out bool memoryRecorded)
        {
            memoryRecorded = false;
            if (payloads?.Memory == null
                || payloads.Codex == null
                || payloads.Reward == null
                || CurrentSave?.economy == null
                || CurrentSave.collections?.events == null
                || CurrentSave.memoryJournal == null
                || CurrentTama == null
                || string.IsNullOrWhiteSpace(payloads.Memory.RecordId)
                || string.IsNullOrWhiteSpace(payloads.Codex.RecordId))
            {
                return false;
            }

            var reward = payloads.Reward;
            var economy = CurrentSave.economy;
            var codexAlreadyRecorded = ContainsOrdinal(
                CurrentSave.collections.events,
                payloads.Codex.RecordId);
            var memoryAlreadyRecorded = HasMemorySource(payloads.Memory.RecordId);
            var rewardAlreadyRecorded = codexAlreadyRecorded || memoryAlreadyRecorded;
            var rewardValid = reward.Amount >= 0;
            switch (reward.Kind)
            {
                case DreamStoryRewardKind.None:
                    rewardValid &= reward.Amount == 0;
                    break;
                case DreamStoryRewardKind.Coin:
                    rewardValid &= rewardAlreadyRecorded
                        || CanAddMysteryReward(economy.milkCoins, reward.Amount);
                    break;
                case DreamStoryRewardKind.MilkDrop:
                    rewardValid &= rewardAlreadyRecorded
                        || CanAddMysteryReward(economy.milkDrops, reward.Amount);
                    break;
                case DreamStoryRewardKind.CollectionFragment:
                    rewardValid &= rewardAlreadyRecorded
                        || CanAddMysteryReward(
                            economy.collectionFragments,
                            reward.Amount);
                    break;
                default:
                    rewardValid = false;
                    break;
            }

            if (!rewardValid
                || (!codexAlreadyRecorded && CurrentSave.collections.events.Count >= 512))
            {
                return false;
            }

            if (!memoryAlreadyRecorded)
            {
                memoryRecorded = memoryJournalSystem.TryRecord(
                    CurrentSave.memoryJournal,
                    new MemoryJournalDraft(
                        MemoryJournalKind.Story,
                        payloads.Memory.RecordId,
                        payloads.ReceiptId,
                        payloads.Memory.DetailId,
                        payloads.CompletedAt,
                        CurrentTama.name,
                        CurrentTama.form,
                        payloads.Memory.Title,
                        payloads.Memory.Detail,
                        true),
                    out _);
                if (!memoryRecorded)
                {
                    return false;
                }
            }

            AddUniqueRecord(CurrentSave.collections.events, payloads.Codex.RecordId);
            if (rewardAlreadyRecorded)
            {
                return true;
            }

            switch (reward.Kind)
            {
                case DreamStoryRewardKind.Coin:
                    economy.milkCoins += reward.Amount;
                    break;
                case DreamStoryRewardKind.MilkDrop:
                    economy.milkDrops += reward.Amount;
                    break;
                case DreamStoryRewardKind.CollectionFragment:
                    economy.collectionFragments += reward.Amount;
                    break;
            }

            return true;
        }

        private MilkroomInvestigationSystem GetMilkroomInvestigationSystem()
        {
            return milkroomInvestigationSystem
                ?? (milkroomInvestigationSystem = new MilkroomInvestigationSystem());
        }

        public MilkroomInvestigationSnapshot GetMilkroomInvestigationSnapshot(
            float zoomScale)
        {
            return GetMilkroomInvestigationSnapshot(zoomScale, DateTimeOffset.Now);
        }

        public MilkroomInvestigationSnapshot GetMilkroomInvestigationSnapshot(
            float zoomScale,
            DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return MilkroomInvestigationSnapshot.Hidden;
            }

            CurrentSave.EnsureRuntimeDefaults();
            return GetMilkroomInvestigationSystem().BuildSnapshot(
                CurrentSave.milkroomInvestigation,
                CurrentSave.dreamStorySeason,
                new MilkroomInvestigationContext(
                    Math.Max(0.01f, zoomScale),
                    now.Hour));
        }

        public MilkroomInvestigationSnapshot GetMilkroomInvestigationRecallSnapshot()
        {
            if (CurrentSave == null)
            {
                return MilkroomInvestigationSnapshot.Hidden;
            }

            CurrentSave.EnsureRuntimeDefaults();
            return GetMilkroomInvestigationSystem().BuildRecallSnapshot(
                CurrentSave.milkroomInvestigation,
                CurrentSave.dreamStorySeason);
        }

        public MilkroomInvestigationInspectResult TryInspectMilkroomHotspot(
            string hotspotId,
            float zoomScale)
        {
            return TryInspectMilkroomHotspot(
                hotspotId,
                zoomScale,
                DateTimeOffset.Now);
        }

        public MilkroomInvestigationInspectResult TryInspectMilkroomHotspot(
            string hotspotId,
            float zoomScale,
            DateTimeOffset now)
        {
            var result = GetMilkroomInvestigationSystem().TryInspect(
                CurrentSave?.milkroomInvestigation,
                CurrentSave?.dreamStorySeason,
                hotspotId,
                new MilkroomInvestigationContext(
                    Math.Max(0.01f, zoomScale),
                    now.Hour));
            if (!result.Applied)
            {
                return result;
            }

            SaveGame();
            MilkroomInvestigationChanged?.Invoke();
            JourneyHubChanged?.Invoke();
            return result;
        }

        public MilkroomInvestigationChoiceResult TryChooseMilkroomInvestigation(
            string episodeId,
            string choiceId)
        {
            return TryChooseMilkroomInvestigation(
                episodeId,
                choiceId,
                DateTimeOffset.Now);
        }

        public MilkroomInvestigationChoiceResult TryChooseMilkroomInvestigation(
            string episodeId,
            string choiceId,
            DateTimeOffset completedAt)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return GetMilkroomInvestigationSystem().TryChoose(
                    null,
                    null,
                    episodeId,
                    choiceId,
                    string.Empty,
                    completedAt,
                    _ => false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var memoryRecorded = false;
            var result = GetMilkroomInvestigationSystem().TryChoose(
                CurrentSave.milkroomInvestigation,
                CurrentSave.dreamStorySeason,
                episodeId,
                choiceId,
                $"milkroom-investigation:{episodeId}",
                completedAt,
                payloads => CommitMilkroomInvestigationPayloads(
                    payloads,
                    out memoryRecorded));
            if (!result.Applied)
            {
                return result;
            }

            SaveGame();
            if (memoryRecorded)
            {
                MemoryJournalChanged?.Invoke();
            }

            MilkroomInvestigationChanged?.Invoke();
            JourneyHubChanged?.Invoke();
            return result;
        }

        private bool CommitMilkroomInvestigationPayloads(
            MilkroomInvestigationChoicePayloadBundle payloads,
            out bool memoryRecorded)
        {
            memoryRecorded = false;
            if (payloads?.Memory == null
                || payloads.Codex == null
                || payloads.Reward == null
                || CurrentSave?.economy == null
                || CurrentSave.collections?.events == null
                || CurrentSave.memoryJournal == null
                || CurrentTama == null
                || string.IsNullOrWhiteSpace(payloads.Memory.RecordId)
                || string.IsNullOrWhiteSpace(payloads.Codex.RecordId))
            {
                return false;
            }

            var reward = payloads.Reward;
            var economy = CurrentSave.economy;
            var codexAlreadyRecorded = ContainsOrdinal(
                CurrentSave.collections.events,
                payloads.Codex.RecordId);
            var memoryAlreadyRecorded = HasMemorySource(payloads.Memory.RecordId);
            var rewardAlreadyRecorded = codexAlreadyRecorded || memoryAlreadyRecorded;
            var validReward = reward.Amount >= 0;
            switch (reward.Kind)
            {
                case MilkroomInvestigationRewardKind.None:
                    validReward &= reward.Amount == 0;
                    break;
                case MilkroomInvestigationRewardKind.Coin:
                    validReward &= rewardAlreadyRecorded
                        || CanAddMysteryReward(economy.milkCoins, reward.Amount);
                    break;
                case MilkroomInvestigationRewardKind.MilkDrop:
                    validReward &= rewardAlreadyRecorded
                        || CanAddMysteryReward(economy.milkDrops, reward.Amount);
                    break;
                case MilkroomInvestigationRewardKind.CollectionFragment:
                    validReward &= rewardAlreadyRecorded
                        || CanAddMysteryReward(
                            economy.collectionFragments,
                            reward.Amount);
                    break;
                default:
                    validReward = false;
                    break;
            }

            if (!validReward
                || (!codexAlreadyRecorded && CurrentSave.collections.events.Count >= 512))
            {
                return false;
            }

            if (!memoryAlreadyRecorded)
            {
                memoryRecorded = memoryJournalSystem.TryRecord(
                    CurrentSave.memoryJournal,
                    new MemoryJournalDraft(
                        MemoryJournalKind.Story,
                        payloads.Memory.RecordId,
                        payloads.ReceiptId,
                        payloads.Memory.DetailId,
                        payloads.CompletedAt,
                        CurrentTama.name,
                        CurrentTama.form,
                        payloads.Memory.Title,
                        payloads.Memory.Detail,
                        true),
                    out _);
                if (!memoryRecorded)
                {
                    return false;
                }
            }

            AddUniqueRecord(CurrentSave.collections.events, payloads.Codex.RecordId);
            if (rewardAlreadyRecorded)
            {
                return true;
            }

            switch (reward.Kind)
            {
                case MilkroomInvestigationRewardKind.Coin:
                    economy.milkCoins += reward.Amount;
                    break;
                case MilkroomInvestigationRewardKind.MilkDrop:
                    economy.milkDrops += reward.Amount;
                    break;
                case MilkroomInvestigationRewardKind.CollectionFragment:
                    economy.collectionFragments += reward.Amount;
                    break;
            }

            return true;
        }

        public NpcRelationshipEpisodeChoiceResult TryApplyNpcRelationshipEpisodeChoice(
            string episodeId,
            string choiceId,
            string receiptId)
        {
            return TryApplyNpcRelationshipEpisodeChoice(
                DateTimeOffset.Now,
                episodeId,
                choiceId,
                receiptId);
        }

        public NpcRelationshipEpisodeChoiceResult TryApplyNpcRelationshipEpisodeChoice(
            DateTimeOffset completedAt,
            string episodeId,
            string choiceId,
            string receiptId)
        {
            if (CurrentSave == null)
            {
                return new NpcRelationshipEpisodeChoiceResult(
                    NpcRelationshipEpisodeChoiceStatus.MissingState,
                    null,
                    null,
                    receiptId,
                    0,
                    0,
                    default);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var result = npcRelationshipEpisodeSystem.TryApplyChoice(
                CurrentSave.npcRelationshipEpisodes,
                CurrentSave.npcVisits,
                CurrentTama,
                episodeId,
                choiceId,
                receiptId,
                completedAt,
                GetPostgameResearchProfileEffectSnapshot().NpcEpisodeAffinityBonus);
            if (!result.Applied)
            {
                return result;
            }

            var memoryRecorded = memoryJournalSystem.TryRecord(
                CurrentSave.memoryJournal,
                new MemoryJournalDraft(
                    MemoryJournalKind.Story,
                    result.MemorySourceId,
                    result.ReceiptId,
                    result.MemoryDetailId,
                    completedAt,
                    CurrentTama.name,
                    CurrentTama.form,
                    result.MemoryTitle,
                    result.MemoryDetail,
                    true),
                out _);
            AddUniqueRecord(
                CurrentSave.collections.events,
                $"npc_episode_{result.CompletionId}");
            SaveGame();

            if (memoryRecorded)
            {
                MemoryJournalChanged?.Invoke();
            }

            JourneyHubChanged?.Invoke();
            NpcRelationshipEpisodeCompleted?.Invoke(result);
            return result;
        }

        private void TryActivateRelationshipQuestForVisit(
            NpcVisitResolutionResult visitResult)
        {
            if (CurrentSave == null || visitResult == null)
            {
                return;
            }

            var relationship = npcRelationshipQuestSystem.ObserveRelationship(
                CurrentSave.npcVisits,
                visitResult.NpcId);
            var eligible = npcRelationshipQuestSystem.GetEligibleQuests(
                visitResult.NpcId,
                relationship.Affinity);
            if (eligible == null || eligible.Count == 0)
            {
                return;
            }

            var startIndex = Math.Max(0, relationship.Visits - 1) % eligible.Count;
            for (var offset = 0; offset < eligible.Count; offset += 1)
            {
                var quest = eligible[(startIndex + offset) % eligible.Count];
                var offerId = $"npc_quest_offer_{visitResult.OccurrenceId}_{quest.Id}";
                var activated = npcRelationshipQuestSystem.TryActivate(
                    CurrentSave.npcRelationshipQuests,
                    CurrentSave.npcVisits,
                    visitResult.NpcId,
                    quest.Id,
                    offerId,
                    DateTimeOffset.Now);
                if (activated.Applied
                    || activated.Status == NpcQuestActivationStatus.AlreadyActive)
                {
                    return;
                }
            }
        }

        public NextActionGoalBoardSnapshot GetNextActionGoalBoardSnapshot()
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return NextActionGoalBoardSystem.BuildLateLevel(0, 0, 0, 0, 0);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var gate = lateLevelGrowthSystem.EvaluateGate(
                CurrentTama,
                CurrentSave.milkGrowth);
            return NextActionGoalBoardSystem.BuildLateLevel(
                CurrentTama.level,
                CurrentSave.lateLevelGrowth?.progressUnits ?? 0,
                CurrentTama.stats?.affection ?? 0,
                gate.QualifyingMilkTypeCount,
                gate.StableStatusCount);
        }

        public RandomEventJournalSnapshot GetRandomEventJournalSnapshot()
        {
            return GetRandomEventJournalSnapshot(DateTimeOffset.Now);
        }

        public RandomEventJournalSnapshot GetRandomEventJournalSnapshot(DateTimeOffset now)
        {
            return RandomEventJournalSystem.Build(CurrentSave?.randomEvents, now);
        }

        public AutonomousLifeDiscoveryCollectionSnapshot GetAutonomousLifeDiscoverySnapshot()
        {
            return AutonomousLifeDiscoveryCatalog.CreateSnapshot(CurrentSave?.autonomousLife);
        }

        public WeeklyCareJourneySnapshot GetWeeklyCareJourneySnapshot()
        {
            return GetWeeklyCareJourneySnapshot(DateTimeOffset.Now);
        }

        public WeeklyCareJourneySnapshot GetWeeklyCareJourneySnapshot(DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return weeklyCareJourneySystem.BuildSnapshot(null, now);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var week = weeklyCareJourneySystem.ReconcileWeek(
                CurrentSave.weeklyCareJourney,
                now);
            if (week.StateChanged)
            {
                SaveGame();
                JourneyHubChanged?.Invoke();
            }

            return weeklyCareJourneySystem.BuildSnapshot(
                CurrentSave.weeklyCareJourney,
                now);
        }

        public WeeklyCareClaimResult TryClaimWeeklyCareJourneyReward(
            string claimReceiptId)
        {
            return TryClaimWeeklyCareJourneyReward(DateTimeOffset.Now, claimReceiptId);
        }

        public WeeklyCareClaimResult TryClaimWeeklyCareJourneyReward(
            DateTimeOffset now,
            string claimReceiptId)
        {
            if (CurrentSave == null)
            {
                return new WeeklyCareClaimResult(
                    WeeklyCareClaimStatus.MissingState,
                    WeeklyCareJourneySystem.GetWeekKey(now),
                    claimReceiptId,
                    default);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var result = weeklyCareJourneySystem.TryClaimReward(
                CurrentSave.weeklyCareJourney,
                CurrentSave.economy,
                now,
                claimReceiptId);
            if (result.Applied)
            {
                AddUniqueRecord(
                    CurrentSave.collections.events,
                    $"weekly_care_{result.WeekKey}");
                RefreshDerivedCollectionRecords();
                SaveGame();
                JourneyHubChanged?.Invoke();
            }

            return result;
        }

        public DecorationWorkshopQuote GetDecorationWorkshopQuote(string variantId)
        {
            var wallet = GetDecorationWorkshopWalletSnapshot();
            return decorationWorkshopSystem.BuildQuote(
                CurrentSave?.decorationWorkshop,
                wallet,
                variantId);
        }

        public DecorationWorkshopCraftResult TryCraftDecorationWorkshopVariant(
            string variantId,
            string receiptKey)
        {
            var wallet = GetDecorationWorkshopWalletSnapshot();
            var result = decorationWorkshopSystem.TryCraft(
                CurrentSave?.decorationWorkshop,
                wallet,
                variantId,
                receiptKey);
            if (!result.Applied || CurrentSave?.economy == null)
            {
                return result;
            }

            CurrentSave.economy.milkCoins = result.WalletAfter.Coins;
            CurrentSave.economy.milkDrops = result.WalletAfter.MilkDrops;
            CurrentSave.economy.collectionFragments = result.WalletAfter.CollectionFragments;
            SaveGame();
            DecorationChanged?.Invoke();
            JourneyHubChanged?.Invoke();
            return result;
        }

        public DecorationWorkshopSelectionResult TrySelectDecorationWorkshopVariant(
            DecorationSlot slot,
            string variantId)
        {
            var result = decorationWorkshopSystem.TrySelect(
                CurrentSave?.decorationWorkshop,
                slot,
                variantId);
            if (!result.Changed)
            {
                return result;
            }

            SaveGame();
            DecorationChanged?.Invoke();
            JourneyHubChanged?.Invoke();
            return result;
        }

        public DecorationWorkshopRenderSnapshot GetDecorationWorkshopRenderSnapshot()
        {
            return decorationWorkshopSystem.BuildRenderSnapshot(
                CurrentSave?.decorationWorkshop);
        }

        public CollectionSetAlbumPublicSnapshot GetCollectionSetAlbumSnapshot()
        {
            if (CurrentSave == null)
            {
                return collectionSetAlbumSystem.BuildPublicProgressSnapshot(null, null);
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (collectionSetAlbumSystem.RecalculateProgress(
                    CurrentSave.collectionSetAlbum,
                    CurrentSave.collections) > 0)
            {
                SaveGame();
                JourneyHubChanged?.Invoke();
            }

            return collectionSetAlbumSystem.BuildPublicProgressSnapshot(
                CurrentSave.collectionSetAlbum,
                CurrentSave.collections);
        }

        public CollectionSetAlbumClaimResult TryClaimCollectionSetAlbumReward(
            string setId,
            string receiptKey)
        {
            if (CurrentSave == null)
            {
                return new CollectionSetAlbumClaimResult(
                    CollectionSetAlbumClaimStatus.MissingState,
                    setId,
                    receiptKey,
                    default);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var snapshot = collectionSetAlbumSystem.BuildPublicProgressSnapshot(
                CurrentSave.collectionSetAlbum,
                CurrentSave.collections);
            var progress = snapshot.Find(setId);
            var adjustedReward = progress == null
                ? default
                : ApplyPostgameResearchCollectionReward(progress.Reward);
            if (progress?.CanClaimReward == true
                && !CanAddCollectionSetReward(CurrentSave.economy, adjustedReward))
            {
                return new CollectionSetAlbumClaimResult(
                    CollectionSetAlbumClaimStatus.TrackingCapacityFull,
                    setId,
                    receiptKey,
                    default);
            }

            var result = collectionSetAlbumSystem.TryClaimReward(
                CurrentSave.collectionSetAlbum,
                CurrentSave.collections,
                setId,
                receiptKey);
            if (!result.Applied)
            {
                return result;
            }

            adjustedReward = ApplyPostgameResearchCollectionReward(result.Reward);
            CurrentSave.economy.milkCoins += adjustedReward.Coins;
            CurrentSave.economy.milkDrops += adjustedReward.MilkDrops;
            CurrentSave.economy.collectionFragments += adjustedReward.CollectionFragments;
            SaveGame();
            JourneyHubChanged?.Invoke();
            return new CollectionSetAlbumClaimResult(
                result.Status,
                result.SetId,
                result.ReceiptKey,
                adjustedReward);
        }

        public PostgameResearchSnapshot GetPostgameResearchSnapshot()
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return postgameResearchSystem.BuildSnapshot(
                    0,
                    null,
                    default);
            }

            CurrentSave.EnsureRuntimeDefaults();
            return postgameResearchSystem.BuildSnapshot(
                CurrentTama.level,
                CurrentSave.postgameResearch,
                BuildPostgameResearchSources());
        }

        public PostgameResearchProfileEffectSnapshot GetPostgameResearchProfileEffectSnapshot()
        {
            return postgameResearchProfileEffectSystem.BuildSnapshot(
                CurrentTama?.level ?? 0,
                CurrentSave?.postgameResearch);
        }

        public bool TrySelectPostgameResearchProfile(string profileId)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!postgameResearchSystem.TrySelectProfile(
                    CurrentTama.level,
                    CurrentSave.postgameResearch,
                    profileId))
            {
                return false;
            }

            SaveGame();
            JourneyHubChanged?.Invoke();
            return true;
        }

        public PostgameResearchUnlockResult TryUnlockPostgameResearchNode(string nodeId)
        {
            return TryUnlockPostgameResearchNode(
                nodeId,
                $"postgame-research:{nodeId}",
                DateTimeOffset.Now);
        }

        public PostgameResearchUnlockResult TryUnlockPostgameResearchNode(
            string nodeId,
            string receiptKey,
            DateTimeOffset completedAt)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return new PostgameResearchUnlockResult(
                    PostgameResearchUnlockStatus.MissingState,
                    nodeId,
                    receiptKey,
                    string.Empty);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var result = postgameResearchSystem.TryUnlock(
                CurrentTama.level,
                CurrentSave.postgameResearch,
                BuildPostgameResearchSources(),
                nodeId,
                receiptKey);
            if (!result.Applied)
            {
                return result;
            }

            var definition = PostgameResearchCatalog.Find(result.NodeId);
            AddUniqueRecord(CurrentSave.collections.events, result.NodeId);
            var memoryAlreadyRecorded = definition != null
                && HasMemorySource(result.NodeId);
            var memoryRecorded = !memoryAlreadyRecorded
                && definition != null
                && memoryJournalSystem.TryRecord(
                    CurrentSave.memoryJournal,
                    new MemoryJournalDraft(
                        MemoryJournalKind.Story,
                        result.NodeId,
                        result.ReceiptKey,
                        result.ProfileId,
                        completedAt,
                        CurrentTama.name,
                        CurrentTama.form,
                        definition.Title,
                        definition.Detail,
                        true),
                    out _);
            SaveGame();
            if (memoryRecorded)
            {
                MemoryJournalChanged?.Invoke();
            }

            JourneyHubChanged?.Invoke();
            PostgameResearchChanged?.Invoke(result);
            return result;
        }

        private PostgameResearchSourceSnapshot BuildPostgameResearchSources()
        {
            var save = CurrentSave;
            return new PostgameResearchSourceSnapshot(
                save?.starLegacy?.maturationCycle?.completedCycles ?? 0,
                save?.milkBlending?.masteryResearchRecordIds?.Count ?? 0,
                save?.collectionSetAlbum?.claimedSetIds?.Count ?? 0,
                save?.playMiniGames?.totalBouncyJumpSuccesses ?? 0,
                save?.npcRelationshipEpisodes?.completedEpisodeIds?.Count ?? 0);
        }

        private bool ReconcilePostgameResearchRecords(DateTimeOffset reconciledAt)
        {
            var state = CurrentSave?.postgameResearch;
            var collections = CurrentSave?.collections;
            var journal = CurrentSave?.memoryJournal;
            if (state?.unlockedNodeIds == null
                || collections?.events == null
                || journal == null
                || CurrentTama == null)
            {
                return false;
            }

            var changed = false;
            for (var index = 0; index < state.unlockedNodeIds.Count; index += 1)
            {
                var nodeId = state.unlockedNodeIds[index];
                var definition = PostgameResearchCatalog.Find(nodeId);
                if (definition == null)
                {
                    continue;
                }

                if (!ContainsOrdinal(collections.events, nodeId))
                {
                    AddUniqueRecord(collections.events, nodeId);
                    changed = true;
                }

                if (HasMemorySource(nodeId))
                {
                    continue;
                }

                var occurrenceId = $"postgame-research-reconcile:{nodeId}:{reconciledAt.UtcDateTime.Ticks}";
                if (memoryJournalSystem.TryRecord(
                        journal,
                        new MemoryJournalDraft(
                            MemoryJournalKind.Story,
                            nodeId,
                            occurrenceId,
                            definition.ProfileId,
                            reconciledAt,
                            CurrentTama.name,
                            CurrentTama.form,
                            definition.Title,
                            definition.Detail,
                            true),
                        out _))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private DecorationWorkshopWalletSnapshot GetDecorationWorkshopWalletSnapshot()
        {
            var economy = CurrentSave?.economy;
            return new DecorationWorkshopWalletSnapshot(
                economy?.milkCoins ?? 0,
                economy?.milkDrops ?? 0,
                economy?.collectionFragments ?? 0);
        }

        private static bool CanAddCollectionSetReward(
            EconomySaveData economy,
            CollectionSetAlbumReward reward)
        {
            return economy != null
                && (long)economy.milkCoins + reward.Coins <= int.MaxValue
                && (long)economy.milkDrops + reward.MilkDrops <= int.MaxValue
                && (long)economy.collectionFragments + reward.CollectionFragments <= int.MaxValue;
        }

        private CollectionSetAlbumReward ApplyPostgameResearchCollectionReward(
            CollectionSetAlbumReward reward)
        {
            var effect = GetPostgameResearchProfileEffectSnapshot();
            return new CollectionSetAlbumReward(
                reward.Coins,
                reward.MilkDrops,
                effect.ApplyCollectionFragmentReward(reward.CollectionFragments));
        }

        public MilkBlendingPanelSnapshot GetMilkBlendingSnapshot()
        {
            if (CurrentSave == null)
            {
                return MilkBlendingPanelSnapshot.CreateDefault();
            }

            CurrentSave.EnsureRuntimeDefaults();
            return milkBlendingSystem.BuildSnapshot(
                CurrentSave.milkBlending,
                CurrentSave.economy,
                IsMilkUnlocked);
        }

        public SleepScheduleSnapshot GetSleepScheduleSnapshot()
        {
            return GetSleepScheduleSnapshot(DateTimeOffset.Now);
        }

        public SleepScheduleSnapshot GetSleepScheduleSnapshot(DateTimeOffset now)
        {
            var snapshot = sleepScheduleSystem.BuildSnapshot(
                CurrentSave?.sleepSchedule,
                CurrentTama,
                now);
            if (snapshot.StateWasNormalized && CurrentSave != null)
            {
                SaveGame();
                SleepScheduleChanged?.Invoke();
            }

            return snapshot;
        }

        public SleepScheduleStartResult StartSleepSchedule(int scheduledHours)
        {
            return StartSleepSchedule(
                scheduledHours,
                $"sleep_{Guid.NewGuid():N}",
                DateTimeOffset.Now);
        }

        public SleepScheduleStartResult StartSleepSchedule(
            int scheduledHours,
            string receiptKey,
            DateTimeOffset now)
        {
            var result = sleepScheduleSystem.TryStart(
                CurrentSave?.sleepSchedule,
                CurrentTama,
                scheduledHours,
                receiptKey,
                now);
            if (result.StateChanged && CurrentSave != null)
            {
                SaveGame();
                SleepScheduleChanged?.Invoke();
            }

            return result;
        }

        public SleepScheduleWakeResult WakeSleepSchedule()
        {
            return WakeSleepSchedule(DateTimeOffset.Now);
        }

        public SleepScheduleWakeResult WakeSleepSchedule(DateTimeOffset now)
        {
            var snapshot = GetSleepScheduleSnapshot(now);
            var result = snapshot.IsDue
                ? sleepScheduleSystem.TryCompleteDue(
                    CurrentSave?.sleepSchedule,
                    CurrentTama,
                    now)
                : sleepScheduleSystem.TryWakeEarly(
                    CurrentSave?.sleepSchedule,
                    CurrentTama,
                    now);
            if (!result.StateChanged || CurrentSave == null)
            {
                return result;
            }

            if (result.Applied && result.ElapsedMinutes >= 30)
            {
                RegisterCareAction("rest");
                RegisterDailyCareAction("rest");
                RefreshDerivedCollectionRecords();
            }

            SaveGame();
            SleepScheduleChanged?.Invoke();
            return result;
        }

        public MilkBlendResult TryBlendMilk(string milkId, string ingredientId)
        {
            return TryBlendMilk(milkId, ingredientId, false);
        }

        public MilkBlendResult TryBlendMilk(
            string milkId,
            string ingredientId,
            bool manualStirCompleted)
        {
            return TryBlendMilk(
                milkId,
                ingredientId,
                $"blend_{Guid.NewGuid():N}",
                DateTimeOffset.Now,
                UnityEngine.Random.value,
                manualStirCompleted);
        }

        public MilkBlendResult TryBlendMilk(
            string milkId,
            string ingredientId,
            string receiptKey,
            DateTimeOffset blendedAt)
        {
            return TryBlendMilk(
                milkId,
                ingredientId,
                receiptKey,
                blendedAt,
                UnityEngine.Random.value);
        }

        public MilkBlendResult TryBlendMilk(
            string milkId,
            string ingredientId,
            string receiptKey,
            DateTimeOffset blendedAt,
            double specialResultRoll)
        {
            return TryBlendMilk(
                milkId,
                ingredientId,
                receiptKey,
                blendedAt,
                specialResultRoll,
                false);
        }

        public MilkBlendResult TryBlendMilk(
            string milkId,
            string ingredientId,
            string receiptKey,
            DateTimeOffset blendedAt,
            double specialResultRoll,
            bool manualStirCompleted)
        {
            var effect = GetPostgameResearchProfileEffectSnapshot();
            var adjustedSpecialResultRoll = starLineageSystem.ApplyBlendingSpecialResultRoll(
                effect.ApplyBlendingSpecialResultRoll(specialResultRoll),
                CurrentSave?.starLineage);
            var result = milkBlendingSystem.TryBlend(
                CurrentSave?.milkBlending,
                CurrentTama,
                CurrentSave?.economy,
                CurrentSave?.snackInventory,
                milkId,
                ingredientId,
                IsMilkUnlocked,
                receiptKey,
                blendedAt,
                adjustedSpecialResultRoll,
                manualStirCompleted);
            if (result == null || !result.applied || CurrentSave == null)
            {
                return result;
            }

            RegisterCareAction("blend");
            RegisterDailyCareAction("blend");
            AddUniqueRecord(CurrentSave.collections.events, $"milk_blend_{result.resultSnackId}");
            var masteryRecordIds = result.newMasteryResearchRecordIds;
            for (var index = 0; index < masteryRecordIds.Count; index += 1)
            {
                var researchRecord = MilkBlendingCatalog.FindMasteryResearchRecord(
                    masteryRecordIds[index]);
                if (researchRecord == null)
                {
                    continue;
                }

                AddUniqueRecord(CurrentSave.collections.events, researchRecord.recordId);
                if (memoryJournalSystem.TryRecord(
                        CurrentSave.memoryJournal,
                        new MemoryJournalDraft(
                            MemoryJournalKind.Story,
                            researchRecord.recordId,
                            result.receiptKey,
                            result.ingredientId,
                            blendedAt,
                            CurrentTama.name,
                            CurrentTama.form,
                            researchRecord.title,
                            researchRecord.detail),
                        out _))
                {
                    MemoryJournalChanged?.Invoke();
                }
            }

            if (result.firstDiscovery
                && memoryJournalSystem.TryRecord(
                    CurrentSave.memoryJournal,
                    new MemoryJournalDraft(
                        MemoryJournalKind.Story,
                        $"milk_blend_{result.resultSnackId}",
                        result.receiptKey,
                        result.ingredientId,
                        blendedAt,
                        CurrentTama.name,
                        CurrentTama.form,
                        "새 우유 섞기 발견",
                        result.message),
                    out _))
            {
                MemoryJournalChanged?.Invoke();
            }

            RefreshDerivedCollectionRecords();
            SaveGame();
            MilkBlendingChanged?.Invoke(result);
            return result;
        }

        public StarLegacyPanelViewModel GetStarLegacyViewModel()
        {
            if (CurrentSave == null)
            {
                return StarLegacyPanelViewModel.Hidden();
            }

            CurrentSave.EnsureRuntimeDefaults();
            return StarLegacyPanelViewModel.Create(
                starLegacyEvolutionSystem.Evaluate(
                    CurrentTama,
                    CurrentSave.unlocks,
                    CurrentSave.starLegacy),
                finalMaturationCycleSystem.BuildSnapshot(
                    CurrentSave.starLegacy.maturationCycle));
        }

        public StarEggGenerationEligibilityStatus GetStarEggGenerationEligibility()
        {
            if (CurrentSave == null)
            {
                return StarEggGenerationEligibilityStatus.MissingState;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (CurrentSave.starLineage.pendingTraitGenerationNumber > 0)
            {
                return StarEggGenerationEligibilityStatus.PendingLineageTraitSelection;
            }

            return starLegacyEvolutionSystem.EvaluateNewGenerationEligibility(
                CurrentTama,
                CurrentSave.unlocks,
                CurrentSave.starLegacy);
        }

        public StarLineageSnapshot GetStarLineageSnapshot()
        {
            if (CurrentSave == null)
            {
                return starLineageSystem.BuildSnapshot(null);
            }

            CurrentSave.EnsureRuntimeDefaults();
            return starLineageSystem.BuildSnapshot(CurrentSave.starLineage);
        }

        public StarLineageSelectionResult TrySelectStarLineageTrait(string traitId)
        {
            if (CurrentSave == null)
            {
                return starLineageSystem.TrySelectTrait(null, traitId, string.Empty);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var result = starLineageSystem.TrySelectTrait(
                CurrentSave.starLineage,
                traitId,
                $"star-lineage-trait:{Guid.NewGuid():N}");
            if (!result.Applied)
            {
                return result;
            }

            SaveGame();
            StarLineageChanged?.Invoke();
            JourneyHubChanged?.Invoke();
            return result;
        }

        public bool BeginStarEggGeneration()
        {
            return BeginStarEggGeneration(
                $"ct_star_{Guid.NewGuid():N}",
                DateTimeOffset.Now).applied;
        }

        public StarEggGenerationStartResult BeginStarEggGeneration(
            string nextTamaId,
            DateTimeOffset startedAt)
        {
            if (CurrentSave == null)
            {
                return starLegacyEvolutionSystem.TryBeginStarEggGeneration(
                    null,
                    null,
                    null,
                    nextTamaId,
                    startedAt);
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (CurrentSave.starLineage.pendingTraitGenerationNumber > 0)
            {
                return new StarEggGenerationStartResult(
                    StarEggGenerationStartStatus.PendingLineageTraitSelection,
                    null,
                    string.Empty,
                    0,
                    string.Empty);
            }

            var outgoingTama = CurrentTama;
            var result = starLegacyEvolutionSystem.TryBeginStarEggGeneration(
                outgoingTama,
                CurrentSave.unlocks,
                CurrentSave.starLegacy,
                nextTamaId,
                startedAt);
            if (!result.applied)
            {
                return result;
            }

            var lineageResult = starLineageSystem.RecordTransition(
                CurrentSave.starLineage,
                outgoingTama,
                result.generationNumber,
                startedAt,
                $"star-lineage-transition:{Guid.NewGuid():N}");
            if (!lineageResult.Applied)
            {
                throw new InvalidOperationException(
                    $"Failed to record star lineage transition: {lineageResult.Status}");
            }

            var sleepScheduleWasActive = CurrentSave.sleepSchedule?.HasActiveSession == true;
            CurrentSave.sleepSchedule?.ClearActiveSession();
            CurrentSave.cheeseTama = result.nextTama;
            CurrentSave.newGameSetup ??= NewGameSetupSaveData.CreateCompletedForLegacySave();
            CurrentSave.newGameSetup.selectedEggId = NewGameSetupCatalog.StarEggId;
            CurrentSave.newGameSetup.outcomeApplied = true;
            CurrentSave.growthMilestone = GrowthMilestoneSaveData.CreateAcknowledged(
                CheeseTamaGrowthStage.Egg);
            CurrentSave.evolutionMilestone = EvolutionMilestoneSaveData.CreateAcknowledged(
                string.Empty);
            pendingGrowthMilestone = null;
            pendingEvolutionMilestone = null;
            SaveGame();
            SaveDataReplaced?.Invoke();
            if (sleepScheduleWasActive)
            {
                SleepScheduleChanged?.Invoke();
            }
            StarLegacyChanged?.Invoke();
            StarLineageChanged?.Invoke();
            return result;
        }

        public bool AdoptStarEgg()
        {
            return BeginStarEggGeneration();
        }

        public EmmentalEvolutionAttemptResult TryEvolveEmmental()
        {
            if (CurrentSave == null)
            {
                return default;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var result = starLegacyEvolutionSystem.TryApplyEvolution(
                CurrentTama,
                CurrentSave.unlocks,
                CurrentSave.starLegacy,
                $"emmental:{CurrentSave.playerId}:{CurrentTama.id}",
                DateTimeOffset.Now);
            if (!result.applied)
            {
                return result;
            }

            AddUniqueRecord(CurrentSave.collections.evolution,
                StarEggEmmentalEvolutionSystem.EmmentalEvolutionId);
            pendingEvolutionMilestone = result.CreateMilestone(
                $"emmental:{CurrentTama.id}",
                CurrentTama.level);
            RecordMemoryEvolution(pendingEvolutionMilestone);
            SaveGame();
            EvolutionMilestoneAvailable?.Invoke(pendingEvolutionMilestone);
            StarLegacyChanged?.Invoke();
            return result;
        }

        public FinalMaturationClaimResult ClaimFinalMaturationReward()
        {
            if (CurrentSave == null)
            {
                return default;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var next = CurrentSave.starLegacy.maturationCycle.pendingRewards.Count > 0
                ? CurrentSave.starLegacy.maturationCycle.pendingRewards[0]?.rewardId
                : string.Empty;
            var result = finalMaturationCycleSystem.TryClaimNext(
                CurrentSave.starLegacy.maturationCycle,
                CurrentSave.economy,
                CurrentSave.fantasyPowder,
                $"claim:{next}");
            if (result.applied)
            {
                SaveGame();
                FantasyPowderChanged?.Invoke();
                StarLegacyChanged?.Invoke();
            }

            return result;
        }

        public IReadOnlyList<HiddenCareerCardViewData> GetVisibleHiddenCareerCards()
        {
            return hiddenCareerCardSystem.GetVisibleUnlockedCards(CurrentSave?.collections);
        }

        public HiddenCareerBenefitSet GetHiddenCareerBenefits()
        {
            return hiddenCareerCardSystem.GetBenefitSet(CurrentSave?.collections);
        }

        public BondProfileSnapshot GetBondProfile()
        {
            return bondReactionSystem.Observe(CurrentSave);
        }

        public BondReactionResult GetBondReaction(
            BondInteraction interaction,
            string subjectId = "")
        {
            return bondReactionSystem.Evaluate(CurrentSave, interaction, subjectId);
        }

        public CheeseStarDeliveryOffer ObserveCheeseStarDelivery()
        {
            if (CurrentSave == null)
            {
                return CheeseStarDeliverySystem.ObserveEntry(null, false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var offer = CheeseStarDeliverySystem.ObserveEntry(
                CurrentSave.cheeseStarDelivery,
                CurrentSave.unlocks.starMilkUnlocked);
            if (offer.StateChanged)
            {
                SaveGame();
                CheeseStarDeliveryChanged?.Invoke();
            }

            return offer;
        }

        public CheeseStarDeliveryClaimResult ClaimCheeseStarDelivery()
        {
            if (CurrentSave == null)
            {
                return null;
            }

            if (!CheeseStarDeliverySystem.TryClaim(
                    CurrentSave.cheeseStarDelivery,
                    CurrentSave.unlocks.starMilkUnlocked,
                    out var result))
            {
                return result;
            }

            CurrentSave.economy.milkCoins = SaturatingAdd(
                CurrentSave.economy.milkCoins,
                result.Reward.MilkCoins);
            CurrentSave.economy.milkDrops = SaturatingAdd(
                CurrentSave.economy.milkDrops,
                result.Reward.MilkDrops);
            CurrentSave.economy.starDrops = SaturatingAdd(
                CurrentSave.economy.starDrops,
                result.Reward.StarDrops);
            if (result.Reward.FantasyPowder > 0)
            {
                fantasyPowderSystem.GrantPowder(
                    CurrentSave.fantasyPowder,
                    result.Reward.FantasyPowder);
                FantasyPowderChanged?.Invoke();
            }
            SaveGame();
            CheeseStarDeliveryChanged?.Invoke();
            return result;
        }

        public FantasyPowderPanelSnapshot GetFantasyPowderSnapshot()
        {
            if (CurrentSave == null)
            {
                return FantasyPowderPanelSnapshot.CreateHidden();
            }

            CurrentSave.EnsureRuntimeDefaults();
            var careerBenefits = GetHiddenCareerBenefits();
            return fantasyPowderSystem.BuildSnapshot(
                CurrentSave.unlocks,
                CurrentSave.fantasyPowder,
                careerBenefits.RecipeHintProgress);
        }

        public FantasyPowderAttemptResult TryAttemptFantasyPowderRecipe(string recipeId)
        {
            if (CurrentSave == null)
            {
                return fantasyPowderSystem.TryAttempt(
                    null,
                    null,
                    null,
                    null,
                    recipeId,
                    string.Empty,
                    0d);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var careerBenefits = GetHiddenCareerBenefits();
            var result = fantasyPowderSystem.TryAttempt(
                CurrentSave.unlocks,
                CurrentSave.fantasyPowder,
                CurrentSave.snackInventory,
                CurrentSave.economy,
                recipeId,
                Guid.NewGuid().ToString("N"),
                UnityEngine.Random.value,
                careerBenefits.RareByproductWeightPercent);
            if (!result.applied)
            {
                return result;
            }

            if (starLegacyEvolutionSystem.RecordFantasyResonance(
                    CurrentTama,
                    CurrentSave.unlocks,
                    CurrentSave.starLegacy,
                    1) > 0)
            {
                StarLegacyChanged?.Invoke();
            }

            if (result.newDiscovery)
            {
                AddUniqueRecord(CurrentSave.collections.events, "fantasy_recipe_discovered");
            }

            SaveGame();
            FantasyPowderChanged?.Invoke();
            return result;
        }

        public bool RecordMemoryReturn(ReturnSummaryData summary)
        {
            if (CurrentSave == null || summary == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!memoryJournalSystem.TryRecordReturn(
                    CurrentSave.memoryJournal,
                    summary.id,
                    summary.elapsedMinutes,
                    DateTimeOffset.Now,
                    CurrentTama.name,
                    CurrentTama.form,
                    out _))
            {
                return false;
            }

            SaveGame();
            MemoryJournalChanged?.Invoke();
            return true;
        }

        public bool RecordMemoryGrowth(GrowthMilestoneData milestone)
        {
            if (CurrentSave == null || milestone == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var stageDefinition = CheeseTamaGrowthStageCatalog.Get(milestone.stage);
            var stableMilestoneId = string.IsNullOrWhiteSpace(stageDefinition.RecordId)
                ? $"growth_stage_{(int)milestone.stage}"
                : stageDefinition.RecordId;
            if (!memoryJournalSystem.TryRecordGrowth(
                    CurrentSave.memoryJournal,
                    stableMilestoneId,
                    stableMilestoneId,
                    milestone.level,
                    CheeseTamaGrowthStageCatalog.Get(milestone.stage).DisplayName,
                    DateTimeOffset.Now,
                    CurrentTama.name,
                    CurrentTama.form,
                    false,
                    string.Empty,
                    out _))
            {
                return false;
            }

            SaveGame();
            MemoryJournalChanged?.Invoke();
            return true;
        }

        public bool RecordMemoryEvolution(EvolutionMilestoneData milestone)
        {
            if (CurrentSave == null || milestone == null || !milestone.result.HasEvolution)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var stableEvolutionId = milestone.result.EvolutionId;
            if (!memoryJournalSystem.TryRecordEvolution(
                    CurrentSave.memoryJournal,
                    stableEvolutionId,
                    stableEvolutionId,
                    CurrentTama.level,
                    milestone.result.DisplayName,
                    DateTimeOffset.Now,
                    CurrentTama.name,
                    CurrentTama.form,
                    false,
                    string.Empty,
                    out _))
            {
                return false;
            }

            SaveGame();
            MemoryJournalChanged?.Invoke();
            return true;
        }

        public bool AcknowledgeLatestMemoryRecall(string memoryId)
        {
            if (CurrentSave == null
                || !memoryJournalSystem.AcknowledgeRecall(CurrentSave.memoryJournal, memoryId))
            {
                return false;
            }

            SaveGame();
            MemoryJournalChanged?.Invoke();
            return true;
        }

        public bool MarkFirstDayJourneyShown()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!FirstDayJourneySystem.MarkIntroShown(CurrentSave.firstDayJourney))
            {
                return false;
            }

            SaveGame();
            FirstDayJourneyChanged?.Invoke();
            return true;
        }

        public bool RecordFirstDayJourneyCollectionOpened()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!FirstDayJourneySystem.TryRecordCollectionOpened(
                    CurrentSave.firstDayJourney,
                    DateTimeOffset.Now))
            {
                return false;
            }

            SaveGame();
            FirstDayJourneyChanged?.Invoke();
            return true;
        }

        public FirstDayJourneyRewardResult ClaimFirstDayJourneyReward()
        {
            if (CurrentSave == null)
            {
                return default;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var result = FirstDayJourneySystem.ClaimCompletionReward(CurrentSave.firstDayJourney);
            if (!result.Granted)
            {
                return result;
            }

            CurrentSave.economy.milkCoins = SaturatingAdd(
                CurrentSave.economy.milkCoins,
                result.MilkCoins);
            CurrentSave.economy.milkDrops = SaturatingAdd(
                CurrentSave.economy.milkDrops,
                result.MilkDrops);
            CurrentSave.economy.collectionFragments = SaturatingAdd(
                CurrentSave.economy.collectionFragments,
                result.CollectionFragments);
            SaveGame();
            FirstDayJourneyChanged?.Invoke();
            return result;
        }

        public StarRouteProgress GetStarRouteProgress()
        {
            var progress = StarRouteSystem.Evaluate(CurrentTama, CurrentSave?.milkGrowth);
            if (!(CurrentSave?.starLegacy?.starRoutePermanentlyUnlocked ?? false))
            {
                return progress;
            }

            return new StarRouteProgress(
                progress.level,
                progress.maximumLevel,
                progress.completedMilkCount,
                progress.requiredMilkCount,
                true,
                "별빛 길은 이후 세대에도 계속 열려 있습니다.");
        }

        public bool AcknowledgeStarRouteUnlock()
        {
            if (!HasPendingStarRouteUnlock)
            {
                return false;
            }

            CurrentSave.starRoute.unlockAcknowledged = true;
            AddUniqueRecord(CurrentSave.collections.events, StarRouteEnteredEventId);
            SaveGame();
            return true;
        }

        public DecorationShopSnapshot GetDecorationShopSnapshot()
        {
            if (CurrentSave == null)
            {
                return DecorationShopSnapshot.CreateDefault();
            }

            CurrentSave.EnsureRuntimeDefaults();
            var decoration = CurrentSave.decorations;
            return new DecorationShopSnapshot(
                CurrentSave.economy.milkCoins,
                CurrentSave.economy.milkDrops,
                CurrentSave.economy.collectionFragments,
                decoration.ownedItemIds,
                decoration.equippedWallId,
                decoration.equippedFloorId,
                decoration.equippedAccentId,
                decoration.equippedWindowId,
                decoration.equippedShelfId,
                decoration.equippedBedsideId);
        }

        public DecorationTransactionResult TryPurchaseDecoration(string itemId)
        {
            if (CurrentSave == null)
            {
                return DecorationShopRules.Purchase(itemId, DecorationShopSnapshot.CreateDefault());
            }

            var result = DecorationShopRules.Purchase(itemId, GetDecorationShopSnapshot());
            if (!result.Succeeded)
            {
                return result;
            }

            ApplyDecorationSnapshot(result.snapshot);
            SaveGame();
            DecorationChanged?.Invoke();
            return result;
        }

        public DecorationTransactionResult TryEquipDecoration(string itemId)
        {
            if (CurrentSave == null)
            {
                return DecorationShopRules.ToggleEquip(itemId, DecorationShopSnapshot.CreateDefault());
            }

            var result = DecorationShopRules.ToggleEquip(itemId, GetDecorationShopSnapshot());
            if (!result.Succeeded)
            {
                return result;
            }

            ApplyDecorationSnapshot(result.snapshot);
            SaveGame();
            DecorationChanged?.Invoke();
            return result;
        }

        public MilkroomThemeUnlockResult TryUnlockMilkroomTheme(string themeId)
        {
            var result = milkroomThemeUnlockSystem.TryUnlock(CurrentSave, themeId);
            if (!result.Succeeded || CurrentSave == null)
            {
                return result;
            }

            CurrentSave.milkroomThemeId = result.ThemeId;
            SaveGame();
            DecorationChanged?.Invoke();
            return result;
        }

        public bool TrySelectMilkroomTheme(string themeId)
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var themeDefinition = MilkroomThemeCatalog.Find(themeId);
            if (themeDefinition == null)
            {
                return false;
            }

            var normalizedThemeId = themeDefinition.Id;
            if (!milkroomThemeUnlockSystem.IsVisible(CurrentSave, normalizedThemeId)
                || !milkroomThemeUnlockSystem.IsOwned(CurrentSave, normalizedThemeId))
            {
                return false;
            }

            CurrentSave.milkroomThemeId = normalizedThemeId;
            SaveGame();
            DecorationChanged?.Invoke();
            return true;
        }

        public DecorationPlacementSnapshot GetDecorationPlacementSnapshot()
        {
            if (CurrentSave == null)
            {
                return DecorationPlacementSystem.MigrateFromLegacySlots(
                    DecorationCatalog.GetDefault(DecorationSlot.Accent)?.id,
                    DecorationCatalog.GetDefault(DecorationSlot.Shelf)?.id,
                    DecorationCatalog.GetDefault(DecorationSlot.Bedside)?.id);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var decoration = CurrentSave.decorations;
            return CurrentSave.decorationPlacement.GetSnapshot(
                decoration.equippedAccentId,
                decoration.equippedShelfId,
                decoration.equippedBedsideId);
        }

        public bool TryCommitDecorationPlacement(DecorationPlacementSnapshot snapshot)
        {
            if (CurrentSave == null || snapshot == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var decoration = CurrentSave.decorations;
            var reconciled = DecorationPlacementSystem.ReconcileWithEquippedSlots(
                snapshot,
                decoration.equippedAccentId,
                decoration.equippedShelfId,
                decoration.equippedBedsideId);
            var changed = CurrentSave.decorationPlacement.ReplaceSnapshot(reconciled);
            if (changed)
            {
                SaveGame();
                DecorationChanged?.Invoke();
            }

            return true;
        }

        private void ApplyDecorationSnapshot(DecorationShopSnapshot snapshot)
        {
            if (CurrentSave == null || snapshot == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            CurrentSave.economy.milkCoins = snapshot.milkCoins;
            CurrentSave.economy.milkDrops = snapshot.milkDrops;
            CurrentSave.economy.collectionFragments = snapshot.collectionFragments;
            CurrentSave.decorations.ownedItemIds = new List<string>(snapshot.OwnedItemIds);
            CurrentSave.decorations.equippedWallId = snapshot.equippedWallId;
            CurrentSave.decorations.equippedFloorId = snapshot.equippedFloorId;
            CurrentSave.decorations.equippedAccentId = snapshot.equippedAccentId;
            CurrentSave.decorations.equippedWindowId = snapshot.equippedWindowId;
            CurrentSave.decorations.equippedShelfId = snapshot.equippedShelfId;
            CurrentSave.decorations.equippedBedsideId = snapshot.equippedBedsideId;
            CurrentSave.decorationPlacement.EnsureRuntimeDefaults(
                snapshot.equippedAccentId,
                snapshot.equippedShelfId,
                snapshot.equippedBedsideId);
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Scene authoring adds a serialized GameManager in edit mode.
                // Loading here would advance or migrate the player's real save
                // merely because an editor utility rebuilt a scene.
                return;
            }
#endif

            if (Instance != null && Instance != this)
            {
                DestroyGameObjectSafely(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LocalAccountRuntime.EnsureInitialized();
            dataRegistry = dataRegistry != null ? dataRegistry : GetComponent<DataRegistry>();
            saveManager = saveManager != null ? saveManager : GetComponent<SaveManager>();
            if (saveManager != null && LocalAccountRuntime.Snapshot.HasPendingDeletion)
            {
                LocalAccountRuntime.TryResumePendingDeletion(
                    saveManager.TryPurgeLocalAccountSaveArtifacts);
            }
            LoadOrCreateGame();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            RefreshApplicationSuspendedState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            applicationHasFocus = hasFocus;
            RefreshApplicationSuspendedState();
        }

        private void OnApplicationQuit()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif

            applicationQuitting = true;
            SaveGame();
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

            pendingGrowthMilestone = null;
            pendingCareEvent = CareEventResult.None();
            pendingEvolutionMilestone = null;
            lastMilkGrowthMilestoneReward = MilkGrowthMilestoneRewardResult.None;
            CurrentSave = saveManager.LoadOrCreate();
            var retiredSleepScheduleCleared = CurrentSave?.sleepSchedule?.HasActiveSession == true;
            CurrentSave?.EnsureRuntimeDefaults();
            if (CurrentSave?.sleepSchedule?.HasActiveSession == true)
            {
                // Scheduled sleep is no longer player-facing. Clear only the
                // obsolete active session so old saves cannot remain input-
                // blocked, while preserving completed recovery receipts.
                CurrentSave.sleepSchedule.ClearActiveSession();
            }
            var durableStarRouteChanged = ReconcileDurableStarRouteUnlock();
            RestorePendingCareEventFromSave();
            var setupOutcomeChanged = ApplyNewGameSetupOutcomeIfNeeded();
            var fantasyStarterChanged = EnsureFantasyPowderStarterGrant();
            var now = DateTimeOffset.Now;
            var dailyCareChanged = EnsureDailyCareDate();
            var sessionDateChanged = EnsureMilkroomSessionDate();
            var postgameRecordsChanged = ReconcilePostgameResearchRecords(now);
            var dreamStoryStateChanged = CurrentSave != null
                && GetDreamStorySeasonSystem().NormalizeState(CurrentSave.dreamStorySeason);
            var lifeChapterStateChanged = CurrentSave != null
                && lifeChapterSystem.NormalizeState(CurrentSave.lifeChapters);
            var investigationStateChanged = CurrentSave != null
                && GetMilkroomInvestigationSystem().NormalizeState(
                    CurrentSave.milkroomInvestigation);
            var starLineageStateChanged = CurrentSave != null
                && starLineageSystem.NormalizeState(CurrentSave.starLineage);
            var masteryResult = miniGameMasterySystem.Reconcile(
                GetLifeRecordsSnapshot(),
                CurrentSave?.miniGameMastery,
                now);
            if (CurrentSave != null)
            {
                CurrentSave.miniGameMastery = masteryResult.State;
            }
            var journeyStateChanged = npcRelationshipQuestSystem.NormalizeState(
                    CurrentSave?.npcRelationshipQuests)
                | npcRelationshipQuestSystem.NormalizeRelationships(CurrentSave?.npcVisits)
                | decorationWorkshopSystem.NormalizeState(CurrentSave?.decorationWorkshop);
            var activeQuestWasExpired = CurrentSave?.npcRelationshipQuests?.activeQuest
                ?.terminalExpired ?? false;
            npcRelationshipQuestSystem.ObserveActive(
                CurrentSave?.npcRelationshipQuests,
                now);
            journeyStateChanged |= !activeQuestWasExpired
                && (CurrentSave?.npcRelationshipQuests?.activeQuest?.terminalExpired ?? false);
            journeyStateChanged |= weeklyCareJourneySystem.ReconcileWeek(
                    CurrentSave?.weeklyCareJourney,
                    now)
                .StateChanged;
            journeyStateChanged |= collectionSetAlbumSystem.RecalculateProgress(
                    CurrentSave?.collectionSetAlbum,
                    CurrentSave?.collections) > 0;
            presenceSessionStarted = false;
            LastTimeProgression = ApplyOfflineProgressAndPrepareSummary(now);
            if (LastTimeProgression.applied
                || dailyCareChanged
                || sessionDateChanged
                || setupOutcomeChanged
                || fantasyStarterChanged
                || durableStarRouteChanged
                || postgameRecordsChanged
                || dreamStoryStateChanged
                || lifeChapterStateChanged
                || investigationStateChanged
                || starLineageStateChanged
                || masteryResult.StateChanged
                || journeyStateChanged
                || retiredSleepScheduleCleared)
            {
                saveManager.Save(CurrentSave);
            }
            else if (saveManager.LastLoadMigratedData)
            {
                saveManager.SaveMigration(CurrentSave);
            }

            QueueCurrentGrowthMilestoneIfNeeded();
            ResolveNormalEvolutionIfEligible();
            if (starLegacyEvolutionSystem.ReconcileAfterLoad(
                    CurrentTama,
                    CurrentSave.starLegacy,
                    now))
            {
                saveManager.SaveMigration(CurrentSave);
            }
            QueueCurrentEvolutionMilestoneIfNeeded();
            SaveDataReplaced?.Invoke();
            if (pendingReturnSummary != null)
            {
                ReturnSummaryAvailable?.Invoke(pendingReturnSummary);
            }

            if (HasPendingStarRouteUnlock)
            {
                StarRouteUnlockAvailable?.Invoke();
            }
        }

        public void ReloadGame()
        {
            LoadOrCreateGame();
        }

        public LocalAccountAttemptResult RegisterLocalAccount(
            string email,
            string password,
            string confirmation)
        {
            if (!CanUseLocalAccountData())
            {
                return LocalAccountDataFailure();
            }

            var initializedAccountId = string.Empty;
            var result = LocalAccountRuntime.TrySignUp(
                email,
                password,
                confirmation,
                accountId =>
                {
                    saveManager.Save(CurrentSave);
                    if (!saveManager.TryInitializeLocalAccountSave(accountId, CurrentSave))
                    {
                        return false;
                    }

                    initializedAccountId = accountId;
                    return true;
                });
            if (!result.Succeeded && LocalAccountRuntime.IsValidAccountId(initializedAccountId))
            {
                saveManager.TryPurgeLocalAccountSaveArtifacts(initializedAccountId);
            }

            return CompleteLocalAccountTransition(result);
        }

        public LocalAccountAttemptResult LoginLocalAccount(string email, string password)
        {
            if (!CanUseLocalAccountData())
            {
                return LocalAccountDataFailure();
            }

            var result = LocalAccountRuntime.TrySignIn(
                email,
                password,
                TryPrepareLocalAccountTransition);
            return CompleteLocalAccountTransition(result);
        }

        public LocalAccountAttemptResult LogoutLocalAccount()
        {
            if (!TryPrepareLocalAccountTransition())
            {
                return LocalAccountDataFailure();
            }

            var result = LocalAccountRuntime.TrySignOut();
            return CompleteLocalAccountTransition(result);
        }

        public LocalAccountAttemptResult DeleteLocalAccount(
            string password,
            string confirmation)
        {
            saveManager = saveManager != null ? saveManager : GetComponent<SaveManager>();
            if (saveManager == null)
            {
                return LocalAccountDataFailure();
            }

            var wasSignedIn = LocalAccountRuntime.Snapshot.IsSignedIn;
            var result = LocalAccountRuntime.TryDelete(
                password,
                confirmation,
                saveManager.TryPurgeLocalAccountSaveArtifacts);
            return CompleteLocalAccountTransition(
                result,
                wasSignedIn && !result.Snapshot.IsSignedIn);
        }

        private bool TryPrepareLocalAccountTransition()
        {
            if (!CanUseLocalAccountData())
            {
                return false;
            }

            try
            {
                SaveGame();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        private bool CanUseLocalAccountData()
        {
            saveManager = saveManager != null ? saveManager : GetComponent<SaveManager>();
            return saveManager != null && CurrentSave != null;
        }

        private LocalAccountAttemptResult CompleteLocalAccountTransition(
            LocalAccountAttemptResult result,
            bool sessionChangedDespiteFailure = false)
        {
            if (!result.Succeeded && !sessionChangedDespiteFailure)
            {
                return result;
            }

            ResetTransientRuntimeState();
            LoadOrCreateGame();
            LocalAccountChanged?.Invoke();
            return result;
        }

        private static LocalAccountAttemptResult LocalAccountDataFailure()
        {
            LocalAccountRuntime.EnsureInitialized();
            return new LocalAccountAttemptResult(
                LocalAccountAttemptStatus.AccountDataOperationFailed,
                LocalAccountRuntime.Snapshot);
        }

        public void ResetGame()
        {
            ResetGameInternal(true);
        }

        private void ResetGameInternal(bool notifySaveDataReplaced)
        {
            ResetTransientRuntimeState();
            if (saveManager == null)
            {
                CurrentSave = SaveManager.CreateDefaultSave();
                if (notifySaveDataReplaced)
                {
                    SaveDataReplaced?.Invoke();
                }

                return;
            }

            saveManager.DeleteSave();
            CurrentSave = saveManager.LoadOrCreate();
            LastTimeProgression = TimeProgressionResult.None();
            presenceSessionStarted = false;
            if (notifySaveDataReplaced)
            {
                SaveDataReplaced?.Invoke();
            }
        }

        private void ResetTransientRuntimeState()
        {
            pendingReturnSummary = null;
            pendingGrowthMilestone = null;
            pendingCareEvent = CareEventResult.None();
            pendingEvolutionMilestone = null;
            lastMilkGrowthMilestoneReward = MilkGrowthMilestoneRewardResult.None;
            applicationPaused = false;
            applicationHasFocus = true;
            applicationSuspended = false;
            LastTimeProgression = TimeProgressionResult.None();
            presenceSessionStarted = false;
            pendingCloudApplyGuard = null;
        }

        public bool TryGetPendingReturnSummary(out ReturnSummaryData summary)
        {
            summary = pendingReturnSummary;
            return summary != null;
        }

        public bool ConsumePendingReturnSummary(string summaryId)
        {
            if (pendingReturnSummary == null
                || string.IsNullOrWhiteSpace(summaryId)
                || !string.Equals(pendingReturnSummary.id, summaryId, StringComparison.Ordinal))
            {
                return false;
            }

            pendingReturnSummary = null;
            return true;
        }

        public bool TryGetPendingGrowthMilestone(out GrowthMilestoneData milestone)
        {
            milestone = pendingGrowthMilestone;
            return milestone != null;
        }

        public bool AcknowledgeGrowthMilestone(string milestoneId)
        {
            if (pendingGrowthMilestone == null
                || string.IsNullOrWhiteSpace(milestoneId)
                || !string.Equals(pendingGrowthMilestone.id, milestoneId, StringComparison.Ordinal)
                || CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            CurrentSave.growthMilestone.acknowledgedStage = pendingGrowthMilestone.stage;
            pendingGrowthMilestone = null;
            SaveGame();
            return true;
        }

        public bool TryGetPendingCareEvent(out CareEventResult careEvent)
        {
            careEvent = pendingCareEvent;
            return pendingCareEvent.occurred;
        }

        public bool ConsumePendingCareEvent(string occurrenceId)
        {
            if (!pendingCareEvent.occurred
                || string.IsNullOrWhiteSpace(occurrenceId)
                || !string.Equals(pendingCareEvent.occurrenceId, occurrenceId, StringComparison.Ordinal))
            {
                return false;
            }

            pendingCareEvent = CareEventResult.None();
            if (CurrentSave?.randomEvents?.pendingEvent != null)
            {
                CurrentSave.randomEvents.pendingEvent.Clear();
                SaveGame();
            }
            return true;
        }

        public bool TryResolvePendingCareEventChoice(
            string occurrenceId,
            string choiceId,
            out CareEventChoiceResult result)
        {
            result = default;
            if (CurrentSave == null
                || CurrentTama == null
                || !pendingCareEvent.occurred
                || string.IsNullOrWhiteSpace(occurrenceId)
                || !string.Equals(pendingCareEvent.occurrenceId, occurrenceId, StringComparison.Ordinal))
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var existingReceipt = FindCareEventChoiceReceipt(occurrenceId);
            if (existingReceipt != null)
            {
                result = BuildChoiceResultFromReceipt(
                    existingReceipt,
                    CareEventChoiceResolutionStatus.AlreadyApplied,
                    GetHiddenCareerBenefits().NegativeEffectMitigationPercent);
                pendingCareEvent = CareEventResult.None();
                CurrentSave.randomEvents.pendingEvent.Clear();
                SaveGame();
                return true;
            }

            result = careEventChoiceSystem.ApplyChoice(
                pendingCareEvent,
                choiceId,
                CurrentTama,
                CurrentSave.economy,
                GetHiddenCareerBenefits().NegativeEffectMitigationPercent);
            if (!result.applied)
            {
                return false;
            }

            CurrentSave.randomEvents.choiceReceipts.Add(new CareEventChoiceReceiptSaveEntry
            {
                occurrenceId = result.occurrenceId,
                eventId = result.eventId,
                choiceId = result.choiceId,
                resolvedAtIso = DateTimeOffset.Now.ToString("O")
            });
            while (CurrentSave.randomEvents.choiceReceipts.Count > RandomEventSaveData.MaximumChoiceReceipts)
            {
                CurrentSave.randomEvents.choiceReceipts.RemoveAt(0);
            }

            pendingCareEvent = CareEventResult.None();
            CurrentSave.randomEvents.pendingEvent.Clear();
            SaveGame();
            return true;
        }

        public void PersistNewGameSetup(NewGameSetupSaveData state)
        {
            if (CurrentSave == null || state == null)
            {
                return;
            }

            CurrentSave.newGameSetup = state;
            state.EnsureRuntimeDefaults();
            ApplyNewGameSetupOutcomeIfNeeded();
            SaveGame();
        }

        public void ResetProgress()
        {
            TryResetProgress(
                ProgressResetMode.CareProgressOnly,
                ProgressResetPolicy.CareProgressConfirmationPhrase);
        }

        public ProgressResetPreview GetProgressResetPreview(ProgressResetMode mode)
        {
            return ProgressResetPolicy.BuildPreview(mode);
        }

        public ProgressResetResult TryResetProgress(
            ProgressResetMode mode,
            string confirmation)
        {
            var preview = GetProgressResetPreview(mode);
            if (!preview.IsSupported)
            {
                return ProgressResetResult.CreateFailure(
                    ProgressResetResultStatus.UnsupportedMode,
                    preview,
                    "지원하지 않는 초기화 방식입니다.");
            }

            if (CurrentSave == null || saveManager == null)
            {
                return ProgressResetResult.CreateFailure(
                    ProgressResetResultStatus.MissingState,
                    preview,
                    "초기화할 로컬 저장 데이터를 불러오지 못했습니다.");
            }

            if (!ProgressResetPolicy.MatchesConfirmation(preview, confirmation))
            {
                return ProgressResetResult.CreateFailure(
                    ProgressResetResultStatus.ConfirmationMismatch,
                    preview,
                    "확인 문구가 일치하지 않아 아무 데이터도 변경하지 않았습니다.");
            }

            CheeseTamaSaveData replacement;
            try
            {
                replacement = mode == ProgressResetMode.CareProgressOnly
                    ? BuildCareProgressResetSave(CurrentSave)
                    : SaveManager.CreateDefaultSave();
            }
            catch (ArgumentException)
            {
                replacement = null;
            }

            if (replacement == null)
            {
                return ProgressResetResult.CreateFailure(
                    ProgressResetResultStatus.MissingState,
                    preview,
                    "현재 저장 데이터를 안전하게 복제하지 못해 초기화를 중단했습니다.");
            }

            try
            {
                // Persist the independent replacement before swapping the live object.
                // A failed write therefore cannot partially mutate CurrentSave.
                saveManager.Save(replacement);
            }
            catch (IOException)
            {
                return CreateResetPersistenceFailure(preview);
            }
            catch (UnauthorizedAccessException)
            {
                return CreateResetPersistenceFailure(preview);
            }
            catch (ArgumentException)
            {
                return CreateResetPersistenceFailure(preview);
            }
            catch (NotSupportedException)
            {
                return CreateResetPersistenceFailure(preview);
            }

            var recoveryArtifactsPurged = mode != ProgressResetMode.FullLocalData
                || saveManager.TryPurgeRecoveryArtifacts();
            CurrentSave = replacement;
            ResetTransientRuntimeState();
            SaveDataReplaced?.Invoke();
            return ProgressResetResult.CreateApplied(
                preview,
                true,
                mode == ProgressResetMode.CareProgressOnly
                    ? "현재 치즈타마의 육성 진행만 새로 시작했습니다."
                    : recoveryArtifactsPurged
                        ? "로컬 저장 데이터와 이전 복구본을 기본 상태로 초기화했습니다."
                        : "로컬 저장은 초기화했지만 이전 복구 파일 일부를 삭제하지 못했습니다.");
        }

        public CloudSyncResult SynchronizeCloudSave(ICloudSaveProvider provider = null)
        {
            if (CurrentSave == null || saveManager == null)
            {
                pendingCloudApplyGuard = null;
                return new CloudSyncResult(
                    CloudSyncAction.InvalidLocal,
                    null,
                    null,
                    "Local save is unavailable; cloud data was not touched.");
            }

            try
            {
                // Persist the comparison snapshot without changing its logical recency.
                // Download/conflict results remain advisory until explicit confirmation.
                saveManager.SaveWithoutAdvancingTimestamp(CurrentSave);
            }
            catch (IOException)
            {
                pendingCloudApplyGuard = null;
                return CreateCloudLocalPersistenceFailure();
            }
            catch (UnauthorizedAccessException)
            {
                pendingCloudApplyGuard = null;
                return CreateCloudLocalPersistenceFailure();
            }
            catch (ArgumentException)
            {
                pendingCloudApplyGuard = null;
                return CreateCloudLocalPersistenceFailure();
            }
            catch (NotSupportedException)
            {
                pendingCloudApplyGuard = null;
                return CreateCloudLocalPersistenceFailure();
            }

            var local = BuildCurrentCloudPayload();
            var result = cloudSaveSyncCoordinator.Synchronize(
                local,
                provider ?? SteamCloudProviderFactory.CreateDefault());
            pendingCloudApplyGuard = result.Remote != null
                && (result.Action == CloudSyncAction.DownloadedRemote
                    || result.Action == CloudSyncAction.ConflictNeedsResolution)
                ? new CloudApplyGuard(local, result.Remote, result.Action)
                : null;
            return result;
        }

        public CloudSaveApplyResult TryApplyCloudSave(
            CloudSyncResult result,
            string confirmation)
        {
            if (saveManager == null || CurrentSave == null)
            {
                return CloudSaveApplyResult.Failure(
                    "로컬 저장을 불러오지 못해 클라우드 저장을 적용하지 않았습니다.");
            }

            if (result.Action != CloudSyncAction.DownloadedRemote
                && result.Action != CloudSyncAction.ConflictNeedsResolution)
            {
                return CloudSaveApplyResult.Failure(
                    "적용 가능한 클라우드 비교 결과가 아닙니다.");
            }

            if (!string.Equals(
                    confirmation?.Trim(),
                    CloudSaveApplyConfirmationPhrase,
                    StringComparison.Ordinal))
            {
                return CloudSaveApplyResult.Failure(
                    $"클라우드 저장을 쓰려면 {CloudSaveApplyConfirmationPhrase}를 정확히 입력하세요.");
            }

            if (result.Remote == null
                || !result.Remote.IsValid()
                || !string.Equals(
                    result.Remote.slotId,
                    CloudSaveSlotRules.PrimarySlotId,
                    StringComparison.Ordinal))
            {
                return CloudSaveApplyResult.Failure(
                    "클라우드 저장 데이터가 유효하지 않아 로컬 저장을 유지했습니다.");
            }

            var currentLocal = BuildCurrentCloudPayload();
            if (pendingCloudApplyGuard == null
                || !pendingCloudApplyGuard.Matches(
                    currentLocal,
                    result.Remote,
                    result.Action))
            {
                pendingCloudApplyGuard = null;
                return CloudSaveApplyResult.Failure(
                    "동기화 이후 로컬 저장이 변경되었습니다. 다시 동기화한 뒤 선택하세요.");
            }

            if (!saveManager.TryReplaceFromCloudPayload(result.Remote, out _))
            {
                return CloudSaveApplyResult.Failure(
                    "클라우드 저장을 안전하게 기록하지 못해 기존 로컬 저장을 유지했습니다.");
            }

            pendingCloudApplyGuard = null;
            ResetTransientRuntimeState();
            LoadOrCreateGame();
            return CurrentSave != null
                ? CloudSaveApplyResult.Success("선택한 클라우드 저장을 적용했습니다.")
                : CloudSaveApplyResult.Failure(
                    "클라우드 저장을 기록했지만 다시 불러오지 못했습니다.");
        }

        private CloudSavePayload BuildCurrentCloudPayload()
        {
            if (CurrentSave == null)
            {
                return null;
            }

            var modifiedAt = DateTimeOffset.TryParse(
                CurrentTama?.lastSavedAtIso,
                out var parsedModifiedAt)
                ? parsedModifiedAt
                : DateTimeOffset.UnixEpoch;
            return CloudSavePayload.Create(
                CloudSaveSlotRules.PrimarySlotId,
                JsonUtility.ToJson(CurrentSave, true),
                Math.Max(0L, modifiedAt.UtcDateTime.Ticks),
                modifiedAt);
        }

        private static CheeseTamaSaveData BuildCareProgressResetSave(
            CheeseTamaSaveData current)
        {
            if (current == null)
            {
                return null;
            }

            var replacement = JsonUtility.FromJson<CheeseTamaSaveData>(
                JsonUtility.ToJson(current));
            if (replacement == null)
            {
                return null;
            }

            replacement.EnsureRuntimeDefaults();
            var defaults = SaveManager.CreateDefaultSave();

            replacement.cheeseTama = defaults.cheeseTama;
            replacement.milkGrowth = defaults.milkGrowth;
            replacement.careHistory = defaults.careHistory;
            replacement.growthMilestone = defaults.growthMilestone;
            replacement.evolutionMilestone = defaults.evolutionMilestone;
            replacement.lateLevelGrowth = defaults.lateLevelGrowth;

            // Preserve durable histories and receipts while discarding only active work.
            replacement.randomEvents.pendingEvent.Clear();
            replacement.sleepSchedule.ClearActiveSession();
            replacement.npcRelationshipQuests.activeQuest.Clear();
            replacement.EnsureRuntimeDefaults();
            return replacement;
        }

        private static ProgressResetResult CreateResetPersistenceFailure(
            ProgressResetPreview preview)
        {
            return ProgressResetResult.CreateFailure(
                ProgressResetResultStatus.PersistenceFailed,
                preview,
                "저장 파일을 안전하게 갱신하지 못해 초기화를 적용하지 않았습니다.");
        }

        private static CloudSyncResult CreateCloudLocalPersistenceFailure()
        {
            return new CloudSyncResult(
                CloudSyncAction.InvalidLocal,
                null,
                null,
                "Local save could not be persisted; cloud data was not touched.");
        }

        private sealed class CloudApplyGuard
        {
            private readonly string localHash;
            private readonly long localRevision;
            private readonly long localModifiedUtcTicks;
            private readonly string remoteHash;
            private readonly long remoteRevision;
            private readonly long remoteModifiedUtcTicks;
            private readonly CloudSyncAction action;

            public CloudApplyGuard(
                CloudSavePayload local,
                CloudSavePayload remote,
                CloudSyncAction action)
            {
                localHash = local?.contentHash ?? string.Empty;
                localRevision = local?.revision ?? -1L;
                localModifiedUtcTicks = local?.modifiedUtcTicks ?? -1L;
                remoteHash = remote?.contentHash ?? string.Empty;
                remoteRevision = remote?.revision ?? -1L;
                remoteModifiedUtcTicks = remote?.modifiedUtcTicks ?? -1L;
                this.action = action;
            }

            public bool Matches(
                CloudSavePayload local,
                CloudSavePayload remote,
                CloudSyncAction requestedAction)
            {
                return local != null
                    && remote != null
                    && action == requestedAction
                    && string.Equals(localHash, local.contentHash, StringComparison.OrdinalIgnoreCase)
                    && localRevision == local.revision
                    && localModifiedUtcTicks == local.modifiedUtcTicks
                    && string.Equals(remoteHash, remote.contentHash, StringComparison.OrdinalIgnoreCase)
                    && remoteRevision == remote.revision
                    && remoteModifiedUtcTicks == remote.modifiedUtcTicks;
            }
        }

        public void SaveGame()
        {
            if (saveManager == null || CurrentSave == null)
            {
                return;
            }

            saveManager.Save(CurrentSave);
        }

        public bool TryRenameCurrentTama(string requestedName, out string errorMessage)
        {
            if (CurrentTama == null)
            {
                errorMessage = "치즈타마 정보를 불러오지 못했습니다.";
                return false;
            }

            if (!CheeseTamaNameSystem.TryNormalize(
                    requestedName,
                    out var normalizedName,
                    out errorMessage))
            {
                return false;
            }

            CurrentTama.name = normalizedName;
            CurrentTama.hasCustomName = true;
            SaveGame();
            return true;
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
            var dailyCareChanged = EnsureDailyCareDate();
            EnsureMilkroomPresenceSession();

            var safeSeconds = Math.Max(1, seconds);
            var session = CurrentSave.milkroomSession;
            var previousMinute = session.currentSessionSeconds / 60;
            session.currentSessionSeconds += safeSeconds;
            session.todaySeconds += safeSeconds;
            session.totalSeconds += safeSeconds;
            var currentMinute = session.currentSessionSeconds / 60;

            var rewardMessage = GrantPresenceRewards(previousMinute, currentMinute);
            if (dailyCareChanged || !string.IsNullOrWhiteSpace(rewardMessage) || currentMinute > previousMinute)
            {
                RefreshDerivedCollectionRecords();
                SaveGame();
            }

            return rewardMessage;
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

        public bool TryGetPendingEvolutionMilestone(out EvolutionMilestoneData milestone)
        {
            milestone = pendingEvolutionMilestone;
            return milestone != null;
        }

        public bool AcknowledgeEvolutionMilestone(string occurrenceId)
        {
            if (CurrentSave == null
                || pendingEvolutionMilestone == null
                || !string.Equals(pendingEvolutionMilestone.occurrenceId, occurrenceId, StringComparison.Ordinal))
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            CurrentSave.evolutionMilestone.acknowledgedEvolutionId = pendingEvolutionMilestone.result.EvolutionId;
            pendingEvolutionMilestone = null;
            SaveGame();
            return true;
        }

        private bool ResolveNormalEvolutionIfEligible()
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (!evolutionSystem.TryApplyNormalEvolution(
                    CurrentTama,
                    CurrentSave.milkGrowth,
                    CurrentSave.careHistory,
                    out var result))
            {
                return false;
            }

            collectionSystem.RegisterEvolution(CurrentSave.collections, result.EvolutionId);
            pendingEvolutionMilestone = new EvolutionMilestoneData(
                Guid.NewGuid().ToString("N"),
                result,
                CurrentTama.level);
            RecordMemoryEvolution(pendingEvolutionMilestone);
            SaveGame();
            EvolutionMilestoneAvailable?.Invoke(pendingEvolutionMilestone);
            return true;
        }

        private void QueueCurrentEvolutionMilestoneIfNeeded()
        {
            if (CurrentSave == null || CurrentTama == null || string.IsNullOrWhiteSpace(CurrentTama.evolutionId))
            {
                pendingEvolutionMilestone = null;
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (string.Equals(
                    CurrentSave.evolutionMilestone.acknowledgedEvolutionId,
                    CurrentTama.evolutionId,
                    StringComparison.Ordinal))
            {
                pendingEvolutionMilestone = null;
                return;
            }

            if (pendingEvolutionMilestone != null
                && string.Equals(pendingEvolutionMilestone.result.EvolutionId, CurrentTama.evolutionId, StringComparison.Ordinal))
            {
                return;
            }

            var profile = EvolutionSystem.FindNormalEvolution(CurrentTama.evolutionId);
            if (profile == null)
            {
                CurrentSave.evolutionMilestone.acknowledgedEvolutionId = CurrentTama.evolutionId;
                SaveGame();
                return;
            }

            var result = new NormalEvolutionResult(profile, 0);
            pendingEvolutionMilestone = new EvolutionMilestoneData(
                Guid.NewGuid().ToString("N"),
                result,
                CurrentTama.level);
            RecordMemoryEvolution(pendingEvolutionMilestone);
            EvolutionMilestoneAvailable?.Invoke(pendingEvolutionMilestone);
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
                var now = DateTimeOffset.Now;
                weeklyCareJourneySystem.RecordEvent(
                    CurrentSave.weeklyCareJourney,
                    WeeklyCareEventIds.Discovery,
                    1,
                    now,
                    $"weekly_discovery_{CurrentSave.collections.events.Count}_{eventId}");
                SaveGame();
                JourneyHubChanged?.Invoke();
            }

            return added;
        }

        public bool TryClaimCollectionFragmentReward(
            CollectionRecordCategory category,
            string recordId)
        {
            if (CurrentSave == null || string.IsNullOrWhiteSpace(recordId))
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (CurrentSave.economy.collectionFragments >= int.MaxValue
                || !collectionSystem.TryClaimFragmentReward(CurrentSave.collections, category, recordId))
            {
                return false;
            }

            CurrentSave.economy.collectionFragments += 1;
            SaveGame();
            return true;
        }

        public int ClaimAllCollectionFragmentRewards()
        {
            if (CurrentSave == null)
            {
                return 0;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var remainingCapacityLong = (long)int.MaxValue - CurrentSave.economy.collectionFragments;
            var remainingCapacity = remainingCapacityLong <= 0L
                ? 0
                : (int)Math.Min(remainingCapacityLong, int.MaxValue);
            var claimedCount = collectionSystem.ClaimAllFragmentRewards(
                CurrentSave.collections,
                remainingCapacity);
            if (claimedCount <= 0)
            {
                return 0;
            }

            CurrentSave.economy.collectionFragments += claimedCount;
            SaveGame();
            return claimedCount;
        }

        public int ClaimDiscoveryCollectionFragmentRewards()
        {
            if (CurrentSave == null)
            {
                return 0;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var remainingCapacityLong = (long)int.MaxValue - CurrentSave.economy.collectionFragments;
            var remainingCapacity = remainingCapacityLong <= 0L
                ? 0
                : (int)Math.Min(remainingCapacityLong, int.MaxValue);
            var claimedCount = collectionSystem.ClaimFragmentRewards(
                CurrentSave.collections,
                CollectionRecordCategory.Milk,
                remainingCapacity);
            claimedCount += collectionSystem.ClaimFragmentRewards(
                CurrentSave.collections,
                CollectionRecordCategory.Event,
                remainingCapacity - claimedCount);
            if (claimedCount <= 0)
            {
                return 0;
            }

            CurrentSave.economy.collectionFragments += claimedCount;
            SaveGame();
            return claimedCount;
        }

        public int ClaimCollectionFragmentRewards(CollectionRecordCategory category)
        {
            if (CurrentSave == null)
            {
                return 0;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var remainingCapacityLong = (long)int.MaxValue - CurrentSave.economy.collectionFragments;
            var remainingCapacity = remainingCapacityLong <= 0L
                ? 0
                : (int)Math.Min(remainingCapacityLong, int.MaxValue);
            var claimedCount = collectionSystem.ClaimFragmentRewards(
                CurrentSave.collections,
                category,
                remainingCapacity);
            if (claimedCount <= 0)
            {
                return 0;
            }

            CurrentSave.economy.collectionFragments += claimedCount;
            SaveGame();
            return claimedCount;
        }

        public bool IsMilkUnlocked(string milkId)
        {
            var milk = MilkCatalog.Find(milkId);
            if (milk == null)
            {
                return false;
            }

            if (milk.id == MilkCatalog.BasicMilkId)
            {
                return true;
            }

            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            if (milk.id == MilkCatalog.StarMilkId)
            {
                return CanUnlockStarMilk();
            }

            var requiredGrowthLevel = GetMilkGrowthLevel(milk.requiredMilkId);
            return milk.IsUnlocked(requiredGrowthLevel);
        }

        public bool UnlockStarMilk()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var durableStarRouteChanged = ReconcileDurableStarRouteUnlock();
            if (CurrentSave.unlocks.starMilkUnlocked)
            {
                durableStarRouteChanged |= starLegacyEvolutionSystem.MarkStarRoutePermanentlyUnlocked(
                        CurrentSave.unlocks,
                        CurrentSave.starLegacy);
                if (durableStarRouteChanged)
                {
                    SaveGame();
                }

                return false;
            }

            if (!CanUnlockStarMilk())
            {
                return false;
            }

            CurrentSave.unlocks.starEggUnlocked = true;
            CurrentSave.unlocks.starMilkUnlocked = true;
            CurrentSave.unlocks.fantasyPowderEnabled = true;
            starLegacyEvolutionSystem.MarkStarRoutePermanentlyUnlocked(
                CurrentSave.unlocks,
                CurrentSave.starLegacy);
            EnsureFantasyPowderStarterGrant();
            CurrentSave.starRoute.unlockAcknowledged = false;
            CurrentSave.starRoute.unlockedAtIso = DateTimeOffset.Now.ToString("O");
            AddUniqueRecord(CurrentSave.collections.milk, MilkCatalog.StarMilkId);
            AddUniqueRecord(CurrentSave.collections.events, "star_milk_unlocked");
            SaveGame();
            StarRouteUnlockAvailable?.Invoke();
            return true;
        }

        public bool RefreshMilkUnlocks()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var durableStarRouteChanged = ReconcileDurableStarRouteUnlock();
            var canUnlockStarMilk = CanUnlockStarMilk();
            if (!canUnlockStarMilk)
            {
                var hadStarUnlock = CurrentSave.unlocks.starEggUnlocked
                    || CurrentSave.unlocks.starMilkUnlocked
                    || CurrentSave.unlocks.fantasyPowderEnabled;
                CurrentSave.unlocks.starEggUnlocked = false;
                CurrentSave.unlocks.starMilkUnlocked = false;
                CurrentSave.unlocks.fantasyPowderEnabled = false;
                if (hadStarUnlock)
                {
                    SaveGame();
                }

                return false;
            }

            if (CurrentSave.unlocks.starMilkUnlocked)
            {
                if (durableStarRouteChanged)
                {
                    SaveGame();
                }

                return durableStarRouteChanged;
            }

            CurrentSave.unlocks.starEggUnlocked = true;
            CurrentSave.unlocks.starMilkUnlocked = true;
            CurrentSave.unlocks.fantasyPowderEnabled = true;
            starLegacyEvolutionSystem.MarkStarRoutePermanentlyUnlocked(
                CurrentSave.unlocks,
                CurrentSave.starLegacy);
            EnsureFantasyPowderStarterGrant();
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
            lastMilkGrowthMilestoneReward = MilkGrowthMilestoneRewardResult.None;
            var entry = milkGrowthSystem.AddGrowthPoints(CurrentSave.milkGrowth, milkId, points);
            if (entry != null)
            {
                CurrentTama.growthHistory.lastFedMilkId = milkId;
                CurrentTama.growthHistory.mostUsedMilkId = milkId;
                lastMilkGrowthMilestoneReward = MilkGrowthMilestoneRewardSystem.ClaimReachedMilestones(
                    milkId,
                    entry.growthLevel,
                    CurrentSave.claimedMilkGrowthRewardKeys);
                if (lastMilkGrowthMilestoneReward.granted)
                {
                    CurrentSave.economy.milkCoins = SaturatingAdd(
                        CurrentSave.economy.milkCoins,
                        lastMilkGrowthMilestoneReward.milkCoins);
                    CurrentSave.economy.milkDrops = SaturatingAdd(
                        CurrentSave.economy.milkDrops,
                        lastMilkGrowthMilestoneReward.milkDrops);
                    CurrentSave.economy.collectionFragments = SaturatingAdd(
                        CurrentSave.economy.collectionFragments,
                        lastMilkGrowthMilestoneReward.collectionFragments);
                    RegisterMilkGrowthRewardRecords(lastMilkGrowthMilestoneReward);
                }

                RefreshMilkUnlocks();
                SaveGame();
                if (lastMilkGrowthMilestoneReward.granted)
                {
                    MilkGrowthMilestoneRewardGranted?.Invoke(lastMilkGrowthMilestoneReward);
                }
            }

            return entry;
        }

        private void RegisterMilkGrowthRewardRecords(MilkGrowthMilestoneRewardResult reward)
        {
            if (CurrentSave == null || reward == null || !reward.granted)
            {
                return;
            }

            for (var level = 4; level <= MilkGrowthMilestoneRewardSystem.MaximumRewardLevel; level += 1)
            {
                var claimKey = MilkGrowthMilestoneRewardSystem.BuildClaimKey(reward.milkId, level);
                for (var index = 0; index < reward.claimedKeys.Count; index += 1)
                {
                    if (!string.Equals(reward.claimedKeys[index], claimKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    AddUniqueRecord(
                        CurrentSave.collections.events,
                        MilkGrowthMilestoneRewardSystem.BuildEventId(reward.milkId, level));
                    break;
                }
            }
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

        private int GetMilkGrowthLevel(string milkId)
        {
            if (string.IsNullOrWhiteSpace(milkId))
            {
                return 0;
            }

            return FindMilkGrowth(milkId)?.growthLevel ?? 0;
        }

        private bool CanUnlockStarMilk()
        {
            if (CurrentSave?.starLegacy?.starRoutePermanentlyUnlocked ?? false)
            {
                return true;
            }

            if (CurrentSave == null || CurrentTama == null || CurrentTama.level < UnlockSystem.MaxLevel)
            {
                return false;
            }

            foreach (var milk in MilkCatalog.MainMilks)
            {
                if (milk == null || GetMilkGrowthLevel(milk.id) < MilkCatalog.MainMilkMaxGrowthLevel)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ReconcileDurableStarRouteUnlock()
        {
            if (CurrentSave == null)
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            return starLegacyEvolutionSystem.ReconcileStarRouteUnlock(
                CurrentTama,
                CurrentSave.unlocks,
                CurrentSave.starLegacy,
                !string.IsNullOrWhiteSpace(CurrentSave.starRoute?.unlockedAtIso));
        }

        public void RegisterCareAction(string actionId, int amount = 1)
        {
            if (CurrentSave == null || string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var safeAmount = Math.Max(1, amount);
            var now = DateTimeOffset.Now;
            var history = CurrentSave.careHistory;
            history.totalCareActions += 1;
            history.lastCareActionId = actionId;
            history.lastCareActionAtIso = now.ToString("O");

            switch (actionId)
            {
                case "feed_milk":
                case "feed_warm_milk":
                case "feed_cold_milk":
                case "feed_nutty_milk":
                case "feed_rich_milk":
                case "feed_fermented_milk":
                case "feed_coffee_milk":
                    history.milkFeeds += safeAmount;
                    break;
                case "feed_star_milk":
                    history.starMilkFeeds += safeAmount;
                    break;
                case "feed_snack":
                    history.snacksFed += safeAmount;
                    break;
                case "cook":
                case "blend":
                    history.cookings += safeAmount;
                    break;
                case "play":
                    history.playSessions += safeAmount;
                    break;
                case "pet":
                    history.petSessions += safeAmount;
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

            var starLegacyChanged = false;
            if (string.Equals(actionId, "feed_star_milk", StringComparison.Ordinal))
            {
                starLegacyChanged = starLegacyEvolutionSystem.RecordStarMilkCare(
                    CurrentTama,
                    CurrentSave.unlocks,
                    CurrentSave.starLegacy,
                    safeAmount) > 0;
            }

            if (CurrentTama != null && CurrentTama.level >= CurrentTama.maxLevel)
            {
                var baseMaturationProgress = GetPostgameResearchProfileEffectSnapshot()
                    .ApplyFinalMaturationProgress(
                        GetFinalMaturationProgress(actionId, safeAmount));
                var maturationProgress = finalMaturationCycleSystem.AddProgress(
                    CurrentTama,
                    CurrentSave.starLegacy.maturationCycle,
                    starLineageSystem.ApplyFinalMaturationProgress(
                        baseMaturationProgress,
                        CurrentSave.starLineage),
                    $"care:{history.totalCareActions}:{history.lastCareActionAtIso}",
                    CurrentSave.unlocks.starMilkUnlocked,
                    CurrentSave.unlocks.fantasyPowderEnabled,
                    GetPostgameResearchProfileEffectSnapshot().StarDropRewardBonus);
                starLegacyChanged |= maturationProgress.applied;
            }

            if (FirstDayJourneySystem.TryRecordCareAction(
                    CurrentSave.firstDayJourney,
                    actionId,
                    now))
            {
                FirstDayJourneyChanged?.Invoke();
            }

            if (memoryJournalSystem.TryRecordFirstDailyCare(
                    CurrentSave.memoryJournal,
                    actionId,
                    now,
                    CurrentTama.name,
                    CurrentTama.form,
                    out _))
            {
                MemoryJournalChanged?.Invoke();
            }

            var weeklyEventId = ResolveWeeklyCareEventId(actionId);
            if (!string.IsNullOrEmpty(weeklyEventId))
            {
                var weeklyResult = weeklyCareJourneySystem.RecordEvent(
                    CurrentSave.weeklyCareJourney,
                    weeklyEventId,
                    safeAmount,
                    now,
                    $"weekly_care_{history.totalCareActions}_{actionId}");
                if (weeklyResult.Applied)
                {
                    JourneyHubChanged?.Invoke();
                }
            }

            if (AdvanceNpcWeeklyQuests(actionId, safeAmount, now))
            {
                JourneyHubChanged?.Invoke();
            }

            var lifeChapterResult = lifeChapterSystem.TryRecordCareAction(
                CurrentSave.lifeChapters,
                CurrentTama?.level ?? 0,
                actionId,
                now);
            if (lifeChapterResult.StateChanged)
            {
                LifeChapterChanged?.Invoke();
                JourneyHubChanged?.Invoke();
            }


            if (starLegacyChanged)
            {
                StarLegacyChanged?.Invoke();
            }

            if (Application.isPlaying)
            {
                TryQueueNpcVisit();
            }

            CareActionRegistered?.Invoke(actionId);
        }

        private bool AdvanceNpcWeeklyQuests(
            string actionId,
            int amount,
            DateTimeOffset now)
        {
            if (CurrentSave?.npcAfterstory == null
                || CurrentSave.npcRelationshipEpisodes == null)
            {
                return false;
            }

            var applied = false;
            if (!string.IsNullOrEmpty(ResolveWeeklyCareEventId(actionId)))
            {
                applied |= npcAfterstorySystem.TryAdvanceWeeklyQuest(
                    CurrentSave.npcAfterstory,
                    CurrentSave.npcRelationshipEpisodes,
                    NpcVisitSystem.MilkyDoctorId,
                    NpcWeeklyObjectiveIds.CareAction,
                    amount,
                    now).Applied;
            }

            if (string.Equals(actionId, "blend", StringComparison.Ordinal))
            {
                applied |= npcAfterstorySystem.TryAdvanceWeeklyQuest(
                    CurrentSave.npcAfterstory,
                    CurrentSave.npcRelationshipEpisodes,
                    NpcVisitSystem.FermentationFairyId,
                    NpcWeeklyObjectiveIds.MilkBlend,
                    amount,
                    now).Applied;
            }

            if (string.Equals(actionId, "play", StringComparison.Ordinal)
                || string.Equals(actionId, "clean", StringComparison.Ordinal))
            {
                applied |= npcAfterstorySystem.TryAdvanceWeeklyQuest(
                    CurrentSave.npcAfterstory,
                    CurrentSave.npcRelationshipEpisodes,
                    NpcVisitSystem.MilkCatId,
                    NpcWeeklyObjectiveIds.PlayOrClean,
                    amount,
                    now).Applied;
            }

            return applied;
        }

        private static string ResolveWeeklyCareEventId(string actionId)
        {
            return actionId switch
            {
                "feed_milk" or "feed_warm_milk" or "feed_cold_milk"
                    or "feed_nutty_milk" or "feed_rich_milk"
                    or "feed_fermented_milk" or "feed_coffee_milk"
                    or "feed_star_milk" or "feed_snack" => WeeklyCareEventIds.Feed,
                "cook" => WeeklyCareEventIds.Cook,
                "blend" => WeeklyCareEventIds.Blend,
                "play" or "pet" => WeeklyCareEventIds.Play,
                "clean" => WeeklyCareEventIds.Clean,
                "rest" => WeeklyCareEventIds.Rest,
                _ => string.Empty
            };
        }

        public bool RegisterDailyCareAction(string actionId)
        {
            return RegisterDailyCareAction(actionId, true);
        }

        private static int GetFinalMaturationProgress(string actionId, int amount)
        {
            var safeAmount = Math.Max(1, amount);
            var perAction = actionId switch
            {
                "feed_star_milk" => 8,
                "feed_milk" or "feed_warm_milk" or "feed_cold_milk"
                    or "feed_nutty_milk" or "feed_rich_milk"
                    or "feed_fermented_milk" or "feed_coffee_milk" => 5,
                "play" => 5,
                "clean" => 4,
                "rest" => 3,
                "feed_snack" => 3,
                "cook" or "blend" => 3,
                "pet" => 2,
                "wait_hour" => 1,
                _ => 1
            };
            return Math.Min(1000, perAction * safeAmount);
        }

        private bool RegisterDailyCareAction(string actionId, bool grantCompletionReward)
        {
            if (CurrentSave == null || string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            CurrentSave.EnsureRuntimeDefaults();
            EnsureDailyCareDate();
            var daily = CurrentSave.dailyCare;
            var recognizedAction = true;

            switch (actionId)
            {
                case "feed_milk":
                case "feed_warm_milk":
                case "feed_cold_milk":
                case "feed_nutty_milk":
                case "feed_rich_milk":
                case "feed_fermented_milk":
                case "feed_coffee_milk":
                case "feed_star_milk":
                    daily.milkFeeds += 1;
                    break;
                case "feed_snack":
                    daily.snacksFed += 1;
                    break;
                case "cook":
                case "blend":
                    daily.cookings += 1;
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
                default:
                    recognizedAction = false;
                    break;
            }

            if (!recognizedAction)
            {
                return false;
            }

            if (!IsDailyRoutineComplete(daily) || daily.lastCompletedDateKey == daily.dateKey)
            {
                return false;
            }

            if (!grantCompletionReward)
            {
                return false;
            }

            daily.completedRoutineCount += 1;
            daily.lastCompletedDateKey = daily.dateKey;
            daily.lastCompletedAtIso = DateTimeOffset.Now.ToString("O");
            CurrentSave.economy.milkCoins = SaturatingAdd(
                CurrentSave.economy.milkCoins,
                DailyRoutineMilkCoinReward);
            CurrentSave.economy.milkDrops = SaturatingAdd(
                CurrentSave.economy.milkDrops,
                DailyRoutineMilkDropReward);
            CurrentSave.economy.collectionFragments = SaturatingAdd(
                CurrentSave.economy.collectionFragments,
                DailyRoutineCollectionFragmentReward);
            AddUniqueRecord(CurrentSave.collections.events, DailyRoutineCompleteEventId);
            DailyRoutineCompleted?.Invoke();
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
            var starRouteJustUnlocked = false;
            changed |= EnsureDailyCareDate();
            changed |= EnsureMilkroomSessionDate();
            changed |= ReconcileDurableStarRouteUnlock();
            var canUnlockStarMilk = CanUnlockStarMilk();
            if (!canUnlockStarMilk
                && (CurrentSave.unlocks.starEggUnlocked
                    || CurrentSave.unlocks.starMilkUnlocked
                    || CurrentSave.unlocks.fantasyPowderEnabled))
            {
                CurrentSave.unlocks.starEggUnlocked = false;
                CurrentSave.unlocks.starMilkUnlocked = false;
                CurrentSave.unlocks.fantasyPowderEnabled = false;
                changed = true;
            }

            if (canUnlockStarMilk && !CurrentSave.unlocks.starMilkUnlocked)
            {
                CurrentSave.unlocks.starEggUnlocked = true;
                CurrentSave.unlocks.starMilkUnlocked = true;
                CurrentSave.unlocks.fantasyPowderEnabled = true;
                changed |= starLegacyEvolutionSystem.MarkStarRoutePermanentlyUnlocked(
                    CurrentSave.unlocks,
                    CurrentSave.starLegacy);
                CurrentSave.starRoute.unlockAcknowledged = false;
                CurrentSave.starRoute.unlockedAtIso = DateTimeOffset.Now.ToString("O");
                starRouteJustUnlocked = true;
                changed = true;
            }
            else if (canUnlockStarMilk
                && CurrentSave.unlocks.starMilkUnlocked
                && string.IsNullOrWhiteSpace(CurrentSave.starRoute.unlockedAtIso))
            {
                CurrentSave.starRoute.unlockedAtIso = DateTimeOffset.Now.ToString("O");
                changed = true;
            }

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

            if (CurrentTama?.growthHistory != null
                && CurrentTama.growthHistory.sameMilkFeedStreak >= FeedingStatusSystem.MilkAversionStreakThreshold)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, "milk_aversion");
            }

            if (CurrentTama?.stats != null && CurrentTama.stats.overfullness > 0)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, "overfull");
            }

            if (CurrentTama?.stats != null && CurrentTama.stats.bodyChillIntensity > 0)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, "body_chill");
            }

            if (CurrentTama?.stats != null && CurrentTama.stats.fermentedAftertasteIntensity > 0)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, "fermented_aftertaste");
            }

            if (CurrentTama?.stats != null && CurrentTama.stats.sleepRhythmDisruptionIntensity > 0)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.events, "sleep_rhythm_disruption");
            }

            if (CurrentSave.unlocks.starMilkUnlocked)
            {
                changed |= AddUniqueRecord(CurrentSave.collections.milk, MilkCatalog.StarMilkId);
                changed |= AddUniqueRecord(CurrentSave.collections.events, "star_milk_unlocked");
            }

            changed |= EnsureFantasyPowderStarterGrant();

            if (CurrentTama != null)
            {
                foreach (var stage in CheeseTamaGrowthStageCatalog.All)
                {
                    if (CheeseTamaGrowthStageCatalog.IsReached(CurrentTama, stage.Stage))
                    {
                        changed |= AddUniqueRecord(CurrentSave.collections.evolution, stage.RecordId);
                    }
                }

                QueueCurrentGrowthMilestoneIfNeeded();
                if (ResolveNormalEvolutionIfEligible())
                {
                    changed = true;
                }

                QueueCurrentEvolutionMilestoneIfNeeded();
            }

            if (CurrentTama != null && CurrentTama.isHatched)
            {
                var evolutionId = string.IsNullOrWhiteSpace(CurrentTama.evolutionId)
                    ? CurrentTama.form
                    : CurrentTama.evolutionId;
                changed |= AddUniqueRecord(CurrentSave.collections.evolution, evolutionId);
            }

            changed |= UnlockHiddenCollectionRecords();

            var hiddenCareer = hiddenCareerCardSystem.TryUnlockNextEligible(
                CurrentSave,
                DateTimeOffset.Now);
            if (hiddenCareer.Unlocked)
            {
                changed = true;
                HiddenCareerCardChanged?.Invoke();
            }

            if (changed)
            {
                SaveGame();
            }

            if (starRouteJustUnlocked)
            {
                StarRouteUnlockAvailable?.Invoke();
            }
        }

        private bool EnsureFantasyPowderStarterGrant()
        {
            if (CurrentSave?.unlocks == null
                || !CurrentSave.unlocks.starMilkUnlocked
                || !CurrentSave.unlocks.fantasyPowderEnabled)
            {
                return false;
            }

            CurrentSave.fantasyPowder ??= new FantasyPowderSaveData();
            CurrentSave.fantasyPowder.EnsureRuntimeDefaults();
            if (CurrentSave.fantasyPowder.starterGrantClaimed)
            {
                return false;
            }

            CurrentSave.fantasyPowder.starterGrantClaimed = true;
            fantasyPowderSystem.GrantPowder(CurrentSave.fantasyPowder, 3);
            FantasyPowderChanged?.Invoke();
            return true;
        }

        public CareEventResult TryRollCareEvent()
        {
            return RollCareEvent(false);
        }

        public CareEventResult ForceCareEvent()
        {
            return RollCareEvent(true);
        }

        private CareEventResult RollCareEvent(bool force)
        {
            if (CurrentSave == null || CurrentTama == null || pendingCareEvent.occurred)
            {
                return CareEventResult.None();
            }

            CurrentSave.EnsureRuntimeDefaults();
            var onboarding = CurrentSave.onboarding;
            if (!force && onboarding != null && (!onboarding.completed || onboarding.replaying))
            {
                return CareEventResult.None();
            }

            var now = DateTimeOffset.Now;
            EnsureRandomEventDate(now);
            var eventSave = CurrentSave.randomEvents;
            if (!force
                && (eventSave.eventsToday >= MaximumCareEventCardsPerDay
                    || IsFutureIso(eventSave.nextAllowedAtIso, now)))
            {
                return CareEventResult.None();
            }

            var randomEventWeightPercent = GetHiddenCareerBenefits().RandomEventWeightPercent;
            var candidate = randomEventSystem.RollCareEvent(
                CurrentTama,
                force,
                randomEventWeightPercent);
            if (!candidate.occurred && !force)
            {
                candidate = seasonalCareEventSystem.Roll(
                    now,
                    UnityEngine.Random.value,
                    UnityEngine.Random.value,
                    false,
                    randomEventWeightPercent);
            }
            if (!candidate.occurred)
            {
                return candidate;
            }

            var history = FindRandomEventHistory(candidate.eventId, false);
            if (!force
                && history != null
                && WasWithinMinutes(history.lastOccurredAtIso, now, CareEventPerIdCooldownMinutes))
            {
                return CareEventResult.None();
            }

            var firstDiscovery = !CurrentSave.collections.events.Contains(candidate.eventId);
            AddUniqueRecord(CurrentSave.collections.events, candidate.eventId);
            history ??= FindRandomEventHistory(candidate.eventId, true);
            history.totalOccurrences = SaturatingAdd(history.totalOccurrences, 1);
            history.lastOccurredAtIso = now.ToString("O");
            eventSave.eventsToday = SaturatingAdd(eventSave.eventsToday, 1);
            eventSave.lastEventId = candidate.eventId;
            eventSave.nextAllowedAtIso = now.AddMinutes(CareEventGlobalCooldownMinutes).ToString("O");

            pendingCareEvent = new CareEventResult(
                true,
                Guid.NewGuid().ToString("N"),
                candidate.eventId,
                candidate.title,
                candidate.message,
                firstDiscovery);
            eventSave.pendingEvent.Set(pendingCareEvent);
            RefreshDerivedCollectionRecords();
            SaveGame();
            CareEventAvailable?.Invoke(pendingCareEvent);
            return pendingCareEvent;
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
            changed |= AddThresholdRecord(history.petSessions, 1, "pet_first");
            changed |= AddThresholdRecord(history.petSessions, 10, "pet_sessions_10");
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
            daily.cookings = 0;
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

        private TimeProgressionResult ApplyOfflineProgressAndPrepareSummary(DateTimeOffset now)
        {
            if (CurrentTama == null || CurrentTama.stats == null)
            {
                return TimeProgressionResult.None();
            }

            var lastSaved = TimeUtility.ParseOrDefault(CurrentTama.lastSavedAtIso, now);
            var elapsedMinutes = Math.Max(0, (int)Math.Floor((now - lastSaved).TotalMinutes));
            var before = ReturnSummaryStatsSnapshot.Capture(CurrentTama);
            var economy = CurrentSave?.economy;
            var coinsBefore = economy?.milkCoins ?? 0;
            var dropsBefore = economy?.milkDrops ?? 0;
            var fragmentsBefore = economy?.collectionFragments ?? 0;

            var result = timeProgressionSystem.ApplyOfflineProgress(CurrentTama, now);
            if (!result.applied)
            {
                return result;
            }

            var after = ReturnSummaryStatsSnapshot.Capture(CurrentTama);
            pendingReturnSummary = new ReturnSummaryData(
                Guid.NewGuid().ToString("N"),
                elapsedMinutes,
                result.hours,
                before,
                after,
                (economy?.milkCoins ?? 0) - coinsBefore,
                (economy?.milkDrops ?? 0) - dropsBefore,
                (economy?.collectionFragments ?? 0) - fragmentsBefore);
            RecordMemoryReturn(pendingReturnSummary);
            return result;
        }

        private void RefreshApplicationSuspendedState()
        {
            var suspended = applicationPaused || !applicationHasFocus;
            if (!Application.isPlaying || applicationQuitting || applicationSuspended == suspended)
            {
                return;
            }

            applicationSuspended = suspended;
            if (suspended)
            {
                SaveGame();
                return;
            }

            if (CurrentSave == null)
            {
                return;
            }

            var dailyCareChanged = EnsureDailyCareDate();
            var sessionDateChanged = EnsureMilkroomSessionDate();
            LastTimeProgression = ApplyOfflineProgressAndPrepareSummary(DateTimeOffset.Now);
            if (LastTimeProgression.applied || dailyCareChanged || sessionDateChanged)
            {
                SaveGame();
            }

            SaveDataReplaced?.Invoke();
            if (pendingReturnSummary != null)
            {
                ReturnSummaryAvailable?.Invoke(pendingReturnSummary);
            }
        }

        private void QueueCurrentGrowthMilestoneIfNeeded()
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var currentStage = CheeseTamaGrowthStageCatalog.Resolve(CurrentTama);
            if ((int)currentStage <= (int)CurrentSave.growthMilestone.acknowledgedStage)
            {
                pendingGrowthMilestone = null;
                return;
            }

            if (pendingGrowthMilestone != null && pendingGrowthMilestone.stage == currentStage)
            {
                return;
            }

            pendingGrowthMilestone = new GrowthMilestoneData(
                Guid.NewGuid().ToString("N"),
                currentStage,
                CurrentTama.level);
            RecordMemoryGrowth(pendingGrowthMilestone);
            GrowthMilestoneAvailable?.Invoke(pendingGrowthMilestone);
        }

        private void EnsureRandomEventDate(DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var eventSave = CurrentSave.randomEvents;
            var todayKey = now.ToString("yyyy-MM-dd");
            if (eventSave.dateKey == todayKey)
            {
                return;
            }

            eventSave.dateKey = todayKey;
            eventSave.eventsToday = 0;
        }

        private RandomEventHistorySaveEntry FindRandomEventHistory(string eventId, bool create)
        {
            if (CurrentSave == null || string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            CurrentSave.EnsureRuntimeDefaults();
            foreach (var entry in CurrentSave.randomEvents.history)
            {
                if (entry != null && string.Equals(entry.eventId, eventId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            if (!create)
            {
                return null;
            }

            var created = new RandomEventHistorySaveEntry { eventId = eventId };
            CurrentSave.randomEvents.history.Add(created);
            return created;
        }

        private void RestorePendingCareEventFromSave()
        {
            pendingCareEvent = CurrentSave?.randomEvents?.pendingEvent?.ToResult()
                ?? CareEventResult.None();
        }

        private CareEventChoiceReceiptSaveEntry FindCareEventChoiceReceipt(string occurrenceId)
        {
            var receipts = CurrentSave?.randomEvents?.choiceReceipts;
            if (receipts == null || string.IsNullOrWhiteSpace(occurrenceId))
            {
                return null;
            }

            for (var index = receipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = receipts[index];
                if (receipt != null
                    && string.Equals(receipt.occurrenceId, occurrenceId, StringComparison.Ordinal))
                {
                    return receipt;
                }
            }

            return null;
        }

        private static CareEventChoiceResult BuildChoiceResultFromReceipt(
            CareEventChoiceReceiptSaveEntry receipt,
            CareEventChoiceResolutionStatus status,
            int negativeEffectMitigationPercent)
        {
            if (receipt != null
                && RandomEventSystem.TryGetDefinition(receipt.eventId, out var definition)
                && definition.TryGetChoice(receipt.choiceId, out var choice))
            {
                return new CareEventChoiceResult(
                    status,
                    receipt.occurrenceId,
                    receipt.eventId,
                    receipt.choiceId,
                    choice.resultTitle,
                    choice.resultMessage,
                    CareEventChoiceSystem.ApplyNegativeEffectMitigation(
                        choice.effect,
                        negativeEffectMitigationPercent));
            }

            return new CareEventChoiceResult(
                status,
                receipt?.occurrenceId,
                receipt?.eventId,
                receipt?.choiceId,
                string.Empty,
                string.Empty,
                default);
        }

        private bool ApplyNewGameSetupOutcomeIfNeeded()
        {
            var state = CurrentSave?.newGameSetup;
            if (state == null || !state.completed || state.outcomeApplied)
            {
                return false;
            }

            if (!state.skipped && !state.legacySuppressed)
            {
                if (NewGameSetupCatalog.TryGetEgg(state.selectedEggId, out _))
                {
                    CurrentTama.eggType = state.selectedEggId;
                }

                if (!string.IsNullOrWhiteSpace(state.temperamentSeed?.dominantTraitId))
                {
                    CurrentTama.growthHistory.careStyle =
                        state.temperamentSeed.dominantTraitId;
                    CurrentTama.stats.Apply(GetInitialTemperamentEffect(
                        state.temperamentSeed.dominantTraitId));
                }

                if (NewGameSetupCatalog.TryGetFirstMilk(state.selectedFirstMilkId, out _))
                {
                    var entry = milkGrowthSystem.AddGrowthPoints(
                        CurrentSave.milkGrowth,
                        state.selectedFirstMilkId,
                        1);
                    CurrentTama.growthHistory.lastFedMilkId = state.selectedFirstMilkId;
                    CurrentTama.growthHistory.mostUsedMilkId = state.selectedFirstMilkId;
                    AddUniqueRecord(CurrentSave.collections.milk, state.selectedFirstMilkId);
                    if (entry != null)
                    {
                        AddMilkGrowthMilestoneRecords(entry.milkId, entry.growthLevel);
                    }
                }
            }

            state.outcomeApplied = true;
            return true;
        }

        private static StatEffect GetInitialTemperamentEffect(string dominantTraitId)
        {
            switch (dominantTraitId)
            {
                case NewGameSetupCatalog.LivelyTraitId:
                    return new StatEffect { mood = 4, sleepiness = -3 };
                case NewGameSetupCatalog.ExpressiveTraitId:
                    return new StatEffect { mood = 3, affection = 4 };
                case NewGameSetupCatalog.CalmTraitId:
                    return new StatEffect { cleanliness = 4, sleepiness = -2 };
                case NewGameSetupCatalog.FocusedTraitId:
                    return new StatEffect { maturation = 4 };
                default:
                    return default;
            }
        }

        private CareActionSystem CreateCareActionSystem()
        {
            var careActions = new CareActionSystem();
            careActions.ConfigureLateLevelGrowth(
                CurrentSave?.lateLevelGrowth,
                CurrentSave?.milkGrowth);
            careActions.ConfigureRecoveryEffectPercent(GetCareRecoveryEffectPercent());
            return careActions;
        }

        public int GetCareRecoveryEffectPercent()
        {
            return Math.Min(
                100,
                GetHiddenCareerBenefits().RecoveryEffectPercent
                + starLineageSystem.GetRecoveryBonusPercent(CurrentSave?.starLineage));
        }

        private static bool IsFutureIso(string isoValue, DateTimeOffset now)
        {
            return DateTimeOffset.TryParse(isoValue, out var parsed) && parsed > now;
        }

        private static bool WasWithinMinutes(string isoValue, DateTimeOffset now, int minutes)
        {
            if (!DateTimeOffset.TryParse(isoValue, out var parsed))
            {
                return false;
            }

            var elapsed = now - parsed;
            return elapsed >= TimeSpan.Zero && elapsed < TimeSpan.FromMinutes(Math.Max(0, minutes));
        }

        private static int SaturatingAdd(int current, int amount)
        {
            var result = (long)current + amount;
            if (result > int.MaxValue)
            {
                return int.MaxValue;
            }

            return result < int.MinValue ? int.MinValue : (int)result;
        }

        private static bool IsDailyRoutineComplete(DailyCareSaveData daily)
        {
            return daily != null
                && daily.milkFeeds + daily.snacksFed >= DailyCareSaveData.EatingGoal
                && daily.cookings >= DailyCareSaveData.CookingGoal
                && daily.playSessions >= DailyCareSaveData.PlayGoal
                && daily.cleanings >= DailyCareSaveData.CleanGoal
                && daily.rests >= DailyCareSaveData.RestGoal;
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
