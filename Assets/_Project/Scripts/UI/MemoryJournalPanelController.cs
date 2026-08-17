using System;
using System.Text;
using CheeseTama.Gameplay.Memories;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Callback-driven uGUI view. The scene owner supplies the save object and persistence callback,
    /// so this panel does not depend on GameManager initialization order.
    /// </summary>
    public sealed class MemoryJournalPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text unreadCountText;
        [SerializeField] private Text entriesText;
        [SerializeField] private Text emptyStateText;
        [SerializeField] private Button markAllReadButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private readonly MemoryJournalSystem journalSystem = new MemoryJournalSystem();
        private MemoryJournalSaveData boundJournal;
        private Func<MemoryJournalSaveData> journalProvider;
        private Action<MemoryJournalSaveData> persistenceRequested;
        private Func<string, bool> hiddenUnlockResolver;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public int UnreadCount => journalSystem.CountUnread(boundJournal);

        public void Configure(
            GameObject root,
            Text headerLabel,
            Text unreadLabel,
            Text journalEntriesLabel,
            Text emptyLabel,
            Button markReadButton,
            Button closePanelButton,
            TopMenuController menuController = null,
            BottomActionBarController actionBarController = null,
            DevPanelController developerPanelController = null)
        {
            RestoreControls();
            RemoveButtonListeners();
            overlayRoot = root;
            titleText = headerLabel;
            unreadCountText = unreadLabel;
            entriesText = journalEntriesLabel;
            emptyStateText = emptyLabel;
            markAllReadButton = markReadButton;
            closeButton = closePanelButton;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            AddButtonListeners();

            if (titleText != null)
            {
                titleText.text = "치즈타마 추억일기";
            }

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            Refresh();
        }

        public void Bind(
            MemoryJournalSaveData journal,
            Action<MemoryJournalSaveData> saveRequested = null,
            Func<string, bool> unlockResolver = null)
        {
            journalProvider = null;
            boundJournal = journal;
            persistenceRequested = saveRequested;
            hiddenUnlockResolver = unlockResolver;
            boundJournal?.EnsureRuntimeDefaults();
            Refresh();
        }

        public void BindProvider(
            Func<MemoryJournalSaveData> getJournal,
            Action<MemoryJournalSaveData> saveRequested = null,
            Func<string, bool> unlockResolver = null)
        {
            journalProvider = getJournal;
            persistenceRequested = saveRequested;
            hiddenUnlockResolver = unlockResolver;
            Refresh();
        }

        public void Open()
        {
            if (overlayRoot == null)
            {
                return;
            }

            UnityEngine.Object.FindFirstObjectByType<CheeseTamaProfileMenuController>()?.CloseForChildNavigation();
            Refresh();
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            SuspendControls();
            EventSystem.current?.SetSelectedGameObject(
                closeButton != null ? closeButton.gameObject : markAllReadButton?.gameObject);
        }

        public void Close()
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            RestoreControls();
        }

        public bool MarkRead(string memoryId)
        {
            if (!journalSystem.TryMarkRead(boundJournal, memoryId))
            {
                return false;
            }

            persistenceRequested?.Invoke(boundJournal);
            Refresh();
            return true;
        }

        public void MarkAllRead()
        {
            if (journalSystem.MarkAllRead(boundJournal) <= 0)
            {
                return;
            }

            persistenceRequested?.Invoke(boundJournal);
            Refresh();
        }

        public void Refresh()
        {
            if (journalProvider != null)
            {
                boundJournal = journalProvider();
                boundJournal?.EnsureRuntimeDefaults();
            }

            var memories = journalSystem.GetNewestFirst(boundJournal, hiddenUnlockResolver);
            var unread = journalSystem.CountUnread(boundJournal);

            if (unreadCountText != null)
            {
                unreadCountText.text = unread > 0 ? $"새 추억 {unread}" : "모두 읽음";
            }

            if (markAllReadButton != null)
            {
                markAllReadButton.interactable = unread > 0;
            }

            var hasEntries = memories.Count > 0;
            if (entriesText != null)
            {
                entriesText.gameObject.SetActive(hasEntries);
                entriesText.text = hasEntries ? BuildEntryText(memories) : string.Empty;
            }

            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(!hasEntries);
                emptyStateText.text = "아직 기록된 추억이 없어요.\n함께 돌보고 놀아주며 첫 장을 채워보세요.";
            }
        }

        private void OnEnable()
        {
            AddButtonListeners();
            Refresh();
        }

        private void OnDisable()
        {
            RestoreControls();
        }

        private void Update()
        {
            if (IsOpen && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        private void AddButtonListeners()
        {
            if (markAllReadButton != null)
            {
                markAllReadButton.onClick.RemoveListener(MarkAllRead);
                markAllReadButton.onClick.AddListener(MarkAllRead);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void RemoveButtonListeners()
        {
            if (markAllReadButton != null)
            {
                markAllReadButton.onClick.RemoveListener(MarkAllRead);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        private static string BuildEntryText(System.Collections.Generic.IReadOnlyList<MemoryJournalPresentation> memories)
        {
            var builder = new StringBuilder(memories.Count * 120);
            for (var index = 0; index < memories.Count; index += 1)
            {
                var memory = memories[index];
                if (memory == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("\n\n");
                }

                builder.Append(memory.Unread ? "<color=#E49A3A>●</color> " : "○ ");
                builder.Append(EscapeRichText(FormatDate(memory.DateKey)));
                builder.Append(" · ");
                builder.Append(EscapeRichText(ResolveKindLabel(memory)));
                builder.Append("\n<b>");
                builder.Append(EscapeRichText(memory.Title));
                builder.Append("</b>\n“");
                builder.Append(EscapeRichText(memory.Quote));
                builder.Append('”');
            }

            return builder.ToString();
        }

        private static string ResolveKindLabel(MemoryJournalPresentation memory)
        {
            if (memory.IsMasked)
            {
                return "비밀";
            }

            return memory.Kind switch
            {
                MemoryJournalKind.Return => "귀환",
                MemoryJournalKind.Growth => "성장",
                MemoryJournalKind.Evolution => "진화",
                MemoryJournalKind.Story => "이야기",
                _ => "돌봄"
            };
        }

        private static string FormatDate(string dateKey)
        {
            return string.IsNullOrWhiteSpace(dateKey) ? "날짜 미상" : dateKey.Replace('-', '.');
        }

        private static string EscapeRichText(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            actionBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (bottomActionBarController != null) bottomActionBarController.enabled = false;
            if (devPanelController != null) devPanelController.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = topMenuWasEnabled;
            if (bottomActionBarController != null) bottomActionBarController.enabled = actionBarWasEnabled;
            if (devPanelController != null) devPanelController.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }
    }
}
