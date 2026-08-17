using System;
using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Data;
using CheeseTama.Gameplay;
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
using CheeseTama.Collections.HiddenCareers;
using CheeseTama.Gameplay.Bond;
using CheeseTama.Gameplay.Stats;
using CheeseTama.Gameplay.Sleep;
using CheeseTama.Save;
using CheeseTama.Utilities;
using CheeseTama.UI;
using UnityEngine;

namespace CheeseTama.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        private const string DailyRoutineCompleteEventId = "daily_routine_complete";
        private const string DailyRoutineThreeEventId = "daily_routine_3";
        private const string SessionFiveMinuteEventId = "session_5m";
        private const string SessionTenMinuteEventId = "session_10m";
        private const string SessionTwentyMinuteEventId = "session_20m";
        private const string SessionThirtyMinuteEventId = "session_30m";
        private const string MilkDropCatchEventId = "milk_drop_catch";
        private const string BouncyJumpEventId = "bouncy_jump";
        private const string StarRouteEnteredEventId = "star_route_entered";
        private const int MaximumCareEventCardsPerDay = 5;
        private const int CareEventGlobalCooldownMinutes = 5;
        private const int CareEventPerIdCooldownMinutes = 30;

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
        private readonly CareEventChoiceSystem careEventChoiceSystem = new CareEventChoiceSystem();
        private readonly MemoryJournalSystem memoryJournalSystem = new MemoryJournalSystem();
        private readonly FantasyPowderHiddenRecipeSystem fantasyPowderSystem =
            new FantasyPowderHiddenRecipeSystem();
        private readonly StarEggEmmentalEvolutionSystem starLegacyEvolutionSystem =
            new StarEggEmmentalEvolutionSystem();
        private readonly FinalMaturationCycleSystem finalMaturationCycleSystem =
            new FinalMaturationCycleSystem();
        private readonly HiddenCareerCardSystem hiddenCareerCardSystem =
            new HiddenCareerCardSystem();
        private readonly BondReactionSystem bondReactionSystem = new BondReactionSystem();
        private readonly NpcVisitSystem npcVisitSystem = new NpcVisitSystem();
        private readonly MilkBlendingSystem milkBlendingSystem = new MilkBlendingSystem();
        private readonly SleepScheduleSystem sleepScheduleSystem = new SleepScheduleSystem();
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

        public static GameManager Instance { get; private set; }
        public event Action SaveDataReplaced;
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
        public event Action HiddenCareerCardChanged;
        public event Action<NpcVisitOffer> NpcVisitAvailable;
        public event Action<MilkBlendResult> MilkBlendingChanged;
        public event Action SleepScheduleChanged;

        public DataRegistry DataRegistry => dataRegistry;
        public CheeseTamaSaveData CurrentSave { get; private set; }
        public CheeseTamaModel CurrentTama => CurrentSave?.cheeseTama;
        public TimeProgressionResult LastTimeProgression { get; private set; }
        public string SaveFilePath => saveManager != null ? saveManager.SaveFilePath : string.Empty;
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

        public bool TryGetPendingNpcVisit(out NpcVisitOffer offer)
        {
            offer = null;
            return CurrentSave != null
                && npcVisitSystem.TryGetPending(CurrentSave.npcVisits, out offer);
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

            RefreshDerivedCollectionRecords();
            SaveGame();
            return true;
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
            return TryBlendMilk(
                milkId,
                ingredientId,
                $"blend_{Guid.NewGuid():N}",
                DateTimeOffset.Now);
        }

        public MilkBlendResult TryBlendMilk(
            string milkId,
            string ingredientId,
            string receiptKey,
            DateTimeOffset blendedAt)
        {
            var result = milkBlendingSystem.TryBlend(
                CurrentSave?.milkBlending,
                CurrentTama,
                CurrentSave?.economy,
                CurrentSave?.snackInventory,
                milkId,
                ingredientId,
                IsMilkUnlocked,
                receiptKey,
                blendedAt);
            if (result == null || !result.applied || CurrentSave == null)
            {
                return result;
            }

            RegisterCareAction("blend");
            RegisterDailyCareAction("blend");
            AddUniqueRecord(CurrentSave.collections.events, $"milk_blend_{result.resultSnackId}");
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
                        "새 블렌딩 발견",
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
            return starLegacyEvolutionSystem.EvaluateNewGenerationEligibility(
                CurrentTama,
                CurrentSave.unlocks,
                CurrentSave.starLegacy);
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
            var result = starLegacyEvolutionSystem.TryBeginStarEggGeneration(
                CurrentTama,
                CurrentSave.unlocks,
                CurrentSave.starLegacy,
                nextTamaId,
                startedAt);
            if (!result.applied)
            {
                return result;
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
            return fantasyPowderSystem.BuildSnapshot(
                CurrentSave.unlocks,
                CurrentSave.fantasyPowder);
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
            var result = fantasyPowderSystem.TryAttempt(
                CurrentSave.unlocks,
                CurrentSave.fantasyPowder,
                CurrentSave.snackInventory,
                CurrentSave.economy,
                recipeId,
                Guid.NewGuid().ToString("N"),
                UnityEngine.Random.value);
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
                return DecorationShopRules.Equip(itemId, DecorationShopSnapshot.CreateDefault());
            }

            var result = DecorationShopRules.Equip(itemId, GetDecorationShopSnapshot());
            if (!result.Succeeded)
            {
                return result;
            }

            ApplyDecorationSnapshot(result.snapshot);
            SaveGame();
            DecorationChanged?.Invoke();
            return result;
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
            CurrentSave.decorations.ownedItemIds = new List<string>(snapshot.OwnedItemIds);
            CurrentSave.decorations.equippedWallId = snapshot.equippedWallId;
            CurrentSave.decorations.equippedFloorId = snapshot.equippedFloorId;
            CurrentSave.decorations.equippedAccentId = snapshot.equippedAccentId;
            CurrentSave.decorations.equippedWindowId = snapshot.equippedWindowId;
            CurrentSave.decorations.equippedShelfId = snapshot.equippedShelfId;
            CurrentSave.decorations.equippedBedsideId = snapshot.equippedBedsideId;
        }

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
            var durableStarRouteChanged = ReconcileDurableStarRouteUnlock();
            RestorePendingCareEventFromSave();
            var setupOutcomeChanged = ApplyNewGameSetupOutcomeIfNeeded();
            var fantasyStarterChanged = EnsureFantasyPowderStarterGrant();
            var now = DateTimeOffset.Now;
            var dailyCareChanged = EnsureDailyCareDate();
            var sessionDateChanged = EnsureMilkroomSessionDate();
            presenceSessionStarted = false;
            LastTimeProgression = ApplyOfflineProgressAndPrepareSummary(now);
            if (LastTimeProgression.applied
                || dailyCareChanged
                || sessionDateChanged
                || setupOutcomeChanged
                || fantasyStarterChanged
                || durableStarRouteChanged)
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

        public void ResetGame()
        {
            ResetGameInternal(true);
        }

        private void ResetGameInternal(bool notifySaveDataReplaced)
        {
            pendingReturnSummary = null;
            pendingGrowthMilestone = null;
            pendingCareEvent = CareEventResult.None();
            pendingEvolutionMilestone = null;
            applicationPaused = false;
            applicationHasFocus = true;
            applicationSuspended = false;
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
                result = BuildChoiceResultFromReceipt(existingReceipt, CareEventChoiceResolutionStatus.AlreadyApplied);
                pendingCareEvent = CareEventResult.None();
                CurrentSave.randomEvents.pendingEvent.Clear();
                SaveGame();
                return true;
            }

            result = careEventChoiceSystem.ApplyChoice(
                pendingCareEvent,
                choiceId,
                CurrentTama,
                CurrentSave.economy);
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
            GameSettingsSaveData preservedSettings = null;
            if (CurrentSave != null)
            {
                CurrentSave.EnsureRuntimeDefaults();
                preservedSettings = CurrentSave.settings;
            }

            ResetGameInternal(false);

            if (CurrentSave != null && preservedSettings != null)
            {
                CurrentSave.settings = preservedSettings;
                SaveGame();
            }

            SaveDataReplaced?.Invoke();
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

        public string PlayMilkDropCatch()
        {
            return CompleteMilkDropMiniGame(5, 0, MilkDropMiniGameRules.CalculateScore(5)).message;
        }

        public MilkDropMiniGameRewardStatus GetMilkDropMiniGameRewardStatus()
        {
            return GetMilkDropMiniGameRewardStatus(DateTimeOffset.Now);
        }

        public MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(int caught, int missed, int score)
        {
            return CompleteMilkDropMiniGame(caught, missed, score, DateTimeOffset.Now, true);
        }

        public MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(
            int caught,
            int missed,
            int score,
            bool allowCurrencyReward)
        {
            return CompleteMilkDropMiniGame(
                caught,
                missed,
                score,
                DateTimeOffset.Now,
                allowCurrencyReward);
        }

        public CleaningMiniGameCompletionResult CompleteCleaningMiniGame(
            int cleanedSpots,
            int missedSpots,
            int score)
        {
            var safeCleaned = Math.Max(0, cleanedSpots);
            var safeMissed = Math.Max(0, missedSpots);
            var safeScore = CleaningMiniGameRules.ClampReportedScore(safeCleaned, score);
            if (CurrentSave == null || CurrentTama == null)
            {
                return new CleaningMiniGameCompletionResult(
                    safeScore,
                    safeCleaned,
                    safeMissed,
                    0,
                    "치즈타마 저장 데이터를 불러오지 못했습니다.",
                    false);
            }

            if (!CleaningMiniGameRules.QualifiesForCareReward(safeCleaned))
            {
                return new CleaningMiniGameCompletionResult(
                    safeScore,
                    safeCleaned,
                    safeMissed,
                    0,
                    $"얼룩을 {CleaningMiniGameRules.MinimumCleanedSpotsForCareReward}개 이상 닦아야 돌봄으로 기록돼요.",
                    false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            var beforeCleanliness = CurrentTama.stats != null
                ? CurrentTama.stats.cleanliness
                : 0;
            var careActions = CreateCareActionSystem();
            var careResult = careActions.Clean(CurrentTama);
            var afterCleanliness = CurrentTama.stats != null
                ? CurrentTama.stats.cleanliness
                : beforeCleanliness;
            RegisterCareAction("clean");
            var dailyCompleted = RegisterDailyCareAction("clean");
            RefreshDerivedCollectionRecords();
            SaveGame();

            var message = careResult.message ?? string.Empty;
            if (dailyCompleted)
            {
                message = CombineMessages(message, DailyRoutineRewardMessage);
            }

            return new CleaningMiniGameCompletionResult(
                safeScore,
                safeCleaned,
                safeMissed,
                Math.Max(0, afterCleanliness - beforeCleanliness),
                message,
                careResult.success);
        }

        public BouncyJumpCompletionResult CompleteBouncyJumpMiniGame(
            int successfulJumps,
            int missedJumps,
            int score,
            int highestCombo)
        {
            var sessionResult = BouncyJumpMiniGameRules.Complete(
                successfulJumps,
                missedJumps,
                score,
                highestCombo);
            if (CurrentSave == null || CurrentTama == null)
            {
                return new BouncyJumpCompletionResult(
                    false,
                    sessionResult.successes,
                    sessionResult.misses,
                    sessionResult.score,
                    0,
                    "치즈타마 저장 데이터를 불러오지 못했습니다.");
            }

            CurrentSave.EnsureRuntimeDefaults();
            var playSave = CurrentSave.playMiniGames;
            playSave.totalBouncyJumpSessions = SaturatingAdd(playSave.totalBouncyJumpSessions, 1);
            playSave.totalBouncyJumpSuccesses = SaturatingAdd(
                playSave.totalBouncyJumpSuccesses,
                sessionResult.successes);
            playSave.highestBouncyJumpScore = Math.Max(
                playSave.highestBouncyJumpScore,
                sessionResult.score);

            if (!sessionResult.qualifiesForCare)
            {
                SaveGame();
                return new BouncyJumpCompletionResult(
                    false,
                    sessionResult.successes,
                    sessionResult.misses,
                    sessionResult.score,
                    playSave.highestBouncyJumpScore,
                    $"성공 {BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare}회부터 놀이 돌봄으로 기록돼요. 최고 점수는 저장했어요.");
            }

            var careActions = CreateCareActionSystem();
            var careResult = careActions.Play(CurrentTama);
            RegisterCareAction("play");
            var dailyCompleted = RegisterDailyCareAction("play");
            AddUniqueRecord(CurrentSave.collections.events, BouncyJumpEventId);
            RefreshDerivedCollectionRecords();
            SaveGame();

            var message = CombineMessages("말랑 점프 기록과 최고 점수를 저장했어요.", careResult.message);
            if (dailyCompleted)
            {
                message = CombineMessages(message, DailyRoutineRewardMessage);
            }

            return new BouncyJumpCompletionResult(
                true,
                sessionResult.successes,
                sessionResult.misses,
                sessionResult.score,
                playSave.highestBouncyJumpScore,
                message);
        }

        public MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(
            int caught,
            int missed,
            int score,
            DateTimeOffset completedAt)
        {
            return CompleteMilkDropMiniGame(caught, missed, score, completedAt, true);
        }

        private MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(
            int caught,
            int missed,
            int score,
            DateTimeOffset completedAt,
            bool allowCurrencyReward)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return new MilkDropMiniGameRewardResult(
                    0,
                    0,
                    Math.Max(0, missed),
                    0,
                    0,
                    "치즈타마 저장 데이터를 불러오지 못했습니다.");
            }

            CurrentSave.EnsureRuntimeDefaults();
            EnsureMilkroomPresenceSession();
            var rewardStatus = GetMilkDropMiniGameRewardStatus(completedAt);
            var calculatedReward = MilkDropMiniGameRules.CalculateReward(caught, missed, score);
            var reward = calculatedReward;
            var canGrantCurrencyReward = allowCurrencyReward
                && rewardStatus.isAvailable
                && calculatedReward.HasReward;
            if (canGrantCurrencyReward)
            {
                CurrentSave.milkroomSession.lastMilkDropMiniGameRewardAtIso = completedAt.ToString("O");
            }
            else if (!rewardStatus.isAvailable || !allowCurrencyReward)
            {
                if (rewardStatus.shouldRepairTimestamp)
                {
                    CurrentSave.milkroomSession.lastMilkDropMiniGameRewardAtIso = completedAt.ToString("O");
                }

                var cooldownMessageSuffix = rewardStatus.remainingSeconds > 0
                    ? $"{MilkDropMiniGameRules.FormatCooldown(rewardStatus.remainingSeconds)} 뒤에 다시 받을 수 있어요"
                    : "다음 판부터 다시 받을 수 있어요";
                var cooldownMessage = calculatedReward.caught > 0
                    ? $"우유방울 {calculatedReward.caught}개를 받았어요! 미니게임 자원 보상은 {cooldownMessageSuffix}. 이번 판은 점수와 돌봄만 기록됐어요."
                    : $"이번에는 우유방울을 받지 못했어요. 미니게임 자원 보상은 {cooldownMessageSuffix}.";
                reward = new MilkDropMiniGameRewardResult(
                    calculatedReward.score,
                    calculatedReward.caught,
                    calculatedReward.missed,
                    0,
                    0,
                    cooldownMessage,
                    false,
                    rewardStatus.remainingSeconds);
            }

            CurrentSave.economy.milkCoins = SaturatingAdd(CurrentSave.economy.milkCoins, reward.milkCoins);
            CurrentSave.economy.milkDrops = SaturatingAdd(CurrentSave.economy.milkDrops, reward.milkDrops);

            if (reward.caught > 0)
            {
                CurrentSave.milkroomSession.todayMilkDropCatches = SaturatingAdd(
                    CurrentSave.milkroomSession.todayMilkDropCatches,
                    1);
                CurrentSave.milkroomSession.totalMilkDropCatches = SaturatingAdd(
                    CurrentSave.milkroomSession.totalMilkDropCatches,
                    1);
                AddUniqueRecord(CurrentSave.collections.events, MilkDropCatchEventId);
            }

            var careActions = CreateCareActionSystem();
            var careResult = careActions.Play(CurrentTama);
            RegisterCareAction("play");
            // Practice runs still count toward today's play goal, but cannot pay the
            // daily-routine bundle from the same no-reward result screen.
            var dailyCompleted = RegisterDailyCareAction(
                "play",
                allowCurrencyReward && rewardStatus.isAvailable);
            RefreshDerivedCollectionRecords();
            SaveGame();

            var message = reward.message;
            if (!string.IsNullOrWhiteSpace(careResult.message))
            {
                message = $"{message} {careResult.message}";
            }

            if (dailyCompleted)
            {
                message = $"{message} {DailyRoutineRewardMessage}";
            }

            return new MilkDropMiniGameRewardResult(
                reward.score,
                reward.caught,
                reward.missed,
                reward.milkCoins,
                reward.milkDrops,
                message,
                reward.currencyRewardGranted,
                reward.rewardCooldownRemainingSeconds);
        }

        private MilkDropMiniGameRewardStatus GetMilkDropMiniGameRewardStatus(DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return new MilkDropMiniGameRewardStatus(
                    false,
                    MilkDropMiniGameRules.RewardCooldownSeconds,
                    false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            return MilkDropMiniGameRules.EvaluateRewardCooldown(
                CurrentSave.milkroomSession.lastMilkDropMiniGameRewardAtIso,
                now);
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
                SaveGame();
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
            var history = CurrentSave.careHistory;
            history.totalCareActions += 1;
            history.lastCareActionId = actionId;
            history.lastCareActionAtIso = DateTimeOffset.Now.ToString("O");

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
                var maturationProgress = finalMaturationCycleSystem.AddProgress(
                    CurrentTama,
                    CurrentSave.starLegacy.maturationCycle,
                    GetFinalMaturationProgress(actionId, safeAmount),
                    $"care:{history.totalCareActions}:{history.lastCareActionAtIso}",
                    CurrentSave.unlocks.starMilkUnlocked,
                    CurrentSave.unlocks.fantasyPowderEnabled);
                starLegacyChanged |= maturationProgress.applied;
            }

            if (FirstDayJourneySystem.TryRecordCareAction(
                    CurrentSave.firstDayJourney,
                    actionId,
                    DateTimeOffset.Now))
            {
                FirstDayJourneyChanged?.Invoke();
            }

            if (memoryJournalSystem.TryRecordFirstDailyCare(
                    CurrentSave.memoryJournal,
                    actionId,
                    DateTimeOffset.Now,
                    CurrentTama.name,
                    CurrentTama.form,
                    out _))
            {
                MemoryJournalChanged?.Invoke();
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

            var candidate = randomEventSystem.RollCareEvent(CurrentTama, force);
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
            CareEventChoiceResolutionStatus status)
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
                    choice.effect);
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
            return careActions;
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
