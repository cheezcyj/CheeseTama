using CheeseTama.Core;
using CheeseTama.Gameplay.Growth;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class GrowthJourneyController : MonoBehaviour
    {
        public const string OverlayObjectName = "Growth Journey Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text milkProgressText;
        [SerializeField] private Text nextGoalText;
        [SerializeField] private Text unlockText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private GameManager boundManager;
        private bool configured;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomBarWasEnabled;
        private bool devPanelWasEnabled;
        private bool openedForUnlock;
        private float refreshTimer;
        private GameObject previouslySelectedObject;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeSelf;

        public void Configure(
            GameObject root,
            Text heading,
            Text levelLabel,
            Text milkLabel,
            Text goalLabel,
            Text unlockedLabel,
            Button closeJourneyButton,
            Button openJourneyButton,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            RestoreControls();
            BindManager(null);
            UnbindButtons();
            overlayRoot = root;
            titleText = heading;
            levelText = levelLabel;
            milkProgressText = milkLabel;
            nextGoalText = goalLabel;
            unlockText = unlockedLabel;
            closeButton = closeJourneyButton;
            openButton = openJourneyButton;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = overlayRoot != null && closeButton != null && openButton != null;
            BindButtons();
            SetOverlayActive(false);
            if (Application.isPlaying)
            {
                BindManager(GameManager.Instance);
                TryOpenPendingUnlock();
            }
        }

        private void OnEnable()
        {
            if (!configured)
            {
                return;
            }

            BindButtons();
            BindManager(GameManager.Instance);
            TryOpenPendingUnlock();
        }

        private void OnDisable()
        {
            UnbindButtons();
            BindManager(null);
            RestoreControls();
        }

        private void Update()
        {
            if (!configured || !Application.isPlaying)
            {
                return;
            }

            if (IsBlockingGameplay && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 0.5f;
            BindManager(GameManager.Instance);
            if (IsBlockingGameplay)
            {
                RefreshContent();
            }
            else
            {
                TryOpenPendingUnlock();
            }
        }

        public void Open()
        {
            UnityEngine.Object.FindFirstObjectByType<CheeseTamaProfileMenuController>()?.CloseForChildNavigation();
            OpenInternal(false);
        }

        public void Close()
        {
            if (openedForUnlock)
            {
                boundManager?.AcknowledgeStarRouteUnlock();
            }

            openedForUnlock = false;
            SetOverlayActive(false);
            RestoreControls();
            if (EventSystem.current != null
                && previouslySelectedObject != null
                && previouslySelectedObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previouslySelectedObject);
            }

            previouslySelectedObject = null;
        }

        private void OpenInternal(bool unlockAnnouncement)
        {
            if (!configured || !Application.isPlaying || IsAnotherModalBlocking())
            {
                return;
            }

            openedForUnlock = unlockAnnouncement;
            previouslySelectedObject = EventSystem.current?.currentSelectedGameObject;
            RefreshContent();
            SuspendControls();
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
        }

        private void TryOpenPendingUnlock()
        {
            if (boundManager != null && boundManager.HasPendingStarRouteUnlock)
            {
                OpenInternal(true);
            }
        }

        private void RefreshContent()
        {
            var progress = boundManager?.GetStarRouteProgress()
                ?? StarRouteSystem.Evaluate(null, null);
            ApplyProgress(progress);
        }

        private void ApplyProgress(StarRouteProgress progress)
        {
            if (!progress.unlocked)
            {
                SetText(titleText, "치즈타마 성장 여정");
                SetText(levelText, "꾸준히 돌보며 성장을 이어가 주세요.");
                SetText(milkProgressText, "여러 우유와 함께 새로운 모습을 발견해 보세요.");
                SetText(nextGoalText, "다음 성장 목표를 향해 돌봄을 이어가세요.");
                SetText(unlockText, "새로운 성장 길은 조건 달성 후 발견됩니다.");
                return;
            }

            if (titleText != null)
            {
                titleText.text = openedForUnlock ? "별빛 루트가 열렸어요!" : "치즈타마 성장 여정";
            }

            SetText(levelText, $"성장 레벨  {progress.level}/{progress.maximumLevel}");
            SetText(milkProgressText,
                $"주요 우유 완전 성장  {progress.completedMilkCount}/{progress.requiredMilkCount}");
            SetText(nextGoalText, progress.nextGoal);
            SetText(unlockText, "별빛 알 · 별빛 우유 사용 가능");
        }

        private bool IsAnotherModalBlocking()
        {
            var container = overlayRoot != null && overlayRoot.transform.parent != null
                ? overlayRoot.transform.parent
                : transform;
            var blockers = new[]
            {
                NewGameSetupController.OverlayObjectName, "First Meeting Onboarding Overlay", "Return Summary Overlay",
                "Growth Achievement Overlay", "Evolution Achievement Overlay", "Care Event Overlay",
                "Cleaning Mini Game Overlay", "Milk Drop Catch Overlay", "Bouncy Jump Overlay",
                "Play Choice Overlay", "Decoration Shop Overlay", "CheeseTama Name Dialog",
                "Settings Modal", "Confirm Reset Dialog", "Decorate Overlay", "Milk Panel",
                "Cooking Panel", "Snack Panel", "Dev Panel",
                FirstDayJourneyController.OverlayObjectName, "Cheese Star Delivery Overlay",
                "Memory Journal Overlay", "Fantasy Powder Overlay", SaveRecoveryNoticeController.OverlayObjectName,
                CheeseTamaProfileMenuController.OverlayObjectName,
                InputBindingsPanelController.OverlayObjectName, "Milk Blending Overlay", CookingChoicePanelController.OverlayObjectName,
                NpcVisitCardController.OverlayObjectName,
                JourneyHubPanelController.OverlayObjectName,
                SleepSchedulePanelController.OverlayObjectName
            };

            for (var index = 0; index < blockers.Length; index += 1)
            {
                var blocker = container.Find(blockers[index]);
                if (blocker != null && blocker.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void BindManager(GameManager manager)
        {
            if (boundManager == manager)
            {
                return;
            }

            if (boundManager != null)
            {
                boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
                boundManager.StarRouteUnlockAvailable -= HandleStarRouteAvailable;
            }

            boundManager = manager;
            if (boundManager != null)
            {
                boundManager.SaveDataReplaced += HandleSaveDataReplaced;
                boundManager.StarRouteUnlockAvailable += HandleStarRouteAvailable;
            }
        }

        private void HandleSaveDataReplaced()
        {
            RefreshContent();
            TryOpenPendingUnlock();
        }

        private void HandleStarRouteAvailable()
        {
            TryOpenPendingUnlock();
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
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
            if (bottomActionBarController != null) bottomActionBarController.enabled = bottomBarWasEnabled;
            if (devPanelController != null) devPanelController.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }

        private void BindButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Open);
                openButton.onClick.AddListener(Open);
            }
        }

        private void UnbindButtons()
        {
            closeButton?.onClick.RemoveListener(Close);
            openButton?.onClick.RemoveListener(Open);
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
