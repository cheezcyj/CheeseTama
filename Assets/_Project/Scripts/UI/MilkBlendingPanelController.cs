using System;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Snacks;
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
        [SerializeField] private Text discoveredResultText;
        [SerializeField] private Text requiredCostText;
        [SerializeField] private Text blendCountText;
        [SerializeField] private Text specialResultText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text[] milkNameTexts;
        [SerializeField] private Text[] milkStateTexts;
        [SerializeField] private Button[] milkButtons;
        [SerializeField] private Text[] ingredientNameTexts;
        [SerializeField] private Text[] ingredientStateTexts;
        [SerializeField] private Button[] ingredientButtons;
        [SerializeField] private Button blendButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image stirProgressImage;
        [SerializeField] private Text stirPromptText;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController actionBarController;
        [SerializeField] private DevPanelController developerPanelController;

        private Func<MilkBlendingPanelSnapshot> snapshotProvider;
        private Func<string, string, MilkBlendResult> blendCommand;
        private Func<string, string, MilkBlendResult> manualStirBlendCommand;
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
        private bool stirActive;
        private bool stirCompletionArmed;
        private bool stirCompletionPresented;
        private bool blendDispatchInProgress;
        private float stirProgress;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public string SelectedMilkId => selectedMilkId;
        public string SelectedIngredientId => selectedIngredientId;
        public bool IsStirActive => stirActive;
        public float StirProgress => stirProgress;

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
            DevPanelController devController = null,
            Func<string, string, MilkBlendResult> executeManualStirBlend = null,
            Image manualStirProgressImage = null,
            Text manualStirLabel = null,
            Text discoveredResultLabel = null,
            Text requiredCostLabel = null,
            Text blendCountLabel = null,
            Text specialResultLabel = null)
        {
            panelRoot = root;
            balanceText = balanceLabel;
            detailText = detailLabel;
            resultText = resultLabel;
            discoveredResultText = discoveredResultLabel;
            requiredCostText = requiredCostLabel;
            blendCountText = blendCountLabel;
            specialResultText = specialResultLabel;
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
            manualStirBlendCommand = executeManualStirBlend;
            closeAction = onClosed;
            topMenuController = menuController;
            actionBarController = bottomController;
            developerPanelController = devController;
            stirProgressImage = manualStirProgressImage;
            stirPromptText = manualStirLabel;

            selectedMilkId = string.Empty;
            selectedIngredientId = string.Empty;
            statusMessage = string.Empty;
            ResetStirPresentation();
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
            ResetStirPresentation();
            Render();
            EventSystem.current?.SetSelectedGameObject(
                blendButton != null ? blendButton.gameObject : closeButton?.gameObject);
            return true;
        }

        public void Close()
        {
            ResetStirPresentation();
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
            if (IsSelectionLocked())
            {
                return;
            }

            var milk = MilkCatalog.Find(milkId);
            if (milk == null || !snapshot.IsMilkUnlocked(milk.id))
            {
                return;
            }

            selectedMilkId = milk.id;
            stirCompletionPresented = false;
            statusMessage = string.Empty;
            Render();
        }

        public void SelectIngredient(string ingredientId)
        {
            if (IsSelectionLocked())
            {
                return;
            }

            var ingredient = MilkBlendingCatalog.FindIngredient(ingredientId);
            if (ingredient == null)
            {
                return;
            }

            selectedIngredientId = ingredient.id;
            stirCompletionPresented = false;
            statusMessage = string.Empty;
            Render();
        }

        public void BlendSelected()
        {
            ExecuteSelected(false);
        }

        public void BlendSelectedFromStir()
        {
            ExecuteSelected(true);
        }

        public void HandleStirProgress(DirectManipulationProgress progress)
        {
            if (!IsOpen)
            {
                ResetStirPresentation();
                return;
            }

            stirActive = progress.IsActive;
            stirProgress = Mathf.Clamp01(progress.NormalizedProgress);
            stirCompletionArmed = !progress.IsActive && stirProgress >= 0.999f;
            if (progress.IsActive)
            {
                stirCompletionPresented = false;
            }
            RenderStirPresentation();
            RenderMilkOptions();
            RenderIngredientOptions();
            RefreshBlendButtonState();
        }

        private void ExecuteSelected(bool manualStir)
        {
            if (blendDispatchInProgress)
            {
                return;
            }

            if (!manualStir && stirActive)
            {
                statusMessage = "원을 다 저은 뒤에 버튼을 눌러 주세요.";
                Render();
                return;
            }

            if (manualStir && stirCompletionPresented && !stirCompletionArmed)
            {
                return;
            }

            if (manualStir && !stirCompletionArmed)
            {
                statusMessage = "원을 끝까지 그려야 직접 젓기 보상을 받아요.";
                Render();
                return;
            }

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

            var command = manualStir
                ? manualStirBlendCommand ?? blendCommand
                : blendCommand;
            if (command == null)
            {
                statusMessage = "우유 섞기를 준비하지 못했어요. 잠시 뒤 다시 시도해 주세요.";
                Render();
                return;
            }

            blendDispatchInProgress = true;
            stirCompletionArmed = false;
            var manualStirApplied = false;
            try
            {
                var result = command(selectedMilkId, selectedIngredientId);
                manualStirApplied = manualStir && result != null && result.applied;
                statusMessage = BuildStatusMessage(result);
                snapshot = snapshotProvider?.Invoke()
                    ?? MilkBlendingPanelSnapshot.CreateDefault();
                EnsureSelections();
            }
            finally
            {
                blendDispatchInProgress = false;
                ResetStirPresentation(manualStirApplied);
                Render();
            }
        }

        private static string BuildStatusMessage(MilkBlendResult result)
        {
            if (result == null)
            {
                return "우유 섞기를 끝내지 못했어요. 다시 시도해 주세요.";
            }

            if (!result.applied)
            {
                return string.IsNullOrWhiteSpace(result.message)
                    ? "우유 섞기를 끝내지 못했어요. 다시 시도해 주세요."
                    : result.message;
            }

            var message = "섞기를 마쳤어요.";
            if (result.masteryReward?.granted == true)
            {
                message += " 새 배움 보상도 받았어요.";
            }

            if (result.manualStirMilkCoinReward > 0)
            {
                message += $" 직접 저은 보상: 코인 +{result.manualStirMilkCoinReward}.";
            }

            return message;
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
                $"코인 {snapshot.milkCoins} · 우유방울 {snapshot.milkDrops} · 도감 조각 {snapshot.collectionFragments}");
            RenderMilkOptions();
            RenderIngredientOptions();
            RenderSelection();
            SetText(statusText, statusMessage);
            RenderStirPresentation();
            RefreshBlendButtonState();
        }

        private void RefreshBlendButtonState()
        {
            SetInteractable(
                blendButton,
                !stirActive
                    && !blendDispatchInProgress
                    && blendCommand != null
                    && !string.IsNullOrEmpty(selectedMilkId)
                    && !string.IsNullOrEmpty(selectedIngredientId)
                    && snapshot.IsMilkUnlocked(selectedMilkId));
        }

        private void RenderStirPresentation()
        {
            if (stirProgressImage != null)
            {
                stirProgressImage.fillAmount = Mathf.Clamp01(stirProgress);
            }

            if (stirPromptText == null)
            {
                return;
            }

            if (stirActive)
            {
                stirPromptText.text = $"원을 따라 저어요\n{Mathf.RoundToInt(stirProgress * 100f)}%";
            }
            else if (stirCompletionArmed)
            {
                stirPromptText.text = "잘 저었어요!\n보상을 받는 중";
            }
            else if (stirCompletionPresented)
            {
                stirPromptText.text = "젓기 완료!\n아래 결과를 확인해요";
            }
            else
            {
                stirPromptText.text = $"손가락으로 원을 그려요\n완료 보상 +{MilkBlendingSystem.ManualStirMilkCoinReward} 코인";
            }
        }

        private void ResetStirPresentation(bool showCompletion = false)
        {
            stirActive = false;
            stirCompletionArmed = false;
            stirCompletionPresented = showCompletion;
            stirProgress = 0f;
            RenderStirPresentation();
        }

        private void RenderMilkOptions()
        {
            for (var index = 0; index < MilkBlendingCatalog.AllMilkIds.Length; index += 1)
            {
                var milkId = MilkBlendingCatalog.AllMilkIds[index];
                var milk = MilkCatalog.Find(milkId);
                var unlocked = snapshot.IsMilkUnlocked(milkId);
                SetText(Get(milkNameTexts, index), ResolveMilkDisplayName(milk));
                SetText(
                    Get(milkStateTexts, index),
                    !unlocked
                        ? "잠김"
                        : string.Equals(selectedMilkId, milkId, StringComparison.Ordinal)
                            ? "선택"
                            : "사용 가능");
                SetInteractable(Get(milkButtons, index), unlocked && !IsSelectionLocked());
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
                SetText(
                    Get(ingredientStateTexts, index),
                    BuildCompactMasteryProgress(ingredient.id, useCount));
                SetInteractable(Get(ingredientButtons, index), !IsSelectionLocked());
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
                RenderResultSummary("???", "선택해 주세요", "0회", "없음");
                return;
            }

            SetText(
                detailText,
                $"<b>{milk.displayName} + {ingredient.displayName}</b>\n{ingredient.description}\n"
                + BuildMasteryProgressLine(ingredient.id)
                + BuildMasteryResearchLine(ingredient.id)
                + "\n"
                + "서로 어울리지 않는 조합은 코인과 재료를 쓰지 않아요.");

            var recipe = MilkBlendingCatalog.FindRecipe(milk.id, ingredient.id);
            var useCount = snapshot.GetIngredientBlendCount(ingredient.id);
            if (recipe == null)
            {
                RenderResultSummary("???", "사용하지 않음", $"{useCount}회", "없음");
                return;
            }

            var cost = MilkBlendingCatalog.FormatCost(recipe);
            var specialResult = BuildSpecialResultValue(recipe, ingredient.id);
            if (!snapshot.IsDiscovered(recipe.resultSnackId))
            {
                RenderResultSummary("???", cost, $"{useCount}회", specialResult);
                return;
            }

            var resultSnack = recipe.ResultSnack;
            RenderResultSummary(
                ResolveSnackDisplayName(resultSnack),
                cost,
                $"{useCount}회",
                specialResult);
        }

        private void RenderResultSummary(
            string discoveredResult,
            string requiredCost,
            string blendCount,
            string specialResult)
        {
            var safeDiscoveredResult = string.IsNullOrWhiteSpace(discoveredResult)
                ? "???"
                : discoveredResult.Trim();
            var safeRequiredCost = string.IsNullOrWhiteSpace(requiredCost)
                ? "사용하지 않음"
                : requiredCost.Trim();
            var safeBlendCount = string.IsNullOrWhiteSpace(blendCount)
                ? "0회"
                : blendCount.Trim();
            var safeSpecialResult = string.IsNullOrWhiteSpace(specialResult)
                ? "없음"
                : specialResult.Trim();

            SetText(
                discoveredResultText,
                $"<b>발견한 결과</b>\n{safeDiscoveredResult}");
            SetText(
                requiredCostText,
                $"<b>필요한 것</b>\n{safeRequiredCost}");
            SetText(
                blendCountText,
                $"<b>섞은 횟수</b>\n{safeBlendCount}");
            SetText(
                specialResultText,
                $"<b>특별 결과</b>\n{safeSpecialResult}");

            var hasSplitSummary = discoveredResultText != null
                || requiredCostText != null
                || blendCountText != null
                || specialResultText != null;
            SetText(
                resultText,
                hasSplitSummary
                    ? string.Empty
                    : $"발견한 결과  {safeDiscoveredResult}\n"
                      + $"필요한 것  {safeRequiredCost}\n"
                      + $"섞은 횟수  {safeBlendCount}\n"
                      + $"특별 결과  {safeSpecialResult.Replace("\n", " · ")}");
        }

        private string BuildCompactMasteryProgress(string ingredientId, int useCount)
        {
            var stage = snapshot.GetMasteryStage(ingredientId);
            var next = MilkBlendingCatalog.GetNextMasteryMilestone(useCount);
            return next == null
                ? $"실력 {stage} · 완료"
                : $"실력 {stage} · {useCount}/{next.requiredUseCount}회";
        }

        private string BuildMasteryProgressLine(string ingredientId)
        {
            var useCount = snapshot.GetIngredientBlendCount(ingredientId);
            var stage = snapshot.GetMasteryStage(ingredientId);
            var next = MilkBlendingCatalog.GetNextMasteryMilestone(useCount);
            if (next == null)
            {
                return $"섞기 실력 레벨 {stage} · 이 재료는 모두 배웠어요";
            }

            var remaining = Math.Max(0, next.requiredUseCount - useCount);
            return $"섞기 실력 레벨 {stage} · 다음 배움까지 {remaining}회";
        }

        private string BuildMasteryResearchLine(string ingredientId)
        {
            var recordCount = snapshot.GetMasteryResearchRecordCount(ingredientId);
            var totalCount = MilkBlendingCatalog.AllMasteryMilestones.Length;
            var latest = snapshot.GetLatestMasteryResearchRecord(ingredientId);
            return latest == null
                ? $"\n배운 기록  {recordCount}/{totalCount}"
                : $"\n배운 기록  {recordCount}/{totalCount} · {latest.title}";
        }

        private string BuildSpecialResultValue(
            MilkBlendRecipeDefinition recipe,
            string ingredientId)
        {
            if (recipe == null || !recipe.HasSpecialResult)
            {
                return "없음";
            }

            if (!snapshot.IsDiscovered(recipe.specialResultSnackId))
            {
                return "낮은 확률로 ???";
            }

            var specialSnack = recipe.SpecialResultSnack;
            var specialCount = snapshot.GetBlendCount(
                ingredientId,
                recipe.specialResultSnackId);
            return $"{ResolveSnackDisplayName(specialSnack)}\n발견 {specialCount}회";
        }

        private static string ResolveMilkDisplayName(MilkDefinition milk)
        {
            return string.IsNullOrWhiteSpace(milk?.displayName)
                ? "알 수 없는 우유"
                : milk.displayName;
        }

        private static string ResolveSnackDisplayName(SnackDefinition snack)
        {
            return string.IsNullOrWhiteSpace(snack?.displayName)
                ? "알 수 없는 간식"
                : snack.displayName;
        }

        private void HandleCloseClicked()
        {
            Close();
            closeAction?.Invoke();
        }

        private bool IsSelectionLocked()
        {
            return stirActive || stirCompletionArmed || blendDispatchInProgress;
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
