using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class NewGameSetupController : MonoBehaviour
    {
        public const string OverlayObjectName = "New Game Setup Overlay";
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private GameObject eggStepRoot;
        [SerializeField] private GameObject firstMilkStepRoot;
        [SerializeField] private Text progressText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button[] eggButtons = Array.Empty<Button>();
        [SerializeField] private Text[] eggButtonLabels = Array.Empty<Text>();
        [SerializeField] private Button[] firstMilkButtons = Array.Empty<Button>();
        [SerializeField] private Text[] firstMilkButtonLabels = Array.Empty<Text>();
        [SerializeField] private Button backButton;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject skipConfirmationRoot;
        [SerializeField] private Button continueSetupButton;
        [SerializeField] private Button confirmSkipButton;

        private Func<NewGameSetupSaveData> stateProvider;
        private Action<NewGameSetupSaveData> persistCommand;
        private Action<NewGameSetupSaveData> completedCommand;
        private UnityAction[] eggButtonActions = Array.Empty<UnityAction>();
        private UnityAction[] firstMilkButtonActions = Array.Empty<UnityAction>();
        private Text advanceButtonLabel;
        private bool configured;
        private TopMenuController topMenuController;
        private BottomActionBarController bottomActionBarController;
        private DevPanelController devPanelController;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;
        private GameManager boundManager;

        public bool IsBlockingGameplay => overlayRoot != null && overlayRoot.activeInHierarchy;

        public void Configure(
            GameObject root,
            GameObject eggSelectionRoot,
            GameObject firstMilkSelectionRoot,
            Text progressLabel,
            Text titleLabel,
            Text descriptionLabel,
            Text currentSelectionLabel,
            Text feedbackLabel,
            Button[] eggOptionButtons,
            Text[] eggOptionLabels,
            Button[] firstMilkOptionButtons,
            Text[] firstMilkOptionLabels,
            Button previousButton,
            Button primaryButton,
            Button setupSkipButton,
            GameObject setupSkipConfirmationRoot,
            Button keepSettingUpButton,
            Button setupConfirmSkipButton,
            Func<NewGameSetupSaveData> getState,
            Action<NewGameSetupSaveData> persistState,
            Action<NewGameSetupSaveData> onCompleted = null,
            TopMenuController menuController = null,
            BottomActionBarController actionBarController = null,
            DevPanelController developerPanelController = null)
        {
            UnbindControls();

            overlayRoot = root;
            eggStepRoot = eggSelectionRoot;
            firstMilkStepRoot = firstMilkSelectionRoot;
            progressText = progressLabel;
            titleText = titleLabel;
            bodyText = descriptionLabel;
            selectionText = currentSelectionLabel;
            statusText = feedbackLabel;
            eggButtons = eggOptionButtons ?? Array.Empty<Button>();
            eggButtonLabels = eggOptionLabels ?? Array.Empty<Text>();
            firstMilkButtons = firstMilkOptionButtons ?? Array.Empty<Button>();
            firstMilkButtonLabels = firstMilkOptionLabels ?? Array.Empty<Text>();
            backButton = previousButton;
            advanceButton = primaryButton;
            skipButton = setupSkipButton;
            skipConfirmationRoot = setupSkipConfirmationRoot;
            continueSetupButton = keepSettingUpButton;
            confirmSkipButton = setupConfirmSkipButton;
            stateProvider = getState;
            persistCommand = persistState;
            completedCommand = onCompleted;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            advanceButtonLabel = advanceButton != null
                ? advanceButton.GetComponentInChildren<Text>(true)
                : null;
            configured = overlayRoot != null && stateProvider != null;

            if (bodyText != null)
            {
                bodyText.alignment = TextAnchor.MiddleCenter;
            }

            EnsureBlockingCanvasGroup();
            BindControls();
            CloseSkipConfirmation();
            Refresh();
        }

        private void OnEnable()
        {
            if (!configured)
            {
                return;
            }

            BindControls();
            BindManager(GameManager.Instance);
            Refresh();
        }

        private void OnDisable()
        {
            UnbindControls();
            BindManager(null);
            CloseSkipConfirmation();
            RestoreGameplayControls();
        }

        private void Update()
        {
            if (Application.isPlaying && boundManager != GameManager.Instance)
            {
                BindManager(GameManager.Instance);
                Refresh();
            }

            if (skipConfirmationRoot != null
                && skipConfirmationRoot.activeSelf
                && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                CancelSkip();
            }
        }

        public void Refresh()
        {
            if (!configured)
            {
                SetActive(overlayRoot, false);
                return;
            }

            var state = stateProvider?.Invoke();
            if (state == null)
            {
                SetActive(overlayRoot, false);
                return;
            }

            if (state.EnsureRuntimeDefaults())
            {
                persistCommand?.Invoke(state);
            }

            RefreshFromState(state);
        }

        public void SelectEgg(string eggId)
        {
            var state = GetState();
            if (state == null)
            {
                return;
            }

            if (!NewGameSetupSystem.TrySelectEgg(state, eggId, out var errorMessage))
            {
                SetStatus(errorMessage);
                return;
            }

            Commit(state, false);
        }

        public void SelectFirstMilk(string milkId)
        {
            var state = GetState();
            if (state == null)
            {
                return;
            }

            if (!NewGameSetupSystem.TrySelectFirstMilk(state, milkId, out var errorMessage))
            {
                SetStatus(errorMessage);
                return;
            }

            Commit(state, false);
        }

        public void Advance()
        {
            var state = GetState();
            if (state == null)
            {
                return;
            }

            if (!NewGameSetupSystem.TryAdvance(state, out var errorMessage))
            {
                SetStatus(errorMessage);
                return;
            }

            Commit(state, state.completed);
        }

        public void GoBack()
        {
            var state = GetState();
            if (state == null)
            {
                return;
            }

            if (!NewGameSetupSystem.TryGoBack(state, out var errorMessage))
            {
                SetStatus(errorMessage);
                return;
            }

            Commit(state, false);
        }

        public void RequestSkip()
        {
            var state = GetState();
            if (state == null || state.completed)
            {
                return;
            }

            if (skipConfirmationRoot == null)
            {
                SetStatus("건너뛰기 확인창을 열 수 없어요.");
                return;
            }

            SetActive(skipConfirmationRoot, true);
            skipConfirmationRoot.transform.SetAsLastSibling();
            SetMainControlsInteractable(false);
            SetInteractable(continueSetupButton, true);
            SetInteractable(confirmSkipButton, true);
            if (EventSystem.current != null && continueSetupButton != null)
            {
                EventSystem.current.SetSelectedGameObject(continueSetupButton.gameObject);
            }
        }

        public void CancelSkip()
        {
            CloseSkipConfirmation();
            Refresh();
        }

        public void ConfirmSkip()
        {
            var state = GetState();
            if (state == null)
            {
                CloseSkipConfirmation();
                return;
            }

            if (!NewGameSetupSystem.TrySkip(state, out var errorMessage))
            {
                CloseSkipConfirmation();
                SetStatus(errorMessage);
                RefreshFromState(state);
                return;
            }

            CloseSkipConfirmation();
            Commit(state, true);
        }

        private NewGameSetupSaveData GetState()
        {
            if (!configured)
            {
                return null;
            }

            var state = stateProvider?.Invoke();
            if (state == null)
            {
                SetStatus("새 게임 설정 정보를 불러오지 못했어요.");
                return null;
            }

            state.EnsureRuntimeDefaults();
            return state;
        }

        private void Commit(NewGameSetupSaveData state, bool notifyCompleted)
        {
            SetStatus(string.Empty);
            persistCommand?.Invoke(state);
            RefreshFromState(state);
            if (notifyCompleted)
            {
                completedCommand?.Invoke(state);
            }
        }

        private void RefreshFromState(NewGameSetupSaveData state)
        {
            CloseSkipConfirmation();
            if (state == null || state.completed)
            {
                SetActive(overlayRoot, false);
                RestoreGameplayControls();
                return;
            }

            SetActive(overlayRoot, true);
            SuspendGameplayControls();
            overlayRoot.transform.SetAsLastSibling();
            var isEggStep = state.currentStep == NewGameSetupStep.EggSelection;
            SetActive(eggStepRoot, isEggStep);
            SetActive(firstMilkStepRoot, !isEggStep);

            SetText(progressText, isEggStep ? "새 게임 설정 · 1/2" : "새 게임 설정 · 2/2");
            SetText(titleText, isEggStep ? "함께할 알을 골라 주세요" : "첫 우유를 골라 주세요");
            SetText(
                bodyText,
                isEggStep
                    ? "다섯 알은 서로 다른 초기 성향의 바탕을 가지고 있어요."
                    : "알과 첫 우유의 조합으로 치즈타마의 초기 성향이 정해져요.");
            SetText(selectionText, BuildSelectionSummary(state, isEggStep));
            SetText(advanceButtonLabel, isEggStep ? "다음" : "밀크룸 입장");

            RefreshChoiceButtons(
                eggButtons,
                eggButtonLabels,
                NewGameSetupCatalog.EggChoices,
                state.selectedEggId,
                isEggStep);
            RefreshChoiceButtons(
                firstMilkButtons,
                firstMilkButtonLabels,
                NewGameSetupCatalog.FirstMilkChoices,
                state.selectedFirstMilkId,
                !isEggStep);

            SetActive(backButton != null ? backButton.gameObject : null, !isEggStep);
            SetInteractable(backButton, !isEggStep);
            SetInteractable(advanceButton, NewGameSetupSystem.CanAdvance(state));
            SetInteractable(skipButton, true);
        }

        private static string BuildSelectionSummary(
            NewGameSetupSaveData state,
            bool isEggStep)
        {
            var eggName = NewGameSetupCatalog.TryGetEgg(state.selectedEggId, out var egg)
                ? egg.DisplayName
                : "아직 선택하지 않음";
            if (isEggStep)
            {
                return $"선택한 알: {eggName}";
            }

            var milkName = NewGameSetupCatalog.TryGetFirstMilk(
                state.selectedFirstMilkId,
                out var milk)
                ? milk.DisplayName
                : "아직 선택하지 않음";
            return $"선택한 알: {eggName}\n첫 우유: {milkName}";
        }

        private static void RefreshChoiceButtons(
            Button[] buttons,
            Text[] labels,
            System.Collections.Generic.IReadOnlyList<NewGameSetupChoiceDefinition> choices,
            string selectedId,
            bool stepActive)
        {
            for (var index = 0; index < buttons.Length; index++)
            {
                var hasChoice = index < choices.Count;
                var button = buttons[index];
                SetInteractable(button, stepActive && hasChoice);

                if (index >= labels.Length || labels[index] == null)
                {
                    continue;
                }

                if (!hasChoice)
                {
                    labels[index].text = string.Empty;
                    continue;
                }

                var choice = choices[index];
                labels[index].text = string.Equals(
                    choice.Id,
                    selectedId,
                    StringComparison.Ordinal)
                    ? $"✓ {choice.DisplayName}"
                    : choice.DisplayName;
            }
        }

        private void EnsureBlockingCanvasGroup()
        {
            if (overlayRoot == null)
            {
                return;
            }

            var canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void BindManager(GameManager manager)
        {
            if (boundManager == manager) return;
            if (boundManager != null) boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
            boundManager = manager;
            if (boundManager != null) boundManager.SaveDataReplaced += HandleSaveDataReplaced;
        }

        private void HandleSaveDataReplaced()
        {
            Refresh();
        }

        private void SuspendGameplayControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            actionBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (bottomActionBarController != null) bottomActionBarController.enabled = false;
            if (devPanelController != null) devPanelController.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreGameplayControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = topMenuWasEnabled;
            if (bottomActionBarController != null) bottomActionBarController.enabled = actionBarWasEnabled;
            if (devPanelController != null) devPanelController.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }

        private void BindControls()
        {
            UnbindControls();
            eggButtonActions = BindChoiceButtons(
                eggButtons,
                NewGameSetupCatalog.EggChoices.Count,
                HandleEggButton);
            firstMilkButtonActions = BindChoiceButtons(
                firstMilkButtons,
                NewGameSetupCatalog.FirstMilkChoices.Count,
                HandleFirstMilkButton);

            backButton?.onClick.AddListener(GoBack);
            advanceButton?.onClick.AddListener(Advance);
            skipButton?.onClick.AddListener(RequestSkip);
            continueSetupButton?.onClick.AddListener(CancelSkip);
            confirmSkipButton?.onClick.AddListener(ConfirmSkip);
        }

        private void UnbindControls()
        {
            UnbindChoiceButtons(eggButtons, eggButtonActions);
            UnbindChoiceButtons(firstMilkButtons, firstMilkButtonActions);
            eggButtonActions = Array.Empty<UnityAction>();
            firstMilkButtonActions = Array.Empty<UnityAction>();

            backButton?.onClick.RemoveListener(GoBack);
            advanceButton?.onClick.RemoveListener(Advance);
            skipButton?.onClick.RemoveListener(RequestSkip);
            continueSetupButton?.onClick.RemoveListener(CancelSkip);
            confirmSkipButton?.onClick.RemoveListener(ConfirmSkip);
        }

        private static UnityAction[] BindChoiceButtons(
            Button[] buttons,
            int choiceCount,
            Action<int> handler)
        {
            var actions = new UnityAction[buttons.Length];
            var bindCount = Math.Min(buttons.Length, choiceCount);
            for (var index = 0; index < bindCount; index++)
            {
                var capturedIndex = index;
                UnityAction action = () => handler(capturedIndex);
                actions[index] = action;
                buttons[index]?.onClick.AddListener(action);
            }

            return actions;
        }

        private static void UnbindChoiceButtons(Button[] buttons, UnityAction[] actions)
        {
            var count = Math.Min(buttons?.Length ?? 0, actions?.Length ?? 0);
            for (var index = 0; index < count; index++)
            {
                if (buttons[index] != null && actions[index] != null)
                {
                    buttons[index].onClick.RemoveListener(actions[index]);
                }
            }
        }

        private void HandleEggButton(int index)
        {
            if (index >= 0 && index < NewGameSetupCatalog.EggChoices.Count)
            {
                SelectEgg(NewGameSetupCatalog.EggChoices[index].Id);
            }
        }

        private void HandleFirstMilkButton(int index)
        {
            if (index >= 0 && index < NewGameSetupCatalog.FirstMilkChoices.Count)
            {
                SelectFirstMilk(NewGameSetupCatalog.FirstMilkChoices[index].Id);
            }
        }

        private void SetMainControlsInteractable(bool interactable)
        {
            SetButtonsInteractable(eggButtons, interactable);
            SetButtonsInteractable(firstMilkButtons, interactable);
            SetInteractable(backButton, interactable);
            SetInteractable(advanceButton, interactable);
            SetInteractable(skipButton, interactable);
        }

        private void CloseSkipConfirmation()
        {
            SetActive(skipConfirmationRoot, false);
        }

        private void SetStatus(string message)
        {
            SetText(statusText, message);
        }

        private static void SetButtonsInteractable(Button[] buttons, bool interactable)
        {
            if (buttons == null)
            {
                return;
            }

            for (var index = 0; index < buttons.Length; index++)
            {
                SetInteractable(buttons[index], interactable);
            }
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
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
