using CheeseTama.Core;
using CheeseTama.Gameplay.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class CareEventCardController : MonoBehaviour
    {
        private enum CardStage
        {
            AutomaticEvent,
            ChoicePrompt,
            ChoiceResult
        }

        private const float PresentationDurationSeconds = 0.18f;
        private const float PresentationStartScale = 0.94f;
        private const string FirstChoiceButtonName = "Care Event First Choice Button";
        private const string SecondChoiceButtonName = "Care Event Second Choice Button";
        private const string FollowUpButtonName = "Care Event Follow Up Button";

        private static readonly string[] BlockingUiNames =
        {
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Collection Overlay",
            "Decorate Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel",
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Milk Drop Catch Overlay"
            ,"Cleaning Mini Game Overlay"
            ,"Evolution Achievement Overlay"
            ,"Decoration Shop Overlay"
            ,"New Game Setup Overlay"
            ,"Growth Journey Overlay"
            ,"Play Choice Overlay"
            ,"Bouncy Jump Overlay"
            ,FirstDayJourneyController.OverlayObjectName
            ,"Cheese Star Delivery Overlay"
            ,"Memory Journal Overlay"
            ,"Fantasy Powder Overlay"
            ,SaveRecoveryNoticeController.OverlayObjectName
            ,CheeseTamaProfileMenuController.OverlayObjectName
            ,InputBindingsPanelController.OverlayObjectName
            ,"Milk Blending Overlay"
            ,CookingChoicePanelController.OverlayObjectName
            ,NpcVisitCardController.OverlayObjectName
            ,JourneyHubPanelController.OverlayObjectName
            ,SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject firstDiscoveryBadge;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;
        [SerializeField] private CheeseTamaVisualController visualController;

        private GameManager boundManager;
        private CanvasGroup overlayCanvasGroup;
        private Button firstChoiceButton;
        private Button secondChoiceButton;
        private Button followUpButton;
        private CareEventDefinition displayedDefinition;
        private CareEventFollowUpAction pendingFollowUpAction;
        private CardStage cardStage;
        private bool configured;
        private string displayedOccurrenceId = string.Empty;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;
        private bool presentationAnimating;
        private float presentationStartedAt;
        private Vector3 cardRestingScale = Vector3.one;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeInHierarchy;

        public void Configure(
            GameObject root,
            RectTransform eventCard,
            Text titleLabel,
            Text bodyLabel,
            GameObject discoveryBadge,
            Button closeButton,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController,
            CheeseTamaVisualController tamaVisual)
        {
            RestoreControls();
            UnbindButtons();

            overlayRoot = root;
            cardTransform = eventCard;
            titleText = titleLabel;
            messageText = bodyLabel;
            firstDiscoveryBadge = discoveryBadge;
            confirmButton = closeButton;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            visualController = tamaVisual;
            cardRestingScale = cardTransform != null ? cardTransform.localScale : Vector3.one;
            configured = overlayRoot != null && confirmButton != null;

            EnsureCanvasGroup();
            if (Application.isPlaying)
            {
                EnsureChoiceButtons();
            }

            BindButtons();
            SetOverlayActive(false);
            ResetCardState();
            ResetPresentation();
            if (Application.isPlaying && configured)
            {
                BindManager(GameManager.Instance);
                TryShowPendingEvent();
            }
        }

        private void OnEnable()
        {
            if (!configured || overlayRoot == null || confirmButton == null)
            {
                return;
            }

            EnsureCanvasGroup();
            EnsureChoiceButtons();
            BindButtons();
            if (Application.isPlaying)
            {
                BindManager(GameManager.Instance);
                TryShowPendingEvent();
            }
        }

        private void OnDisable()
        {
            UnbindButtons();
            BindManager(null);
            SetOverlayActive(false);
            displayedOccurrenceId = string.Empty;
            ResetCardState();
            RestoreControls();
            ResetPresentation();
        }

        private void Update()
        {
            if (!configured || overlayRoot == null || confirmButton == null || !Application.isPlaying)
            {
                return;
            }

            UpdatePresentation();
            if (IsBlockingGameplay)
            {
                if (CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
                {
                    if (cardStage != CardStage.ChoicePrompt)
                    {
                        Confirm();
                    }
                }

                return;
            }

            TryShowPendingEvent();
        }

        public void Confirm()
        {
            if (cardStage == CardStage.ChoicePrompt)
            {
                return;
            }

            if (cardStage == CardStage.AutomaticEvent
                && boundManager != null
                && !string.IsNullOrWhiteSpace(displayedOccurrenceId))
            {
                boundManager.ConsumePendingCareEvent(displayedOccurrenceId);
            }

            displayedOccurrenceId = string.Empty;
            SetOverlayActive(false);
            ResetCardState();
            RestoreControls();
            ResetPresentation();
        }

        private void BindManager(GameManager manager)
        {
            if (boundManager == manager)
            {
                return;
            }

            if (boundManager != null)
            {
                boundManager.CareEventAvailable -= HandleCareEventAvailable;
                boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
            }

            boundManager = manager;
            if (boundManager != null)
            {
                boundManager.CareEventAvailable += HandleCareEventAvailable;
                boundManager.SaveDataReplaced += HandleSaveDataReplaced;
            }
        }

        private void HandleCareEventAvailable(CareEventResult eventResult)
        {
            TryShowPendingEvent();
        }

        private void HandleSaveDataReplaced()
        {
            if (IsBlockingGameplay
                && (boundManager == null
                    || !boundManager.TryGetPendingCareEvent(out var pending)
                    || !string.Equals(pending.occurrenceId, displayedOccurrenceId)))
            {
                displayedOccurrenceId = string.Empty;
                SetOverlayActive(false);
                ResetCardState();
                RestoreControls();
                ResetPresentation();
            }

            TryShowPendingEvent();
        }

        private void TryShowPendingEvent()
        {
            if (!configured
                || overlayRoot == null
                || confirmButton == null
                || !Application.isPlaying
                || IsBlockingGameplay)
            {
                return;
            }

            if (boundManager == null)
            {
                BindManager(GameManager.Instance);
            }

            if (boundManager == null
                || !boundManager.TryGetPendingCareEvent(out var pending)
                || !pending.occurred
                || string.IsNullOrWhiteSpace(pending.occurrenceId)
                || IsAnotherModalBlocking())
            {
                return;
            }

            EnsureChoiceButtons();
            BindButtons();
            displayedOccurrenceId = pending.occurrenceId;
            Populate(pending);
            SuspendControls();
            BeginPresentation();
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            if (EventSystem.current != null)
            {
                var selectedButton = cardStage == CardStage.ChoicePrompt
                    ? firstChoiceButton
                    : confirmButton;
                if (selectedButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(selectedButton.gameObject);
                }
            }

            if (visualController != null)
            {
                visualController.Bind(boundManager.CurrentTama);
                visualController.ReactEvent(pending.eventId);
            }
        }

        private bool IsAnotherModalBlocking()
        {
            var onboarding = GetComponent<FirstMeetingOnboardingController>();
            if (onboarding != null && onboarding.IsBlockingGameplay)
            {
                return true;
            }

            var onboardingSave = boundManager != null ? boundManager.CurrentSave?.onboarding : null;
            if (onboardingSave != null && (!onboardingSave.completed || onboardingSave.replaying))
            {
                return true;
            }

            foreach (var childName in BlockingUiNames)
            {
                var child = transform.Find(childName);
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void Populate(CareEventResult eventResult)
        {
            var title = eventResult.title;
            var message = eventResult.message;
            displayedDefinition = null;
            if (RandomEventSystem.TryGetDefinition(eventResult.eventId, out var definition))
            {
                displayedDefinition = definition;
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = definition.title;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    message = definition.message;
                }
            }

            SetText(titleText, string.IsNullOrWhiteSpace(title) ? "밀크룸의 작은 순간" : title);
            SetText(messageText, string.IsNullOrWhiteSpace(message) ? "치즈타마와 새로운 순간을 발견했어요." : message);
            if (firstDiscoveryBadge != null)
            {
                firstDiscoveryBadge.SetActive(eventResult.firstDiscovery);
            }

            if (displayedDefinition != null
                && displayedDefinition.RequiresChoice
                && firstChoiceButton != null
                && secondChoiceButton != null)
            {
                ShowChoicePrompt(displayedDefinition);

                return;
            }

            cardStage = CardStage.AutomaticEvent;
            SetButtonVisible(firstChoiceButton, false);
            SetButtonVisible(secondChoiceButton, false);
            SetButtonVisible(confirmButton, true);
        }

        private void ShowChoicePrompt(CareEventDefinition definition)
        {
            if (definition == null || !definition.RequiresChoice)
            {
                return;
            }

            cardStage = CardStage.ChoicePrompt;
            ConfigureChoiceButtonLabel(firstChoiceButton, definition.Choices[0].label);
            ConfigureChoiceButtonLabel(secondChoiceButton, definition.Choices[1].label);
            if (firstChoiceButton != null)
            {
                firstChoiceButton.interactable = true;
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.interactable = true;
            }

            SetButtonVisible(firstChoiceButton, true);
            SetButtonVisible(secondChoiceButton, true);
            SetButtonVisible(confirmButton, false);
        }

        private void ChooseFirst()
        {
            Choose(0);
        }

        private void ChooseSecond()
        {
            Choose(1);
        }

        private void Choose(int choiceIndex)
        {
            if (cardStage != CardStage.ChoicePrompt
                || displayedDefinition == null
                || !displayedDefinition.RequiresChoice
                || choiceIndex < 0
                || choiceIndex >= displayedDefinition.Choices.Count
                || boundManager == null
                || !boundManager.TryGetPendingCareEvent(out var pending)
                || !string.Equals(pending.occurrenceId, displayedOccurrenceId))
            {
                return;
            }

            if (firstChoiceButton != null)
            {
                firstChoiceButton.interactable = false;
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.interactable = false;
            }

            var choice = displayedDefinition.Choices[choiceIndex];
            if (!boundManager.TryResolvePendingCareEventChoice(
                    pending.occurrenceId,
                    choice.id,
                    out var result))
            {
                if (firstChoiceButton != null)
                {
                    firstChoiceButton.interactable = true;
                }

                if (secondChoiceButton != null)
                {
                    secondChoiceButton.interactable = true;
                }

                return;
            }

            ShowChoiceResult(result);
            if (EventSystem.current != null)
            {
                var selectedButton = followUpButton != null && followUpButton.gameObject.activeInHierarchy
                    ? followUpButton
                    : confirmButton;
                EventSystem.current.SetSelectedGameObject(selectedButton != null ? selectedButton.gameObject : null);
            }
        }

        private void ShowChoiceResult(CareEventChoiceResult result)
        {
            cardStage = CardStage.ChoiceResult;
            SetText(titleText, string.IsNullOrWhiteSpace(result.title) ? "선택한 순간" : result.title);
            var summary = result.effect.BuildSummary();
            var body = string.IsNullOrWhiteSpace(result.message)
                ? "선택한 행동이 치즈타마와 밀크룸에 반영되었어요."
                : result.message;
            SetText(messageText, string.IsNullOrWhiteSpace(summary) ? body : $"{body}\n\n{summary}");
            if (firstDiscoveryBadge != null)
            {
                firstDiscoveryBadge.SetActive(false);
            }

            SetButtonVisible(firstChoiceButton, false);
            SetButtonVisible(secondChoiceButton, false);
            pendingFollowUpAction = result.effect.followUpAction;
            var followUpLabel = GetFollowUpButtonLabel(pendingFollowUpAction);
            ConfigureChoiceButtonLabel(followUpButton, followUpLabel);
            SetButtonVisible(followUpButton, !string.IsNullOrWhiteSpace(followUpLabel));
            SetButtonVisible(confirmButton, true);
        }

        private void HandleFollowUpRequested()
        {
            if (cardStage != CardStage.ChoiceResult
                || pendingFollowUpAction == CareEventFollowUpAction.None)
            {
                return;
            }

            var action = pendingFollowUpAction;
            Confirm();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            OpenFollowUpDestination(action);
        }

        private void OpenFollowUpDestination(CareEventFollowUpAction action)
        {
            switch (action)
            {
                case CareEventFollowUpAction.FeedMilk:
                    GetComponent<MilkPanelController>()?.Open();
                    break;
                case CareEventFollowUpAction.Cook:
                    GetComponent<CookingPanelController>()?.Open();
                    break;
                case CareEventFollowUpAction.Clean:
                    GetComponent<CleaningMiniGameController>()?.Open();
                    break;
                case CareEventFollowUpAction.Rest:
                    GetComponent<SleepScheduleBridge>()?.Open();
                    break;
                case CareEventFollowUpAction.Play:
                    GetComponent<PlayChoicePanelController>()?.Open();
                    break;
                case CareEventFollowUpAction.OpenCollection:
                    topMenuController?.OpenCollection();
                    break;
            }
        }

        private static string GetFollowUpButtonLabel(CareEventFollowUpAction action)
        {
            switch (action)
            {
                case CareEventFollowUpAction.FeedMilk:
                    return "우유 챙기러 가기";
                case CareEventFollowUpAction.Cook:
                    return "요리하러 가기";
                case CareEventFollowUpAction.Clean:
                    return "청소하러 가기";
                case CareEventFollowUpAction.Rest:
                    return "쉬게 하러 가기";
                case CareEventFollowUpAction.Play:
                    return "함께 놀러 가기";
                case CareEventFollowUpAction.OpenCollection:
                    return "도감 보러 가기";
                default:
                    return string.Empty;
            }
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomActionBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
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

        private void EnsureCanvasGroup()
        {
            if (overlayRoot == null)
            {
                overlayCanvasGroup = null;
                return;
            }

            overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
            {
                overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            }

            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;
        }

        private void BeginPresentation()
        {
            EnsureCanvasGroup();
            if (AccessibilityRuntime.ReducedMotion)
            {
                if (overlayCanvasGroup != null)
                {
                    overlayCanvasGroup.alpha = 1f;
                }

                if (cardTransform != null)
                {
                    cardTransform.localScale = cardRestingScale;
                }

                presentationAnimating = false;
                return;
            }

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
            }

            if (cardTransform != null)
            {
                cardTransform.localScale = cardRestingScale * PresentationStartScale;
            }

            presentationStartedAt = Time.realtimeSinceStartup;
            presentationAnimating = true;
        }

        private void UpdatePresentation()
        {
            if (!presentationAnimating || !IsBlockingGameplay)
            {
                return;
            }

            var elapsed = Time.realtimeSinceStartup - presentationStartedAt;
            var normalized = Mathf.Clamp01(elapsed / PresentationDurationSeconds);
            var eased = 1f - Mathf.Pow(1f - normalized, 3f);
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = eased;
            }

            if (cardTransform != null)
            {
                cardTransform.localScale = cardRestingScale
                    * Mathf.Lerp(PresentationStartScale, 1f, eased);
            }

            if (normalized >= 1f)
            {
                presentationAnimating = false;
            }
        }

        private void ResetPresentation()
        {
            presentationAnimating = false;
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 1f;
            }

            if (cardTransform != null)
            {
                cardTransform.localScale = cardRestingScale;
            }
        }

        private void EnsureChoiceButtons()
        {
            if (confirmButton == null || cardTransform == null)
            {
                return;
            }

            firstChoiceButton = FindOrCreateChoiceButton(
                FirstChoiceButtonName,
                new Vector2(48f, -350f));
            secondChoiceButton = FindOrCreateChoiceButton(
                SecondChoiceButtonName,
                new Vector2(348f, -350f));
            followUpButton = FindOrCreateChoiceButton(
                FollowUpButtonName,
                new Vector2(48f, -350f));
        }

        private Button FindOrCreateChoiceButton(string buttonName, Vector2 anchoredPosition)
        {
            var existing = cardTransform.Find(buttonName);
            var button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
            {
                button = Instantiate(confirmButton, cardTransform, false);
                button.name = buttonName;
                button.onClick.RemoveAllListeners();
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = new Vector2(284f, 52f);
            }

            ConfigureChoiceButtonLabel(button, string.Empty);
            SetButtonVisible(button, false);
            return button;
        }

        private static void ConfigureChoiceButtonLabel(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                return;
            }

            label.text = value ?? string.Empty;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 18;
            AccessibilityRuntime.ApplyCurrent(label);
        }

        private void BindButtons()
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
            if (firstChoiceButton != null)
            {
                firstChoiceButton.onClick.RemoveListener(ChooseFirst);
                firstChoiceButton.onClick.AddListener(ChooseFirst);
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.onClick.RemoveListener(ChooseSecond);
                secondChoiceButton.onClick.AddListener(ChooseSecond);
            }

            if (followUpButton != null)
            {
                followUpButton.onClick.RemoveListener(HandleFollowUpRequested);
                followUpButton.onClick.AddListener(HandleFollowUpRequested);
            }
        }

        private void UnbindButtons()
        {
            confirmButton?.onClick.RemoveListener(Confirm);
            firstChoiceButton?.onClick.RemoveListener(ChooseFirst);
            secondChoiceButton?.onClick.RemoveListener(ChooseSecond);
            followUpButton?.onClick.RemoveListener(HandleFollowUpRequested);
        }

        private void ResetCardState()
        {
            displayedDefinition = null;
            pendingFollowUpAction = CareEventFollowUpAction.None;
            cardStage = CardStage.AutomaticEvent;
            SetButtonVisible(firstChoiceButton, false);
            SetButtonVisible(secondChoiceButton, false);
            SetButtonVisible(followUpButton, false);
            SetButtonVisible(confirmButton, true);
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
}
