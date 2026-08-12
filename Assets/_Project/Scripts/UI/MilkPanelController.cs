using System.Collections;
using System.Text;
using CheeseTama.Core;
using CheeseTama.Gameplay.Milk;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text selectedMilkGrowthText;
        [SerializeField] private Text selectedMilkEffectText;
        [SerializeField] private Text selectedMilkUnlockText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button[] milkTabButtons;
        [SerializeField] private Button[] feedButtons;
        [SerializeField] private Button closeButton;
        [SerializeField] private MilkroomUIController milkroomUi;

        private string activeMilkId = MilkCatalog.BasicMilkId;
        private Coroutine restorePanelRoutine;

        public void Configure(
            GameObject root,
            Text titleLabel,
            Text detailLabel,
            Text selectedGrowthLabel,
            Text selectedEffectLabel,
            Text selectedUnlockLabel,
            Text statusLabel,
            Button[] tabButtons,
            Button[] milkFeedButtons,
            Button panelCloseButton,
            MilkroomUIController uiController)
        {
            panelRoot = root;
            titleText = titleLabel;
            detailText = detailLabel;
            selectedMilkGrowthText = selectedGrowthLabel;
            selectedMilkEffectText = selectedEffectLabel;
            selectedMilkUnlockText = selectedUnlockLabel;
            statusText = statusLabel;
            milkTabButtons = tabButtons;
            feedButtons = milkFeedButtons;
            closeButton = panelCloseButton;
            milkroomUi = uiController;

            BindButtons();
            activeMilkId = MilkCatalog.BasicMilkId;
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
            var milks = MilkCatalog.VisibleMilks;
            if (milkTabButtons != null)
            {
                for (var i = 0; i < milkTabButtons.Length && i < milks.Length; i++)
                {
                    BindTabButton(milkTabButtons[i], milks[i].id);
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        private void BindTabButton(Button button, string milkId)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectMilk(milkId));
        }

        public void Open()
        {
            CancelPendingRestore(false);
            BindButtons();
            SetActive(panelRoot, true);
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

        public void Refresh()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            milkroomUi ??= Object.FindFirstObjectByType<MilkroomUIController>();
            milkroomUi?.Bind(manager.CurrentSave);

            var activeMilk = MilkCatalog.Find(activeMilkId);
            if (activeMilk == null || !ShouldShowMilk(manager, activeMilk))
            {
                activeMilk = MilkCatalog.BasicMilk;
                activeMilkId = activeMilk.id;
            }

            RefreshButtons(manager);
            SetText(titleText, FormatMilkTitle(manager, activeMilk));
            SetText(detailText, string.Empty);
            SetText(selectedMilkGrowthText, FormatSelectedMilkMeta(activeMilk));
            SetText(selectedMilkEffectText, FormatSelectedMilkDescriptionAndEffects(activeMilk));
            SetText(selectedMilkUnlockText, FormatSelectedMilkUnlockState(manager, activeMilk));
            SetText(statusText, manager.IsMilkUnlocked(activeMilk.id)
                ? $"{activeMilk.displayName}를 먹일 수 있습니다."
                : $"{activeMilk.displayName}는 조건을 만족하면 줄 수 있습니다.");
        }

        private void RefreshButtons(GameManager manager)
        {
            var milks = MilkCatalog.VisibleMilks;
            for (var i = 0; i < milks.Length; i++)
            {
                var milk = milks[i];
                var unlocked = manager.IsMilkUnlocked(milk.id);
                var visible = ShouldShowMilk(manager, milk);
                var selected = activeMilkId == milk.id;

                var tabButton = GetButton(milkTabButtons, i);
                if (tabButton != null)
                {
                    SetActive(tabButton.gameObject, visible);
                    tabButton.interactable = visible;
                    SetTabVisual(tabButton, selected, unlocked);
                }

                var feedButton = GetButton(feedButtons, i);
                if (feedButton != null)
                {
                    SetActive(feedButton.gameObject, visible && selected);
                    feedButton.interactable = visible && unlocked && selected;
                }
            }
        }

        private static bool ShouldShowMilk(GameManager manager, MilkDefinition milk)
        {
            return milk.id != MilkCatalog.StarMilkId || manager.IsMilkUnlocked(MilkCatalog.StarMilkId);
        }

        private static string FormatMilkTitle(GameManager manager, MilkDefinition milk)
        {
            return $"{milk.displayName} ({FormatGrowth(manager, milk)})";
        }

        private static string FormatSelectedMilkMeta(MilkDefinition milk)
        {
            return $"<b>희귀도</b>  {milk.rarity}";
        }

        private static string FormatSelectedMilkDescriptionAndEffects(MilkDefinition milk)
        {
            return $"<b>설명</b>\n<size=4> </size>\n{milk.description}\n\n<b>효과</b>  {FormatEffects(milk)}";
        }

        private static string FormatSelectedMilkUnlockState(GameManager manager, MilkDefinition milk)
        {
            if (manager.IsMilkUnlocked(milk.id))
            {
                return $"<b>해금</b>  완료 · 바로 줄 수 있습니다.";
            }

            return $"<b>해금 조건</b>  {FormatUnlockRequirement(milk)}";
        }

        private static string FormatGrowth(GameManager manager, MilkDefinition milk)
        {
            var entry = manager.FindMilkGrowth(milk.id);
            var level = entry?.growthLevel ?? 0;
            var points = entry?.growthPoints ?? 0;
            return $"성장 Lv.{level} / {points}점";
        }

        private static string FormatUnlockRequirement(MilkDefinition milk)
        {
            if (milk.id == MilkCatalog.StarMilkId)
            {
                return "치즈타마 Lv.33 + 주요 우유 전부 Lv.5";
            }

            var requiredMilk = MilkCatalog.Find(milk.requiredMilkId);
            if (requiredMilk == null)
            {
                return "처음부터 사용 가능";
            }

            return $"{requiredMilk.displayName} Lv.{milk.requiredMilkLevel}";
        }

        private static string FormatEffects(MilkDefinition milk)
        {
            var builder = new StringBuilder();
            AppendEffect(builder, "포만감", milk.hunger);
            AppendEffect(builder, "기분", milk.mood);
            AppendEffect(builder, "청결", milk.cleanliness);
            AppendEffect(builder, "졸림", milk.sleepiness);
            AppendEffect(builder, "건강", milk.health);
            AppendEffect(builder, "숙성도", milk.maturation);
            AppendEffect(builder, "애정", milk.affection);

            return builder.Length == 0 ? "성장 진행" : builder.ToString();
        }

        private static void AppendEffect(StringBuilder builder, string label, int value)
        {
            if (value == 0)
            {
                return;
            }

            builder.Append(' ');
            builder.Append(label);
            builder.Append(value > 0 ? " +" : " ");
            builder.Append(value);
        }

        private void SelectMilk(string milkId)
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            var milk = MilkCatalog.Find(milkId);
            if (milk == null)
            {
                return;
            }

            if (!ShouldShowMilk(manager, milk))
            {
                SetText(statusText, "조건을 만족하면 표시됩니다.");
                return;
            }

            activeMilkId = milkId;
            milkroomUi ??= Object.FindFirstObjectByType<MilkroomUIController>();
            milkroomUi?.AdvanceCareTip();
            Refresh();
        }

        private static Button GetButton(Button[] buttons, int index)
        {
            return buttons != null && index >= 0 && index < buttons.Length ? buttons[index] : null;
        }

        private static void SetTabVisual(Button button, bool selected, bool unlocked)
        {
            if (button == null)
            {
                return;
            }

            var color = selected
                ? new Color(1f, 0.74f, 0.24f, 1f)
                : unlocked
                    ? new Color(1f, 0.9f, 0.62f, 0.88f)
                    : new Color(0.74f, 0.70f, 0.64f, 0.72f);

            if (button.TryGetComponent(out Image image))
            {
                image.color = color;
            }

            var colors = button.colors;
            colors.normalColor = color;
            colors.disabledColor = new Color(0.74f, 0.70f, 0.64f, 0.52f);
            colors.selectedColor = color;
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform != null && labelTransform.TryGetComponent(out Text label))
            {
                label.color = unlocked
                    ? new Color(0.26f, 0.16f, 0.08f)
                    : new Color(0.46f, 0.42f, 0.36f);
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
