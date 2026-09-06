using System;
using CheeseTama.Gameplay.Story;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Renders only caller-provided investigation snapshots. Hotspot input and modal ownership stay
    /// with MilkroomInvestigationInputController so closing, focus loss, and another modal cannot
    /// accidentally advance investigation state.
    /// </summary>
    public sealed class MilkroomInvestigationPanelController : MonoBehaviour
    {
        public const string PanelObjectName = "Milkroom Investigation Panel";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Text firstChoiceLabel;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private Text secondChoiceLabel;
        [SerializeField] private Button closeButton;

        private Func<string, string, MilkroomInvestigationChoiceResult> choiceCallback;
        private Action closeRequested;
        private MilkroomInvestigationSnapshot displayedSnapshot;
        private bool configured;
        private bool showingResult;
        private bool choicePending;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool ShowingResult => showingResult;
        public bool ShowingRecall => displayedSnapshot?.IsRecall == true;
        public string DisplayedEpisodeId => displayedSnapshot?.EpisodeId ?? string.Empty;
        public string RenderedTitle => titleLabel != null ? titleLabel.text : string.Empty;
        public string RenderedBody => bodyLabel != null ? bodyLabel.text : string.Empty;
        public string RenderedProgress => progressLabel != null ? progressLabel.text : string.Empty;
        public string RenderedStatus => statusLabel != null ? statusLabel.text : string.Empty;

        public void Configure(
            GameObject root,
            Text title,
            Text body,
            Text progress,
            Text status,
            Button firstChoice,
            Text firstChoiceText,
            Button secondChoice,
            Text secondChoiceText,
            Button close,
            Func<string, string, MilkroomInvestigationChoiceResult> resolveChoice,
            Action onCloseRequested)
        {
            UnbindButtons();
            panelRoot = root;
            titleLabel = title;
            bodyLabel = body;
            progressLabel = progress;
            statusLabel = status;
            firstChoiceButton = firstChoice;
            firstChoiceLabel = firstChoiceText;
            secondChoiceButton = secondChoice;
            secondChoiceLabel = secondChoiceText;
            closeButton = close;
            choiceCallback = resolveChoice;
            closeRequested = onCloseRequested;
            configured = panelRoot != null
                && firstChoiceButton != null
                && secondChoiceButton != null
                && closeButton != null
                && choiceCallback != null
                && closeRequested != null;
            BindButtons();
            Hide();
        }

        public bool ShowSnapshot(MilkroomInvestigationSnapshot snapshot)
        {
            if (!configured || snapshot == null || !snapshot.Visible)
            {
                Hide();
                return false;
            }

            displayedSnapshot = snapshot;
            showingResult = false;
            choicePending = false;
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            BindButtons();
            RenderSnapshot(snapshot);
            return true;
        }

        public bool ShowInspection(MilkroomInvestigationInspectResult result)
        {
            if (result == null || !ShowSnapshot(result.Snapshot))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(result.ClueTitle))
            {
                SetText(titleLabel, result.ClueTitle);
                SetText(bodyLabel, result.ClueDetail);
            }

            SetText(statusLabel, result.UserMessage);
            return true;
        }

        public void Hide()
        {
            displayedSnapshot = null;
            showingResult = false;
            choicePending = false;
            ClearPresentation();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void RequestClose()
        {
            if (closeRequested != null)
            {
                closeRequested();
                return;
            }

            Hide();
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
            UnbindButtons();
            displayedSnapshot = null;
            showingResult = false;
            choicePending = false;
            ClearPresentation();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            choiceCallback = null;
            closeRequested = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsOpen)
            {
                RequestClose();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && IsOpen)
            {
                RequestClose();
            }
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
            MilkroomInvestigationChoiceResult result;
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
                SetText(statusLabel, "선택을 기록하지 못했어요. 잠시 뒤 다시 시도해 주세요.");
                return;
            }

            showingResult = true;
            displayedSnapshot = null;
            SetText(titleLabel, "시즌 3 프롤로그");
            SetText(bodyLabel, result.UserMessage);
            SetText(progressLabel, "조사 완료");
            SetText(statusLabel, "기억과 도감에 한 번만 기록했어요.");
            SetChoiceVisible(false);
        }

        private void RenderSnapshot(MilkroomInvestigationSnapshot snapshot)
        {
            SetText(titleLabel, snapshot.Title);
            SetText(
                progressLabel,
                snapshot.IsRecall
                    ? "완료한 조사 회상"
                    : $"단서 {snapshot.DiscoveredClueCount}/{snapshot.RequiredClueCount}");

            if (snapshot.IsRecall)
            {
                var recallBody = string.IsNullOrEmpty(snapshot.ResultMessage)
                    ? snapshot.Body
                    : snapshot.Body + "\n\n그때의 선택\n" + snapshot.ResultMessage;
                SetText(bodyLabel, recallBody);
                SetText(statusLabel, "회상에서는 기록과 보상을 다시 적용하지 않아요.");
                SetChoiceVisible(false);
                return;
            }

            SetText(bodyLabel, snapshot.Body);
            if (!snapshot.CanChoose)
            {
                SetText(statusLabel, "창문, 선반, 서랍의 조사 지점을 확인해 주세요.");
                SetChoiceVisible(false);
                return;
            }

            SetText(statusLabel, "세 단서를 모두 찾았어요. 다음 기록 방식을 선택하세요.");
            SetText(firstChoiceLabel, snapshot.Choices[0].Label);
            SetText(secondChoiceLabel, snapshot.Choices[1].Label);
            SetChoiceVisible(true);
        }

        private void ClearPresentation()
        {
            SetText(titleLabel, string.Empty);
            SetText(bodyLabel, string.Empty);
            SetText(progressLabel, string.Empty);
            SetText(statusLabel, string.Empty);
            SetText(firstChoiceLabel, string.Empty);
            SetText(secondChoiceLabel, string.Empty);
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
            closeButton.onClick.RemoveListener(RequestClose);
            closeButton.onClick.AddListener(RequestClose);
        }

        private void UnbindButtons()
        {
            firstChoiceButton?.onClick.RemoveListener(ChooseFirst);
            secondChoiceButton?.onClick.RemoveListener(ChooseSecond);
            closeButton?.onClick.RemoveListener(RequestClose);
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

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
