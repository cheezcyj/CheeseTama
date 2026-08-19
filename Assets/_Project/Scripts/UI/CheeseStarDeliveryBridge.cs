using System;
using System.Globalization;
using CheeseTama.Core;
using CheeseTama.Gameplay.Deliveries;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class CheeseStarDeliveryBridge : MonoBehaviour
    {
        private const float PollIntervalSeconds = 0.3f;

        public const string EntryNotificationBadgeObjectName = "Delivery Notification Badge";
        public const string PendingEntryLabel = "오늘배달";
        public const string ClaimedEntryLabel = "오늘배달 받음";

        private static readonly string[] BlockingModalNames =
        {
            NewGameSetupController.OverlayObjectName,
            "First Meeting Onboarding Overlay",
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            FirstDayJourneyController.OverlayObjectName,
            GrowthJourneyController.OverlayObjectName,
            PlayChoicePanelController.OverlayObjectName,
            BouncyJumpMiniGameController.OverlayObjectName,
            CleaningMiniGameController.OverlayObjectName,
            "Milk Drop Catch Overlay",
            "Care Event Overlay",
            "Star Route Unlock Overlay",
            "Fantasy Powder Overlay",
            SaveRecoveryNoticeController.OverlayObjectName,
            CheeseTamaProfileMenuController.OverlayObjectName,
            "Memory Journal Overlay",
            "Decoration Shop Overlay",
            "Decorate Overlay",
            "Settings Modal",
            "Confirm Reset Dialog",
            InputBindingsPanelController.OverlayObjectName,
            "Milk Blending Overlay",
            CookingChoicePanelController.OverlayObjectName,
            NpcVisitCardController.OverlayObjectName,
            JourneyHubPanelController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName,
            "CheeseTama Name Dialog",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel"
        };

        [SerializeField] private CheeseStarDeliveryCardController cardController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;
        [SerializeField] private Transform modalContainer;
        [SerializeField] private Button entryButton;

        private GameManager configuredManager;
        private GameManager boundManager;
        private float nextPollAt;
        private bool configured;
        private bool evaluating;
        private bool claimInFlight;
        private bool managerEventsBound;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;
        private string displayedDateKey = string.Empty;
        private string lastAutoHandledDateKey = string.Empty;
        private string observedEligibleDateKey = string.Empty;
        private string entryPresentationDateKey = string.Empty;
        private GameObject entryNotificationBadge;

        public bool IsBlockingGameplay => cardController != null
            && cardController.IsBlockingGameplay;

        public string LastAutoHandledDateKey => lastAutoHandledDateKey;

        public void BindEntryButton(Button deliveryEntryButton)
        {
            if (entryButton != deliveryEntryButton)
            {
                if (entryNotificationBadge != null)
                {
                    entryNotificationBadge.SetActive(false);
                }

                entryButton = deliveryEntryButton;
                entryNotificationBadge = null;
                entryPresentationDateKey = string.Empty;
            }

            EnsureEntryNotificationBadge();
            RefreshEntryPresentation();
        }

        public void RefreshEntryPresentation()
        {
            if (entryButton == null || evaluating)
            {
                return;
            }

            EnsureManagerBinding();
            if (boundManager == null)
            {
                ApplyEntryPresentation(null);
                return;
            }

            evaluating = true;
            try
            {
                var offer = boundManager.ObserveCheeseStarDelivery();
                if (offer != null && offer.CanClaim && offer.StateChanged)
                {
                    observedEligibleDateKey = offer.DateKey;
                }

                ApplyEntryPresentation(offer);
            }
            finally
            {
                evaluating = false;
            }
        }

        public void Configure(
            CheeseStarDeliveryCardController deliveryCardController,
            GameManager gameManager,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController,
            Transform blockingModalContainer = null)
        {
            HideCardAndRestoreControls();
            BindManager(null);

            cardController = deliveryCardController;
            configuredManager = gameManager;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            modalContainer = blockingModalContainer != null
                ? blockingModalContainer
                : transform;
            configured = cardController != null;
            displayedDateKey = string.Empty;
            lastAutoHandledDateKey = string.Empty;
            observedEligibleDateKey = string.Empty;
            entryPresentationDateKey = string.Empty;
            nextPollAt = 0f;

            cardController?.Hide();
            BindManager(configuredManager);
            RefreshEntryPresentation();
        }

        public bool TryShowPendingDelivery()
        {
            if (!configured || evaluating || cardController == null)
            {
                return false;
            }

            EnsureManagerBinding();
            if (boundManager == null
                || cardController.IsVisible
                || IsPriorityFlowPending())
            {
                return false;
            }

            if (IsCurrentLocalDate(lastAutoHandledDateKey))
            {
                return false;
            }

            evaluating = true;
            try
            {
                var offer = boundManager.ObserveCheeseStarDelivery();
                if (offer == null)
                {
                    ApplyEntryPresentation(null);
                    return false;
                }

                ApplyEntryPresentation(offer);

                if (!offer.CanClaim)
                {
                    if (offer.Status != CheeseStarDeliveryOfferStatus.InvalidSaveData)
                    {
                        lastAutoHandledDateKey = offer.DateKey;
                    }

                    return false;
                }

                if (offer.StateChanged)
                {
                    observedEligibleDateKey = offer.DateKey;
                }
                else if (!string.Equals(
                    observedEligibleDateKey,
                    offer.DateKey,
                    StringComparison.Ordinal))
                {
                    // The date watermark was already persisted by an earlier bridge instance.
                    // Treat it as today's completed automatic presentation, while leaving the
                    // unclaimed delivery available to explicit/manual entry points.
                    lastAutoHandledDateKey = offer.DateKey;
                    return false;
                }

                return TryShowOfferInternal(offer, respectAutoHandledDate: true);
            }
            finally
            {
                evaluating = false;
            }
        }

        public bool TryShowOffer(CheeseStarDeliveryOffer offer)
        {
            if (!configured
                || evaluating
                || offer == null
                || cardController == null
                || cardController.IsVisible)
            {
                return false;
            }

            ApplyEntryPresentation(offer);
            if (!offer.CanClaim || HasActiveBlockingModal(modalContainer))
            {
                return false;
            }

            return TryShowOfferInternal(offer, respectAutoHandledDate: false);
        }

        public void ResetAutoDisplayForCurrentSave()
        {
            HideCardAndRestoreControls();
            displayedDateKey = string.Empty;
            lastAutoHandledDateKey = string.Empty;
            observedEligibleDateKey = string.Empty;
            entryPresentationDateKey = string.Empty;
            nextPollAt = 0f;
            RefreshEntryPresentation();
        }

        private void OnEnable()
        {
            if (!configured)
            {
                return;
            }

            BindManager(configuredManager != null
                ? configuredManager
                : GameManager.Instance);
            nextPollAt = 0f;
            RefreshEntryPresentation();
        }

        private void OnDisable()
        {
            BindManager(null);
            HideCardAndRestoreControls();
        }

        private void OnDestroy()
        {
            BindManager(null);
        }

        private void Update()
        {
            if (!configured
                || !Application.isPlaying
                || Time.unscaledTime < nextPollAt)
            {
                return;
            }

            nextPollAt = Time.unscaledTime + PollIntervalSeconds;
            if (entryButton != null && !IsCurrentLocalDate(entryPresentationDateKey))
            {
                RefreshEntryPresentation();
            }

            if (!cardController.IsVisible)
            {
                TryShowPendingDelivery();
            }
        }

        private bool TryShowOfferInternal(
            CheeseStarDeliveryOffer offer,
            bool respectAutoHandledDate)
        {
            if (string.IsNullOrWhiteSpace(offer.DateKey)
                || (respectAutoHandledDate
                    && string.Equals(
                        lastAutoHandledDateKey,
                        offer.DateKey,
                        StringComparison.Ordinal)))
            {
                return false;
            }

            SuspendControls();
            if (!cardController.Show(
                    offer,
                    HandleClaimRequested,
                    HandleLaterRequested))
            {
                RestoreControls();
                return false;
            }

            displayedDateKey = offer.DateKey;
            lastAutoHandledDateKey = offer.DateKey;
            return true;
        }

        private void HandleClaimRequested()
        {
            if (claimInFlight)
            {
                return;
            }

            EnsureManagerBinding();
            if (boundManager == null)
            {
                cardController?.SetInteractionEnabled(true);
                return;
            }

            claimInFlight = true;
            CheeseStarDeliveryClaimResult result;
            try
            {
                result = boundManager.ClaimCheeseStarDelivery();
            }
            finally
            {
                claimInFlight = false;
            }

            RefreshEntryPresentation();

            if (result == null)
            {
                cardController?.SetInteractionEnabled(true);
                return;
            }

            HideCardAndRestoreControls();
        }

        private void HandleLaterRequested()
        {
            displayedDateKey = string.Empty;
            RestoreControls();
        }

        private void HandleDeliveryChanged()
        {
            if (evaluating)
            {
                return;
            }

            if (claimInFlight)
            {
                nextPollAt = 0f;
                return;
            }

            var lastClaimedDateKey = boundManager?
                .CurrentSave?
                .cheeseStarDelivery?
                .lastClaimedDateKey;
            if (cardController != null
                && cardController.IsVisible
                && !string.IsNullOrEmpty(displayedDateKey)
                && string.Equals(
                    displayedDateKey,
                    lastClaimedDateKey,
                    StringComparison.Ordinal))
            {
                HideCardAndRestoreControls();
            }

            RefreshEntryPresentation();
            nextPollAt = 0f;
        }

        private void HandleSaveDataReplaced()
        {
            ResetAutoDisplayForCurrentSave();
            EnsureManagerBinding();
        }

        private void EnsureManagerBinding()
        {
            var desired = configuredManager != null
                ? configuredManager
                : GameManager.Instance;
            if (boundManager != desired)
            {
                BindManager(desired);
            }
        }

        private void BindManager(GameManager manager)
        {
            var shouldSubscribe = manager != null && isActiveAndEnabled;
            if (boundManager == manager && managerEventsBound == shouldSubscribe)
            {
                return;
            }

            if (boundManager != null && managerEventsBound)
            {
                boundManager.CheeseStarDeliveryChanged -= HandleDeliveryChanged;
                boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
            }

            boundManager = manager;
            managerEventsBound = false;
            if (shouldSubscribe)
            {
                boundManager.CheeseStarDeliveryChanged += HandleDeliveryChanged;
                boundManager.SaveDataReplaced += HandleSaveDataReplaced;
                managerEventsBound = true;
            }
        }

        private bool IsPriorityFlowPending()
        {
            var saveData = boundManager?.CurrentSave;
            if (saveData != null)
            {
                if (saveData.newGameSetup == null
                    || !saveData.newGameSetup.completed
                    || saveData.onboarding == null
                    || !saveData.onboarding.completed
                    || saveData.onboarding.replaying)
                {
                    return true;
                }

                var firstDayJourney = saveData.firstDayJourney;
                if (firstDayJourney != null
                    && !firstDayJourney.legacySuppressed
                    && !firstDayJourney.introShown)
                {
                    return true;
                }
            }

            if (boundManager != null
                && (boundManager.HasPendingReturnSummary
                    || boundManager.HasPendingGrowthMilestone
                    || boundManager.HasPendingEvolutionMilestone
                    || boundManager.HasPendingCareEvent
                    || boundManager.HasPendingStarRouteUnlock))
            {
                return true;
            }

            return HasActiveBlockingModal(modalContainer);
        }

        private static bool HasActiveBlockingModal(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            for (var childIndex = 0; childIndex < root.childCount; childIndex += 1)
            {
                var child = root.GetChild(childIndex);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (IsBlockingModalName(child.name)
                    || HasActiveBlockingModal(child))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBlockingModalName(string objectName)
        {
            for (var index = 0; index < BlockingModalNames.Length; index += 1)
            {
                if (string.Equals(
                        BlockingModalNames[index],
                        objectName,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCurrentLocalDate(string dateKey)
        {
            return DateTime.TryParseExact(
                    dateKey,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed)
                && parsed.Date == DateTime.Now.Date;
        }

        private void ApplyEntryPresentation(CheeseStarDeliveryOffer offer)
        {
            if (entryButton == null)
            {
                return;
            }

            EnsureEntryNotificationBadge();
            entryPresentationDateKey = offer?.DateKey ?? string.Empty;

            var isAvailable = offer != null
                && offer.Status == CheeseStarDeliveryOfferStatus.Available;
            var isClaimed = offer != null
                && offer.Status == CheeseStarDeliveryOfferStatus.AlreadyClaimed;
            var label = entryButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = isClaimed
                    ? ClaimedEntryLabel
                    : PendingEntryLabel;
            }

            entryButton.interactable = isAvailable;
            if (entryNotificationBadge != null)
            {
                entryNotificationBadge.SetActive(isAvailable);
            }
        }

        private void EnsureEntryNotificationBadge()
        {
            if (entryButton == null)
            {
                return;
            }

            var existing = entryButton.transform.Find(EntryNotificationBadgeObjectName);
            if (existing != null)
            {
                entryNotificationBadge = existing.gameObject;
            }
            else
            {
                entryNotificationBadge = new GameObject(EntryNotificationBadgeObjectName);
                entryNotificationBadge.transform.SetParent(entryButton.transform, false);
            }

            var badgeRect = entryNotificationBadge.GetComponent<RectTransform>();
            if (badgeRect == null)
            {
                badgeRect = entryNotificationBadge.AddComponent<RectTransform>();
            }

            badgeRect.anchorMin = Vector2.one;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = new Vector2(16f, 16f);

            var badgeImage = entryNotificationBadge.GetComponent<Image>();
            if (badgeImage == null)
            {
                badgeImage = entryNotificationBadge.AddComponent<Image>();
            }

            badgeImage.color = new Color(0.92f, 0.12f, 0.1f, 1f);
            badgeImage.raycastTarget = false;
            StarterSceneBuilder.ApplyCircleImage(badgeImage);

            var outline = entryNotificationBadge.GetComponent<Outline>();
            if (outline == null)
            {
                outline = entryNotificationBadge.AddComponent<Outline>();
            }

            outline.effectColor = new Color(1f, 0.92f, 0.76f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            entryNotificationBadge.transform.SetAsLastSibling();
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomActionBarWasEnabled = bottomActionBarController != null
                && bottomActionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;

            if (topMenuController != null)
            {
                topMenuController.enabled = false;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = false;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = false;
            }

            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null)
            {
                topMenuController.enabled = topMenuWasEnabled;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = bottomActionBarWasEnabled;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = devPanelWasEnabled;
            }

            controlsSuspended = false;
        }

        private void HideCardAndRestoreControls()
        {
            cardController?.Hide();
            displayedDateKey = string.Empty;
            RestoreControls();
        }
    }
}
