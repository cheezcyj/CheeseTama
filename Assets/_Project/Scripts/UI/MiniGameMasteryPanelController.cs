using System;
using System.Globalization;
using System.Text;
using CheeseTama.Gameplay.MiniGames;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Callback-driven uGUI board. The scene owner supplies the authoritative mastery snapshot;
    /// this component only presents non-economic medals, profile badges, and decorative titles.
    /// Attach the controller outside the overlay so an inactive overlay cannot disable its opener.
    /// </summary>
    public sealed class MiniGameMasteryPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Mini Game Mastery Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text positionText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect contentScrollRect;

        private Func<MiniGameMasteryBoardSnapshot> snapshotProvider;
        private Action<bool> blockingChanged;
        private Action beforeEntryOpen;
        private MiniGameMasteryBoardSnapshot snapshot =
            MiniGameMasteryBoardSnapshot.Empty;
        private int selectedIndex;
        private bool blockingNotified;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool BlocksGameplayInput => IsOpen;
        public int SelectedIndex => selectedIndex;
        public MiniGameMasteryBoardSnapshot Snapshot => snapshot;
        public string SelectedGameId => GetSelectedGame()?.GameId ?? string.Empty;

        public void Configure(
            GameObject root,
            Button opener,
            Text headerLabel,
            Text summaryLabel,
            Text detailLabel,
            Text positionLabel,
            Button previousGameButton,
            Button nextGameButton,
            Button closePanelButton,
            Func<MiniGameMasteryBoardSnapshot> getSnapshot,
            Action<bool> onBlockingChanged = null)
        {
            RemoveButtonListeners();
            ReleaseBlockingNotification();

            overlayRoot = root;
            openButton = opener;
            titleText = headerLabel;
            summaryText = summaryLabel;
            detailText = detailLabel;
            positionText = positionLabel;
            previousButton = previousGameButton;
            nextButton = nextGameButton;
            closeButton = closePanelButton;
            snapshotProvider = getSnapshot;
            blockingChanged = onBlockingChanged;
            selectedIndex = 0;
            contentScrollRect = detailText != null
                ? detailText.GetComponentInParent<ScrollRect>(true)
                : null;

            AddButtonListeners();
            SetText(titleText, "생활놀이 메달 · 실력판");
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            Refresh();
        }

        public bool Open()
        {
            if (overlayRoot == null)
            {
                return false;
            }

            Refresh();
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            ResetScrollToTop();
            NotifyBlocking(true);
            EventSystem.current?.SetSelectedGameObject(
                closeButton != null
                    ? closeButton.gameObject
                    : nextButton?.gameObject);
            return true;
        }

        public void ConfigureEntryNavigation(Action onBeforeOpen)
        {
            beforeEntryOpen = onBeforeOpen;
        }

        public void Close()
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            ReleaseBlockingNotification();
        }

        public void ShowPrevious()
        {
            var count = snapshot.Games.Count;
            if (count <= 0)
            {
                return;
            }

            selectedIndex = (selectedIndex - 1 + count) % count;
            RefreshSelectedGame();
            ResetScrollToTop();
        }

        public void ShowNext()
        {
            var count = snapshot.Games.Count;
            if (count <= 0)
            {
                return;
            }

            selectedIndex = (selectedIndex + 1) % count;
            RefreshSelectedGame();
            ResetScrollToTop();
        }

        public void Refresh()
        {
            var selectedGameId = SelectedGameId;
            snapshot = snapshotProvider?.Invoke() ?? MiniGameMasteryBoardSnapshot.Empty;
            selectedIndex = ResolveSelectionIndex(selectedGameId);

            SetText(summaryText, BuildSummaryText(snapshot));
            RefreshSelectedGame();
        }

        private void OnEnable()
        {
            AddButtonListeners();
            Refresh();
        }

        private void OnDisable()
        {
            ReleaseBlockingNotification();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
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

        private void RefreshSelectedGame()
        {
            var game = GetSelectedGame();
            var count = snapshot.Games.Count;
            SetText(
                positionText,
                count > 0
                    ? $"생활놀이 {selectedIndex + 1} / {count}"
                    : "생활놀이 실력");
            SetText(
                detailText,
                game != null
                    ? BuildGameDetailText(game)
                    : "표시할 생활놀이 기록이 없어요.");

            var hasMultiple = count > 1;
            SetInteractable(previousButton, hasMultiple);
            SetInteractable(nextButton, hasMultiple);
        }

        private MiniGameMasteryProgressSnapshot GetSelectedGame()
        {
            return selectedIndex >= 0 && selectedIndex < snapshot.Games.Count
                ? snapshot.Games[selectedIndex]
                : null;
        }

        private void ResetScrollToTop()
        {
            if (contentScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            contentScrollRect.StopMovement();
            contentScrollRect.verticalNormalizedPosition = 1f;
        }

        private int ResolveSelectionIndex(string preferredGameId)
        {
            if (!string.IsNullOrEmpty(preferredGameId))
            {
                for (var index = 0; index < snapshot.Games.Count; index += 1)
                {
                    if (string.Equals(
                            snapshot.Games[index].GameId,
                            preferredGameId,
                            StringComparison.Ordinal))
                    {
                        return index;
                    }
                }
            }

            return snapshot.Games.Count == 0
                ? 0
                : Math.Max(0, Math.Min(selectedIndex, snapshot.Games.Count - 1));
        }

        private void AddButtonListeners()
        {
            Bind(openButton, OpenFromButton);
            Bind(previousButton, ShowPrevious);
            Bind(nextButton, ShowNext);
            Bind(closeButton, Close);
        }

        private void RemoveButtonListeners()
        {
            openButton?.onClick.RemoveListener(OpenFromButton);
            previousButton?.onClick.RemoveListener(ShowPrevious);
            nextButton?.onClick.RemoveListener(ShowNext);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void OpenFromButton()
        {
            if (!IsOpen)
            {
                beforeEntryOpen?.Invoke();
            }

            Open();
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

        private static string BuildSummaryText(MiniGameMasteryBoardSnapshot value)
        {
            var safe = value ?? MiniGameMasteryBoardSnapshot.Empty;
            var summary = $"메달 {safe.MedalCount}/{safe.MaximumMedalCount}"
                + $" · 금메달 {safe.GoldMedalCount}/{safe.MaximumGoldMedalCount}";
            if (safe.AllGold)
            {
                return summary + "\n네 가지 생활놀이의 금메달을 모두 땄어요.";
            }

            return safe.HasAnyScore
                ? summary + "\n최고 기록이 오르면 배지와 장식 칭호가 차례로 열려요."
                : summary + "\n생활놀이 최고 기록을 세워 첫 동메달에 도전해 보세요.";
        }

        private static string BuildGameDetailText(MiniGameMasteryProgressSnapshot game)
        {
            var builder = new StringBuilder();
            builder.Append("<b>");
            builder.Append(EscapeRichText(game.DisplayName));
            builder.AppendLine("</b>");
            builder.Append("최고 기록 ");
            builder.Append(FormatScore(game.HighestScore));
            builder.Append("점 · 도전 ");
            builder.Append(game.TotalSessions.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("회");

            if (game.CurrentAchievement == null)
            {
                builder.AppendLine("현재 메달 없음");
                builder.AppendLine("프로필 배지와 장식 칭호는 첫 동메달부터 열려요.");
            }
            else
            {
                builder.Append("현재 ");
                builder.Append(game.CurrentAchievement.MedalDisplayName);
                builder.AppendLine("메달");
                builder.Append("프로필 배지 · ");
                builder.AppendLine(EscapeRichText(game.CurrentAchievement.BadgeDisplayName));
                builder.Append("장식 칭호 · 「");
                builder.Append(EscapeRichText(game.CurrentAchievement.TitleDisplayName));
                builder.AppendLine("」");
            }

            builder.AppendLine();
            if (game.IsGold)
            {
                builder.Append("금메달 기준 ");
                builder.Append(FormatScore(game.GoldScoreThreshold));
                builder.AppendLine("점 달성");
                builder.Append("이 종목의 배지와 칭호를 모두 모았어요.");
            }
            else
            {
                builder.Append("다음 기준 · ");
                builder.Append(MiniGameMasterySystem.GetMedalDisplayName(game.NextMedal));
                builder.Append("메달 ");
                builder.Append(FormatScore(game.NextScoreThreshold));
                builder.AppendLine("점");
                builder.Append(FormatScore(game.HighestScore));
                builder.Append(" / ");
                builder.Append(FormatScore(game.NextScoreThreshold));
                builder.Append("점  (");
                builder.Append(game.NextProgressPercent.ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("%)");
                builder.Append(FormatScore(game.ScoreRemaining));
                builder.Append("점 더 기록하면 다음 배지와 칭호가 열려요.");
            }

            return builder.ToString();
        }

        private static string FormatScore(int value)
        {
            return Math.Max(0, value).ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string EscapeRichText(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
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

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label == null)
            {
                return;
            }

            label.text = value ?? string.Empty;
            AccessibilityRuntime.ApplyCurrent(label);
        }
    }
}
