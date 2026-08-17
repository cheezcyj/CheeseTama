using System.Collections;
using CheeseTama.Core;
using CheeseTama.Gameplay.Care;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class SnackPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text[] titleTexts;
        [SerializeField] private Text[] detailTexts;
        [SerializeField] private Text[] quantityTexts;
        [SerializeField] private Button[] feedButtons;
        [SerializeField] private Text inventoryListText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button closeButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;

        private readonly CareActionSystem careActions = new CareActionSystem();
        private string statusMessage = string.Empty;
        private Coroutine restorePanelRoutine;

        public void Configure(
            GameObject root,
            Text[] snackTitleLabels,
            Text[] snackDetailLabels,
            Text[] snackQuantityLabels,
            Button[] snackFeedButtons,
            Text snackInventoryListLabel,
            Text statusLabel,
            Button panelCloseButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            panelRoot = root;
            titleTexts = snackTitleLabels;
            detailTexts = snackDetailLabels;
            quantityTexts = snackQuantityLabels;
            feedButtons = snackFeedButtons;
            inventoryListText = snackInventoryListLabel;
            statusText = statusLabel;
            closeButton = panelCloseButton;
            milkroomUi = uiController;
            visualController = cheeseTamaVisual;

            BindButtons();
            Refresh();
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
            var snacks = SnackCatalog.VisibleSnackItems;
            if (feedButtons != null)
            {
                for (var i = 0; i < feedButtons.Length && i < snacks.Length; i++)
                {
                    var snack = snacks[i];
                    var button = feedButtons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => FeedSnack(snack));
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
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

            statusMessage = string.Empty;
            Refresh();
            ResetScrollToTop();
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

        public void Refresh()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            var saveData = manager.CurrentSave;
            saveData?.EnsureRuntimeDefaults();

            var snacks = SnackCatalog.VisibleSnackItems;
            var hasAnySnack = false;
            var inventoryList = string.Empty;
            for (var i = 0; i < snacks.Length; i++)
            {
                var snack = snacks[i];
                var quantity = GetQuantity(saveData, snack.id);
                hasAnySnack |= quantity > 0;
                inventoryList = AppendInventoryLine(inventoryList, snack, quantity);

                SetText(Get(titleTexts, i), snack.displayName);
                SetText(Get(detailTexts, i), $"{snack.description}\n효과: {FormatEffect(snack)}");
                SetText(Get(quantityTexts, i), $"수량 {quantity}");

                var feedButton = Get(feedButtons, i);
                if (feedButton != null)
                {
                    feedButton.interactable = quantity > 0;
                }
            }

            SetText(inventoryListText, inventoryList);
            if (string.IsNullOrWhiteSpace(statusMessage))
            {
                statusMessage = hasAnySnack
                    ? "먹일 간식을 선택하세요."
                    : "요리한 간식이 없습니다. 요리에서 먼저 만들어 주세요.";
            }

            SetText(statusText, statusMessage);
        }

        private void FeedSnack(SnackDefinition snack)
        {
            if (snack == null)
            {
                statusMessage = "선택한 간식 데이터를 찾지 못했습니다.";
                Refresh();
                return;
            }

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            if (manager.IsSleepScheduleActive)
            {
                statusMessage = "치즈타마가 자는 중이에요. 먼저 깨운 뒤 간식을 주세요.";
                Refresh();
                return;
            }

            var saveData = manager.CurrentSave;
            saveData?.EnsureRuntimeDefaults();
            var entry = FindEntry(saveData, snack.id);
            if (entry == null || entry.quantity <= 0)
            {
                statusMessage = $"{snack.displayName} 수량이 없습니다.";
                Refresh();
                return;
            }

            careActions.ConfigureLateLevelGrowth(
                manager.CurrentSave?.lateLevelGrowth,
                manager.CurrentSave?.milkGrowth);
            var careResult = careActions.FeedSnack(manager.CurrentTama, snack);
            if (!careResult.success)
            {
                statusMessage = careResult.message;
                Refresh();
                return;
            }

            entry.quantity = Mathf.Max(0, entry.quantity - 1);
            manager.RegisterCareAction("feed_snack");
            var routineMessage = manager.RegisterDailyCareAction("feed_snack")
                ? GameManager.DailyRoutineRewardMessage
                : string.Empty;
            var discoveryMessage = RegisterSnackDiscovery(manager, snack);
            if (careResult.hatched)
            {
                manager.RegisterCurrentEvolutionDiscovery();
            }

            manager.RefreshDerivedCollectionRecords();
            manager.SaveGame();

            ResolveUiController()?.Bind(saveData);
            ResolveUiController()?.ShowMessage(CombineMessages(careResult.message, routineMessage));
            ResolveUiController()?.ShowEventMessage(discoveryMessage);

            var visual = ResolveVisualController();
            if (visual != null)
            {
                visual.Bind(manager.CurrentTama);
                var visualAction = careResult.hatched
                    ? CheeseTamaVisualAction.Hatch
                    : careResult.leveledUp
                    ? CheeseTamaVisualAction.LevelUp
                    : CheeseTamaVisualAction.FeedSnack;
                if (string.IsNullOrWhiteSpace(snack.reactionEventId))
                {
                    visual.ReactAction(visualAction, careResult.hatched);
                }
                else
                {
                    visual.ReactEvent(snack.reactionEventId, visualAction);
                }

                HideDuringReaction(visual);
            }

            statusMessage = $"{snack.displayName}을 먹였습니다. 남은 수량 {entry.quantity}";
            Refresh();
        }

        private MilkroomUIController ResolveUiController()
        {
            if (milkroomUi != null)
            {
                return milkroomUi;
            }

            milkroomUi = Object.FindFirstObjectByType<MilkroomUIController>();
            return milkroomUi;
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

        private static string RegisterSnackDiscovery(GameManager manager, SnackDefinition snack)
        {
            if (manager == null)
            {
                return string.Empty;
            }

            var message = manager.RegisterEventDiscovery("cheese_snack_fed")
                ? "치즈타마 간식 기록이 추가되었습니다."
                : string.Empty;

            if (snack != null)
            {
                manager.RegisterEventDiscovery(snack.eventId);
            }

            var tama = manager.CurrentTama;
            if (tama != null
                && tama.stats != null
                && tama.stats.cleanliness < 60
                && manager.RegisterEventDiscovery("crumbly_snack"))
            {
                message = CombineMessages(message, "부스러진 간식 순간이 기록되었습니다.");
            }

            return message;
        }

        private static SnackInventorySaveEntry FindEntry(CheeseTamaSaveData saveData, string snackId)
        {
            if (saveData == null || saveData.snackInventory == null || string.IsNullOrWhiteSpace(snackId))
            {
                return null;
            }

            return saveData.snackInventory.Find(item => item != null && item.snackId == snackId);
        }

        private static int GetQuantity(CheeseTamaSaveData saveData, string snackId)
        {
            return Mathf.Max(0, FindEntry(saveData, snackId)?.quantity ?? 0);
        }

        private static string AppendInventoryLine(string current, SnackDefinition snack, int quantity)
        {
            var line = $"{GetShortSnackName(snack),-8}  수량 {quantity}    먹이기";
            return string.IsNullOrEmpty(current) ? line : $"{current}\n{line}";
        }

        private static string GetShortSnackName(SnackDefinition snack)
        {
            return snack?.id switch
            {
                SnackCatalog.WarmMilkSoupId => "우유 수프",
                SnackCatalog.SoftSnackDoughId => "간식 반죽",
                SnackCatalog.ColdMilkPuddingId => "우유 푸딩",
                SnackCatalog.NuttyCheeseCrackerId => "치즈 크래커",
                SnackCatalog.RichMilkRisottoId => "밀크 리조또",
                SnackCatalog.FermentedYogurtBowlId => "요거트볼",
                SnackCatalog.CoffeeMilkJellyId => "커피 젤리",
                _ => snack?.displayName ?? "알 수 없음"
            };
        }

        private static string FormatEffect(SnackDefinition snack)
        {
            var text = string.Empty;
            text = AppendEffect(text, snack.hunger, "포만감");
            text = AppendEffect(text, snack.mood, "기분");
            text = AppendEffect(text, snack.cleanliness, "청결");
            text = AppendEffect(text, snack.sleepiness, "졸림");
            text = AppendEffect(text, snack.health, "건강");
            text = AppendEffect(text, snack.affection, "애정");
            text = AppendEffect(text, snack.maturation, "성숙도");
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

        private static T Get<T>(T[] items, int index)
        {
            return items != null && index >= 0 && index < items.Length ? items[index] : default(T);
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

        private void ResetScrollToTop()
        {
            if (panelRoot == null)
            {
                return;
            }

            var scrollRect = panelRoot.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.verticalNormalizedPosition = 1f;
            if (scrollRect.content != null)
            {
                scrollRect.content.anchoredPosition = new Vector2(12f, -12f);
            }
        }
    }
}
