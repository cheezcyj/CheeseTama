using System;
using CheeseTama.Gameplay.Growth;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Presentation-only overlay for the caller-provided current life chapter snapshot. It does
    /// not inspect the chapter catalog, player level, save DTO, memory journal, or economy.
    /// </summary>
    public sealed class LifeChapterPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Life Chapter Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text[] objectiveLabels;
        [SerializeField] private Button entryButton;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Text firstChoiceLabel;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private Text secondChoiceLabel;
        [SerializeField] private Button closeButton;

        private Func<LifeChapterSnapshot> snapshotProvider;
        private Func<string, string, LifeChapterChoiceResult> choiceCallback;
        private Action<bool> blockingChanged;
        private Action beforeEntryOpen;
        private LifeChapterSnapshot displayedSnapshot;
        private bool configured;
        private bool showingResult;
        private bool choicePending;
        private bool blockingActive;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public bool ShowingResult => showingResult;
        public string DisplayedChapterId => displayedSnapshot?.ChapterId ?? string.Empty;
        public string RenderedTitle => titleLabel != null ? titleLabel.text : string.Empty;
        public string RenderedBody => bodyLabel != null ? bodyLabel.text : string.Empty;
        public string RenderedProgress => progressLabel != null ? progressLabel.text : string.Empty;
        public bool EntryVisible => entryButton != null && entryButton.gameObject.activeSelf;

        public void Configure(
            GameObject root,
            Text title,
            Text body,
            Text progress,
            Text[] objectiveTexts,
            Button firstChoice,
            Text firstChoiceText,
            Button secondChoice,
            Text secondChoiceText,
            Button close,
            Func<LifeChapterSnapshot> provideSnapshot,
            Func<string, string, LifeChapterChoiceResult> resolveChoice,
            Action<bool> onBlockingChanged = null)
        {
            NotifyBlocking(false);
            UnbindButtons();
            overlayRoot = root;
            titleLabel = title;
            bodyLabel = body;
            progressLabel = progress;
            objectiveLabels = objectiveTexts;
            firstChoiceButton = firstChoice;
            firstChoiceLabel = firstChoiceText;
            secondChoiceButton = secondChoice;
            secondChoiceLabel = secondChoiceText;
            closeButton = close;
            snapshotProvider = provideSnapshot;
            choiceCallback = resolveChoice;
            blockingChanged = onBlockingChanged;
            configured = overlayRoot != null
                && HasObjectiveLabels(objectiveLabels)
                && firstChoiceButton != null
                && secondChoiceButton != null
                && closeButton != null
                && snapshotProvider != null
                && choiceCallback != null;
            BindButtons();
            Close();
            RefreshEntryVisibility();
        }

        /// <summary>
        /// Optionally binds a separate Journey/Profile entry button. Its visibility is derived only
        /// from the injected spoiler-safe snapshot, so a locked chapter leaks no level or title.
        /// </summary>
        public void ConfigureEntryButton(Button button, Action onBeforeOpen = null)
        {
            UnbindEntryButton();
            if (entryButton != null && entryButton != button)
            {
                entryButton.gameObject.SetActive(false);
                entryButton.interactable = false;
            }

            entryButton = button;
            beforeEntryOpen = onBeforeOpen;
            BindEntryButton();
            RefreshEntryVisibility();
        }

        public bool Open()
        {
            if (!configured)
            {
                Close();
                return false;
            }

            var snapshot = snapshotProvider();
            SetEntryVisible(snapshot?.Visible == true);
            if (IsOpen && showingResult)
            {
                overlayRoot.transform.SetAsLastSibling();
                return true;
            }

            return ShowSnapshot(snapshot);
        }

        public bool Refresh()
        {
            if (!configured)
            {
                Close();
                return false;
            }

            var snapshot = snapshotProvider();
            SetEntryVisible(snapshot?.Visible == true);
            if (!IsOpen)
            {
                return snapshot?.Visible == true;
            }

            if (showingResult)
            {
                return true;
            }

            return ShowSnapshot(snapshot);
        }

        public bool RefreshEntryVisibility()
        {
            if (entryButton == null)
            {
                return false;
            }

            var visible = configured && snapshotProvider?.Invoke()?.Visible == true;
            SetEntryVisible(visible);
            return visible;
        }

        public bool RefreshEntryVisibility(LifeChapterSnapshot snapshot)
        {
            if (entryButton == null)
            {
                return false;
            }

            var visible = configured && snapshot?.Visible == true;
            SetEntryVisible(visible);
            return visible;
        }

        public void Close()
        {
            displayedSnapshot = null;
            showingResult = false;
            choicePending = false;
            ClearPresentation();
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            NotifyBlocking(false);
        }

        public void ChooseFirst()
        {
            ResolveChoice(0);
        }

        public void ChooseSecond()
        {
            ResolveChoice(1);
        }

        private void OnEnable()
        {
            if (configured)
            {
                BindButtons();
                BindEntryButton();
            }
        }

        private void OnDisable()
        {
            NotifyBlocking(false);
            UnbindButtons();
            displayedSnapshot = null;
            showingResult = false;
            choicePending = false;
            ClearPresentation();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnbindEntryButton();
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

        private bool ShowSnapshot(LifeChapterSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.Visible)
            {
                Close();
                return false;
            }

            showingResult = false;
            choicePending = false;
            displayedSnapshot = snapshot;
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            NotifyBlocking(true);
            BindButtons();
            RenderSnapshot(snapshot);
            return true;
        }

        private void ResolveChoice(int choiceIndex)
        {
            if (!IsOpen
                || showingResult
                || choicePending
                || displayedSnapshot == null
                || !displayedSnapshot.CanChoose
                || choiceIndex < 0
                || choiceIndex >= displayedSnapshot.Choices.Count)
            {
                return;
            }

            var chapterId = displayedSnapshot.ChapterId;
            var choiceId = displayedSnapshot.Choices[choiceIndex].Id;
            choicePending = true;
            LifeChapterChoiceResult result;
            try
            {
                result = choiceCallback(chapterId, choiceId);
            }
            finally
            {
                choicePending = false;
            }

            if (result == null || !result.Applied)
            {
                SetText(bodyLabel, "선택을 기록하지 못했어요. 잠시 뒤 다시 시도해 주세요.");
                return;
            }

            showingResult = true;
            displayedSnapshot = null;
            SetText(titleLabel, "기억에 남은 선택");
            SetText(bodyLabel, result.UserMessage);
            SetText(progressLabel, string.Empty);
            SetObjectiveVisible(false);
            SetChoiceVisible(false);
            RefreshEntryVisibility();
        }

        private void RenderSnapshot(LifeChapterSnapshot snapshot)
        {
            SetText(titleLabel, snapshot.Title);
            SetText(bodyLabel, snapshot.Body);
            SetText(
                progressLabel,
                $"생활 목표  {snapshot.CompletedObjectiveCount}/{snapshot.Objectives.Count}");

            for (var index = 0; index < objectiveLabels.Length; index += 1)
            {
                var label = objectiveLabels[index];
                if (label == null)
                {
                    continue;
                }

                var visible = index < snapshot.Objectives.Count;
                label.gameObject.SetActive(visible);
                if (!visible)
                {
                    SetText(label, string.Empty);
                    continue;
                }

                var objective = snapshot.Objectives[index];
                SetText(
                    label,
                    $"{(objective.Completed ? "✓" : "○")} {objective.DisplayName}");
                label.color = objective.Completed
                    ? new Color(0.25f, 0.52f, 0.34f)
                    : new Color(0.35f, 0.29f, 0.24f);
            }

            if (snapshot.CanChoose)
            {
                SetText(firstChoiceLabel, snapshot.Choices[0].Label);
                SetText(secondChoiceLabel, snapshot.Choices[1].Label);
                SetChoiceVisible(true);
            }
            else
            {
                SetText(firstChoiceLabel, string.Empty);
                SetText(secondChoiceLabel, string.Empty);
                SetChoiceVisible(false);
            }
        }

        private void ClearPresentation()
        {
            SetText(titleLabel, string.Empty);
            SetText(bodyLabel, string.Empty);
            SetText(progressLabel, string.Empty);
            SetText(firstChoiceLabel, string.Empty);
            SetText(secondChoiceLabel, string.Empty);
            SetObjectiveVisible(false);
            SetChoiceVisible(false);
        }

        private void BindButtons()
        {
            if (!configured)
            {
                return;
            }

            firstChoiceButton.onClick.RemoveListener(ChooseFirst);
            firstChoiceButton.onClick.AddListener(ChooseFirst);
            secondChoiceButton.onClick.RemoveListener(ChooseSecond);
            secondChoiceButton.onClick.AddListener(ChooseSecond);
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        private void UnbindButtons()
        {
            firstChoiceButton?.onClick.RemoveListener(ChooseFirst);
            secondChoiceButton?.onClick.RemoveListener(ChooseSecond);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void BindEntryButton()
        {
            if (entryButton == null)
            {
                return;
            }

            entryButton.onClick.RemoveListener(OpenFromEntry);
            entryButton.onClick.AddListener(OpenFromEntry);
        }

        private void UnbindEntryButton()
        {
            entryButton?.onClick.RemoveListener(OpenFromEntry);
        }

        private void OpenFromEntry()
        {
            if (!IsOpen)
            {
                beforeEntryOpen?.Invoke();
            }

            Open();
        }

        private void SetObjectiveVisible(bool visible)
        {
            if (objectiveLabels == null)
            {
                return;
            }

            for (var index = 0; index < objectiveLabels.Length; index += 1)
            {
                if (objectiveLabels[index] != null)
                {
                    objectiveLabels[index].gameObject.SetActive(visible);
                    if (!visible)
                    {
                        SetText(objectiveLabels[index], string.Empty);
                    }
                }
            }
        }

        private void SetChoiceVisible(bool visible)
        {
            if (firstChoiceButton != null)
            {
                firstChoiceButton.gameObject.SetActive(visible);
                firstChoiceButton.interactable = visible;
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.gameObject.SetActive(visible);
                secondChoiceButton.interactable = visible;
            }
        }

        private void SetEntryVisible(bool visible)
        {
            if (entryButton != null)
            {
                entryButton.gameObject.SetActive(visible);
                entryButton.interactable = visible;
            }
        }

        private void NotifyBlocking(bool active)
        {
            if (blockingActive == active)
            {
                return;
            }

            blockingActive = active;
            blockingChanged?.Invoke(active);
        }

        private static bool HasObjectiveLabels(Text[] labels)
        {
            if (labels == null || labels.Length < LifeChapterCatalog.ObjectivesPerChapter)
            {
                return false;
            }

            for (var index = 0; index < LifeChapterCatalog.ObjectivesPerChapter; index += 1)
            {
                if (labels[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
