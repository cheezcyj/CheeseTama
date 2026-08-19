using CheeseTama.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class PlayChoicePanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Play Choice Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private Button milkDropButton;
        [SerializeField] private Button bouncyJumpButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private MilkDropMiniGameController milkDropController;
        [SerializeField] private BouncyJumpMiniGameController bouncyJumpController;

        private bool configured;
        private GameObject previouslySelectedObject;
        private TopMenuController topMenuController;
        private BottomActionBarController bottomActionBarController;
        private DevPanelController devPanelController;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeSelf;

        public void Configure(
            GameObject root,
            Text statusLabel,
            Button openMilkDropButton,
            Button openBouncyJumpButton,
            Button closePanelButton,
            MilkDropMiniGameController milkDropMiniGame,
            BouncyJumpMiniGameController bouncyJumpMiniGame,
            TopMenuController menuController = null,
            BottomActionBarController actionBarController = null,
            DevPanelController developerPanelController = null)
        {
            UnbindButtons();
            overlayRoot = root;
            statusText = statusLabel;
            milkDropButton = openMilkDropButton;
            bouncyJumpButton = openBouncyJumpButton;
            closeButton = closePanelButton;
            milkDropController = milkDropMiniGame;
            bouncyJumpController = bouncyJumpMiniGame;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = overlayRoot != null
                && milkDropButton != null
                && bouncyJumpButton != null
                && closeButton != null;
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
            RestoreControls();
        }

        private void Update()
        {
            if (IsBlockingGameplay && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        public bool Open()
        {
            if (!configured || !Application.isPlaying || IsAnyModalActive())
            {
                return false;
            }

            previouslySelectedObject = EventSystem.current?.currentSelectedGameObject;
            var manager = GameManager.Instance;
            var rewardStatus = manager?.GetMilkDropMiniGameRewardStatus();
            SetText(rewardStatus.HasValue && rewardStatus.Value.isAvailable
                ? "우유방울 받기는 지금 자원 보상을 받을 수 있어요."
                : "우유방울 받기는 연습할 수 있고, 자원 보상은 30분마다 다시 열려요.");
            SetActive(true);
            SuspendControls();
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(milkDropButton.gameObject);
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

        private void OpenMilkDrop()
        {
            Close();
            milkDropController ??= Object.FindFirstObjectByType<MilkDropMiniGameController>();
            milkDropController?.Open();
        }

        private void OpenBouncyJump()
        {
            Close();
            bouncyJumpController ??= Object.FindFirstObjectByType<BouncyJumpMiniGameController>();
            bouncyJumpController?.Open();
        }

        private bool IsAnyModalActive()
        {
            var parent = overlayRoot != null && overlayRoot.transform.parent != null
                ? overlayRoot.transform.parent
                : transform;
            var onboarding = parent.GetComponent<FirstMeetingOnboardingController>();
            if (onboarding != null && onboarding.IsBlockingGameplay)
            {
                var save = GameManager.Instance?.CurrentSave?.onboarding;
                if (save == null || save.currentStep != CheeseTama.Save.FirstMeetingOnboardingStep.Care)
                {
                    return true;
                }
            }

            var blockers = new[]
            {
                NewGameSetupController.OverlayObjectName, "Return Summary Overlay",
                "Growth Achievement Overlay", "Evolution Achievement Overlay", "Care Event Overlay",
                "Cleaning Mini Game Overlay", "Milk Drop Catch Overlay", "Bouncy Jump Overlay",
                "Growth Journey Overlay", "Decoration Shop Overlay", "CheeseTama Name Dialog",
                "Settings Modal", "Confirm Reset Dialog", "Decorate Overlay", "Milk Panel",
                "Cooking Panel", "Snack Panel", "Dev Panel",
                FirstDayJourneyController.OverlayObjectName, "Cheese Star Delivery Overlay",
                "Memory Journal Overlay", "Fantasy Powder Overlay", SaveRecoveryNoticeController.OverlayObjectName,
                InputBindingsPanelController.OverlayObjectName, "Milk Blending Overlay", CookingChoicePanelController.OverlayObjectName,
                NpcVisitCardController.OverlayObjectName,
                JourneyHubPanelController.OverlayObjectName,
                SleepSchedulePanelController.OverlayObjectName
            };

            for (var index = 0; index < blockers.Length; index += 1)
            {
                var child = parent.Find(blockers[index]);
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void BindButtons()
        {
            if (milkDropButton != null)
            {
                milkDropButton.onClick.RemoveListener(OpenMilkDrop);
                milkDropButton.onClick.AddListener(OpenMilkDrop);
            }

            if (bouncyJumpButton != null)
            {
                bouncyJumpButton.onClick.RemoveListener(OpenBouncyJump);
                bouncyJumpButton.onClick.AddListener(OpenBouncyJump);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void UnbindButtons()
        {
            milkDropButton?.onClick.RemoveListener(OpenMilkDrop);
            bouncyJumpButton?.onClick.RemoveListener(OpenBouncyJump);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void SetActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }

        private void SetText(string value)
        {
            if (statusText != null)
            {
                statusText.text = value ?? string.Empty;
            }
        }

        private void SuspendControls()
        {
            if (controlsSuspended) return;
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
            if (!controlsSuspended) return;
            if (topMenuController != null) topMenuController.enabled = topMenuWasEnabled;
            if (bottomActionBarController != null) bottomActionBarController.enabled = actionBarWasEnabled;
            if (devPanelController != null) devPanelController.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }
    }
}
