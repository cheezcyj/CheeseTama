using System;
using CheeseTama.Gameplay.Milk;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkBlendingPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text balanceText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text[] milkNameTexts;
        [SerializeField] private Text[] milkStateTexts;
        [SerializeField] private Button[] milkButtons;
        [SerializeField] private Text[] ingredientNameTexts;
        [SerializeField] private Text[] ingredientStateTexts;
        [SerializeField] private Button[] ingredientButtons;
        [SerializeField] private Button blendButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController actionBarController;
        [SerializeField] private DevPanelController developerPanelController;

        private Func<MilkBlendingPanelSnapshot> snapshotProvider;
        private Func<string, string, MilkBlendResult> blendCommand;
        private Action closeAction;
        private MilkBlendingPanelSnapshot snapshot =
            MilkBlendingPanelSnapshot.CreateDefault();
        private string selectedMilkId = string.Empty;
        private string selectedIngredientId = string.Empty;
        private string statusMessage = string.Empty;
        private bool controlsSuspended;
        private bool previousTopEnabled;
        private bool previousBottomEnabled;
        private bool previousDevEnabled;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public string SelectedMilkId => selectedMilkId;
        public string SelectedIngredientId => selectedIngredientId;

        public void Configure(
            GameObject root,
            Text balanceLabel,
            Text detailLabel,
            Text resultLabel,
            Text statusLabel,
            Text[] milkNameLabels,
            Text[] milkStateLabels,
            Button[] milkSelectionButtons,
            Text[] ingredientNameLabels,
            Text[] ingredientStateLabels,
            Button[] ingredientSelectionButtons,
            Button createButton,
            Button panelCloseButton,
            Func<MilkBlendingPanelSnapshot> getSnapshot,
            Func<string, string, MilkBlendResult> executeBlend,
            Action onClosed = null,
            TopMenuController menuController = null,
            BottomActionBarController bottomController = null,
            DevPanelController devController = null)
        {
            panelRoot = root;
            balanceText = balanceLabel;
            detailText = detailLabel;
            resultText = resultLabel;
            statusText = statusLabel;
            milkNameTexts = milkNameLabels;
            milkStateTexts = milkStateLabels;
            milkButtons = milkSelectionButtons;
            ingredientNameTexts = ingredientNameLabels;
            ingredientStateTexts = ingredientStateLabels;
            ingredientButtons = ingredientSelectionButtons;
            blendButton = createButton;
            closeButton = panelCloseButton;
            snapshotProvider = getSnapshot;
            blendCommand = executeBlend;
            closeAction = onClosed;
            topMenuController = menuController;
            actionBarController = bottomController;
            developerPanelController = devController;

            selectedMilkId = string.Empty;
            selectedIngredientId = string.Empty;
            statusMessage = string.Empty;
            BindButtons();
            Refresh();
            Close();
        }

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            BindButtons();
            Refresh();
        }

        private void Update()
        {
            if (IsOpen && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                HandleCloseClicked();
            }
        }

        public bool Open()
        {
            BindButtons();
            statusMessage = string.Empty;
            Refresh();
            if (panelRoot == null)
            {
                return false;
            }

            SetActive(panelRoot, true);
            SuspendControls();
            panelRoot.transform.SetAsLastSibling();
            Render();
            EventSystem.current?.SetSelectedGameObject(
                blendButton != null ? blendButton.gameObject : closeButton?.gameObject);
            return true;
        }

        public void Close()
        {
            SetActive(panelRoot, false);
            RestoreControls();
        }

        public void Refresh()
        {
            snapshot = snapshotProvider?.Invoke()
                ?? MilkBlendingPanelSnapshot.CreateDefault();
            EnsureSelections();
            Render();
        }

        public void SelectMilk(string milkId)
        {
            var milk = MilkCatalog.Find(milkId);
            if (milk == null || !snapshot.IsMilkUnlocked(milk.id))
            {
                return;
            }

            selectedMilkId = milk.id;
            statusMessage = string.Empty;
            Render();
        }

        public void SelectIngredient(string ingredientId)
        {
            var ingredient = MilkBlendingCatalog.FindIngredient(ingredientId);
            if (ingredient == null)
            {
                return;
            }

            selectedIngredientId = ingredient.id;
            statusMessage = string.Empty;
            Render();
        }

        public void BlendSelected()
        {
            if (string.IsNullOrEmpty(selectedMilkId)
                || string.IsNullOrEmpty(selectedIngredientId))
            {
                statusMessage = "우유와 재료를 하나씩 선택해 주세요.";
                Render();
                return;
            }

            if (!snapshot.IsMilkUnlocked(selectedMilkId))
            {
                statusMessage = "아직 사용할 수 없는 우유입니다.";
                Render();
                return;
            }

            if (blendCommand == null)
            {
                statusMessage = "블렌딩 기능이 아직 연결되지 않았습니다.";
                Render();
                return;
            }

            var result = blendCommand(selectedMilkId, selectedIngredientId);
            statusMessage = result?.message ?? "블렌딩 요청을 처리하지 못했습니다.";
            Refresh();
        }

        private void EnsureSelections()
        {
            if (MilkCatalog.Find(selectedMilkId) == null
                || !snapshot.IsMilkUnlocked(selectedMilkId))
            {
                selectedMilkId = FindFirstUnlockedMilkId();
            }

            if (MilkBlendingCatalog.FindIngredient(selectedIngredientId) == null)
            {
                selectedIngredientId = MilkBlendingCatalog.AllIngredients.Length > 0
                    ? MilkBlendingCatalog.AllIngredients[0].id
                    : string.Empty;
            }
        }

        private string FindFirstUnlockedMilkId()
        {
            for (var index = 0; index < MilkBlendingCatalog.AllMilkIds.Length; index += 1)
            {
                var milkId = MilkBlendingCatalog.AllMilkIds[index];
                if (snapshot.IsMilkUnlocked(milkId))
                {
                    return milkId;
                }
            }

            return string.Empty;
        }

        private void BindButtons()
        {
            if (milkButtons != null)
            {
                for (var index = 0; index < milkButtons.Length; index += 1)
                {
                    var button = milkButtons[index];
                    if (index < MilkBlendingCatalog.AllMilkIds.Length)
                    {
                        var milkId = MilkBlendingCatalog.AllMilkIds[index];
                        BindButton(button, () => SelectMilk(milkId));
                    }
                    else
                    {
                        ClearButton(button);
                    }
                }
            }

            if (ingredientButtons != null)
            {
                for (var index = 0; index < ingredientButtons.Length; index += 1)
                {
                    var button = ingredientButtons[index];
                    if (index < MilkBlendingCatalog.AllIngredients.Length)
                    {
                        var ingredientId = MilkBlendingCatalog.AllIngredients[index].id;
                        BindButton(button, () => SelectIngredient(ingredientId));
                    }
                    else
                    {
                        ClearButton(button);
                    }
                }
            }

            BindButton(blendButton, BlendSelected);
            BindButton(closeButton, HandleCloseClicked);
        }

        private void Render()
        {
            snapshot ??= MilkBlendingPanelSnapshot.CreateDefault();
            SetText(
                balanceText,
                $"우유코인 {snapshot.milkCoins} · 우유방울 {snapshot.milkDrops} · 수집 조각 {snapshot.collectionFragments}");
            RenderMilkOptions();
            RenderIngredientOptions();
            RenderSelection();
            SetText(statusText, statusMessage);
            SetInteractable(
                blendButton,
                blendCommand != null
                    && !string.IsNullOrEmpty(selectedMilkId)
                    && !string.IsNullOrEmpty(selectedIngredientId)
                    && snapshot.IsMilkUnlocked(selectedMilkId));
        }

        private void RenderMilkOptions()
        {
            for (var index = 0; index < MilkBlendingCatalog.AllMilkIds.Length; index += 1)
            {
                var milkId = MilkBlendingCatalog.AllMilkIds[index];
                var milk = MilkCatalog.Find(milkId);
                var unlocked = snapshot.IsMilkUnlocked(milkId);
                SetText(Get(milkNameTexts, index), milk?.displayName ?? milkId);
                SetText(
                    Get(milkStateTexts, index),
                    !unlocked
                        ? "잠김"
                        : string.Equals(selectedMilkId, milkId, StringComparison.Ordinal)
                            ? "선택"
                            : "사용 가능");
                SetInteractable(Get(milkButtons, index), unlocked);
                SetSelected(
                    Get(milkButtons, index),
                    string.Equals(selectedMilkId, milkId, StringComparison.Ordinal));
            }

            ClearUnused(milkNameTexts, MilkBlendingCatalog.AllMilkIds.Length);
            ClearUnused(milkStateTexts, MilkBlendingCatalog.AllMilkIds.Length);
            DisableUnused(milkButtons, MilkBlendingCatalog.AllMilkIds.Length);
        }

        private void RenderIngredientOptions()
        {
            for (var index = 0; index < MilkBlendingCatalog.AllIngredients.Length; index += 1)
            {
                var ingredient = MilkBlendingCatalog.AllIngredients[index];
                var useCount = snapshot.GetIngredientBlendCount(ingredient.id);
                SetText(Get(ingredientNameTexts, index), ingredient.displayName);
                SetText(Get(ingredientStateTexts, index), $"사용 {useCount}회");
                SetInteractable(Get(ingredientButtons, index), true);
                SetSelected(
                    Get(ingredientButtons, index),
                    string.Equals(
                        selectedIngredientId,
                        ingredient.id,
                        StringComparison.Ordinal));
            }

            ClearUnused(ingredientNameTexts, MilkBlendingCatalog.AllIngredients.Length);
            ClearUnused(ingredientStateTexts, MilkBlendingCatalog.AllIngredients.Length);
            DisableUnused(ingredientButtons, MilkBlendingCatalog.AllIngredients.Length);
        }

        private void RenderSelection()
        {
            var milk = MilkCatalog.Find(selectedMilkId);
            var ingredient = MilkBlendingCatalog.FindIngredient(selectedIngredientId);
            if (milk == null || ingredient == null)
            {
                SetText(detailText, "우유와 재료를 하나씩 선택해 주세요.");
                SetText(resultText, string.Empty);
                return;
            }

            SetText(
                detailText,
                $"<b>{milk.displayName} + {ingredient.displayName}</b>\n{ingredient.description}\n"
                + "서로 어울리지 않는 조합은 재화를 소비하지 않습니다.");

            var recipe = MilkBlendingCatalog.FindRecipe(milk.id, ingredient.id);
            if (recipe == null || !snapshot.IsDiscovered(recipe.resultSnackId))
            {
                SetText(resultText, "완성 결과  ???");
                return;
            }

            var resultSnack = recipe.ResultSnack;
            var useCount = snapshot.GetBlendCount(ingredient.id, recipe.resultSnackId);
            SetText(
                resultText,
                $"발견한 결과  {resultSnack?.displayName ?? recipe.resultSnackId}\n"
                + $"비용  {MilkBlendingCatalog.FormatCost(recipe)} · 만든 횟수 {useCount}회");
        }

        private void HandleCloseClicked()
        {
            Close();
            closeAction?.Invoke();
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            previousTopEnabled = topMenuController != null && topMenuController.enabled;
            previousBottomEnabled = actionBarController != null && actionBarController.enabled;
            previousDevEnabled = developerPanelController != null && developerPanelController.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (actionBarController != null) actionBarController.enabled = false;
            if (developerPanelController != null) developerPanelController.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = previousTopEnabled;
            if (actionBarController != null) actionBarController.enabled = previousBottomEnabled;
            if (developerPanelController != null) developerPanelController.enabled = previousDevEnabled;
            controlsSuspended = false;
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

        private static void ClearButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }

        private static void SetSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = selected
                ? new Color(1f, 0.83f, 0.42f, 1f)
                : Color.white;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        private static void ClearUnused(Text[] values, int usedCount)
        {
            if (values == null)
            {
                return;
            }

            for (var index = Math.Max(0, usedCount); index < values.Length; index += 1)
            {
                SetText(values[index], string.Empty);
            }
        }

        private static void DisableUnused(Button[] values, int usedCount)
        {
            if (values == null)
            {
                return;
            }

            for (var index = Math.Max(0, usedCount); index < values.Length; index += 1)
            {
                SetInteractable(values[index], false);
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static T Get<T>(T[] values, int index) where T : class
        {
            return values != null && index >= 0 && index < values.Length
                ? values[index]
                : null;
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
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
