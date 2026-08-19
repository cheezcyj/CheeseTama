using System;
using CheeseTama.Audio;
using CheeseTama.Core;
using CheeseTama.Data;
using CheeseTama.Gameplay.Growth;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class GrowthMilestoneController : MonoBehaviour
    {
        private static readonly string[] BlockingOverlayNames =
        {
            "Milk Drop Catch Overlay",
            "Cleaning Mini Game Overlay",
            "Evolution Achievement Overlay",
            "Decoration Shop Overlay",
            "Care Event Overlay",
            "First Meeting Onboarding Overlay",
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Decorate Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel"
            ,"New Game Setup Overlay"
            ,"Growth Journey Overlay"
            ,"Play Choice Overlay"
            ,"Bouncy Jump Overlay"
            ,FirstDayJourneyController.OverlayObjectName
            ,"Cheese Star Delivery Overlay"
            ,"Memory Journal Overlay"
            ,"Fantasy Powder Overlay"
            ,SaveRecoveryNoticeController.OverlayObjectName
            ,CheeseTamaProfileMenuController.OverlayObjectName
            ,InputBindingsPanelController.OverlayObjectName
            ,"Milk Blending Overlay"
            ,CookingChoicePanelController.OverlayObjectName
            ,NpcVisitCardController.OverlayObjectName
            ,JourneyHubPanelController.OverlayObjectName
            ,SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Image stageThumbnail;
        [SerializeField] private Text titleText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;
        [SerializeField] private CheeseTamaGrowthVisualSet growthVisualSet;

        private GameManager boundManager;
        private bool configured;
        private string displayedMilestoneId = string.Empty;
        private CheeseTamaGrowthStage displayedStage;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;
        private bool milkroomUiWasEnabled;
        private GameObject previouslySelectedObject;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeSelf;

        public void Configure(
            GameObject root,
            Image thumbnail,
            Text titleLabel,
            Text levelLabel,
            Text descriptionLabel,
            Button closeButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController tamaVisual,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            RestoreControls();
            UnbindButton();

            overlayRoot = root;
            stageThumbnail = thumbnail;
            titleText = titleLabel;
            levelText = levelLabel;
            descriptionText = descriptionLabel;
            confirmButton = closeButton;
            milkroomUi = uiController;
            visualController = tamaVisual;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            growthVisualSet ??= Resources.Load<CheeseTamaGrowthVisualSet>("CheeseTamaGrowthVisualSet");
            configured = overlayRoot != null && confirmButton != null;

            displayedMilestoneId = string.Empty;
            BindButton();
            SetOverlayActive(false);
            if (Application.isPlaying)
            {
                BindManager(GameManager.Instance);
                TryShowPendingMilestone();
            }
        }

        private void OnEnable()
        {
            if (!configured)
            {
                return;
            }

            BindButton();
            if (Application.isPlaying)
            {
                BindManager(GameManager.Instance);
                TryShowPendingMilestone();
            }
        }

        private void OnDisable()
        {
            UnbindButton();
            BindManager(null);
            displayedMilestoneId = string.Empty;
            SetOverlayActive(false);
            RestoreControls();
            RestoreSelection();
        }

        private void Update()
        {
            if (!configured || !Application.isPlaying)
            {
                return;
            }

            if (IsBlockingGameplay && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Confirm();
                return;
            }

            if (!IsBlockingGameplay)
            {
                TryShowPendingMilestone();
            }
        }

        public void Confirm()
        {
            if (!IsBlockingGameplay)
            {
                return;
            }

            if (boundManager != null && !string.IsNullOrWhiteSpace(displayedMilestoneId))
            {
                boundManager.AcknowledgeGrowthMilestone(displayedMilestoneId);
            }

            var completedStage = displayedStage;
            displayedMilestoneId = string.Empty;
            SetOverlayActive(false);
            RestoreControls();
            RestoreSelection();

            if (visualController != null && boundManager != null)
            {
                visualController.Bind(boundManager.CurrentTama);
                var hatched = completedStage == CheeseTamaGrowthStage.Hatchling;
                visualController.ReactAction(
                    hatched ? CheeseTamaVisualAction.Hatch : CheeseTamaVisualAction.LevelUp,
                    hatched);
            }
        }

        private void BindManager(GameManager manager)
        {
            if (boundManager == manager)
            {
                return;
            }

            if (boundManager != null)
            {
                boundManager.GrowthMilestoneAvailable -= HandleMilestoneAvailable;
                boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
            }

            boundManager = manager;
            if (boundManager != null)
            {
                boundManager.GrowthMilestoneAvailable += HandleMilestoneAvailable;
                boundManager.SaveDataReplaced += HandleSaveDataReplaced;
            }
        }

        private void HandleMilestoneAvailable(GrowthMilestoneData milestone)
        {
            TryShowPendingMilestone();
        }

        private void HandleSaveDataReplaced()
        {
            if (IsBlockingGameplay
                && (boundManager == null
                    || !boundManager.TryGetPendingGrowthMilestone(out var pending)
                    || pending == null
                    || !string.Equals(pending.id, displayedMilestoneId, StringComparison.Ordinal)))
            {
                displayedMilestoneId = string.Empty;
                SetOverlayActive(false);
                RestoreControls();
                RestoreSelection();
            }

            TryShowPendingMilestone();
        }

        private void TryShowPendingMilestone()
        {
            if (!configured || !Application.isPlaying || IsBlockingGameplay)
            {
                return;
            }

            boundManager ??= GameManager.Instance;
            if (boundManager == null
                || !boundManager.TryGetPendingGrowthMilestone(out var milestone)
                || milestone == null
                || IsAnotherModalBlocking())
            {
                return;
            }

            displayedMilestoneId = milestone.id;
            displayedStage = milestone.stage;
            Populate(milestone);
            previouslySelectedObject = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            SuspendControls();
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            if (EventSystem.current != null && confirmButton != null)
            {
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
            }

            CheeseTamaAudioController.Instance?.PlayReward();
        }

        private bool IsAnotherModalBlocking()
        {
            if (boundManager != null && boundManager.HasPendingReturnSummary)
            {
                return true;
            }

            var modalContainer = overlayRoot != null && overlayRoot.transform.parent != null
                ? overlayRoot.transform.parent
                : transform;
            var returnSummary = modalContainer.Find("Return Summary Overlay");
            if (returnSummary != null && returnSummary.gameObject.activeInHierarchy)
            {
                return true;
            }

            for (var index = 0; index < BlockingOverlayNames.Length; index += 1)
            {
                var blocker = modalContainer.Find(BlockingOverlayNames[index]);
                if (blocker != null && blocker.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void Populate(GrowthMilestoneData milestone)
        {
            var definition = CheeseTamaGrowthStageCatalog.Get(milestone.stage);
            var heading = milestone.stage == CheeseTamaGrowthStage.Hatchling
                ? $"부화 성공!  {definition.DisplayName}"
                : $"{definition.DisplayName} 달성!";
            SetText(titleText, heading);
            SetText(levelText, $"Lv.{Mathf.Max(1, milestone.level)}  ·  새로운 성장 단계");
            SetText(descriptionText, definition.Description);

            growthVisualSet ??= Resources.Load<CheeseTamaGrowthVisualSet>("CheeseTamaGrowthVisualSet");
            if (stageThumbnail != null)
            {
                var sprite = growthVisualSet != null
                    ? growthVisualSet.GetThumbnail(milestone.stage)
                    : null;
                stageThumbnail.sprite = sprite;
                stageThumbnail.type = Image.Type.Simple;
                stageThumbnail.preserveAspect = true;
                stageThumbnail.color = sprite != null
                    ? Color.white
                    : new Color(1f, 0.86f, 0.48f, 0.42f);
                stageThumbnail.gameObject.SetActive(sprite != null);
            }
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomActionBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
            milkroomUiWasEnabled = milkroomUi != null && milkroomUi.enabled;
            if (topMenuController != null)
            {
                topMenuController.enabled = false;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = false;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = false;
            }

            if (milkroomUi != null)
            {
                milkroomUi.enabled = false;
            }

            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null)
            {
                topMenuController.enabled = topMenuWasEnabled;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = bottomActionBarWasEnabled;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = devPanelWasEnabled;
            }

            if (milkroomUi != null)
            {
                milkroomUi.enabled = milkroomUiWasEnabled;
            }

            controlsSuspended = false;
        }

        private void RestoreSelection()
        {
            if (EventSystem.current != null
                && previouslySelectedObject != null
                && previouslySelectedObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previouslySelectedObject);
            }

            previouslySelectedObject = null;
        }

        private void BindButton()
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
        }

        private void UnbindButton()
        {
            confirmButton?.onClick.RemoveListener(Confirm);
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
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
