using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Gameplay.Input;
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
        [SerializeField] private RectTransform basket;
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
        private HorizontalDragInputTarget basketInputTarget;
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
            RectTransform catcherBasket,
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
            basketInputTarget?.ClearConfiguration();
            basketInputTarget = null;
            AbortSession(false);
            RestoreControls();
            UnbindButtons();
            DestroyDropPool();

            overlayRoot = root;
            playArea = dropPlayArea;
            milkDropTemplate = dropTemplate;
            basket = catcherBasket;
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
                && basket != null
                && cancelButton != null
                && confirmButton != null;

            if (milkDropTemplate != null)
            {
                milkDropTemplate.gameObject.SetActive(false);
            }

            if (basket != null)
            {
                NormalizeTopLeftAnchor(basket);
                SetBasketVisible(false);
            }

            if (playArea != null)
            {
                basketInputTarget = playArea.GetComponent<HorizontalDragInputTarget>()
                    ?? playArea.gameObject.AddComponent<HorizontalDragInputTarget>();
                basketInputTarget.Configure(
                    MoveBasketToScreenPosition,
                    () => sessionActive && IsBlockingGameplay);
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
            basketInputTarget?.ClearConfiguration();
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

            UpdateBasket(deltaTime);
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
            SetBasketVisible(true);
            ResetBasketPosition();

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
            SetBasketVisible(false);
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
            SetBasketVisible(false);
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

            var halfDropWidth = Mathf.Max(16f, entry.rect.rect.width * 0.5f);
            var halfDropHeight = Mathf.Max(16f, entry.rect.rect.height * 0.5f);
            var areaWidth = Mathf.Max(0f, playArea.rect.width);
            var minimumX = halfDropWidth;
            var maximumX = areaWidth - halfDropWidth;
            var x = maximumX > minimumX
                ? Random.Range(minimumX, maximumX)
                : areaWidth * 0.5f;
            entry.rect.anchoredPosition = new Vector2(x, halfDropHeight);
            entry.speed = Random.Range(
                MilkDropMiniGameRules.MinimumFallSpeed,
                MilkDropMiniGameRules.MaximumFallSpeed);
            entry.active = true;
            entry.button.gameObject.SetActive(true);
            entry.rect.SetAsLastSibling();
            basket?.SetAsLastSibling();
        }

        private void UpdateDrops(float deltaTime)
        {
            if (playArea == null)
            {
                return;
            }

            var canCatch = basket != null && basket.gameObject.activeInHierarchy;
            var basketPosition = canCatch ? basket.anchoredPosition : Vector2.zero;
            var basketHalfWidth = canCatch
                ? Mathf.Max(0f, basket.rect.width * 0.5f)
                : 0f;
            var basketHalfHeight = canCatch
                ? Mathf.Max(0f, basket.rect.height * 0.5f)
                : 0f;
            var playAreaBottom = -Mathf.Max(0f, playArea.rect.height);

            for (var index = 0; index < dropPool.Count; index += 1)
            {
                var entry = dropPool[index];
                if (entry == null || !entry.active || entry.rect == null)
                {
                    continue;
                }

                var previousPosition = entry.rect.anchoredPosition;
                entry.rect.anchoredPosition += Vector2.down * entry.speed * deltaTime;
                var currentPosition = entry.rect.anchoredPosition;
                var halfDropWidth = Mathf.Max(16f, entry.rect.rect.width * 0.5f);
                var halfDropHeight = Mathf.Max(16f, entry.rect.rect.height * 0.5f);
                if (canCatch
                    && MilkDropMiniGameRules.DoesDropIntersectBasket(
                        currentPosition.x,
                        previousPosition.y,
                        currentPosition.y,
                        halfDropWidth,
                        halfDropHeight,
                        basketPosition.x,
                        basketPosition.y,
                        basketHalfWidth,
                        basketHalfHeight))
                {
                    CatchDrop(entry);
                    continue;
                }

                var bottom = playAreaBottom - halfDropHeight;
                if (entry.rect.anchoredPosition.y <= bottom)
                {
                    DeactivateDrop(entry);
                    missed += 1;
                }
            }
        }

        private void UpdateBasket(float deltaTime)
        {
            if (!sessionActive || basket == null || playArea == null)
            {
                return;
            }

            RefreshBasketLayout(false);
            var direction = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)
                || UnityEngine.Input.GetKey(KeyCode.A))
            {
                direction -= 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)
                || UnityEngine.Input.GetKey(KeyCode.D))
            {
                direction += 1f;
            }

            if (!Mathf.Approximately(direction, 0f))
            {
                MoveBasketBy(
                    direction
                    * MilkDropMiniGameRules.BasketKeyboardSpeedPixelsPerSecond
                    * Mathf.Max(0f, deltaTime));
            }
        }

        private void MoveBasketToScreenPosition(Vector2 screenPosition, Camera eventCamera)
        {
            if (!sessionActive
                || playArea == null
                || basket == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playArea,
                    screenPosition,
                    eventCamera,
                    out var localPoint))
            {
                return;
            }

            SetBasketCenterX(localPoint.x - playArea.rect.xMin);
        }

        private void MoveBasketBy(float deltaPixels)
        {
            if (basket == null)
            {
                return;
            }

            SetBasketCenterX(basket.anchoredPosition.x + deltaPixels);
        }

        private void SetBasketCenterX(float requestedCenterX)
        {
            if (basket == null || playArea == null)
            {
                return;
            }

            var clampedX = MilkDropMiniGameRules.ClampBasketCenterX(
                playArea.rect.width,
                basket.rect.width,
                requestedCenterX);
            basket.anchoredPosition = new Vector2(clampedX, basket.anchoredPosition.y);
        }

        private void ResetBasketPosition()
        {
            RefreshBasketLayout(true);
        }

        private void RefreshBasketLayout(bool resetHorizontalPosition)
        {
            if (basket == null || playArea == null)
            {
                return;
            }

            NormalizeTopLeftAnchor(basket);
            var playAreaWidth = Mathf.Max(0f, playArea.rect.width);
            var playAreaHeight = Mathf.Max(0f, playArea.rect.height);
            var requestedX = resetHorizontalPosition
                ? playAreaWidth * 0.5f
                : basket.anchoredPosition.x;
            var x = MilkDropMiniGameRules.ClampBasketCenterX(
                playAreaWidth,
                basket.rect.width,
                requestedX);
            var halfHeight = Mathf.Max(0f, basket.rect.height * 0.5f);
            var y = -playAreaHeight
                + halfHeight
                + MilkDropMiniGameRules.BasketBottomPaddingPixels;
            basket.anchoredPosition = new Vector2(x, y);
            basket.SetAsLastSibling();
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
                NormalizeTopLeftAnchor(rect);
                rect.sizeDelta = Vector2.one * MilkDropMiniGameRules.DropSizePixels;
            }

            var entry = new DropEntry(button, rect);
            button.transition = Selectable.Transition.None;
            button.interactable = false;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = Color.white;
            }

            var graphics = button.GetComponentsInChildren<Graphic>(true);
            for (var index = 0; index < graphics.Length; index += 1)
            {
                graphics[index].raycastTarget = false;
            }
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

        private void SetBasketVisible(bool visible)
        {
            if (basket != null && basket.gameObject.activeSelf != visible)
            {
                basket.gameObject.SetActive(visible);
            }
        }

        private static void NormalizeTopLeftAnchor(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
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
                return "그릇을 좌우로 움직여 우유방울을 받아 보세요!\n저장 연결 전에는 자원이 지급되지 않아요.";
            }

            var status = manager.GetMilkDropMiniGameRewardStatus();
            currencyRewardEligibleForSession = status.isAvailable;
            var rewardText = status.isAvailable
                ? "보상 가능 · 이번 판은 점수에 따라 자원을 받을 수 있어요."
                : $"연습 플레이 · 자원 보상 없음 · 다음 보상까지 {MilkDropMiniGameRules.FormatCooldown(status.remainingSeconds)}";
            return $"그릇을 좌우로 움직여 우유방울을 받아 보세요!\n{rewardText}";
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
