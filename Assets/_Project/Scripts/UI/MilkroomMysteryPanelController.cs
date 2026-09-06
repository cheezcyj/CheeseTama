using System;
using CheeseTama.Gameplay.Story;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Presents only the authoritative visible snapshot. Locked chapter metadata is cleared
    /// whenever the panel closes or the provider returns a hidden snapshot.
    /// </summary>
    public sealed class MilkroomMysteryPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Milkroom Mystery Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Text firstChoiceLabel;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private Text secondChoiceLabel;
        [SerializeField] private Button closeButton;

        private Func<MilkroomMysterySnapshot> snapshotProvider;
        private Func<string, string, MilkroomMysteryChoiceResult> choiceCallback;
        private MilkroomMysterySnapshot displayedSnapshot;
        private bool configured;
        private bool showingResult;
        private bool choicePending;
        private Action<bool> blockingChanged;
        private bool blockingActive;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public bool ShowingResult => showingResult;
        public string DisplayedChapterId => displayedSnapshot?.ChapterId ?? string.Empty;
        public string RenderedTitle => titleLabel != null ? titleLabel.text : string.Empty;
        public string RenderedDescription =>
            descriptionLabel != null ? descriptionLabel.text : string.Empty;

        public void Configure(
            GameObject root,
            Text title,
            Text description,
            Button firstChoice,
            Text firstChoiceText,
            Button secondChoice,
            Text secondChoiceText,
            Button close,
            Func<MilkroomMysterySnapshot> provideSnapshot,
            Func<string, string, MilkroomMysteryChoiceResult> resolveChoice,
            Action<bool> onBlockingChanged = null)
        {
            NotifyBlocking(false);
            UnbindButtons();
            overlayRoot = root;
            titleLabel = title;
            descriptionLabel = description;
            firstChoiceButton = firstChoice;
            firstChoiceLabel = firstChoiceText;
            secondChoiceButton = secondChoice;
            secondChoiceLabel = secondChoiceText;
            closeButton = close;
            snapshotProvider = provideSnapshot;
            choiceCallback = resolveChoice;
            blockingChanged = onBlockingChanged;
            configured = overlayRoot != null
                && firstChoiceButton != null
                && secondChoiceButton != null
                && closeButton != null
                && snapshotProvider != null
                && choiceCallback != null;
            BindButtons();
            Close();
        }

        public bool Open()
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

            return Refresh();
        }

        public bool Refresh()
        {
            if (!configured)
            {
                Close();
                return false;
            }

            var snapshot = snapshotProvider();
            if (snapshot == null || !snapshot.Visible || !snapshot.CanChoose)
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
            MilkroomMysteryChoiceResult result;
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
                SetText(descriptionLabel, "선택을 기록하지 못했어요. 잠시 뒤 다시 시도해 주세요.");
                return;
            }

            showingResult = true;
            displayedSnapshot = null;
            SetText(titleLabel, "기억에 남은 선택");
            SetText(descriptionLabel, result.UserMessage);
            SetChoiceVisible(false);
        }

        private void RenderSnapshot(MilkroomMysterySnapshot snapshot)
        {
            SetText(titleLabel, snapshot.Title);
            SetText(descriptionLabel, snapshot.Description);
            SetText(firstChoiceLabel, snapshot.Choices[0].Label);
            SetText(secondChoiceLabel, snapshot.Choices[1].Label);
            SetChoiceVisible(true);
        }

        private void ClearPresentation()
        {
            SetText(titleLabel, string.Empty);
            SetText(descriptionLabel, string.Empty);
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
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        private void UnbindButtons()
        {
            firstChoiceButton?.onClick.RemoveListener(ChooseFirst);
            secondChoiceButton?.onClick.RemoveListener(ChooseSecond);
            closeButton?.onClick.RemoveListener(Close);
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
