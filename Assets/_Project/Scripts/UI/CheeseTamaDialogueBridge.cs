using System;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Dialogue;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Feeding;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Stats;
using UnityEngine;

namespace CheeseTama.UI
{
    /// <summary>
    /// Connects authoritative gameplay notifications to the non-modal CheeseTama
    /// speech bubble. Dialogue memory is session-local and this bridge never
    /// reads from or writes to the save file directly.
    /// </summary>
    public sealed class CheeseTamaDialogueBridge : MonoBehaviour
    {
        public const float DefaultPassiveInitialDelaySeconds = 12f;
        public const float DefaultPassiveIntervalSeconds = 24f;

        private const float PendingRetryIntervalSeconds = 0.25f;
        private const float VisibleModalCheckIntervalSeconds = 0.1f;
        private const int LongReturnThresholdMinutes = 180;

        private static readonly string[] BlockingModalNames =
        {
            "New Game Setup Overlay",
            "First Meeting Onboarding Overlay",
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            "Care Event Overlay",
            "Decoration Shop Overlay",
            "Cooking Panel",
            "Snack Panel Overlay",
            "Cleaning Mini Game Overlay",
            "Milk Drop Catch Overlay",
            "Play Choice Overlay",
            "Bouncy Jump Overlay",
            "Growth Journey Overlay",
            FirstDayJourneyController.OverlayObjectName,
            "Cheese Star Delivery Overlay",
            "Memory Journal Overlay",
            "Fantasy Powder Overlay",
            SaveRecoveryNoticeController.OverlayObjectName,
            CheeseTamaProfileMenuController.OverlayObjectName,
            InputBindingsPanelController.OverlayObjectName,
            "Milk Blending Overlay",
            CookingChoicePanelController.OverlayObjectName,
            NpcVisitCardController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private CheeseTamaSpeechBubbleController speechBubble;
        [SerializeField] private Transform modalContainer;
        [SerializeField, Min(1f)] private float passiveInitialDelaySeconds =
            DefaultPassiveInitialDelaySeconds;
        [SerializeField, Min(5f)] private float passiveIntervalSeconds =
            DefaultPassiveIntervalSeconds;

        private GameManager boundManager;
        private GameManager subscribedManager;
        private bool configured;
        private bool hasPendingDialogue;
        private bool pendingPlaySound;
        private CheeseTamaDialogueRequest pendingRequest;
        private float nextPendingAttemptAt;
        private float nextPassiveDialogueAt;
        private float nextVisibleModalCheckAt;
        private int passiveTick;

        public bool IsConfigured => configured && speechBubble != null;
        public bool HasPendingDialogue => hasPendingDialogue;
        public bool IsModalBlocking => CheckModalBlocking();
        public GameManager BoundManager => boundManager;
        public CheeseTamaSpeechBubbleController SpeechBubble => speechBubble;

        /// <summary>
        /// Configures presentation ownership and binds gameplay notifications.
        /// Repeating this call with the same manager does not duplicate listeners.
        /// </summary>
        public void Configure(
            CheeseTamaSpeechBubbleController bubbleController,
            GameManager manager,
            Transform blockingModalContainer = null)
        {
            var managerChanged = boundManager != manager;
            speechBubble = bubbleController;
            modalContainer = blockingModalContainer != null
                ? blockingModalContainer
                : transform;
            configured = speechBubble != null;
            ClearPendingDialogue();
            Bind(manager);
            if (!managerChanged)
            {
                SeedPendingAuthoritativeDialogue();
            }

            ScheduleNextPassive(passiveInitialDelaySeconds);
        }

        /// <summary>
        /// Replaces the gameplay source while balancing all event subscriptions.
        /// Passing null cleanly detaches the bridge.
        /// </summary>
        public void Bind(GameManager manager)
        {
            if (boundManager == manager)
            {
                SubscribeIfNeeded();
                return;
            }

            Unsubscribe();
            boundManager = manager;
            ClearPendingDialogue();
            SubscribeIfNeeded();
            SeedPendingAuthoritativeDialogue();
            ScheduleNextPassive(passiveInitialDelaySeconds);
        }

        /// <summary>
        /// Explicit entry point for petting callers that want immediate feedback.
        /// The normal CareActionRegistered event also recognizes the "pet" action.
        /// </summary>
        public bool NotifyPet(bool playSound = true)
        {
            return PresentOrQueue(CheeseTamaDialogueRequest.ForPet(), playSound);
        }

        /// <summary>
        /// Explicit entry point for feeding callers that already know the milk.
        /// A negative tone is inferred from recoverable feeding status effects when
        /// the caller leaves tone as Any. A negative growth level is resolved from
        /// the currently bound manager without mutating persistent state.
        /// </summary>
        public bool NotifyFeed(
            string milkId,
            int growthLevel = -1,
            CheeseTamaDialogueTone tone = CheeseTamaDialogueTone.Any,
            bool playSound = true)
        {
            var resolvedMilkId = string.IsNullOrWhiteSpace(milkId)
                ? boundManager?.CurrentTama?.growthHistory?.lastFedMilkId ?? string.Empty
                : milkId;
            var resolvedGrowthLevel = growthLevel >= 0
                ? growthLevel
                : ResolveMilkGrowthLevel(resolvedMilkId);
            var resolvedTone = tone == CheeseTamaDialogueTone.Any
                ? ResolveFeedTone(boundManager?.CurrentTama)
                : tone;

            return PresentOrQueue(
                CheeseTamaDialogueRequest.ForFeed(
                    resolvedMilkId,
                    resolvedGrowthLevel,
                    resolvedTone),
                playSound);
        }

        /// <summary>
        /// Attempts to display the highest-priority notification deferred by a
        /// modal. This is also retried automatically at a throttled rate in Update.
        /// </summary>
        public bool TryPresentPendingDialogue()
        {
            if (!hasPendingDialogue
                || speechBubble == null
                || speechBubble.IsVisible
                || CheckModalBlocking())
            {
                return false;
            }

            if (!speechBubble.Show(pendingRequest, pendingPlaySound))
            {
                return false;
            }

            ClearPendingDialogue();
            return true;
        }

        private void OnEnable()
        {
            SubscribeIfNeeded();
            ScheduleNextPassive(passiveInitialDelaySeconds);
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearPendingDialogue();
            speechBubble?.Hide();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!IsConfigured || !Application.isPlaying)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (speechBubble.IsVisible && now >= nextVisibleModalCheckAt)
            {
                nextVisibleModalCheckAt = now + VisibleModalCheckIntervalSeconds;
                if (CheckModalBlocking())
                {
                    speechBubble.Hide();
                }
            }

            if (hasPendingDialogue && now >= nextPendingAttemptAt)
            {
                nextPendingAttemptAt = now + PendingRetryIntervalSeconds;
                if (TryPresentPendingDialogue())
                {
                    ScheduleNextPassive(passiveIntervalSeconds);
                }

                return;
            }

            if (now < nextPassiveDialogueAt)
            {
                return;
            }

            ScheduleNextPassive(passiveIntervalSeconds);
            if (speechBubble.IsVisible || CheckModalBlocking())
            {
                return;
            }

            var request = BuildPassiveRequest(boundManager?.CurrentTama, passiveTick);
            passiveTick = passiveTick == int.MaxValue ? 0 : passiveTick + 1;
            speechBubble.Show(request, false);
        }

        private void SubscribeIfNeeded()
        {
            if (!isActiveAndEnabled
                || boundManager == null
                || subscribedManager == boundManager)
            {
                return;
            }

            Unsubscribe();
            subscribedManager = boundManager;
            subscribedManager.CareActionRegistered += HandleCareActionRegistered;
            subscribedManager.ReturnSummaryAvailable += HandleReturnSummaryAvailable;
            subscribedManager.GrowthMilestoneAvailable += HandleGrowthMilestoneAvailable;
            subscribedManager.EvolutionMilestoneAvailable += HandleEvolutionMilestoneAvailable;
            subscribedManager.CareEventAvailable += HandleCareEventAvailable;
            subscribedManager.MilkGrowthMilestoneRewardGranted += HandleMilkGrowthRewardGranted;
            subscribedManager.SaveDataReplaced += HandleSaveDataReplaced;
        }

        private void Unsubscribe()
        {
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.CareActionRegistered -= HandleCareActionRegistered;
            subscribedManager.ReturnSummaryAvailable -= HandleReturnSummaryAvailable;
            subscribedManager.GrowthMilestoneAvailable -= HandleGrowthMilestoneAvailable;
            subscribedManager.EvolutionMilestoneAvailable -= HandleEvolutionMilestoneAvailable;
            subscribedManager.CareEventAvailable -= HandleCareEventAvailable;
            subscribedManager.MilkGrowthMilestoneRewardGranted -= HandleMilkGrowthRewardGranted;
            subscribedManager.SaveDataReplaced -= HandleSaveDataReplaced;
            subscribedManager = null;
        }

        private void HandleCareActionRegistered(string actionId)
        {
            if (string.Equals(actionId, "pet", StringComparison.Ordinal))
            {
                NotifyPet();
                return;
            }

            var milkId = ResolveMilkIdForAction(actionId);
            if (!string.IsNullOrWhiteSpace(milkId))
            {
                NotifyFeed(milkId);
            }
        }

        private void HandleReturnSummaryAvailable(ReturnSummaryData summary)
        {
            if (summary == null)
            {
                return;
            }

            var returnBand = summary.elapsedMinutes >= LongReturnThresholdMinutes
                ? "long"
                : "short";
            QueueDialogue(CheeseTamaDialogueRequest.ForReturn(returnBand), true);
        }

        private void HandleGrowthMilestoneAvailable(GrowthMilestoneData milestone)
        {
            if (milestone == null)
            {
                return;
            }

            var stageId = CheeseTamaGrowthStageCatalog.Get(milestone.stage).RecordId;
            QueueDialogue(CheeseTamaDialogueRequest.ForGrowth(stageId), true);
        }

        private void HandleEvolutionMilestoneAvailable(EvolutionMilestoneData milestone)
        {
            if (milestone == null)
            {
                return;
            }

            var evolutionId = milestone.result.EvolutionId;
            if (string.IsNullOrWhiteSpace(evolutionId))
            {
                evolutionId = boundManager?.CurrentTama?.evolutionId ?? string.Empty;
            }

            QueueDialogue(CheeseTamaDialogueRequest.ForEvolution(evolutionId), true);
        }

        private void HandleCareEventAvailable(CareEventResult careEvent)
        {
            if (!careEvent.occurred)
            {
                return;
            }

            QueueDialogue(CheeseTamaDialogueRequest.ForEvent(careEvent.eventId), true);
        }

        private void HandleMilkGrowthRewardGranted(MilkGrowthMilestoneRewardResult reward)
        {
            if (reward == null || !reward.granted)
            {
                return;
            }

            NotifyFeed(
                reward.milkId,
                reward.reachedLevel,
                CheeseTamaDialogueTone.Positive,
                true);
        }

        private void HandleSaveDataReplaced()
        {
            ClearPendingDialogue();
            speechBubble?.Hide();
            passiveTick = 0;
            SeedPendingAuthoritativeDialogue();
            ScheduleNextPassive(passiveInitialDelaySeconds);
        }

        private void SeedPendingAuthoritativeDialogue()
        {
            if (boundManager == null)
            {
                return;
            }

            // Route restored notifications through the same queue as live events.
            // This preserves the established modal deferral and priority ordering.
            if (boundManager.TryGetPendingReturnSummary(out var returnSummary))
            {
                HandleReturnSummaryAvailable(returnSummary);
            }

            if (boundManager.TryGetPendingGrowthMilestone(out var growthMilestone))
            {
                HandleGrowthMilestoneAvailable(growthMilestone);
            }

            if (boundManager.TryGetPendingEvolutionMilestone(out var evolutionMilestone))
            {
                HandleEvolutionMilestoneAvailable(evolutionMilestone);
            }
        }

        private bool PresentOrQueue(CheeseTamaDialogueRequest request, bool playSound)
        {
            if (speechBubble == null)
            {
                return false;
            }

            if (CheckModalBlocking())
            {
                QueueDialogue(request, playSound);
                return false;
            }

            return speechBubble.Show(request, playSound);
        }

        private void QueueDialogue(CheeseTamaDialogueRequest request, bool playSound)
        {
            if (hasPendingDialogue
                && GetExpectedPriority(request) <= GetExpectedPriority(pendingRequest))
            {
                return;
            }

            pendingRequest = request;
            pendingPlaySound = playSound;
            hasPendingDialogue = true;
            nextPendingAttemptAt = Time.unscaledTime + PendingRetryIntervalSeconds;
        }

        private void ClearPendingDialogue()
        {
            pendingRequest = default;
            pendingPlaySound = false;
            hasPendingDialogue = false;
            nextPendingAttemptAt = 0f;
        }

        private bool CheckModalBlocking()
        {
            if (boundManager?.IsSleepScheduleActive == true)
            {
                return true;
            }

            var container = modalContainer != null ? modalContainer : transform;
            if (container == null)
            {
                return false;
            }

            for (var index = 0; index < BlockingModalNames.Length; index += 1)
            {
                var modal = container.Find(BlockingModalNames[index]);
                if (modal != null && modal.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private int ResolveMilkGrowthLevel(string milkId)
        {
            if (boundManager == null || string.IsNullOrWhiteSpace(milkId))
            {
                return 0;
            }

            return boundManager.FindMilkGrowth(milkId)?.growthLevel ?? 0;
        }

        private static string ResolveMilkIdForAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return string.Empty;
            }

            for (var index = 0; index < MilkCatalog.VisibleMilks.Length; index += 1)
            {
                var milk = MilkCatalog.VisibleMilks[index];
                if (milk != null
                    && string.Equals(milk.actionId, actionId, StringComparison.Ordinal))
                {
                    return milk.id;
                }
            }

            return string.Empty;
        }

        private static CheeseTamaDialogueTone ResolveFeedTone(CheeseTamaModel tama)
        {
            if (tama?.stats?.overfullness > 0
                || tama?.growthHistory?.sameMilkFeedStreak
                    >= FeedingStatusSystem.MilkAversionStreakThreshold)
            {
                return CheeseTamaDialogueTone.Negative;
            }

            return CheeseTamaDialogueTone.Positive;
        }

        private static CheeseTamaDialogueRequest BuildPassiveRequest(
            CheeseTamaModel tama,
            int tick)
        {
            var stats = tama?.stats;
            if (stats == null || (tick & 1) != 0)
            {
                return new CheeseTamaDialogueRequest(CheeseTamaDialogueContext.Ambient);
            }

            return CheeseTamaDialogueRequest.ForState(
                CheeseTamaDialogueRules.ResolveState(
                    stats.health,
                    stats.hunger,
                    stats.cleanliness,
                    stats.sleepiness,
                    stats.mood));
        }

        private static int GetExpectedPriority(CheeseTamaDialogueRequest request)
        {
            return request.Context switch
            {
                CheeseTamaDialogueContext.Event => (int)CheeseTamaDialoguePriority.Event,
                CheeseTamaDialogueContext.Evolution => (int)CheeseTamaDialoguePriority.Evolution,
                CheeseTamaDialogueContext.Growth => (int)CheeseTamaDialoguePriority.Growth,
                CheeseTamaDialogueContext.State => (int)CheeseTamaDialoguePriority.State,
                CheeseTamaDialogueContext.Return => (int)CheeseTamaDialoguePriority.Return,
                CheeseTamaDialogueContext.Feed when request.Tone == CheeseTamaDialogueTone.Negative
                    => (int)CheeseTamaDialoguePriority.State,
                CheeseTamaDialogueContext.Feed => (int)CheeseTamaDialoguePriority.FeedMemory,
                CheeseTamaDialogueContext.Pet => (int)CheeseTamaDialoguePriority.Pet,
                _ => (int)CheeseTamaDialoguePriority.Ambient
            };
        }

        private void ScheduleNextPassive(float delaySeconds)
        {
            nextPassiveDialogueAt = Time.unscaledTime + Mathf.Max(1f, delaySeconds);
        }
    }
}
