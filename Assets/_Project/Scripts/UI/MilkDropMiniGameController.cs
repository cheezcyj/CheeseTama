using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkDropMiniGameController : MonoBehaviour
    {
        private static readonly string[] BlockingOverlayNames =
        {
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Care Event Overlay",
            "Cleaning Mini Game Overlay",
            "Evolution Achievement Overlay",
            "Decoration Shop Overlay",
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
            ,InputBindingsPanelController.OverlayObjectName
            ,"Milk Blending Overlay"
            ,CookingChoicePanelController.OverlayObjectName
            ,NpcVisitCardController.OverlayObjectName
            ,JourneyHubPanelController.OverlayObjectName
            ,SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private Button milkDropTemplate;
        [SerializeField] private Text remainingTimeText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text resultText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private readonly List<DropEntry> dropPool = new List<DropEntry>();
        private bool configured;
        private bool sessionActive;
        private bool showingResult;
        private bool rewardCommitted;
        private bool currencyRewardEligibleForSession;
        private float elapsedSeconds;
        private float spawnAccumulator;
        private int caught;
        private int missed;
        private int score;
        private int displayedRemainingSeconds = -1;
        private int displayedScore = -1;
        private int displayedCaught = -1;
        private int displayedMissed = -1;

        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;
        private bool milkroomUiWasEnabled;
        private GameObject previouslySelectedObject;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeSelf;

        public bool IsSessionActive => sessionActive;
        public bool IsShowingResult => showingResult;

        public void Configure(
            GameObject root,
            RectTransform dropPlayArea,
            Button dropTemplate,
            Text timeLabel,
            Text scoreLabel,
            Text resultLabel,
            Button sessionCancelButton,
            Button resultConfirmButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController tamaVisual,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            AbortSession(false);
            RestoreControls();
            UnbindButtons();
            DestroyDropPool();

            overlayRoot = root;
            playArea = dropPlayArea;
            milkDropTemplate = dropTemplate;
            remainingTimeText = timeLabel;
            scoreText = scoreLabel;
            resultText = resultLabel;
            cancelButton = sessionCancelButton;
            confirmButton = resultConfirmButton;
            milkroomUi = uiController;
            visualController = tamaVisual;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = overlayRoot != null
                && playArea != null
                && milkDropTemplate != null
                && cancelButton != null
                && confirmButton != null;

            if (milkDropTemplate != null)
            {
                milkDropTemplate.gameObject.SetActive(false);
            }

            BindButtons();
            SetOverlayActive(false);
            SetButtonVisible(cancelButton, true);
            SetButtonVisible(confirmButton, false);
            if (configured && Application.isPlaying)
            {
                PrewarmPool();
            }
        }

        private void OnEnable()
        {
            if (!configured)
            {
                return;
            }

            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();
            AbortSession(false);
            RestoreControls();
        }

        private void OnDestroy()
        {
            DestroyDropPool();
        }

        private void Update()
        {
            if (!configured || !Application.isPlaying || !IsBlockingGameplay)
            {
                return;
            }

            if (CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                if (sessionActive)
                {
                    Cancel();
                }
                else if (showingResult)
                {
                    CloseResult();
                }

                return;
            }

            if (!sessionActive)
            {
                return;
            }

            var deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            elapsedSeconds += deltaTime;
            spawnAccumulator += deltaTime;
            while (spawnAccumulator >= MilkDropMiniGameRules.SpawnIntervalSeconds)
            {
                spawnAccumulator -= MilkDropMiniGameRules.SpawnIntervalSeconds;
                SpawnDrop();
            }

            UpdateDrops(deltaTime);
            RefreshSessionLabels();
            if (MilkDropMiniGameRules.IsComplete(elapsedSeconds))
            {
                FinishSession();
            }
        }

        public bool Open()
        {
            if (!configured || !Application.isPlaying || IsBlockingGameplay || IsAnotherModalBlocking())
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();
            if (dropPool.Count == 0)
            {
                PrewarmPool();
            }

            elapsedSeconds = 0f;
            spawnAccumulator = 0f;
            caught = 0;
            missed = 0;
            score = 0;
            displayedRemainingSeconds = -1;
            displayedScore = -1;
            displayedCaught = -1;
            displayedMissed = -1;
            rewardCommitted = false;
            showingResult = false;
            sessionActive = true;
            DeactivateAllDrops(false);
            SetText(resultText, PrepareSessionReward(GameManager.Instance));
            SetButtonVisible(cancelButton, true);
            SetButtonVisible(confirmButton, false);

            previouslySelectedObject = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            SuspendControls();
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(null);
            RefreshSessionLabels();
            SpawnDrop();
            return true;
        }

        public void Cancel()
        {
            if (!sessionActive)
            {
                return;
            }

            AbortSession(true);
        }

        public void CloseResult()
        {
            if (!showingResult)
            {
                return;
            }

            showingResult = false;
            SetOverlayActive(false);
            RestoreControls();
            RestoreSelection();
        }

        private void FinishSession()
        {
            if (!sessionActive || rewardCommitted)
            {
                return;
            }

            sessionActive = false;
            DeactivateAllDrops(true);
            score = MilkDropMiniGameRules.CalculateScore(caught);
            rewardCommitted = true;

            var manager = GameManager.Instance;
            var reward = manager != null
                ? manager.CompleteMilkDropMiniGame(
                    caught,
                    missed,
                    score,
                    currencyRewardEligibleForSession)
                : new MilkDropMiniGameRewardResult(
                    score,
                    caught,
                    missed,
                    0,
                    0,
                    "점수는 기록했지만 저장 데이터를 찾지 못해 자원은 지급되지 않았어요.",
                    false,
                    0);
            caught = reward.caught;
            missed = reward.missed;
            score = reward.score;
            showingResult = true;

            SetText(
                resultText,
                $"결과  {score}점\n받은 방울 {caught}개 · 놓친 방울 {missed}개\n{reward.message}");
            SetButtonVisible(cancelButton, false);
            SetButtonVisible(confirmButton, true);
            RefreshSessionLabels();

            if (manager != null)
            {
                milkroomUi?.Bind(manager.CurrentSave);
                milkroomUi?.ShowMessage(reward.message);
                visualController?.Bind(manager.CurrentTama);
            }

            visualController?.ReactEvent("milk_drop_catch", CheeseTamaVisualAction.Play);
            overlayRoot.transform.SetAsLastSibling();
            if (EventSystem.current != null && confirmButton != null)
            {
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
            }
        }

        private void AbortSession(bool showMessage)
        {
            var wasActive = sessionActive || showingResult || IsBlockingGameplay;
            sessionActive = false;
            showingResult = false;
            rewardCommitted = false;
            currencyRewardEligibleForSession = false;
            DeactivateAllDrops(false);
            SetOverlayActive(false);
            RestoreControls();
            RestoreSelection();

            if (showMessage && wasActive)
            {
                milkroomUi?.ShowMessage("우유방울 받기를 취소했어요.");
            }
        }

        private void RefreshSessionLabels()
        {
            var remaining = Mathf.CeilToInt(MilkDropMiniGameRules.GetRemainingSeconds(elapsedSeconds));
            if (remaining != displayedRemainingSeconds)
            {
                displayedRemainingSeconds = remaining;
                SetText(remainingTimeText, $"남은 시간  {remaining}초");
            }

            if (score != displayedScore || caught != displayedCaught || missed != displayedMissed)
            {
                displayedScore = score;
                displayedCaught = caught;
                displayedMissed = missed;
                SetText(scoreText, $"점수  {score}  ·  성공 {caught}  ·  놓침 {missed}");
            }
        }

        private void SpawnDrop()
        {
            if (!sessionActive || playArea == null)
            {
                return;
            }

            var entry = FindAvailableDrop();
            if (entry == null)
            {
                return;
            }

            var areaRect = playArea.rect;
            var halfDropWidth = Mathf.Max(16f, entry.rect.rect.width * 0.5f);
            var halfDropHeight = Mathf.Max(16f, entry.rect.rect.height * 0.5f);
            var minimumX = areaRect.xMin + halfDropWidth;
            var maximumX = areaRect.xMax - halfDropWidth;
            var x = maximumX > minimumX
                ? Random.Range(minimumX, maximumX)
                : areaRect.center.x;
            entry.rect.anchoredPosition = new Vector2(x, areaRect.yMax + halfDropHeight);
            entry.speed = Random.Range(
                MilkDropMiniGameRules.MinimumFallSpeed,
                MilkDropMiniGameRules.MaximumFallSpeed);
            entry.active = true;
            entry.button.gameObject.SetActive(true);
            entry.rect.SetAsLastSibling();
        }

        private void UpdateDrops(float deltaTime)
        {
            if (playArea == null)
            {
                return;
            }

            for (var index = 0; index < dropPool.Count; index += 1)
            {
                var entry = dropPool[index];
                if (entry == null || !entry.active || entry.rect == null)
                {
                    continue;
                }

                entry.rect.anchoredPosition += Vector2.down * entry.speed * deltaTime;
                var halfDropHeight = Mathf.Max(16f, entry.rect.rect.height * 0.5f);
                var bottom = playArea.rect.yMin - halfDropHeight;
                if (entry.rect.anchoredPosition.y <= bottom)
                {
                    DeactivateDrop(entry);
                    missed += 1;
                }
            }
        }

        private void CatchDrop(DropEntry entry)
        {
            if (!sessionActive || entry == null || !entry.active)
            {
                return;
            }

            DeactivateDrop(entry);
            caught += 1;
            score = MilkDropMiniGameRules.CalculateScore(caught);
            RefreshSessionLabels();
        }

        private DropEntry FindAvailableDrop()
        {
            for (var index = 0; index < dropPool.Count; index += 1)
            {
                var entry = dropPool[index];
                if (entry != null && !entry.active && entry.button != null)
                {
                    return entry;
                }
            }

            return dropPool.Count < MilkDropMiniGameRules.MaximumPoolSize
                ? CreateDrop()
                : null;
        }

        private void PrewarmPool()
        {
            while (dropPool.Count < MilkDropMiniGameRules.InitialPoolSize)
            {
                if (CreateDrop() == null)
                {
                    break;
                }
            }
        }

        private DropEntry CreateDrop()
        {
            if (milkDropTemplate == null || playArea == null)
            {
                return null;
            }

            var button = Instantiate(milkDropTemplate, playArea);
            button.name = $"Milk Drop Pool Item {dropPool.Count + 1}";
            button.onClick.RemoveAllListeners();
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                // SpawnDrop uses the play area's local rect (top-left origin), so pooled
                // drops must use the same anchor space instead of the button helper's
                // default bottom-center anchor.
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.one * MilkDropMiniGameRules.DropSizePixels;
            }

            var entry = new DropEntry(button, rect);
            button.onClick.AddListener(() => CatchDrop(entry));
            button.gameObject.SetActive(false);
            dropPool.Add(entry);
            return entry;
        }

        private void DeactivateAllDrops(bool countAsMissed)
        {
            for (var index = 0; index < dropPool.Count; index += 1)
            {
                var entry = dropPool[index];
                if (entry == null || !entry.active)
                {
                    continue;
                }

                if (countAsMissed)
                {
                    missed += 1;
                }

                DeactivateDrop(entry);
            }
        }

        private static void DeactivateDrop(DropEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            entry.active = false;
            if (entry.button != null)
            {
                entry.button.gameObject.SetActive(false);
            }
        }

        private void DestroyDropPool()
        {
            for (var index = 0; index < dropPool.Count; index += 1)
            {
                var entry = dropPool[index];
                if (entry?.button == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(entry.button.gameObject);
                }
                else
                {
                    DestroyImmediate(entry.button.gameObject);
                }
            }

            dropPool.Clear();
        }

        private bool IsAnotherModalBlocking()
        {
            var modalContainer = overlayRoot != null && overlayRoot.transform.parent != null
                ? overlayRoot.transform.parent
                : transform;
            var onboarding = modalContainer.GetComponent<FirstMeetingOnboardingController>();
            if (onboarding != null && onboarding.IsBlockingGameplay)
            {
                var onboardingSave = GameManager.Instance?.CurrentSave?.onboarding;
                if (onboardingSave == null
                    || onboardingSave.currentStep != FirstMeetingOnboardingStep.Care)
                {
                    return true;
                }
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

        private void BindButtons()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
                cancelButton.onClick.AddListener(Cancel);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(CloseResult);
                confirmButton.onClick.AddListener(CloseResult);
            }
        }

        private void UnbindButtons()
        {
            cancelButton?.onClick.RemoveListener(Cancel);
            confirmButton?.onClick.RemoveListener(CloseResult);
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private string PrepareSessionReward(GameManager manager)
        {
            if (manager == null)
            {
                currencyRewardEligibleForSession = false;
                return "떨어지는 우유방울을 눌러서 받아 보세요! 저장 연결 전에는 자원이 지급되지 않아요.";
            }

            var status = manager.GetMilkDropMiniGameRewardStatus();
            currencyRewardEligibleForSession = status.isAvailable;
            return status.isAvailable
                ? "보상 가능 · 이번 판은 점수에 따라 자원을 받을 수 있어요."
                : $"연습 플레이 · 자원 보상 없음 · 다음 보상까지 {MilkDropMiniGameRules.FormatCooldown(status.remainingSeconds)}";
        }

        private sealed class DropEntry
        {
            public DropEntry(Button button, RectTransform rect)
            {
                this.button = button;
                this.rect = rect;
            }

            public readonly Button button;
            public readonly RectTransform rect;
            public bool active;
            public float speed;
        }
    }
}
