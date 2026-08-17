using System;
using CheeseTama.Collections;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class FirstMeetingOnboardingController : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private Image dimImage;
        [SerializeField] private Text stepText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject skipConfirmationRoot;
        [SerializeField] private Button confirmSkipButton;
        [SerializeField] private Button continueTutorialButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button[] actionButtons;
        [SerializeField] private Button milkButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button cleanButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button decorateButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button devModeButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private DevPanelController devPanelController;
        [SerializeField] private GameObject devPanelRoot;
        [SerializeField] private GameObject settingsModal;
        [SerializeField] private MilkPanelController milkPanelController;
        [SerializeField] private CookingPanelController cookingPanelController;
        [SerializeField] private SnackPanelController snackPanelController;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;

        private readonly CollectionSystem collectionSystem = new CollectionSystem();
        private GameManager boundManager;
        private TopMenuController boundTopMenu;
        private bool configured;
        private bool controlsLocked;
        private bool[] actionButtonStates = Array.Empty<bool>();
        private bool collectionButtonState;
        private bool decorateButtonState;
        private bool settingsButtonState;
        private bool devModeButtonState;
        private bool topMenuEnabledState;
        private bool devPanelEnabledState;
        private bool devPanelActiveState;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeSelf;

        public void Configure(
            GameObject root,
            RectTransform onboardingCard,
            Image overlayImage,
            Text stepLabel,
            Text titleLabel,
            Text bodyLabel,
            Button nextButton,
            Button onboardingSkipButton,
            GameObject onboardingSkipConfirmationRoot,
            Button onboardingConfirmSkipButton,
            Button onboardingContinueButton,
            Button onboardingReplayButton,
            Button[] bottomActionButtons,
            Button openMilkButton,
            Button carePlayButton,
            Button careCleanButton,
            Button openCollectionButton,
            Button openDecorateButton,
            Button openSettingsButton,
            Button developerModeButton,
            TopMenuController menuController,
            GameObject settingsRoot,
            MilkPanelController milkPanel,
            CookingPanelController cookingPanel,
            SnackPanelController snackPanel,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            RestoreControls();
            UnbindControls();
            UnbindRuntimeEvents();

            overlayRoot = root;
            cardRect = onboardingCard;
            dimImage = overlayImage;
            stepText = stepLabel;
            titleText = titleLabel;
            bodyText = bodyLabel;
            if (bodyText != null)
            {
                bodyText.alignment = TextAnchor.MiddleCenter;
            }

            primaryButton = nextButton;
            skipButton = onboardingSkipButton;
            skipConfirmationRoot = onboardingSkipConfirmationRoot;
            confirmSkipButton = onboardingConfirmSkipButton;
            continueTutorialButton = onboardingContinueButton;
            replayButton = onboardingReplayButton;
            actionButtons = bottomActionButtons;
            milkButton = openMilkButton;
            playButton = carePlayButton;
            cleanButton = careCleanButton;
            collectionButton = openCollectionButton;
            decorateButton = openDecorateButton;
            settingsButton = openSettingsButton;
            devModeButton = developerModeButton;
            topMenuController = menuController;
            devPanelController = GetComponent<DevPanelController>();
            devPanelRoot = transform.Find("Dev Panel")?.gameObject;
            settingsModal = settingsRoot;
            milkPanelController = milkPanel;
            cookingPanelController = cookingPanel;
            snackPanelController = snackPanel;
            milkroomUi = uiController;
            visualController = cheeseTamaVisual;
            configured = true;

            BindControls();
            CloseSkipConfirmation(false);
            if (!Application.isPlaying)
            {
                SetActive(overlayRoot, false);
                return;
            }

            BindRuntimeEvents();
            RefreshFromSave();
        }

        private void OnEnable()
        {
            if (!configured || !Application.isPlaying)
            {
                return;
            }

            BindControls();
            BindRuntimeEvents();
            RefreshFromSave();
        }

        private void OnDisable()
        {
            UnbindControls();
            UnbindRuntimeEvents();
            CloseSkipConfirmation(false);
            if (Application.isPlaying)
            {
                RestoreControls();
            }
        }

        private void Update()
        {
            if (skipConfirmationRoot != null
                && skipConfirmationRoot.activeSelf
                && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                CancelSkip();
            }
        }

        private void BindControls()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveListener(HandlePrimary);
                primaryButton.onClick.AddListener(HandlePrimary);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(RequestSkip);
                skipButton.onClick.AddListener(RequestSkip);
            }

            if (confirmSkipButton != null)
            {
                confirmSkipButton.onClick.RemoveListener(ConfirmSkip);
                confirmSkipButton.onClick.AddListener(ConfirmSkip);
            }

            if (continueTutorialButton != null)
            {
                continueTutorialButton.onClick.RemoveListener(CancelSkip);
                continueTutorialButton.onClick.AddListener(CancelSkip);
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(ReplayFromBeginning);
                replayButton.onClick.AddListener(ReplayFromBeginning);
            }

        }

        private void UnbindControls()
        {
            primaryButton?.onClick.RemoveListener(HandlePrimary);
            skipButton?.onClick.RemoveListener(RequestSkip);
            confirmSkipButton?.onClick.RemoveListener(ConfirmSkip);
            continueTutorialButton?.onClick.RemoveListener(CancelSkip);
            replayButton?.onClick.RemoveListener(ReplayFromBeginning);
        }

        private void BindRuntimeEvents()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (boundManager != manager)
            {
                if (boundManager != null)
                {
                    boundManager.CareActionRegistered -= HandleCareActionRegistered;
                    boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
                }

                boundManager = manager;
                if (boundManager != null)
                {
                    boundManager.CareActionRegistered += HandleCareActionRegistered;
                    boundManager.SaveDataReplaced += HandleSaveDataReplaced;
                }
            }

            if (boundTopMenu == topMenuController)
            {
                return;
            }

            if (boundTopMenu != null)
            {
                boundTopMenu.CollectionOpening -= HandleCollectionOpening;
            }

            boundTopMenu = topMenuController;
            if (boundTopMenu != null)
            {
                boundTopMenu.CollectionOpening += HandleCollectionOpening;
            }
        }

        private void UnbindRuntimeEvents()
        {
            if (boundManager != null)
            {
                boundManager.CareActionRegistered -= HandleCareActionRegistered;
                boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
                boundManager = null;
            }

            if (boundTopMenu != null)
            {
                boundTopMenu.CollectionOpening -= HandleCollectionOpening;
                boundTopMenu = null;
            }
        }

        private void HandlePrimary()
        {
            if (IsSkipConfirmationOpen())
            {
                return;
            }

            var onboarding = GetOnboarding();
            if (onboarding == null)
            {
                return;
            }

            if (onboarding.currentStep == FirstMeetingOnboardingStep.Welcome)
            {
                ApplySignal(FirstMeetingOnboardingSignal.Continue);
            }
        }

        private void RequestSkip()
        {
            if (skipConfirmationRoot == null || skipConfirmationRoot.activeSelf)
            {
                return;
            }

            SetActive(skipConfirmationRoot, true);
            skipConfirmationRoot.transform.SetAsLastSibling();
            SetInteractable(primaryButton, false);
            SetInteractable(skipButton, false);
            SetButtonsInteractable(actionButtons, false);
            SetInteractable(collectionButton, false);
            if (EventSystem.current != null && continueTutorialButton != null)
            {
                EventSystem.current.SetSelectedGameObject(continueTutorialButton.gameObject);
            }
        }

        private void ConfirmSkip()
        {
            CloseSkipConfirmation(true);
            ApplySignal(FirstMeetingOnboardingSignal.Skip);
        }

        private void CancelSkip()
        {
            CloseSkipConfirmation(true);
        }

        private void CloseSkipConfirmation(bool restoreStepControls)
        {
            SetActive(skipConfirmationRoot, false);
            SetInteractable(primaryButton, true);
            SetInteractable(skipButton, true);

            if (!restoreStepControls)
            {
                return;
            }

            var onboarding = GetOnboarding();
            if (onboarding != null && !onboarding.completed)
            {
                LockControlsFor(onboarding.currentStep);
            }
        }

        public void ReplayFromBeginning()
        {
            CloseSkipConfirmation(false);
            BindRuntimeEvents();
            if (boundManager == null
                || !FirstMeetingOnboardingSystem.StartReplay(boundManager.CurrentSave))
            {
                return;
            }

            boundManager.SaveGame();
            SetActive(settingsModal, false);
            RefreshFromSave();
        }

        private void HandleCareActionRegistered(string actionId)
        {
            if (IsSkipConfirmationOpen())
            {
                return;
            }

            var onboarding = GetOnboarding();
            if (onboarding == null || onboarding.completed)
            {
                return;
            }

            if (onboarding.currentStep == FirstMeetingOnboardingStep.FeedMilk
                && IsMilkFeedAction(actionId))
            {
                ApplySignal(FirstMeetingOnboardingSignal.MilkFeedSucceeded);
            }
            else if (onboarding.currentStep == FirstMeetingOnboardingStep.Care
                && (actionId == "play" || actionId == "clean"))
            {
                ApplySignal(FirstMeetingOnboardingSignal.CareSucceeded);
            }
        }

        private void HandleCollectionOpening()
        {
            if (IsSkipConfirmationOpen())
            {
                return;
            }

            var onboarding = GetOnboarding();
            if (onboarding != null
                && !onboarding.completed
                && onboarding.currentStep == FirstMeetingOnboardingStep.Collection)
            {
                ApplySignal(FirstMeetingOnboardingSignal.CollectionOpened);
            }
        }

        private void HandleSaveDataReplaced()
        {
            CloseSkipConfirmation(false);
            RefreshFromSave();
        }

        private void ApplySignal(FirstMeetingOnboardingSignal signal)
        {
            BindRuntimeEvents();
            if (boundManager == null || boundManager.CurrentSave == null)
            {
                return;
            }

            if (!FirstMeetingOnboardingSystem.TryApply(
                    boundManager.CurrentSave,
                    signal,
                    out var errorMessage))
            {
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    milkroomUi?.ShowMessage(errorMessage);
                }

                return;
            }

            boundManager.SaveGame();
            milkroomUi?.Bind(boundManager.CurrentSave);
            visualController?.Bind(boundManager.CurrentTama);
            RefreshFromSave();
        }

        private bool IsSkipConfirmationOpen()
        {
            return skipConfirmationRoot != null && skipConfirmationRoot.activeSelf;
        }

        private void RefreshFromSave()
        {
            if (!configured || !Application.isPlaying)
            {
                return;
            }

            BindRuntimeEvents();
            var setup = boundManager?.CurrentSave?.newGameSetup;
            if (setup != null && !setup.completed)
            {
                CloseSkipConfirmation(false);
                SetActive(overlayRoot, false);
                RestoreControls();
                return;
            }

            var onboarding = GetOnboarding();
            if (onboarding == null || onboarding.completed)
            {
                CloseSkipConfirmation(false);
                SetActive(overlayRoot, false);
                RestoreControls();
                return;
            }

            CloseCompetingPanels();
            if (onboarding.currentStep == FirstMeetingOnboardingStep.Collection)
            {
                EnsureFirstCollectionReward(onboarding);
            }

            SetActive(overlayRoot, true);
            overlayRoot.transform.SetAsLastSibling();
            LockControlsFor(onboarding.currentStep);
            RefreshPresentation(onboarding);
        }

        public void Refresh()
        {
            RefreshFromSave();
        }

        private OnboardingSaveData GetOnboarding()
        {
            var saveData = boundManager != null ? boundManager.CurrentSave : null;
            if (saveData == null)
            {
                return null;
            }

            saveData.EnsureRuntimeDefaults();
            return saveData.onboarding;
        }

        private void EnsureFirstCollectionReward(OnboardingSaveData onboarding)
        {
            if (onboarding == null
                || onboarding.firstCollectionRewardGranted
                || boundManager == null
                || boundManager.CurrentSave == null)
            {
                return;
            }

            boundManager.RegisterMilkDiscovery(MilkCatalog.BasicMilkId);
            var claimedNow = boundManager.TryClaimCollectionFragmentReward(
                CollectionRecordCategory.Milk,
                MilkCatalog.BasicMilkId);
            var isClaimed = claimedNow || collectionSystem.IsFragmentRewardClaimed(
                boundManager.CurrentSave.collections,
                CollectionRecordCategory.Milk,
                MilkCatalog.BasicMilkId);
            if (!isClaimed)
            {
                return;
            }

            onboarding.firstCollectionRewardGranted = true;
            boundManager.SaveGame();
        }

        private void LockControlsFor(FirstMeetingOnboardingStep step)
        {
            if (!controlsLocked)
            {
                CaptureControlStates();
                controlsLocked = true;
            }

            SetButtonsInteractable(actionButtons, false);
            SetInteractable(collectionButton, false);
            SetInteractable(decorateButton, false);
            SetInteractable(settingsButton, false);
            SetInteractable(devModeButton, false);

            if (step == FirstMeetingOnboardingStep.FeedMilk)
            {
                SetInteractable(milkButton, true);
            }
            else if (step == FirstMeetingOnboardingStep.Care)
            {
                SetInteractable(playButton, true);
                SetInteractable(cleanButton, true);
            }
            else if (step == FirstMeetingOnboardingStep.Collection)
            {
                SetInteractable(collectionButton, true);
            }

            if (topMenuController != null)
            {
                topMenuController.enabled = false;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = false;
            }

            SetActive(devPanelRoot, false);
        }

        private void RestoreControls()
        {
            if (!controlsLocked)
            {
                return;
            }

            RestoreButtonStates(actionButtons, actionButtonStates);
            SetInteractable(collectionButton, collectionButtonState);
            SetInteractable(decorateButton, decorateButtonState);
            SetInteractable(settingsButton, settingsButtonState);
            SetInteractable(devModeButton, devModeButtonState);
            if (topMenuController != null)
            {
                topMenuController.enabled = topMenuEnabledState;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = devPanelEnabledState;
            }

            SetActive(devPanelRoot, devPanelActiveState);

            controlsLocked = false;
        }

        private void CaptureControlStates()
        {
            var buttonCount = actionButtons != null ? actionButtons.Length : 0;
            actionButtonStates = new bool[buttonCount];
            for (var i = 0; i < buttonCount; i += 1)
            {
                actionButtonStates[i] = actionButtons[i] != null && actionButtons[i].interactable;
            }

            collectionButtonState = collectionButton != null && collectionButton.interactable;
            decorateButtonState = decorateButton != null && decorateButton.interactable;
            settingsButtonState = settingsButton != null && settingsButton.interactable;
            devModeButtonState = devModeButton != null && devModeButton.interactable;
            topMenuEnabledState = topMenuController != null && topMenuController.enabled;
            devPanelEnabledState = devPanelController != null && devPanelController.enabled;
            devPanelActiveState = devPanelRoot != null && devPanelRoot.activeSelf;
        }

        private void CloseCompetingPanels()
        {
            milkPanelController?.Close();
            cookingPanelController?.Close();
            snackPanelController?.Close();
            SetActive(settingsModal, false);
        }

        private void RefreshPresentation(OnboardingSaveData onboarding)
        {
            var compact = onboarding.currentStep == FirstMeetingOnboardingStep.FeedMilk
                || onboarding.currentStep == FirstMeetingOnboardingStep.Care
                || onboarding.currentStep == FirstMeetingOnboardingStep.Collection;
            if (compact)
            {
                ApplyCompactLayout();
            }
            else
            {
                ApplyDialogLayout();
            }

            SetActive(titleText != null ? titleText.gameObject : null, !compact);
            SetActive(primaryButton != null ? primaryButton.gameObject : null, !compact);
            SetActive(skipButton != null ? skipButton.gameObject : null, true);

            switch (onboarding.currentStep)
            {
                case FirstMeetingOnboardingStep.Welcome:
                    SetText(stepText, "튜토리얼 · 1/4");
                    SetText(titleText, "밀크룸에 온 걸 환영해요");
                    SetText(bodyText, "작은 치즈 생명체가 당신을 기다리고 있어요.\n우유를 주고 돌보면서 밀크룸 생활을 시작해 볼까요?");
                    SetButtonLabel(primaryButton, "시작하기");
                    break;
                case FirstMeetingOnboardingStep.FeedMilk:
                    SetText(stepText, "튜토리얼 · 2/4");
                    SetText(bodyText, "아래의 ‘우유주기’를 눌러 우유를 한 번 먹여 주세요.");
                    break;
                case FirstMeetingOnboardingStep.Care:
                    SetText(stepText, "튜토리얼 · 3/4");
                    SetText(bodyText, "이제 ‘놀아주기’ 또는 ‘청소하기’로 마음을 표현해 주세요.");
                    break;
                case FirstMeetingOnboardingStep.Collection:
                    SetText(stepText, "튜토리얼 · 4/4");
                    SetText(bodyText, onboarding.firstCollectionRewardGranted
                        ? "첫 도감 조각을 받았어요! 위쪽 ‘도감’을 열어 첫 기록을 확인해 보세요."
                        : "위쪽 ‘도감’을 열어 방금 발견한 첫 기록을 확인해 보세요.");
                    break;
            }
        }

        private void ApplyDialogLayout()
        {
            if (dimImage != null)
            {
                dimImage.color = new Color(0.08f, 0.05f, 0.02f, 0.62f);
            }

            ConfigureCenteredCard(cardRect, new Vector2(760f, 380f));
            ConfigureTopLeft(stepText?.rectTransform, 48f, 34f, 664f, 28f);
            ConfigureTopLeft(titleText?.rectTransform, 48f, 82f, 664f, 52f);
            ConfigureTopLeft(bodyText?.rectTransform, 48f, 154f, 664f, 112f);
            ConfigureTopLeft(primaryButton != null ? primaryButton.GetComponent<RectTransform>() : null, 428f, 300f, 174f, 52f);
            ConfigureTopLeft(skipButton != null ? skipButton.GetComponent<RectTransform>() : null, 616f, 300f, 112f, 52f);
        }

        private void ApplyCompactLayout()
        {
            if (dimImage != null)
            {
                dimImage.color = new Color(0.08f, 0.05f, 0.02f, 0.12f);
            }

            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 1f);
                cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.pivot = new Vector2(0.5f, 1f);
                cardRect.anchoredPosition = new Vector2(0f, -108f);
                cardRect.sizeDelta = new Vector2(760f, 84f);
            }

            ConfigureTopLeft(stepText?.rectTransform, 20f, 16f, 64f, 52f);
            ConfigureTopLeft(bodyText?.rectTransform, 92f, 12f, 512f, 60f);
            ConfigureTopLeft(skipButton != null ? skipButton.GetComponent<RectTransform>() : null, 624f, 18f, 112f, 48f);
        }

        private static bool IsMilkFeedAction(string actionId)
        {
            return !string.IsNullOrWhiteSpace(actionId)
                && actionId.StartsWith("feed_", StringComparison.Ordinal)
                && actionId.EndsWith("milk", StringComparison.Ordinal);
        }

        private static void ConfigureCenteredCard(RectTransform rect, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void ConfigureTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetButtonsInteractable(Button[] buttons, bool interactable)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (var button in buttons)
            {
                SetInteractable(button, interactable);
            }
        }

        private static void RestoreButtonStates(Button[] buttons, bool[] states)
        {
            if (buttons == null || states == null)
            {
                return;
            }

            var count = Math.Min(buttons.Length, states.Length);
            for (var i = 0; i < count; i += 1)
            {
                SetInteractable(buttons[i], states[i]);
            }
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void SetButtonLabel(Button button, string value)
        {
            var label = button != null ? button.transform.Find("Label")?.GetComponent<Text>() : null;
            SetText(label, value);
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
