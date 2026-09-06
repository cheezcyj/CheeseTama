using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Story;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Renders only a caller-provided spoiler-safe snapshot. The controller never reads the
    /// catalog, trigger rules, save DTO, or season size directly.
    /// </summary>
    public sealed class DreamStorySeasonPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Dream Story Season Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Text firstChoiceLabel;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private Text secondChoiceLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button previousRecallButton;
        [SerializeField] private Button nextRecallButton;

        private Func<DreamStorySnapshot> pendingSnapshotProvider;
        private Func<string, DreamStorySnapshot> recallSnapshotProvider;
        private Func<IReadOnlyList<DreamStoryRecallEntry>> recallIndexProvider;
        private Func<string, string, DreamStoryChoiceResult> choiceCallback;
        private Action<bool> blockingChanged;
        private DreamStorySnapshot displayedSnapshot;
        private bool configured;
        private bool showingResult;
        private bool choicePending;
        private bool blockingActive;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public bool ShowingResult => showingResult;
        public bool ShowingRecall => displayedSnapshot?.IsRecall == true;
        public string DisplayedEpisodeId => displayedSnapshot?.EpisodeId ?? string.Empty;
        public string RenderedTitle => titleLabel != null ? titleLabel.text : string.Empty;
        public string RenderedBody => bodyLabel != null ? bodyLabel.text : string.Empty;

        public void Configure(
            GameObject root,
            Text title,
            Text body,
            Button firstChoice,
            Text firstChoiceText,
            Button secondChoice,
            Text secondChoiceText,
            Button close,
            Func<DreamStorySnapshot> providePendingSnapshot,
            Func<string, DreamStorySnapshot> provideRecallSnapshot,
            Func<string, string, DreamStoryChoiceResult> resolveChoice,
            Action<bool> onBlockingChanged = null,
            Func<IReadOnlyList<DreamStoryRecallEntry>> provideRecallIndex = null,
            Button previousRecall = null,
            Button nextRecall = null)
        {
            NotifyBlocking(false);
            UnbindButtons();
            overlayRoot = root;
            titleLabel = title;
            bodyLabel = body;
            firstChoiceButton = firstChoice;
            firstChoiceLabel = firstChoiceText;
            secondChoiceButton = secondChoice;
            secondChoiceLabel = secondChoiceText;
            closeButton = close;
            pendingSnapshotProvider = providePendingSnapshot;
            recallSnapshotProvider = provideRecallSnapshot;
            recallIndexProvider = provideRecallIndex;
            choiceCallback = resolveChoice;
            blockingChanged = onBlockingChanged;
            previousRecallButton = previousRecall;
            nextRecallButton = nextRecall;
            configured = overlayRoot != null
                && firstChoiceButton != null
                && secondChoiceButton != null
                && closeButton != null
                && pendingSnapshotProvider != null
                && recallSnapshotProvider != null
                && choiceCallback != null;
            BindButtons();
            Close();
        }

        public bool Open()
        {
            return OpenPending() || OpenLatestRecall();
        }

        public bool OpenPending()
        {
            if (!configured)
            {
                Close();
                return false;
            }

            if (IsOpen && showingResult)
            {
                overlayRoot.transform.SetAsLastSibling();
                return true;
            }

            return ShowSnapshot(pendingSnapshotProvider());
        }

        public bool OpenRecall(string episodeId)
        {
            if (!configured || string.IsNullOrWhiteSpace(episodeId))
            {
                Close();
                return false;
            }

            return ShowSnapshot(recallSnapshotProvider(episodeId));
        }

        public bool OpenLatestRecall()
        {
            var entries = recallIndexProvider?.Invoke();
            return entries != null && entries.Count > 0
                && OpenRecall(entries[entries.Count - 1].EpisodeId);
        }

        public void ShowPreviousRecall()
        {
            StepRecall(-1);
        }

        public void ShowNextRecall()
        {
            StepRecall(1);
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

        private bool ShowSnapshot(DreamStorySnapshot snapshot)
        {
            if (snapshot == null
                || !snapshot.Visible
                || (!snapshot.IsRecall && !snapshot.CanChoose))
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
            RefreshRecallNavigation();
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

            var episodeId = displayedSnapshot.EpisodeId;
            var choiceId = displayedSnapshot.Choices[choiceIndex].Id;
            choicePending = true;
            DreamStoryChoiceResult result;
            try
            {
                result = choiceCallback(episodeId, choiceId);
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
            SetChoiceVisible(false);
            SetRecallNavigationVisible(false);
        }

        private void RenderSnapshot(DreamStorySnapshot snapshot)
        {
            SetText(titleLabel, snapshot.Title);
            if (snapshot.IsRecall)
            {
                var recallBody = string.IsNullOrWhiteSpace(snapshot.ResultMessage)
                    ? snapshot.Body
                    : snapshot.Body + "\n\n그때의 선택\n" + snapshot.ResultMessage;
                SetText(bodyLabel, recallBody);
                SetChoiceVisible(false);
                RefreshRecallNavigation();
                return;
            }

            SetText(bodyLabel, snapshot.Body);
            SetText(firstChoiceLabel, snapshot.Choices[0].Label);
            SetText(secondChoiceLabel, snapshot.Choices[1].Label);
            SetChoiceVisible(true);
            SetRecallNavigationVisible(false);
        }

        private void ClearPresentation()
        {
            SetText(titleLabel, string.Empty);
            SetText(bodyLabel, string.Empty);
            SetText(firstChoiceLabel, string.Empty);
            SetText(secondChoiceLabel, string.Empty);
            SetChoiceVisible(false);
            SetRecallNavigationVisible(false);
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
            if (previousRecallButton != null)
            {
                previousRecallButton.onClick.RemoveListener(ShowPreviousRecall);
                previousRecallButton.onClick.AddListener(ShowPreviousRecall);
            }

            if (nextRecallButton != null)
            {
                nextRecallButton.onClick.RemoveListener(ShowNextRecall);
                nextRecallButton.onClick.AddListener(ShowNextRecall);
            }
        }

        private void UnbindButtons()
        {
            firstChoiceButton?.onClick.RemoveListener(ChooseFirst);
            secondChoiceButton?.onClick.RemoveListener(ChooseSecond);
            closeButton?.onClick.RemoveListener(Close);
            previousRecallButton?.onClick.RemoveListener(ShowPreviousRecall);
            nextRecallButton?.onClick.RemoveListener(ShowNextRecall);
        }

        private void StepRecall(int direction)
        {
            if (!ShowingRecall || direction == 0)
            {
                return;
            }

            var entries = recallIndexProvider?.Invoke();
            if (entries == null || entries.Count <= 1)
            {
                return;
            }

            var currentIndex = 0;
            for (var index = 0; index < entries.Count; index += 1)
            {
                if (string.Equals(
                        entries[index]?.EpisodeId,
                        DisplayedEpisodeId,
                        StringComparison.Ordinal))
                {
                    currentIndex = index;
                    break;
                }
            }

            var nextIndex = (currentIndex + direction + entries.Count) % entries.Count;
            OpenRecall(entries[nextIndex].EpisodeId);
        }

        private void RefreshRecallNavigation()
        {
            var entries = ShowingRecall ? recallIndexProvider?.Invoke() : null;
            SetRecallNavigationVisible(entries != null && entries.Count > 1);
        }

        private void SetRecallNavigationVisible(bool visible)
        {
            if (previousRecallButton != null)
            {
                previousRecallButton.gameObject.SetActive(visible);
            }

            if (nextRecallButton != null)
            {
                nextRecallButton.gameObject.SetActive(visible);
            }
        }

        private void SetChoiceVisible(bool visible)
        {
            if (firstChoiceButton != null)
            {
                firstChoiceButton.gameObject.SetActive(visible);
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.gameObject.SetActive(visible);
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

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
