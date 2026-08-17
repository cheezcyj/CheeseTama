using CheeseTama.Core;
using CheeseTama.Data;
using CheeseTama.Gameplay.Growth;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Owns the compact profile launcher in the top HUD and routes the five
    /// personal/journey entry points through one modal menu.
    /// </summary>
    public sealed class CheeseTamaProfileMenuController : MonoBehaviour
    {
        public const string OverlayObjectName = "CheeseTama Profile Overlay";

        private static readonly string[] BlockingOverlayNames =
        {
            "New Game Setup Overlay",
            "First Meeting Onboarding Overlay",
            "Save Recovery Notice Overlay",
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            "Milk Drop Catch Overlay",
            "Bouncy Jump Overlay",
            "Play Choice Overlay",
            "Cleaning Mini Game Overlay",
            "Care Event Overlay",
            "Cheese Star Delivery Overlay",
            "First Day Journey Overlay",
            "Growth Journey Overlay",
            "Memory Journal Overlay",
            "Bond Status Overlay",
            "Star Legacy Overlay",
            "Hidden Career Card Overlay",
            "Fantasy Powder Hidden Recipe Overlay",
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Decorate Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel"
            ,InputBindingsPanelController.OverlayObjectName
            ,"Milk Blending Overlay"
            ,CookingChoicePanelController.OverlayObjectName
            ,NpcVisitCardController.OverlayObjectName
            ,SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button profileButton;
        [SerializeField] private Image profileImage;
        [SerializeField] private Text profileNameText;
        [SerializeField] private Text profileDetailText;
        [SerializeField] private Button firstDayJourneyButton;
        [SerializeField] private Button growthJourneyButton;
        [SerializeField] private Button memoryJournalButton;
        [SerializeField] private Button bondStatusButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private CheeseTamaGrowthVisualSet growthVisualSet;
        private bool configured;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;
        private float refreshTimer;
        private GameObject previouslySelectedObject;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;

        public void Configure(
            GameObject root,
            Button openProfileButton,
            Image portraitImage,
            Text nameLabel,
            Text detailLabel,
            Button firstDayButton,
            Button growthButton,
            Button memoryButton,
            Button bondButton,
            Button changeNameButton,
            Button closeProfileButton,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            RestoreControls();
            UnbindButtons();

            overlayRoot = root;
            profileButton = openProfileButton;
            profileImage = portraitImage;
            profileNameText = nameLabel;
            profileDetailText = detailLabel;
            firstDayJourneyButton = firstDayButton;
            growthJourneyButton = growthButton;
            memoryJournalButton = memoryButton;
            bondStatusButton = bondButton;
            renameButton = changeNameButton;
            closeButton = closeProfileButton;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = overlayRoot != null && profileButton != null && closeButton != null;

            BindButtons();
            RefreshProfile();
            Close();
        }

        private void OnEnable()
        {
            if (configured)
            {
                BindButtons();
                RefreshProfile();
            }
        }

        private void OnDisable()
        {
            UnbindButtons();
            RestoreControls();
        }

        private void Update()
        {
            if (!configured || !Application.isPlaying)
            {
                return;
            }

            if (IsOpen && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.5f;
                RefreshProfile();
            }
        }

        public void Open()
        {
            if (!configured || !Application.isPlaying || IsAnotherModalBlocking())
            {
                return;
            }

            previouslySelectedObject = EventSystem.current?.currentSelectedGameObject;
            RefreshProfile();
            SuspendControls();
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(ResolveFirstSelectable());
        }

        public void Close()
        {
            CloseInternal(true);
        }

        /// <summary>
        /// Child panels call this immediately before opening so each controller
        /// captures the restored top/bottom control state instead of nesting it.
        /// </summary>
        public void CloseForChildNavigation()
        {
            CloseInternal(false);
        }

        public void RefreshProfile()
        {
            var tama = GameManager.Instance?.CurrentTama;
            if (profileNameText != null)
            {
                profileNameText.text = tama != null && !string.IsNullOrWhiteSpace(tama.name)
                    ? tama.name
                    : "CheeseTama";
            }

            if (profileDetailText != null)
            {
                var stage = CheeseTamaGrowthStageCatalog.Resolve(tama);
                var stageName = ResolveStageDisplayName(stage);
                profileDetailText.text = tama != null
                    ? $"Lv. {Mathf.Max(1, tama.level)} · {stageName}"
                    : stageName;
            }

            if (profileImage == null)
            {
                return;
            }

            growthVisualSet ??= Resources.Load<CheeseTamaGrowthVisualSet>("CheeseTamaGrowthVisualSet");
            var portrait = growthVisualSet != null
                ? growthVisualSet.GetThumbnail(CheeseTamaGrowthStageCatalog.Resolve(tama))
                : null;
            profileImage.sprite = portrait;
            profileImage.color = portrait != null ? Color.white : new Color(1f, 0.84f, 0.36f, 1f);
            profileImage.preserveAspect = true;
            profileImage.raycastTarget = false;
        }

        private void BindButtons()
        {
            UnbindButtons();
            profileButton?.onClick.AddListener(Open);
            closeButton?.onClick.AddListener(Close);
        }

        private void UnbindButtons()
        {
            profileButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void CloseInternal(bool restoreSelection)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            RestoreControls();
            if (restoreSelection
                && EventSystem.current != null
                && previouslySelectedObject != null
                && previouslySelectedObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previouslySelectedObject);
            }

            previouslySelectedObject = null;
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
                var devPanel = transform.Find("Dev Panel");
                if (devPanel != null)
                {
                    devPanel.gameObject.SetActive(false);
                }

                devPanelController.enabled = false;
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
                bottomActionBarController.enabled = actionBarWasEnabled;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = devPanelWasEnabled;
            }

            controlsSuspended = false;
        }

        private bool IsAnotherModalBlocking()
        {
            var container = overlayRoot != null ? overlayRoot.transform.parent : transform;
            if (container == null)
            {
                return false;
            }

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

        private GameObject ResolveFirstSelectable()
        {
            var candidates = new[]
            {
                firstDayJourneyButton,
                growthJourneyButton,
                memoryJournalButton,
                bondStatusButton,
                renameButton,
                closeButton
            };
            for (var index = 0; index < candidates.Length; index += 1)
            {
                var candidate = candidates[index];
                if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.interactable)
                {
                    return candidate.gameObject;
                }
            }

            return closeButton != null ? closeButton.gameObject : null;
        }

        private static string ResolveStageDisplayName(CheeseTamaGrowthStage stage)
        {
            foreach (var definition in CheeseTamaGrowthStageCatalog.All)
            {
                if (definition.Stage == stage)
                {
                    return definition.DisplayName;
                }
            }

            return "치즈타마";
        }
    }
}
