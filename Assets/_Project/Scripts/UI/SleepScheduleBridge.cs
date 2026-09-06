using CheeseTama.Core;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Sleep;
using UnityEngine;

namespace CheeseTama.UI
{
    /// <summary>
    /// Connects the callback-only sleep panel to GameManager and owns the
    /// Milkroom control lease while the full-screen panel is visible.
    /// </summary>
    public sealed class SleepScheduleBridge : MonoBehaviour
    {
        private static readonly string[] BlockingOverlayNames =
        {
            NewGameSetupController.OverlayObjectName,
            "First Meeting Onboarding Overlay",
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            "Care Event Overlay",
            FirstDayJourneyController.OverlayObjectName,
            GrowthJourneyController.OverlayObjectName,
            PlayChoicePanelController.OverlayObjectName,
            BouncyJumpMiniGameController.OverlayObjectName,
            CleaningMiniGameController.OverlayObjectName,
            "Milk Drop Catch Overlay",
            "Cheese Star Delivery Overlay",
            "Memory Journal Overlay",
            "Fantasy Powder Overlay",
            SaveRecoveryNoticeController.OverlayObjectName,
            CheeseTamaProfileMenuController.OverlayObjectName,
            InputBindingsPanelController.OverlayObjectName,
            NpcVisitCardController.OverlayObjectName,
            JourneyHubPanelController.OverlayObjectName,
            "Milk Blending Overlay",
            CookingChoicePanelController.OverlayObjectName,
            "Decoration Shop Overlay",
            "Decorate Overlay",
            "Settings Modal",
            "Confirm Reset Dialog",
            "CheeseTama Name Dialog",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel"
        };

        [SerializeField] private SleepSchedulePanelController panelController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController actionBarController;
        [SerializeField] private DevPanelController devPanelController;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private Transform modalContainer;

        private GameManager manager;
        private GameManager subscribedManager;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;

        public bool IsBlockingGameplay => panelController != null
            && panelController.BlocksGameplayInput;

        public void Configure(
            SleepSchedulePanelController controller,
            GameManager gameManager,
            TopMenuController menuController,
            BottomActionBarController bottomController,
            DevPanelController developerPanelController,
            MilkroomUIController uiController,
            CheeseTamaVisualController tamaVisual,
            Transform blockingModalContainer)
        {
            RestoreControls();
            Subscribe(null);
            panelController = controller;
            manager = gameManager;
            topMenuController = menuController;
            actionBarController = bottomController;
            devPanelController = developerPanelController;
            milkroomUi = uiController;
            visualController = tamaVisual;
            modalContainer = blockingModalContainer != null
                ? blockingModalContainer
                : transform;
            Subscribe(gameManager);
            RefreshPresentation();
        }

        public bool Open()
        {
            if (panelController == null || IsAnotherModalActive())
            {
                return false;
            }

            return panelController.Open();
        }

        public SleepScheduleSnapshot GetSnapshot()
        {
            EnsureManager();
            return manager?.GetSleepScheduleSnapshot() ?? default;
        }

        public SleepScheduleStartResult StartSchedule(int hours)
        {
            EnsureManager();
            var result = manager != null
                ? manager.StartSleepSchedule(hours)
                : default;
            if (result.Started)
            {
                visualController?.ReactAction(CheeseTamaVisualAction.Rest);
            }

            RefreshPresentation();
            return result;
        }

        public SleepScheduleWakeResult WakeSchedule()
        {
            EnsureManager();
            var result = manager != null
                ? manager.WakeSleepSchedule()
                : default;
            if (result.Applied)
            {
                visualController?.Bind(manager.CurrentTama);
                visualController?.ReactAction(CheeseTamaVisualAction.Rest);
            }

            RefreshPresentation();
            return result;
        }

        public void SetBlocking(bool blocked)
        {
            if (blocked)
            {
                SuspendControls();
            }
            else
            {
                RestoreControls();
            }
        }

        private void OnEnable()
        {
            Subscribe(manager != null ? manager : GameManager.Instance);
        }

        private void OnDisable()
        {
            panelController?.Close();
            Subscribe(null);
            RestoreControls();
        }

        private void Update()
        {
            if (IsBlockingGameplay
                && GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                panelController.Close();
            }
        }

        private void EnsureManager()
        {
            if (manager == null)
            {
                manager = GameManager.Instance;
            }

            Subscribe(manager);
        }

        private void Subscribe(GameManager target)
        {
            if (subscribedManager == target)
            {
                return;
            }

            if (subscribedManager != null)
            {
                subscribedManager.SaveDataReplaced -= HandleSaveDataReplaced;
                subscribedManager.SleepScheduleChanged -= HandleSleepScheduleChanged;
            }

            subscribedManager = target;
            if (subscribedManager != null && isActiveAndEnabled)
            {
                subscribedManager.SaveDataReplaced += HandleSaveDataReplaced;
                subscribedManager.SleepScheduleChanged += HandleSleepScheduleChanged;
            }
        }

        private void HandleSaveDataReplaced()
        {
            panelController?.Close();
            RefreshPresentation();
        }

        private void HandleSleepScheduleChanged()
        {
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            panelController?.Refresh();
            if (manager?.CurrentSave != null)
            {
                milkroomUi?.Bind(manager.CurrentSave);
                visualController?.Bind(manager.CurrentTama);
            }
        }

        private bool IsAnotherModalActive()
        {
            var container = modalContainer != null ? modalContainer : transform;
            for (var index = 0; index < BlockingOverlayNames.Length; index += 1)
            {
                var candidate = container.Find(BlockingOverlayNames[index]);
                if (candidate != null && candidate.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            actionBarWasEnabled = actionBarController != null && actionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (actionBarController != null) actionBarController.enabled = false;
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
            if (actionBarController != null) actionBarController.enabled = actionBarWasEnabled;
            if (devPanelController != null) devPanelController.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }
    }
}
