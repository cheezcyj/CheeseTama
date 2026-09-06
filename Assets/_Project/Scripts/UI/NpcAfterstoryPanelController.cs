using System;
using System.Text;
using CheeseTama.Core;
using CheeseTama.Gameplay.NpcVisits;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Presents only afterstories returned by the visibility-filtered domain snapshot. The
    /// controller requests mutations through callbacks and never owns gameplay or save state.
    /// </summary>
    public sealed class NpcAfterstoryPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button entryButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button previousNpcButton;
        [SerializeField] private Button nextNpcButton;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private Button weeklyClaimButton;
        [SerializeField] private Button previousKeepsakeButton;
        [SerializeField] private Button nextKeepsakeButton;
        [SerializeField] private Button displayKeepsakeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text storyText;
        [SerializeField] private Text firstChoiceText;
        [SerializeField] private Text secondChoiceText;
        [SerializeField] private Text weeklyQuestText;
        [SerializeField] private Text keepsakeText;

        private Func<NpcAfterstoryPanelSnapshot> snapshotProvider;
        private Action<string, string> afterstoryChoiceRequested;
        private Action<string> weeklyClaimRequested;
        private Action<string> keepsakeSelectionRequested;
        private Action closed;
        private Action<bool> blockingChanged;
        private NpcAfterstoryPanelSnapshot snapshot = NpcAfterstoryPanelSnapshot.Empty;
        private int selectedNpcIndex;
        private int selectedKeepsakeIndex;
        private bool blockingNotified;
        private GameManager eventSource;
        private bool eventsBound;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool BlocksGameplayInput => IsOpen;
        public int SelectedNpcIndex => selectedNpcIndex;
        public int SelectedKeepsakeIndex => selectedKeepsakeIndex;
        public NpcAfterstoryPanelSnapshot Snapshot => snapshot;

        public void Configure(
            GameObject root,
            Button openButton,
            Button panelCloseButton,
            Button previousNpc,
            Button nextNpc,
            Button choiceA,
            Button choiceB,
            Button claimWeekly,
            Button previousKeepsake,
            Button nextKeepsake,
            Button displayKeepsake,
            Text headerLabel,
            Text storyLabel,
            Text choiceALabel,
            Text choiceBLabel,
            Text weeklyLabel,
            Text keepsakeLabel,
            Func<NpcAfterstoryPanelSnapshot> getSnapshot,
            Action<string, string> onAfterstoryChoiceRequested,
            Action<string> onWeeklyClaimRequested,
            Action<string> onKeepsakeSelectionRequested,
            Action onClosed = null,
            Action<bool> onBlockingChanged = null,
            GameManager manager = null)
        {
            UnbindEvents();
            UnbindButtons();
            ReleaseBlockingNotification();

            panelRoot = root;
            entryButton = openButton;
            closeButton = panelCloseButton;
            previousNpcButton = previousNpc;
            nextNpcButton = nextNpc;
            firstChoiceButton = choiceA;
            secondChoiceButton = choiceB;
            weeklyClaimButton = claimWeekly;
            previousKeepsakeButton = previousKeepsake;
            nextKeepsakeButton = nextKeepsake;
            displayKeepsakeButton = displayKeepsake;
            titleText = headerLabel;
            storyText = storyLabel;
            firstChoiceText = choiceALabel;
            secondChoiceText = choiceBLabel;
            weeklyQuestText = weeklyLabel;
            keepsakeText = keepsakeLabel;
            snapshotProvider = getSnapshot;
            afterstoryChoiceRequested = onAfterstoryChoiceRequested;
            weeklyClaimRequested = onWeeklyClaimRequested;
            keepsakeSelectionRequested = onKeepsakeSelectionRequested;
            closed = onClosed;
            blockingChanged = onBlockingChanged;
            eventSource = manager;
            selectedNpcIndex = 0;
            selectedKeepsakeIndex = 0;

            BindButtons();
            BindEvents();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            Refresh();
        }

        public bool Open()
        {
            Refresh();
            if (panelRoot == null || !snapshot.HasVisibleContent)
            {
                return false;
            }

            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            NotifyBlocking(true);
            EventSystem.current?.SetSelectedGameObject(
                firstChoiceButton != null && firstChoiceButton.interactable
                    ? firstChoiceButton.gameObject
                    : closeButton?.gameObject);
            return true;
        }

        public void Close()
        {
            CloseInternal(true);
        }

        public void Refresh()
        {
            var preferredNpcId = GetSelectedNpc()?.NpcId ?? string.Empty;
            var preferredKeepsakeId = GetSelectedKeepsake()?.KeepsakeId ?? string.Empty;
            snapshot = snapshotProvider?.Invoke() ?? NpcAfterstoryPanelSnapshot.Empty;
            selectedNpcIndex = ResolveNpcIndex(preferredNpcId);
            selectedKeepsakeIndex = ResolveKeepsakeIndex(preferredKeepsakeId);

            if (entryButton != null)
            {
                entryButton.gameObject.SetActive(snapshot.HasVisibleContent);
            }

            if (!snapshot.HasVisibleContent && IsOpen)
            {
                CloseInternal(false);
            }

            Render();
        }

        public void ShowPreviousNpc()
        {
            var count = snapshot.NpcStories.Count;
            if (count <= 0)
            {
                return;
            }

            selectedNpcIndex = (selectedNpcIndex - 1 + count) % count;
            RenderNpc();
        }

        public void ShowNextNpc()
        {
            var count = snapshot.NpcStories.Count;
            if (count <= 0)
            {
                return;
            }

            selectedNpcIndex = (selectedNpcIndex + 1) % count;
            RenderNpc();
        }

        public void ChooseFirstAfterstoryOption()
        {
            RequestChoice(0);
        }

        public void ChooseSecondAfterstoryOption()
        {
            RequestChoice(1);
        }

        public void ClaimSelectedWeeklyQuest()
        {
            var selected = GetSelectedNpc();
            if (selected == null || !selected.WeeklyQuest.CanClaim)
            {
                return;
            }

            weeklyClaimRequested?.Invoke(selected.NpcId);
            Refresh();
        }

        public void ShowPreviousKeepsake()
        {
            var count = snapshot.OwnedKeepsakes.Count;
            if (count <= 0)
            {
                return;
            }

            selectedKeepsakeIndex = (selectedKeepsakeIndex - 1 + count) % count;
            RenderKeepsake();
        }

        public void ShowNextKeepsake()
        {
            var count = snapshot.OwnedKeepsakes.Count;
            if (count <= 0)
            {
                return;
            }

            selectedKeepsakeIndex = (selectedKeepsakeIndex + 1) % count;
            RenderKeepsake();
        }

        public void DisplaySelectedKeepsake()
        {
            var selected = GetSelectedKeepsake();
            if (selected == null)
            {
                return;
            }

            keepsakeSelectionRequested?.Invoke(selected.KeepsakeId);
            Refresh();
        }

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            BindButtons();
            BindEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindEvents();
            ReleaseBlockingNotification();
        }

        private void OnDestroy()
        {
            UnbindEvents();
            UnbindButtons();
            ReleaseBlockingNotification();
        }

        private void Update()
        {
            if (IsOpen
                && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(
                    CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        private void RequestChoice(int choiceIndex)
        {
            var selected = GetSelectedNpc();
            var definition = selected?.Afterstory.Definition;
            if (selected == null
                || !selected.Afterstory.IsReady
                || definition == null
                || choiceIndex < 0
                || choiceIndex >= definition.Choices.Count)
            {
                return;
            }

            afterstoryChoiceRequested?.Invoke(definition.Id, definition.Choices[choiceIndex].Id);
            Refresh();
        }

        private void Render()
        {
            if (titleText != null)
            {
                titleText.text = "친구들의 다음 이야기";
                AccessibilityRuntime.ApplyCurrent(titleText);
            }

            RenderNpc();
            RenderKeepsake();
        }

        private void RenderNpc()
        {
            var selected = GetSelectedNpc();
            var hasMultiple = snapshot.NpcStories.Count > 1;
            SetInteractable(previousNpcButton, hasMultiple);
            SetInteractable(nextNpcButton, hasMultiple);

            if (storyText != null)
            {
                storyText.text = BuildStoryText(selected);
                AccessibilityRuntime.ApplyCurrent(storyText);
            }

            var definition = selected?.Afterstory.Definition;
            var canChoose = selected != null && selected.Afterstory.IsReady && definition != null;
            SetChoice(firstChoiceButton, firstChoiceText, definition, 0, canChoose);
            SetChoice(secondChoiceButton, secondChoiceText, definition, 1, canChoose);

            if (weeklyQuestText != null)
            {
                weeklyQuestText.text = BuildWeeklyText(selected?.WeeklyQuest);
                AccessibilityRuntime.ApplyCurrent(weeklyQuestText);
            }

            SetInteractable(weeklyClaimButton, selected != null && selected.WeeklyQuest.CanClaim);
        }

        private void RenderKeepsake()
        {
            var selected = GetSelectedKeepsake();
            var hasMultiple = snapshot.OwnedKeepsakes.Count > 1;
            SetInteractable(previousKeepsakeButton, hasMultiple);
            SetInteractable(nextKeepsakeButton, hasMultiple);
            SetInteractable(displayKeepsakeButton, selected != null);

            if (keepsakeText != null)
            {
                if (selected == null)
                {
                    keepsakeText.text = string.Empty;
                }
                else
                {
                    var isDisplayed = string.Equals(
                        selected.KeepsakeId,
                        snapshot.DisplayedKeepsake.Profile?.KeepsakeId,
                        StringComparison.Ordinal);
                    keepsakeText.text = $"<b>{EscapeRichText(selected.Title)}</b>"
                        + (isDisplayed ? " · 현재 진열 중" : " · 진열 가능")
                        + $"\n{EscapeRichText(selected.AmbientDialogue)}";
                }

                AccessibilityRuntime.ApplyCurrent(keepsakeText);
            }
        }

        private static string BuildStoryText(NpcAfterstoryNpcPanelSnapshot selected)
        {
            var definition = selected?.Afterstory.Definition;
            if (definition == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(320);
            builder.Append(EscapeRichText(definition.NpcDisplayName));
            builder.Append("\n<b>");
            builder.Append(EscapeRichText(definition.Title));
            builder.Append("</b>\n");
            builder.Append(EscapeRichText(definition.Description));
            if (selected.Afterstory.IsCompleted)
            {
                builder.Append("\n\n완료한 이야기");
                if (selected.Afterstory.SelectedChoice != null)
                {
                    builder.Append(" · ");
                    builder.Append(EscapeRichText(selected.Afterstory.SelectedChoice.Label));
                    builder.Append("\n");
                    builder.Append(EscapeRichText(selected.Afterstory.SelectedChoice.ResultMessage));
                }
            }

            return builder.ToString();
        }

        private static string BuildWeeklyText(NpcWeeklyQuestSnapshot? optionalSnapshot)
        {
            if (!optionalSnapshot.HasValue)
            {
                return string.Empty;
            }

            var weekly = optionalSnapshot.Value;
            if (!weekly.IsVisible)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(220);
            builder.Append("<b>");
            builder.Append(EscapeRichText(weekly.Definition.Title));
            builder.Append("</b>\n");
            builder.Append(EscapeRichText(weekly.Definition.ObjectiveLabel));
            builder.Append(" · ");
            builder.Append(Math.Min(weekly.Progress, weekly.RequiredProgress));
            builder.Append("/");
            builder.Append(weekly.RequiredProgress);

            switch (weekly.Status)
            {
                case NpcWeeklyQuestSnapshotStatus.ReadyToClaim:
                    builder.Append("\n보상 받기 가능 · 코인 +");
                    builder.Append(weekly.Definition.Reward.Coins);
                    builder.Append(" · 우유방울 +");
                    builder.Append(weekly.Definition.Reward.MilkDrops);
                    break;
                case NpcWeeklyQuestSnapshotStatus.Claimed:
                    builder.Append("\n이번 주 보상을 받았어요.");
                    break;
                case NpcWeeklyQuestSnapshotStatus.ClockRollback:
                    builder.Append("\n기기 시간이 이전 기록보다 빨라 진행을 잠시 확인할 수 없어요.");
                    break;
                case NpcWeeklyQuestSnapshotStatus.InvalidTime:
                    builder.Append("\n기기 시간을 확인해 주세요.");
                    break;
            }

            return builder.ToString();
        }

        private static void SetChoice(
            Button button,
            Text label,
            NpcAfterstoryDefinition definition,
            int choiceIndex,
            bool interactable)
        {
            var choice = definition != null
                && choiceIndex >= 0
                && choiceIndex < definition.Choices.Count
                ? definition.Choices[choiceIndex]
                : null;
            SetInteractable(button, interactable && choice != null);
            if (label != null)
            {
                label.text = choice?.Label ?? string.Empty;
                AccessibilityRuntime.ApplyCurrent(label);
            }
        }

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private NpcAfterstoryNpcPanelSnapshot GetSelectedNpc()
        {
            return selectedNpcIndex >= 0 && selectedNpcIndex < snapshot.NpcStories.Count
                ? snapshot.NpcStories[selectedNpcIndex]
                : null;
        }

        private NpcKeepsakeDisplayProfile GetSelectedKeepsake()
        {
            return selectedKeepsakeIndex >= 0 && selectedKeepsakeIndex < snapshot.OwnedKeepsakes.Count
                ? snapshot.OwnedKeepsakes[selectedKeepsakeIndex]
                : null;
        }

        private int ResolveNpcIndex(string preferredNpcId)
        {
            for (var index = 0; index < snapshot.NpcStories.Count; index += 1)
            {
                if (string.Equals(snapshot.NpcStories[index]?.NpcId, preferredNpcId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return snapshot.NpcStories.Count == 0
                ? 0
                : Math.Max(0, Math.Min(selectedNpcIndex, snapshot.NpcStories.Count - 1));
        }

        private int ResolveKeepsakeIndex(string preferredKeepsakeId)
        {
            for (var index = 0; index < snapshot.OwnedKeepsakes.Count; index += 1)
            {
                if (string.Equals(
                        snapshot.OwnedKeepsakes[index]?.KeepsakeId,
                        preferredKeepsakeId,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return snapshot.OwnedKeepsakes.Count == 0
                ? 0
                : Math.Max(0, Math.Min(selectedKeepsakeIndex, snapshot.OwnedKeepsakes.Count - 1));
        }

        private void BindButtons()
        {
            Bind(entryButton, OpenFromButton);
            Bind(closeButton, Close);
            Bind(previousNpcButton, ShowPreviousNpc);
            Bind(nextNpcButton, ShowNextNpc);
            Bind(firstChoiceButton, ChooseFirstAfterstoryOption);
            Bind(secondChoiceButton, ChooseSecondAfterstoryOption);
            Bind(weeklyClaimButton, ClaimSelectedWeeklyQuest);
            Bind(previousKeepsakeButton, ShowPreviousKeepsake);
            Bind(nextKeepsakeButton, ShowNextKeepsake);
            Bind(displayKeepsakeButton, DisplaySelectedKeepsake);
        }

        private void BindEvents()
        {
            if (eventSource == null || eventsBound)
            {
                return;
            }

            eventSource.JourneyHubChanged += HandleStateChanged;
            eventSource.SaveDataReplaced += HandleStateChanged;
            eventSource.NpcAfterstoryChanged += HandleAfterstoryChanged;
            eventSource.NpcKeepsakeDisplayChanged += HandleKeepsakeChanged;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (eventSource == null || !eventsBound)
            {
                eventsBound = false;
                return;
            }

            eventSource.JourneyHubChanged -= HandleStateChanged;
            eventSource.SaveDataReplaced -= HandleStateChanged;
            eventSource.NpcAfterstoryChanged -= HandleAfterstoryChanged;
            eventSource.NpcKeepsakeDisplayChanged -= HandleKeepsakeChanged;
            eventsBound = false;
        }

        private void HandleStateChanged()
        {
            Refresh();
        }

        private void HandleAfterstoryChanged(NpcAfterstoryChoiceResult _)
        {
            Refresh();
        }

        private void HandleKeepsakeChanged(NpcKeepsakeDisplaySnapshot _)
        {
            Refresh();
        }

        private void UnbindButtons()
        {
            Unbind(entryButton, OpenFromButton);
            Unbind(closeButton, Close);
            Unbind(previousNpcButton, ShowPreviousNpc);
            Unbind(nextNpcButton, ShowNextNpc);
            Unbind(firstChoiceButton, ChooseFirstAfterstoryOption);
            Unbind(secondChoiceButton, ChooseSecondAfterstoryOption);
            Unbind(weeklyClaimButton, ClaimSelectedWeeklyQuest);
            Unbind(previousKeepsakeButton, ShowPreviousKeepsake);
            Unbind(nextKeepsakeButton, ShowNextKeepsake);
            Unbind(displayKeepsakeButton, DisplaySelectedKeepsake);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
        {
            button?.onClick.RemoveListener(action);
        }

        private void OpenFromButton()
        {
            Open();
        }

        private void CloseInternal(bool notifyClosed)
        {
            var wasOpen = IsOpen;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            ReleaseBlockingNotification();
            if (wasOpen && notifyClosed)
            {
                closed?.Invoke();
            }
        }

        private void NotifyBlocking(bool blocked)
        {
            if (blocked == blockingNotified)
            {
                return;
            }

            blockingNotified = blocked;
            blockingChanged?.Invoke(blocked);
        }

        private void ReleaseBlockingNotification()
        {
            NotifyBlocking(false);
        }

        private static string EscapeRichText(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}
