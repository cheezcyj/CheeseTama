using System;
using CheeseTama.Gameplay.HiddenRecipes;
using CheeseTama.Save;
using CheeseTama.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class FantasyPowderHiddenRecipePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text powderQuantityText;
        [SerializeField] private Text attemptCountText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text[] recipeNameTexts;
        [SerializeField] private Text[] recipeStateTexts;
        [SerializeField] private Button[] recipeButtons;
        [SerializeField] private Button attemptButton;
        [SerializeField] private Button closeButton;

        private Func<FantasyPowderPanelSnapshot> snapshotProvider;
        private Func<string, FantasyPowderAttemptResult> attemptCommand;
        private Action closeAction;
        private TopMenuController topMenuController;
        private BottomActionBarController bottomActionBarController;
        private DevPanelController devPanelController;
        private Button openButton;
        private GameManager boundManager;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;
        private FantasyPowderPanelSnapshot snapshot =
            FantasyPowderPanelSnapshot.CreateHidden();
        private string selectedRecipeId = string.Empty;
        private string statusMessage = string.Empty;

        public bool IsFeatureVisible => snapshot != null && snapshot.visible;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public string SelectedRecipeId => selectedRecipeId;

        public void Configure(
            GameObject root,
            Text powderLabel,
            Text attemptLabel,
            Text hintLabel,
            Text detailLabel,
            Text statusLabel,
            Text[] recipeNameLabels,
            Text[] recipeStateLabels,
            Button[] selectionButtons,
            Button tryButton,
            Button panelCloseButton,
            Func<FantasyPowderPanelSnapshot> getSnapshot,
            Func<string, FantasyPowderAttemptResult> executeAttempt,
            Action onClosed = null,
            TopMenuController menuController = null,
            BottomActionBarController actionBarController = null,
            DevPanelController developerPanelController = null)
        {
            RestoreControls();
            panelRoot = root;
            powderQuantityText = powderLabel;
            attemptCountText = attemptLabel;
            hintText = hintLabel;
            detailText = detailLabel;
            statusText = statusLabel;
            recipeNameTexts = recipeNameLabels;
            recipeStateTexts = recipeStateLabels;
            recipeButtons = selectionButtons;
            attemptButton = tryButton;
            closeButton = panelCloseButton;
            snapshotProvider = getSnapshot;
            attemptCommand = executeAttempt;
            closeAction = onClosed;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;

            selectedRecipeId = string.Empty;
            statusMessage = string.Empty;
            BindButtons();
            Refresh();
            Close();
        }

        public void BindEntryButton(Button entryButton, GameManager manager)
        {
            UnbindManager();
            openButton = entryButton;
            boundManager = manager;
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(OpenFromEntry);
                openButton.onClick.AddListener(OpenFromEntry);
            }

            BindManager();
            RefreshEntryVisibility();
        }

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            BindButtons();
            BindManager();
            Refresh();
            RefreshEntryVisibility();
        }

        public bool Open()
        {
            BindButtons();
            statusMessage = string.Empty;
            Refresh();
            if (!IsFeatureVisible)
            {
                ClearLockedPresentation();
                return false;
            }

            SetActive(panelRoot, true);
            if (panelRoot != null)
            {
                panelRoot.transform.SetAsLastSibling();
            }

            Render();
            SuspendControls();
            EventSystem.current?.SetSelectedGameObject(
                attemptButton != null ? attemptButton.gameObject : closeButton?.gameObject);
            return true;
        }

        public void Close()
        {
            SetActive(panelRoot, false);
            RestoreControls();
        }

        private void OnDisable()
        {
            UnbindManager();
            RestoreControls();
        }

        private void OnDestroy()
        {
            UnbindManager();
            openButton?.onClick.RemoveListener(OpenFromEntry);
        }

        private void Update()
        {
            if (IsOpen && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                HandleCloseClicked();
            }
        }

        public void Refresh()
        {
            snapshot = snapshotProvider?.Invoke()
                ?? FantasyPowderPanelSnapshot.CreateHidden();
            if (!snapshot.visible)
            {
                ClearLockedPresentation();
                return;
            }

            if (snapshot.FindRecipe(selectedRecipeId) == null)
            {
                selectedRecipeId = snapshot.RecipeEntries.Count > 0
                    ? snapshot.RecipeEntries[0].recipeId
                    : string.Empty;
            }

            Render();
        }

        public void SelectRecipe(string recipeId)
        {
            if (snapshot == null || !snapshot.visible)
            {
                ClearLockedPresentation();
                return;
            }

            var entry = snapshot.FindRecipe(recipeId);
            if (entry == null)
            {
                return;
            }

            selectedRecipeId = entry.recipeId;
            statusMessage = string.Empty;
            Render();
        }

        public void AttemptSelectedRecipe()
        {
            if (snapshot == null || !snapshot.visible)
            {
                ClearLockedPresentation();
                return;
            }

            if (snapshot.FindRecipe(selectedRecipeId) == null)
            {
                statusMessage = "시도할 조합을 선택해 주세요.";
                Render();
                return;
            }

            if (attemptCommand == null)
            {
                statusMessage = "조합 저장 기능이 연결되지 않았어요.";
                Render();
                return;
            }

            var result = attemptCommand(selectedRecipeId);
            statusMessage = FormatResult(result);
            Refresh();
        }

        private void BindButtons()
        {
            if (recipeButtons != null)
            {
                for (var index = 0; index < recipeButtons.Length; index += 1)
                {
                    var capturedIndex = index;
                    BindButton(
                        recipeButtons[index],
                        () => SelectRecipeAt(capturedIndex));
                }
            }

            BindButton(attemptButton, AttemptSelectedRecipe);
            BindButton(closeButton, HandleCloseClicked);
        }

        private void SelectRecipeAt(int index)
        {
            if (snapshot == null
                || index < 0
                || index >= snapshot.RecipeEntries.Count)
            {
                return;
            }

            SelectRecipe(snapshot.RecipeEntries[index].recipeId);
        }

        private void Render()
        {
            if (snapshot == null || !snapshot.visible)
            {
                ClearLockedPresentation();
                return;
            }

            SetText(powderQuantityText, $"보유 수량 {snapshot.powderQuantity}");
            SetText(
                attemptCountText,
                $"시도 {snapshot.attemptCount}회 · 단서 {snapshot.pityHintLevel}/{FantasyPowderSaveData.MaximumPityHintLevel}");
            SetText(
                hintText,
                string.IsNullOrWhiteSpace(snapshot.hintText)
                    ? "아직 분명한 단서는 없어요."
                    : snapshot.hintText);

            var slotCount = Math.Max(
                recipeButtons?.Length ?? 0,
                Math.Max(
                    recipeNameTexts?.Length ?? 0,
                    recipeStateTexts?.Length ?? 0));
            for (var index = 0; index < slotCount; index += 1)
            {
                var hasEntry = index < snapshot.RecipeEntries.Count;
                var entry = hasEntry ? snapshot.RecipeEntries[index] : null;
                SetText(Get(recipeNameTexts, index), entry?.displayName ?? string.Empty);
                SetText(
                    Get(recipeStateTexts, index),
                    entry == null ? string.Empty : entry.discovered ? "발견" : "미발견");
                SetButtonVisible(Get(recipeButtons, index), hasEntry);
                SetInteractable(Get(recipeButtons, index), hasEntry);
                SetSelected(
                    Get(recipeButtons, index),
                    entry != null
                        && string.Equals(
                            entry.recipeId,
                            selectedRecipeId,
                            StringComparison.Ordinal));
            }

            var selected = snapshot.FindRecipe(selectedRecipeId);
            SetText(
                detailText,
                selected == null
                    ? "표시할 조합이 없어요."
                    : $"<b>{selected.displayName}</b>\n{selected.description}");
            SetText(statusText, statusMessage);
            SetInteractable(
                attemptButton,
                snapshot.canAttempt
                    && selected != null
                    && attemptCommand != null);
        }

        private void ClearLockedPresentation()
        {
            snapshot = FantasyPowderPanelSnapshot.CreateHidden();
            selectedRecipeId = string.Empty;
            statusMessage = string.Empty;
            SetText(powderQuantityText, string.Empty);
            SetText(attemptCountText, string.Empty);
            SetText(hintText, string.Empty);
            SetText(detailText, string.Empty);
            SetText(statusText, string.Empty);

            var slotCount = Math.Max(
                recipeButtons?.Length ?? 0,
                Math.Max(
                    recipeNameTexts?.Length ?? 0,
                    recipeStateTexts?.Length ?? 0));
            for (var index = 0; index < slotCount; index += 1)
            {
                SetText(Get(recipeNameTexts, index), string.Empty);
                SetText(Get(recipeStateTexts, index), string.Empty);
                SetInteractable(Get(recipeButtons, index), false);
            }

            SetInteractable(attemptButton, false);
            SetActive(panelRoot, false);
            RestoreControls();
        }

        private void HandleCloseClicked()
        {
            Close();
            closeAction?.Invoke();
        }

        private void OpenFromEntry()
        {
            Open();
        }

        private void BindManager()
        {
            boundManager ??= GameManager.Instance;
            if (boundManager == null)
            {
                return;
            }

            boundManager.FantasyPowderChanged -= HandleAuthoritativeStateChanged;
            boundManager.SaveDataReplaced -= HandleAuthoritativeStateChanged;
            boundManager.FantasyPowderChanged += HandleAuthoritativeStateChanged;
            boundManager.SaveDataReplaced += HandleAuthoritativeStateChanged;
        }

        private void UnbindManager()
        {
            if (boundManager == null)
            {
                return;
            }

            boundManager.FantasyPowderChanged -= HandleAuthoritativeStateChanged;
            boundManager.SaveDataReplaced -= HandleAuthoritativeStateChanged;
        }

        private void HandleAuthoritativeStateChanged()
        {
            Refresh();
            RefreshEntryVisibility();
        }

        private void RefreshEntryVisibility()
        {
            if (openButton == null)
            {
                return;
            }

            var manager = boundManager ?? GameManager.Instance;
            var unlocks = manager?.CurrentSave?.unlocks;
            openButton.gameObject.SetActive(
                unlocks != null
                && unlocks.starMilkUnlocked
                && unlocks.fantasyPowderEnabled);
        }

        private static string FormatResult(FantasyPowderAttemptResult result)
        {
            if (result == null)
            {
                return "조합 요청을 처리하지 못했어요.";
            }

            if (result.applied)
            {
                return result.message;
            }

            switch (result.status)
            {
                case FantasyPowderAttemptStatus.AlreadyApplied:
                    return "이미 처리된 시도예요.";
                case FantasyPowderAttemptStatus.InvalidReceipt:
                    return "시도 기록을 만들지 못했어요.";
                case FantasyPowderAttemptStatus.UnknownRecipe:
                    return "선택한 조합을 찾지 못했어요.";
                case FantasyPowderAttemptStatus.InvalidRoll:
                    return "조합 결과값이 올바르지 않아요.";
                case FantasyPowderAttemptStatus.InsufficientPowder:
                    return "보유 수량이 부족해요.";
                case FantasyPowderAttemptStatus.RewardCapacityFull:
                    return "보관 공간과 재화 수용량을 먼저 확인해 주세요.";
                case FantasyPowderAttemptStatus.MissingState:
                case FantasyPowderAttemptStatus.MissingTargets:
                    return "저장 데이터 연결을 확인해 주세요.";
                default:
                    return string.Empty;
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static T Get<T>(T[] values, int index)
            where T : class
        {
            return values != null && index >= 0 && index < values.Length
                ? values[index]
                : null;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private static void SetSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = selected
                ? new Color(0.84f, 0.76f, 1f, 1f)
                : Color.white;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        private void SuspendControls()
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

        private void RestoreControls()
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

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
