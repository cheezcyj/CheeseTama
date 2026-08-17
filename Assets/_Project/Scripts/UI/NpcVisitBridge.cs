using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.NpcVisits;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class NpcVisitBridge : MonoBehaviour
    {
        private static readonly string[] BlockingOverlayNames =
        {
            NewGameSetupController.OverlayObjectName,
            "First Meeting Onboarding Overlay",
            SaveRecoveryNoticeController.OverlayObjectName,
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            FirstDayJourneyController.OverlayObjectName,
            GrowthJourneyController.OverlayObjectName,
            PlayChoicePanelController.OverlayObjectName,
            "Milk Drop Catch Overlay",
            BouncyJumpMiniGameController.OverlayObjectName,
            CleaningMiniGameController.OverlayObjectName,
            "Care Event Overlay",
            "Cheese Star Delivery Overlay",
            "Memory Journal Overlay",
            "Fantasy Powder Overlay",
            CheeseTamaProfileMenuController.OverlayObjectName,
            "Settings Modal",
            "Confirm Reset Dialog",
            "Collection Overlay",
            "Decorate Overlay",
            "Decoration Shop Overlay",
            StarLegacyPanelController.OverlayObjectName,
            "Bond Status Overlay",
            "Hidden Career Card Overlay",
            "CheeseTama Name Dialog",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel",
            InputBindingsPanelController.OverlayObjectName,
            "Milk Blending Overlay",
            CookingChoicePanelController.OverlayObjectName,
            NpcVisitCardController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private NpcVisitCardController cardController;
        [SerializeField] private Transform blockingContainer;

        private GameManager manager;
        private GameManager subscribedManager;
        private string suppressedOccurrenceId = string.Empty;
        private float nextPollAt;

        public bool IsBlockingGameplay => cardController != null && cardController.IsBlockingGameplay;

        public void Configure(
            NpcVisitCardController controller,
            GameManager gameManager,
            Transform modalContainer = null)
        {
            cardController = controller;
            blockingContainer = modalContainer;
            Bind(gameManager);
            TryShowPendingVisit();
        }

        public void Bind(GameManager gameManager)
        {
            manager = gameManager;
            Subscribe(gameManager);
        }

        public bool TryShowPendingVisit()
        {
            if (!Application.isPlaying
                || manager == null
                || cardController == null
                || cardController.IsBlockingGameplay
                || IsAnotherModalBlocking()
                || !manager.TryGetPendingNpcVisit(out var offer)
                || offer == null
                || string.Equals(offer.OccurrenceId, suppressedOccurrenceId, StringComparison.Ordinal))
            {
                return false;
            }

            return cardController.Show(
                offer,
                Resolve,
                () => suppressedOccurrenceId = offer.OccurrenceId);
        }

        private NpcVisitResolutionResult Resolve(string occurrenceId, string choiceId)
        {
            if (manager != null
                && manager.TryResolvePendingNpcVisit(occurrenceId, choiceId, out var result))
            {
                suppressedOccurrenceId = occurrenceId;
                return result;
            }

            return null;
        }

        private void Update()
        {
            if (!Application.isPlaying || Time.unscaledTime < nextPollAt)
            {
                return;
            }

            nextPollAt = Time.unscaledTime + 0.5f;
            TryShowPendingVisit();
        }

        private void OnEnable()
        {
            Subscribe(manager);
        }

        private void OnDisable()
        {
            Subscribe(null);
        }

        private void Subscribe(GameManager target)
        {
            if (subscribedManager == target)
            {
                return;
            }

            if (subscribedManager != null)
            {
                subscribedManager.NpcVisitAvailable -= HandleVisitAvailable;
                subscribedManager.SaveDataReplaced -= HandleSaveDataReplaced;
            }

            subscribedManager = target;
            if (subscribedManager != null && isActiveAndEnabled)
            {
                subscribedManager.NpcVisitAvailable += HandleVisitAvailable;
                subscribedManager.SaveDataReplaced += HandleSaveDataReplaced;
            }
        }

        private void HandleVisitAvailable(NpcVisitOffer _)
        {
            TryShowPendingVisit();
        }

        private void HandleSaveDataReplaced()
        {
            suppressedOccurrenceId = string.Empty;
            TryShowPendingVisit();
        }

        private bool IsAnotherModalBlocking()
        {
            if (blockingContainer == null)
            {
                return false;
            }

            foreach (var name in BlockingOverlayNames)
            {
                var candidate = blockingContainer.Find(name);
                if (candidate != null
                    && candidate.gameObject.activeInHierarchy
                    && (cardController == null || candidate.gameObject != cardController.gameObject))
                {
                    if (!string.Equals(name, NpcVisitCardController.OverlayObjectName, StringComparison.Ordinal)
                        || !cardController.IsBlockingGameplay)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
