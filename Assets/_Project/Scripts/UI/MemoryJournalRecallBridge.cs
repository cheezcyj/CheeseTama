using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.Dialogue;
using CheeseTama.Gameplay.Memories;
using UnityEngine;

namespace CheeseTama.UI
{
    /// <summary>
    /// Presents the newest journal memory once the Milkroom is free of modal UI,
    /// then persists the acknowledgement so scene changes cannot replay it.
    /// </summary>
    public sealed class MemoryJournalRecallBridge : MonoBehaviour
    {
        private const float PollIntervalSeconds = 0.35f;

        private static readonly string[] BlockingModalNames =
        {
            NewGameSetupController.OverlayObjectName,
            "First Meeting Onboarding Overlay",
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            GrowthJourneyController.OverlayObjectName,
            PlayChoicePanelController.OverlayObjectName,
            BouncyJumpMiniGameController.OverlayObjectName,
            CleaningMiniGameController.OverlayObjectName,
            "Milk Drop Catch Overlay",
            "Care Event Overlay",
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
            JourneyHubPanelController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName,
            "Decoration Shop Overlay",
            "Decorate Overlay",
            "Settings Modal",
            "CheeseTama Name Dialog",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel"
        };

        [SerializeField] private CheeseTamaSpeechBubbleController speechBubble;
        [SerializeField] private Transform modalContainer;

        private readonly MemoryJournalSystem journalSystem = new MemoryJournalSystem();
        private GameManager manager;
        private GameManager subscribedManager;
        private MemoryJournalRecall pendingRecall;
        private float nextPollAt;

        public void Configure(
            CheeseTamaSpeechBubbleController bubbleController,
            GameManager boundManager,
            Transform blockingModalContainer)
        {
            speechBubble = bubbleController;
            BindManager(boundManager);
            modalContainer = blockingModalContainer != null ? blockingModalContainer : transform;
            nextPollAt = 0f;
            SeedRecallFromCurrentSave();
        }

        private void OnEnable()
        {
            BindManager(manager ?? GameManager.Instance);
            SeedRecallFromCurrentSave();
            nextPollAt = 0f;
        }

        private void OnDisable()
        {
            BindManager(null);
            pendingRecall = null;
        }

        private void Update()
        {
            if (!Application.isPlaying || Time.unscaledTime < nextPollAt)
            {
                return;
            }

            nextPollAt = Time.unscaledTime + PollIntervalSeconds;
            if (manager?.CurrentSave?.memoryJournal == null
                || speechBubble == null
                || speechBubble.IsVisible
                || IsAnotherModalBlocking()
                || pendingRecall == null
                || string.IsNullOrWhiteSpace(pendingRecall.DialogueLine))
            {
                return;
            }

            if (speechBubble.Show(
                    pendingRecall.DialogueLine,
                    CheeseTamaDialoguePriority.FeedMemory,
                    4.5f,
                    false))
            {
                manager.AcknowledgeLatestMemoryRecall(pendingRecall.MemoryId);
                pendingRecall = null;
            }
        }

        private void BindManager(GameManager boundManager)
        {
            if (subscribedManager != null)
            {
                subscribedManager.SaveDataReplaced -= HandleSaveDataReplaced;
            }

            manager = boundManager;
            subscribedManager = boundManager;
            if (subscribedManager != null && isActiveAndEnabled)
            {
                subscribedManager.SaveDataReplaced += HandleSaveDataReplaced;
            }
        }

        private void HandleSaveDataReplaced()
        {
            SeedRecallFromCurrentSave();
            nextPollAt = 0f;
        }

        private void SeedRecallFromCurrentSave()
        {
            pendingRecall = null;
            if (manager?.CurrentSave?.memoryJournal != null)
            {
                journalSystem.TrySelectLatestRecall(
                    manager.CurrentSave.memoryJournal,
                    IsHiddenContentUnlocked,
                    out pendingRecall);
            }
        }

        private bool IsHiddenContentUnlocked(string unlockId)
        {
            var unlocks = manager?.CurrentSave?.unlocks;
            if (unlocks == null || string.IsNullOrWhiteSpace(unlockId))
            {
                return false;
            }

            return unlockId switch
            {
                "star" or "star_route" or "star_milk" => unlocks.starMilkUnlocked,
                "fantasy_powder" => unlocks.fantasyPowderEnabled,
                _ => false
            };
        }

        private bool IsAnotherModalBlocking()
        {
            if (modalContainer == null)
            {
                return false;
            }

            for (var index = 0; index < BlockingModalNames.Length; index += 1)
            {
                var modal = modalContainer.Find(BlockingModalNames[index]);
                if (modal != null && modal.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
