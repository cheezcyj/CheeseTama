using System;
using CheeseTama.Gameplay.Journey;
using CheeseTama.Save;
using CheeseTama.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class FirstDayJourneyController : MonoBehaviour
    {
        public const string OverlayObjectName = "First Day Journey Overlay";
        public const string CardObjectName = "First Day Journey Card";

        [SerializeField] private GameObject cardRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Text progressText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text[] taskTexts;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;

        private Func<FirstDayJourneySaveData> stateProvider;
        private Action markShownCommand;
        private Func<FirstDayJourneyRewardResult> claimCommand;
        private GameManager manager;
        private TopMenuController topMenu;
        private BottomActionBarController actionBar;
        private DevPanelController devPanel;
        private bool subscribed;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;

        public bool IsOpen => cardRoot != null && cardRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;

        public void Configure(
            GameObject root,
            Button opener,
            Text progressLabel,
            Text statusLabel,
            Text[] taskLabels,
            Button rewardButton,
            Button dismissButton,
            Func<FirstDayJourneySaveData> getState,
            Action markShown,
            Func<FirstDayJourneyRewardResult> claimReward,
            GameManager boundManager = null,
            TopMenuController menuController = null,
            BottomActionBarController actionBarController = null,
            DevPanelController developerPanelController = null)
        {
            Unsubscribe();
            cardRoot = root;
            openButton = opener;
            progressText = progressLabel;
            statusText = statusLabel;
            taskTexts = taskLabels;
            claimButton = rewardButton;
            closeButton = dismissButton;
            stateProvider = getState;
            markShownCommand = markShown;
            claimCommand = claimReward;
            manager = boundManager;
            topMenu = menuController;
            actionBar = actionBarController;
            devPanel = developerPanelController;

            BindButton(openButton, Open);
            BindButton(claimButton, ClaimReward);
            BindButton(closeButton, Close);
            Close();
            Refresh();
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreControls();
        }

        private void Update()
        {
            if (!Application.isPlaying || IsOpen || IsAnotherModalBlocking())
            {
                return;
            }

            var state = stateProvider?.Invoke();
            if (state != null && !state.introShown && IsIntroductionEligible())
            {
                Open();
            }
        }

        public void Open()
        {
            var state = stateProvider?.Invoke();
            if (!ShouldBeAvailable(state))
            {
                return;
            }

            UnityEngine.Object.FindFirstObjectByType<CheeseTamaProfileMenuController>()?.CloseForChildNavigation();
            markShownCommand?.Invoke();
            if (cardRoot != null)
            {
                cardRoot.SetActive(true);
                cardRoot.transform.SetAsLastSibling();
            }

            SuspendControls();
            Refresh();
        }

        public void Close()
        {
            if (cardRoot != null)
            {
                cardRoot.SetActive(false);
            }

            RestoreControls();
        }

        public void Refresh()
        {
            var state = stateProvider?.Invoke();
            var available = ShouldBeAvailable(state);
            if (openButton != null)
            {
                openButton.gameObject.SetActive(available);
            }

            if (!available)
            {
                Close();
                return;
            }

            state.EnsureRuntimeDefaults();
            var completeCount = FirstDayJourneySystem.CountCompletedTasks(state);
            SetText(progressText, $"첫날 여정  {completeCount}/{FirstDayJourneySystem.Tasks.Count}");

            for (var index = 0; index < taskTexts?.Length; index += 1)
            {
                if (index >= FirstDayJourneySystem.Tasks.Count)
                {
                    taskTexts[index]?.gameObject.SetActive(false);
                    continue;
                }

                var task = FirstDayJourneySystem.Tasks[index];
                var complete = state.completedTaskIds.Contains(task.Id);
                SetText(taskTexts[index], $"{(complete ? "✓" : "○")} {task.DisplayName}");
                taskTexts[index].color = complete
                    ? new Color(0.25f, 0.52f, 0.34f)
                    : new Color(0.35f, 0.29f, 0.24f);
            }

            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(state.completed && !state.rewardClaimed);
                claimButton.interactable = state.completed && !state.rewardClaimed;
            }

            SetText(
                statusText,
                state.completed
                    ? "모든 경험을 마쳤어요. 첫날 선물을 받아 보세요."
                    : "정해진 순서 없이 천천히 경험해도 괜찮아요.");
        }

        private void ClaimReward()
        {
            var result = claimCommand != null
                ? claimCommand()
                : default;
            SetText(statusText, result.Message);
            Refresh();
            if (result.Granted)
            {
                Close();
            }
        }

        private static bool ShouldBeAvailable(FirstDayJourneySaveData state)
        {
            return state != null && !state.legacySuppressed && !state.rewardClaimed;
        }

        private bool IsIntroductionEligible()
        {
            var save = manager?.CurrentSave;
            return save?.newGameSetup?.completed == true
                && save.onboarding?.completed == true;
        }

        private bool IsAnotherModalBlocking()
        {
            var container = cardRoot != null ? cardRoot.transform.parent : transform;
            if (container == null)
            {
                return false;
            }

            var blockers = new[]
            {
                NewGameSetupController.OverlayObjectName,
                "First Meeting Onboarding Overlay",
                "Return Summary Overlay",
                "Growth Achievement Overlay",
                "Evolution Achievement Overlay",
                GrowthJourneyController.OverlayObjectName,
                "Milk Drop Catch Overlay",
                BouncyJumpMiniGameController.OverlayObjectName,
                PlayChoicePanelController.OverlayObjectName,
                CleaningMiniGameController.OverlayObjectName,
                "Care Event Overlay",
                "CheeseTama Name Dialog"
                ,"Cheese Star Delivery Overlay"
                ,"Memory Journal Overlay"
                ,"Fantasy Powder Overlay"
                ,SaveRecoveryNoticeController.OverlayObjectName
                ,CheeseTamaProfileMenuController.OverlayObjectName
                ,InputBindingsPanelController.OverlayObjectName
                ,"Milk Blending Overlay"
                ,"Cooking Panel"
                ,CookingChoicePanelController.OverlayObjectName
                ,NpcVisitCardController.OverlayObjectName
                ,SleepSchedulePanelController.OverlayObjectName
            };

            for (var index = 0; index < blockers.Length; index += 1)
            {
                var found = container.Find(blockers[index]);
                if (found != null && found.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled)
            {
                return;
            }

            if (manager != null)
            {
                manager.FirstDayJourneyChanged += Refresh;
                manager.SaveDataReplaced += Refresh;
            }

            if (topMenu != null)
            {
                topMenu.CollectionOpening += HandleCollectionOpening;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (manager != null)
            {
                manager.FirstDayJourneyChanged -= Refresh;
                manager.SaveDataReplaced -= Refresh;
            }

            if (topMenu != null)
            {
                topMenu.CollectionOpening -= HandleCollectionOpening;
            }

            subscribed = false;
        }

        private void HandleCollectionOpening()
        {
            manager?.RecordFirstDayJourneyCollectionOpened();
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenu != null && topMenu.enabled;
            actionBarWasEnabled = actionBar != null && actionBar.enabled;
            devPanelWasEnabled = devPanel != null && devPanel.enabled;
            if (topMenu != null) topMenu.enabled = false;
            if (actionBar != null) actionBar.enabled = false;
            if (devPanel != null) devPanel.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenu != null) topMenu.enabled = topMenuWasEnabled;
            if (actionBar != null) actionBar.enabled = actionBarWasEnabled;
            if (devPanel != null) devPanel.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
