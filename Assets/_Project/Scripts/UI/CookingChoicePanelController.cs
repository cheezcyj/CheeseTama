using System;
using CheeseTama.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class CookingChoicePanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Cooking Choice Overlay";

        private static readonly string[] BlockingOverlayNames =
        {
            NewGameSetupController.OverlayObjectName,
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            "Care Event Overlay",
            CleaningMiniGameController.OverlayObjectName,
            "Milk Drop Catch Overlay",
            BouncyJumpMiniGameController.OverlayObjectName,
            PlayChoicePanelController.OverlayObjectName,
            GrowthJourneyController.OverlayObjectName,
            "Decoration Shop Overlay",
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Collection Overlay",
            "Decorate Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel",
            FirstDayJourneyController.OverlayObjectName,
            "Cheese Star Delivery Overlay",
            "Memory Journal Overlay",
            "Fantasy Powder Overlay",
            SaveRecoveryNoticeController.OverlayObjectName,
            CheeseTamaProfileMenuController.OverlayObjectName,
            InputBindingsPanelController.OverlayObjectName,
            "Milk Blending Overlay",
            NpcVisitCardController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName,
            StarLegacyPanelController.OverlayObjectName,
            "Bond Status Overlay",
            "Hidden Career Card Overlay"
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button cookingButton;
        [SerializeField] private Button milkBlendingButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController actionBarController;
        [SerializeField] private DevPanelController developerPanelController;

        private Action openCookingAction;
        private Func<bool> openMilkBlendingAction;
        private GameObject previouslySelectedObject;
        private bool configured;
        private bool controlsSuspended;
        private bool previousTopEnabled;
        private bool previousBottomEnabled;
        private bool previousDevEnabled;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;

        public void Configure(
            GameObject root,
            Button openCookingButton,
            Button openMilkBlendingButton,
            Action showCooking,
            Func<bool> showMilkBlending,
            TopMenuController menuController = null,
            BottomActionBarController bottomController = null,
            DevPanelController devController = null)
        {
            UnbindButtons();
            RestoreControls();

            overlayRoot = root;
            cookingButton = openCookingButton;
            milkBlendingButton = openMilkBlendingButton;
            openCookingAction = showCooking;
            openMilkBlendingAction = showMilkBlending;
            topMenuController = menuController;
            actionBarController = bottomController;
            developerPanelController = devController;
            configured = overlayRoot != null
                && cookingButton != null
                && milkBlendingButton != null;

            BindButtons();
            SetActive(false);
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
            SetActive(false);
            RestoreControls();
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

        public bool Open()
        {
            if (!configured || IsOpen || IsAnyOtherModalActive())
            {
                return false;
            }

            previouslySelectedObject = EventSystem.current?.currentSelectedGameObject;
            SetActive(true);
            SuspendControls();
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(cookingButton.gameObject);
            return true;
        }

        public void Close()
        {
            SetActive(false);
            RestoreControls();

            if (EventSystem.current != null
                && previouslySelectedObject != null
                && previouslySelectedObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previouslySelectedObject);
            }

            previouslySelectedObject = null;
        }

        private void OpenCooking()
        {
            if (!IsOpen)
            {
                return;
            }

            Close();
            openCookingAction?.Invoke();
        }

        private void OpenMilkBlending()
        {
            if (!IsOpen)
            {
                return;
            }

            Close();
            openMilkBlendingAction?.Invoke();
        }

        private bool IsAnyOtherModalActive()
        {
            var container = overlayRoot != null && overlayRoot.transform.parent != null
                ? overlayRoot.transform.parent
                : transform;

            var onboardingRoot = container.Find("First Meeting Onboarding Overlay");
            if (onboardingRoot != null && onboardingRoot.gameObject.activeInHierarchy)
            {
                var onboarding = container.GetComponent<FirstMeetingOnboardingController>();
                var save = GameManager.Instance?.CurrentSave?.onboarding;
                if (onboarding == null
                    || onboarding.IsBlockingGameplay && (save == null
                        || save.currentStep != CheeseTama.Save.FirstMeetingOnboardingStep.Care))
                {
                    return true;
                }
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

        private void BindButtons()
        {
            if (cookingButton != null)
            {
                cookingButton.onClick.RemoveListener(OpenCooking);
                cookingButton.onClick.AddListener(OpenCooking);
            }

            if (milkBlendingButton != null)
            {
                milkBlendingButton.onClick.RemoveListener(OpenMilkBlending);
                milkBlendingButton.onClick.AddListener(OpenMilkBlending);
            }

        }

        private void UnbindButtons()
        {
            cookingButton?.onClick.RemoveListener(OpenCooking);
            milkBlendingButton?.onClick.RemoveListener(OpenMilkBlending);
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            previousTopEnabled = topMenuController != null && topMenuController.enabled;
            previousBottomEnabled = actionBarController != null && actionBarController.enabled;
            previousDevEnabled = developerPanelController != null && developerPanelController.enabled;

            if (topMenuController != null) topMenuController.enabled = false;
            if (actionBarController != null) actionBarController.enabled = false;
            if (developerPanelController != null) developerPanelController.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = previousTopEnabled;
            if (actionBarController != null) actionBarController.enabled = previousBottomEnabled;
            if (developerPanelController != null) developerPanelController.enabled = previousDevEnabled;
            controlsSuspended = false;
        }

        private void SetActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }
    }
}
