using CheeseTama.Core;
using CheeseTama.Gameplay.Growth;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class EvolutionMilestoneController : MonoBehaviour
    {
        private static readonly string[] BlockingNames =
        {
            "First Meeting Onboarding Overlay", "Return Summary Overlay", "Growth Achievement Overlay",
            "Milk Drop Catch Overlay", "Cleaning Mini Game Overlay", "Care Event Overlay",
            "CheeseTama Name Dialog", "Settings Modal", "Confirm Reset Dialog", "Decorate Overlay",
            "Decoration Shop Overlay", "Milk Panel", "Cooking Panel", "Snack Panel", "Dev Panel",
            "New Game Setup Overlay", "Growth Journey Overlay", "Play Choice Overlay", "Bouncy Jump Overlay",
            FirstDayJourneyController.OverlayObjectName, "Cheese Star Delivery Overlay",
            "Memory Journal Overlay", "Fantasy Powder Overlay", SaveRecoveryNoticeController.OverlayObjectName,
            CheeseTamaProfileMenuController.OverlayObjectName,
            InputBindingsPanelController.OverlayObjectName, "Milk Blending Overlay", CookingChoicePanelController.OverlayObjectName,
            NpcVisitCardController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private GameManager manager;
        private EvolutionMilestoneData displayed;
        private bool configured;
        private bool controlsSuspended;
        private bool topWasEnabled;
        private bool bottomWasEnabled;
        private bool devWasEnabled;
        private bool uiWasEnabled;

        public bool IsBlockingGameplay => Application.isPlaying && overlayRoot != null && overlayRoot.activeSelf;

        public void Configure(GameObject root, Text title, Text level, Text description, Button confirm,
            MilkroomUIController ui, CheeseTamaVisualController visual, TopMenuController top,
            BottomActionBarController bottom, DevPanelController dev)
        {
            Unbind();
            overlayRoot = root;
            titleText = title;
            levelText = level;
            descriptionText = description;
            confirmButton = confirm;
            milkroomUi = ui;
            visualController = visual;
            topMenuController = top;
            bottomActionBarController = bottom;
            devPanelController = dev;
            configured = overlayRoot != null && confirmButton != null;
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(Confirm);
            overlayRoot.SetActive(false);
            Bind();
            TryShow();
        }

        private void OnEnable() { Bind(); }
        private void OnDisable() { Unbind(); RestoreControls(); }
        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
        }
        private void Update()
        {
            if (!configured || !Application.isPlaying) return;
            if (IsBlockingGameplay && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel)) { Confirm(); return; }
            if (!IsBlockingGameplay) TryShow();
        }

        private void Bind()
        {
            var resolved = GameManager.Instance;
            if (resolved == manager) return;
            Unbind();
            manager = resolved;
            if (manager != null) manager.EvolutionMilestoneAvailable += HandleAvailable;
        }

        private void Unbind()
        {
            if (manager != null) manager.EvolutionMilestoneAvailable -= HandleAvailable;
            manager = null;
        }

        private void HandleAvailable(EvolutionMilestoneData data) { displayed = data; TryShow(); }

        private void TryShow()
        {
            Bind();
            if (manager == null || IsBlockingGameplay || IsAnotherBlocking()) return;
            if (!manager.TryGetPendingEvolutionMilestone(out displayed) || displayed == null) return;
            titleText.text = $"{displayed.result.DisplayName} 진화!";
            levelText.text = $"Lv.{displayed.level} · 일반 진화 달성";
            descriptionText.text = $"{displayed.result.Description}\n\n{displayed.result.TendencyHint}";
            SuspendControls();
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(confirmButton.gameObject);
        }

        private void Confirm()
        {
            if (!IsBlockingGameplay) return;
            manager?.AcknowledgeEvolutionMilestone(displayed?.occurrenceId);
            overlayRoot.SetActive(false);
            RestoreControls();
            if (manager != null)
            {
                milkroomUi?.Bind(manager.CurrentSave);
                visualController?.Bind(manager.CurrentTama);
                visualController?.ReactAction(CheeseTamaVisualAction.LevelUp);
            }
        }

        private bool IsAnotherBlocking()
        {
            var parent = overlayRoot != null ? overlayRoot.transform.parent : transform;
            foreach (var name in BlockingNames)
            {
                var child = parent.Find(name);
                if (child != null && child.gameObject != overlayRoot && child.gameObject.activeInHierarchy) return true;
            }
            return false;
        }

        private void SuspendControls()
        {
            if (controlsSuspended) return;
            topWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
            devWasEnabled = devPanelController != null && devPanelController.enabled;
            uiWasEnabled = milkroomUi != null && milkroomUi.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (bottomActionBarController != null) bottomActionBarController.enabled = false;
            if (devPanelController != null) devPanelController.enabled = false;
            if (milkroomUi != null) milkroomUi.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended) return;
            if (topMenuController != null) topMenuController.enabled = topWasEnabled;
            if (bottomActionBarController != null) bottomActionBarController.enabled = bottomWasEnabled;
            if (devPanelController != null) devPanelController.enabled = devWasEnabled;
            if (milkroomUi != null) milkroomUi.enabled = uiWasEnabled;
            controlsSuspended = false;
        }
    }
}
