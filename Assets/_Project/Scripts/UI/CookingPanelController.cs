using System.Collections;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class CookingPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text recipeListText;
        [SerializeField] private Button[] recipeButtons;
        [SerializeField] private Button cookButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;

        private SnackDefinition selectedRecipe;
        private Coroutine restorePanelRoutine;

        public void Configure(
            GameObject root,
            Text titleLabel,
            Text detailLabel,
            Text statusLabel,
            Text recipeListLabel,
            Button[] cookingRecipeButtons,
            Button executeButton,
            Button panelCloseButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            panelRoot = root;
            titleText = titleLabel;
            detailText = detailLabel;
            statusText = statusLabel;
            recipeListText = recipeListLabel;
            recipeButtons = cookingRecipeButtons;
            cookButton = executeButton;
            closeButton = panelCloseButton;
            milkroomUi = uiController;
            visualController = cheeseTamaVisual;

            BindButtons();
            SelectRecipe(GetDefaultRecipe());
            Close();
        }

        private void Awake()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            CancelPendingRestore(true);
        }

        private void BindButtons()
        {
            var recipes = SnackCatalog.VisibleCookingRecipes;
            if (recipeButtons != null)
            {
                for (var i = 0; i < recipeButtons.Length && i < recipes.Length; i++)
                {
                    BindRecipeButton(recipeButtons[i], recipes[i]);
                }
            }

            if (cookButton != null)
            {
                cookButton.onClick.RemoveAllListeners();
                cookButton.onClick.AddListener(CookSelectedRecipe);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        private void BindRecipeButton(Button button, SnackDefinition recipe)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectRecipe(recipe));
        }

        public void Open()
        {
            CancelPendingRestore(false);
            BindButtons();
            SetActive(panelRoot, true);
            if (panelRoot != null)
            {
                panelRoot.transform.SetAsLastSibling();
            }

            if (selectedRecipe == null)
            {
                SelectRecipe(GetDefaultRecipe(), false);
                return;
            }

            Refresh();
        }

        public void Close()
        {
            CancelPendingRestore(false);
            SetActive(panelRoot, false);
        }

        public void HideDuringReaction(CheeseTamaVisualController visual)
        {
            if (panelRoot == null
                || !panelRoot.activeSelf
                || visual == null
                || !visual.IsReacting)
            {
                return;
            }

            CancelPendingRestore(false);
            SetActive(panelRoot, false);
            restorePanelRoutine = StartCoroutine(RestoreAfterReaction(visual));
        }

        private IEnumerator RestoreAfterReaction(CheeseTamaVisualController visual)
        {
            while (visual != null && visual.isActiveAndEnabled && visual.IsReacting)
            {
                yield return null;
            }

            restorePanelRoutine = null;
            SetActive(panelRoot, true);
        }

        private void CancelPendingRestore(bool restorePanel)
        {
            if (restorePanelRoutine != null)
            {
                StopCoroutine(restorePanelRoutine);
                restorePanelRoutine = null;
            }

            if (restorePanel)
            {
                SetActive(panelRoot, true);
            }
        }

        private void SelectRecipe(SnackDefinition recipe)
        {
            SelectRecipe(recipe, true);
        }

        private void SelectRecipe(SnackDefinition recipe, bool rotateCareTip)
        {
            selectedRecipe = recipe ?? GetDefaultRecipe();
            if (rotateCareTip)
            {
                milkroomUi ??= Object.FindFirstObjectByType<MilkroomUIController>();
                milkroomUi?.AdvanceCareTip();
            }

            Refresh();
        }

        private void Refresh()
        {
            if (selectedRecipe == null)
            {
                return;
            }

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            var saveData = manager.CurrentSave;
            saveData?.EnsureRuntimeDefaults();

            SetText(titleText, selectedRecipe.displayName);
            SetText(detailText, FormatRecipeDetail(selectedRecipe, saveData));
            SetText(statusText, GetRecipeStateMessage(selectedRecipe, saveData));

            var recipes = SnackCatalog.VisibleCookingRecipes;
            SetText(recipeListText, FormatRecipeList(recipes, selectedRecipe));
            if (recipeButtons != null)
            {
                for (var i = 0; i < recipeButtons.Length && i < recipes.Length; i++)
                {
                    SetSelected(recipeButtons[i], selectedRecipe == recipes[i]);
                }
            }

            if (cookButton != null)
            {
                cookButton.interactable = CanCook(selectedRecipe, saveData);
            }
        }

        private void CookSelectedRecipe()
        {
            if (selectedRecipe == null)
            {
                SelectRecipe(GetDefaultRecipe());
            }

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            var saveData = manager.CurrentSave;
            saveData?.EnsureRuntimeDefaults();
            if (!CanCook(selectedRecipe, saveData))
            {
                SetText(statusText, GetRecipeStateMessage(selectedRecipe, saveData));
                return;
            }

            PayCost(selectedRecipe, saveData.economy);
            AddSnackToInventory(saveData, selectedRecipe.id, 1);
            manager.RegisterCareAction("cook");
            var routineMessage = manager.RegisterDailyCareAction("cook")
                ? GameManager.DailyRoutineRewardMessage
                : string.Empty;
            manager.RegisterEventDiscovery(selectedRecipe.eventId);

            if (!string.IsNullOrWhiteSpace(selectedRecipe.growthMilkId) && selectedRecipe.growthPoints > 0)
            {
                manager.RegisterMilkDiscovery(selectedRecipe.growthMilkId);
                manager.RegisterMilkGrowth(selectedRecipe.growthMilkId, selectedRecipe.growthPoints);
            }

            manager.RefreshDerivedCollectionRecords();
            manager.SaveGame();

            if (milkroomUi == null)
            {
                milkroomUi = Object.FindFirstObjectByType<MilkroomUIController>();
            }

            milkroomUi?.Bind(saveData);
            milkroomUi?.ShowMessage(CombineMessages($"{selectedRecipe.displayName}을 만들었습니다. {selectedRecipe.resultMessage}", routineMessage));
            milkroomUi?.ShowEventMessage(string.Empty);

            var visual = ResolveVisualController();
            if (visual != null)
            {
                visual.Bind(manager.CurrentTama);
                if (string.IsNullOrWhiteSpace(selectedRecipe.reactionEventId))
                {
                    visual.ReactAction(CheeseTamaVisualAction.Cook);
                }
                else
                {
                    visual.ReactEvent(selectedRecipe.reactionEventId, CheeseTamaVisualAction.Cook);
                }

                HideDuringReaction(visual);
            }

            Refresh();
            SetText(statusText, $"{selectedRecipe.displayName}을 만들었습니다. {GetRecipeStateMessage(selectedRecipe, saveData)}");
        }

        private static string CombineMessages(string primary, string secondary)
        {
            if (string.IsNullOrWhiteSpace(primary))
            {
                return secondary ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(secondary))
            {
                return primary;
            }

            return $"{primary} {secondary}";
        }

        private CheeseTamaVisualController ResolveVisualController()
        {
            if (visualController != null)
            {
                return visualController;
            }

            visualController = Object.FindFirstObjectByType<CheeseTamaVisualController>();
            return visualController;
        }

        private static void AddSnackToInventory(CheeseTamaSaveData saveData, string snackId, int amount)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(snackId) || amount <= 0)
            {
                return;
            }

            saveData.EnsureRuntimeDefaults();
            var entry = saveData.snackInventory.Find(item => item != null && item.snackId == snackId);
            if (entry == null)
            {
                entry = new SnackInventorySaveEntry
                {
                    snackId = snackId
                };
                saveData.snackInventory.Add(entry);
            }

            entry.quantity += amount;
        }

        private static bool CanCook(SnackDefinition recipe, CheeseTamaSaveData saveData)
        {
            if (recipe == null || saveData == null || saveData.economy == null)
            {
                return false;
            }

            if (recipe.requiresStarMilk && (saveData.unlocks == null || !saveData.unlocks.starMilkUnlocked))
            {
                return false;
            }

            return saveData.economy.milkCoins >= recipe.coinCost
                && saveData.economy.milkDrops >= recipe.dropCost
                && saveData.economy.collectionFragments >= recipe.fragmentCost;
        }

        private static void PayCost(SnackDefinition recipe, EconomySaveData economy)
        {
            economy.milkCoins -= recipe.coinCost;
            economy.milkDrops -= recipe.dropCost;
            economy.collectionFragments -= recipe.fragmentCost;
        }

        private static string GetRecipeStateMessage(SnackDefinition recipe, CheeseTamaSaveData saveData)
        {
            if (recipe == null)
            {
                return "레시피를 선택하세요.";
            }

            if (saveData == null || saveData.economy == null)
            {
                return "저장 데이터를 불러오지 못했습니다.";
            }

            if (recipe.requiresStarMilk && (saveData.unlocks == null || !saveData.unlocks.starMilkUnlocked))
            {
                return "별빛 우유 해금 후 만들 수 있습니다.";
            }

            if (!CanCook(recipe, saveData))
            {
                return "재료가 부족합니다.";
            }

            return "만들 수 있습니다.";
        }

        private static string FormatRecipeDetail(SnackDefinition recipe, CheeseTamaSaveData saveData)
        {
            return $"{recipe.description}\n필요 재료: {FormatCost(recipe)}\n효과: {FormatEffect(recipe)}";
        }

        private static SnackDefinition GetDefaultRecipe()
        {
            var recipes = SnackCatalog.VisibleCookingRecipes;
            return recipes != null && recipes.Length > 0 ? recipes[0] : SnackCatalog.WarmMilkSoup;
        }

        private static string FormatRecipeList(SnackDefinition[] recipes, SnackDefinition selected)
        {
            if (recipes == null || recipes.Length == 0)
            {
                return "요리 메뉴가 없습니다.";
            }

            var text = string.Empty;
            for (var i = 0; i < recipes.Length; i++)
            {
                var marker = recipes[i] == selected ? "▶" : "•";
                var item = $"{marker} {GetShortRecipeName(recipes[i])}";
                text = string.IsNullOrEmpty(text) ? item : $"{text}    {item}";
                if (i == 3)
                {
                    text += "\n";
                }
            }

            return text;
        }

        private static string GetShortRecipeName(SnackDefinition recipe)
        {
            return recipe?.id switch
            {
                SnackCatalog.WarmMilkSoupId => "우유 수프",
                SnackCatalog.SoftSnackDoughId => "간식 반죽",
                SnackCatalog.ColdMilkPuddingId => "우유 푸딩",
                SnackCatalog.NuttyCheeseCrackerId => "치즈 크래커",
                SnackCatalog.RichMilkRisottoId => "밀크 리조또",
                SnackCatalog.FermentedYogurtBowlId => "요거트볼",
                SnackCatalog.CoffeeMilkJellyId => "커피 젤리",
                _ => recipe?.displayName ?? "알 수 없음"
            };
        }

        private static string FormatCost(SnackDefinition recipe)
        {
            var text = string.Empty;
            text = AppendCost(text, recipe.coinCost, "코인");
            text = AppendCost(text, recipe.dropCost, "우유 방울");
            text = AppendCost(text, recipe.fragmentCost, "도감 조각");
            return string.IsNullOrWhiteSpace(text) ? "없음" : text;
        }

        private static string AppendCost(string current, int amount, string label)
        {
            if (amount <= 0)
            {
                return current;
            }

            var part = $"{label} {amount}";
            return string.IsNullOrWhiteSpace(current) ? part : $"{current}, {part}";
        }

        private static string FormatEffect(SnackDefinition recipe)
        {
            var text = string.Empty;
            text = AppendEffect(text, recipe.hunger, "포만감");
            text = AppendEffect(text, recipe.mood, "기분");
            text = AppendEffect(text, recipe.cleanliness, "청결");
            text = AppendEffect(text, recipe.sleepiness, "졸림");
            text = AppendEffect(text, recipe.health, "건강");
            text = AppendEffect(text, recipe.affection, "애정");
            text = AppendEffect(text, recipe.maturation, "성숙도");
            return string.IsNullOrWhiteSpace(text) ? "상태 유지" : text;
        }

        private static string AppendEffect(string current, int amount, string label)
        {
            if (amount == 0)
            {
                return current;
            }

            var sign = amount > 0 ? "+" : string.Empty;
            var part = $"{label} {sign}{amount}";
            return string.IsNullOrWhiteSpace(current) ? part : $"{current}, {part}";
        }

        private static void SetSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (button.TryGetComponent(out Image image))
            {
                image.color = selected
                    ? new Color(1f, 0.78f, 0.30f, 1f)
                    : new Color(1f, 0.87f, 0.54f, 0.96f);
            }

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

    }
}
