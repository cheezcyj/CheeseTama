using System;
using System.Globalization;
using System.Text;
using CheeseTama.Gameplay.Records;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Callback-driven uGUI view for the public life-record album. The scene owner
    /// supplies the authoritative snapshot and decides how gameplay blocking is applied.
    /// </summary>
    public sealed class LifeRecordsPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Life Records Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text overviewText;
        [SerializeField] private Text episodeText;
        [SerializeField] private Text episodePositionText;
        [SerializeField] private Button previousEpisodeButton;
        [SerializeField] private Button nextEpisodeButton;
        [SerializeField] private Button closeButton;

        private Func<LifeRecordsSnapshot> snapshotProvider;
        private Action closed;
        private Action<bool> blockingChanged;
        private LifeRecordsSnapshot snapshot = LifeRecordsSnapshot.Empty;
        private int selectedEpisodeIndex;
        private bool blockingNotified;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool BlocksGameplayInput => IsOpen;
        public int SelectedEpisodeIndex => selectedEpisodeIndex;
        public LifeRecordsSnapshot Snapshot => snapshot;

        public void Configure(
            GameObject root,
            Text headerLabel,
            Text albumOverviewLabel,
            Text episodeDetailLabel,
            Text episodePositionLabel,
            Button previousButton,
            Button nextButton,
            Button closePanelButton,
            Func<LifeRecordsSnapshot> getSnapshot,
            Action onClosed = null,
            Action<bool> onBlockingChanged = null)
        {
            RemoveButtonListeners();
            ReleaseBlockingNotification();

            overlayRoot = root;
            titleText = headerLabel;
            overviewText = albumOverviewLabel;
            episodeText = episodeDetailLabel;
            episodePositionText = episodePositionLabel;
            previousEpisodeButton = previousButton;
            nextEpisodeButton = nextButton;
            closeButton = closePanelButton;
            snapshotProvider = getSnapshot;
            closed = onClosed;
            blockingChanged = onBlockingChanged;
            selectedEpisodeIndex = 0;

            AddButtonListeners();
            if (titleText != null)
            {
                titleText.text = "생활 기록 앨범";
                AccessibilityRuntime.ApplyCurrent(titleText);
            }

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
            NotifyBlocking(true);
            EventSystem.current?.SetSelectedGameObject(
                closeButton != null
                    ? closeButton.gameObject
                    : nextEpisodeButton?.gameObject);
            return true;
        }

        public void Close()
        {
            CloseInternal(true);
        }

        public void Refresh()
        {
            var selectedEpisodeId = GetSelectedEpisode()?.EpisodeId ?? string.Empty;
            snapshot = snapshotProvider?.Invoke() ?? LifeRecordsSnapshot.Empty;
            selectedEpisodeIndex = ResolveSelectionIndex(selectedEpisodeId);

            if (overviewText != null)
            {
                overviewText.text = BuildOverviewText(snapshot);
                AccessibilityRuntime.ApplyCurrent(overviewText);
            }

            RefreshEpisode();
        }

        public void ShowPreviousEpisode()
        {
            var count = snapshot.CompletedEpisodes.Count;
            if (count <= 0)
            {
                return;
            }

            selectedEpisodeIndex = (selectedEpisodeIndex - 1 + count) % count;
            RefreshEpisode();
        }

        public void ShowNextEpisode()
        {
            var count = snapshot.CompletedEpisodes.Count;
            if (count <= 0)
            {
                return;
            }

            selectedEpisodeIndex = (selectedEpisodeIndex + 1) % count;
            RefreshEpisode();
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

        private void AddButtonListeners()
        {
            if (previousEpisodeButton != null)
            {
                previousEpisodeButton.onClick.RemoveListener(ShowPreviousEpisode);
                previousEpisodeButton.onClick.AddListener(ShowPreviousEpisode);
            }

            if (nextEpisodeButton != null)
            {
                nextEpisodeButton.onClick.RemoveListener(ShowNextEpisode);
                nextEpisodeButton.onClick.AddListener(ShowNextEpisode);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void RemoveButtonListeners()
        {
            if (previousEpisodeButton != null)
            {
                previousEpisodeButton.onClick.RemoveListener(ShowPreviousEpisode);
            }

            if (nextEpisodeButton != null)
            {
                nextEpisodeButton.onClick.RemoveListener(ShowNextEpisode);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        private void CloseInternal(bool notifyClosed)
        {
            var wasOpen = IsOpen;
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
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

        private int ResolveSelectionIndex(string preferredEpisodeId)
        {
            var episodes = snapshot.CompletedEpisodes;
            if (!string.IsNullOrEmpty(preferredEpisodeId))
            {
                for (var index = 0; index < episodes.Count; index += 1)
                {
                    if (string.Equals(
                            episodes[index]?.EpisodeId,
                            preferredEpisodeId,
                            StringComparison.Ordinal))
                    {
                        return index;
                    }
                }
            }

            return episodes.Count == 0
                ? 0
                : Math.Max(0, Math.Min(selectedEpisodeIndex, episodes.Count - 1));
        }

        private NpcEpisodeLifeRecord GetSelectedEpisode()
        {
            return selectedEpisodeIndex >= 0
                && selectedEpisodeIndex < snapshot.CompletedEpisodes.Count
                ? snapshot.CompletedEpisodes[selectedEpisodeIndex]
                : null;
        }

        private void RefreshEpisode()
        {
            var count = snapshot.CompletedEpisodes.Count;
            var hasMultipleEpisodes = count > 1;
            if (previousEpisodeButton != null)
            {
                previousEpisodeButton.interactable = hasMultipleEpisodes;
            }

            if (nextEpisodeButton != null)
            {
                nextEpisodeButton.interactable = hasMultipleEpisodes;
            }

            if (episodePositionText != null)
            {
                episodePositionText.text = count > 0
                    ? $"완료 에피소드 {selectedEpisodeIndex + 1} / {count}"
                    : "완료 에피소드";
                AccessibilityRuntime.ApplyCurrent(episodePositionText);
            }

            if (episodeText != null)
            {
                var episode = GetSelectedEpisode();
                episodeText.text = episode == null
                    ? "아직 다시 볼 수 있는 NPC 에피소드가 없어요."
                    : BuildEpisodeText(episode);
                AccessibilityRuntime.ApplyCurrent(episodeText);
            }
        }

        private static string BuildOverviewText(LifeRecordsSnapshot value)
        {
            var builder = new StringBuilder(320);
            builder.Append("<b>말랑 점프</b>\n");
            if (value.BouncyJump.HasPlayed)
            {
                builder.Append("최고 ");
                builder.Append(value.BouncyJump.HighestScore);
                builder.Append("점 · ");
                builder.Append(value.BouncyJump.TotalSessions);
                builder.Append("회 플레이 · 성공 ");
                builder.Append(value.BouncyJump.TotalSuccesses);
                builder.Append("회");
            }
            else
            {
                builder.Append("아직 플레이 기록이 없어요.");
            }

            builder.Append("\n\n<b>수면 이력</b>\n");
            if (value.HasRecentSleep)
            {
                var visibleCount = Math.Min(3, value.Sleeps.Count);
                for (var index = 0; index < visibleCount; index += 1)
                {
                    if (index > 0)
                    {
                        builder.Append('\n');
                    }

                    var sleep = value.Sleeps[index];
                    builder.Append("• ");
                    builder.Append(EscapeRichText(FormatDate(sleep.CompletedAtIso)));
                    builder.Append(" · ");
                    builder.Append(sleep.WasEarlyWake ? "일찍 일어남" : "예약 수면 완료");
                    builder.Append(" · ");
                    builder.Append(FormatDuration(sleep.ElapsedMinutes));
                    if (index == 0)
                    {
                        builder.Append(" · 졸림 ");
                        builder.Append(FormatSigned(sleep.SleepinessDelta));
                    }
                }

                if (value.Sleeps.Count > visibleCount)
                {
                    builder.Append("\n외 ");
                    builder.Append(value.Sleeps.Count - visibleCount);
                    builder.Append("건");
                }
            }
            else
            {
                builder.Append("아직 완료한 수면 기록이 없어요.");
            }

            builder.Append("\n\n<b>받은 기념품</b>\n");
            if (value.Keepsakes.Count == 0)
            {
                builder.Append("아직 받은 기념품이 없어요.");
            }
            else
            {
                for (var index = 0; index < value.Keepsakes.Count; index += 1)
                {
                    if (index > 0)
                    {
                        builder.Append('\n');
                    }

                    var keepsake = value.Keepsakes[index];
                    builder.Append("• ");
                    builder.Append(EscapeRichText(keepsake.Title));
                    if (!string.IsNullOrEmpty(keepsake.NpcDisplayName))
                    {
                        builder.Append(" · ");
                        builder.Append(EscapeRichText(keepsake.NpcDisplayName));
                    }
                }
            }

            return builder.ToString();
        }

        private static string BuildEpisodeText(NpcEpisodeLifeRecord episode)
        {
            var builder = new StringBuilder(220);
            if (!string.IsNullOrEmpty(episode.NpcDisplayName))
            {
                builder.Append(EscapeRichText(episode.NpcDisplayName));
                builder.Append("\n");
            }

            builder.Append("<b>");
            builder.Append(EscapeRichText(episode.Title));
            builder.Append("</b>");
            if (!string.IsNullOrEmpty(episode.CompletedAtIso))
            {
                builder.Append(" · ");
                builder.Append(EscapeRichText(FormatDate(episode.CompletedAtIso)));
            }

            if (!string.IsNullOrEmpty(episode.Description))
            {
                builder.Append("\n");
                builder.Append(EscapeRichText(episode.Description));
            }

            if (episode.HasChoiceRecord)
            {
                builder.Append("\n\n선택 · ");
                builder.Append(EscapeRichText(episode.ChoiceLabel));
                if (!string.IsNullOrEmpty(episode.ResultMessage))
                {
                    builder.Append("\n");
                    builder.Append(EscapeRichText(episode.ResultMessage));
                }
            }

            return builder.ToString();
        }

        private static string FormatDate(string value)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                ? parsed.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)
                : "날짜 미상";
        }

        private static string FormatDuration(int elapsedMinutes)
        {
            var hours = Math.Max(0, elapsedMinutes) / 60;
            var minutes = Math.Max(0, elapsedMinutes) % 60;
            if (hours <= 0)
            {
                return $"{minutes}분";
            }

            return minutes > 0 ? $"{hours}시간 {minutes}분" : $"{hours}시간";
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString(CultureInfo.InvariantCulture);
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
