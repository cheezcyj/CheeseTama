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
    public sealed class CleaningMiniGameController : MonoBehaviour
    {
        public const string OverlayObjectName = "Cleaning Mini Game Overlay";

        private static readonly string[] BlockingOverlayNames =
        {
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            "Care Event Overlay",
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Decorate Overlay",
            "Decoration Shop Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel",
            "Milk Drop Catch Overlay",
            "New Game Setup Overlay",
            "Growth Journey Overlay",
            "Play Choice Overlay",
            "Bouncy Jump Overlay",
            FirstDayJourneyController.OverlayObjectName,
            "Cheese Star Delivery Overlay",
            "Memory Journal Overlay",
            "Fantasy Powder Overlay",
            SaveRecoveryNoticeController.OverlayObjectName,
            InputBindingsPanelController.OverlayObjectName,
            "Milk Blending Overlay",
            CookingChoicePanelController.OverlayObjectName,
            NpcVisitCardController.OverlayObjectName,
            JourneyHubPanelController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName,
            OverlayObjectName
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private Button dirtSpotTemplate;
        [SerializeField] private Text remainingTimeText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text resultText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private readonly List<DirtSpotEntry> spotPool = new List<DirtSpotEntry>();
        private bool configured;
        private bool sessionActive;
        private bool showingResult;
        private bool completionCommitted;
        private float elapsedSeconds;
        private int cleanedSpots;
        private int missedSpots;
        private int score;
        private int displayedRemainingSeconds = -1;
        private int displayedScore = -1;
        private int displayedCleaned = -1;
        private int displayedMissed = -1;
        private int displayedPhase = -1;
        private int currentPhase = 1;
        private bool nextPhasePending;
        private bool currentPhaseInteracted;
        private DirtSpotEntry activeScrubEntry;

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
        public int PooledSpotCount => spotPool.Count;
        public int ActiveSpotCount => CountActiveSpots();

        public void Configure(
            GameObject root,
            RectTransform cleaningArea,
            Button spotTemplate,
            Text timeLabel,
            Text sessionScoreLabel,
            Text sessionProgressLabel,
            Text sessionResultLabel,
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
            DestroySpotPool();

            overlayRoot = root;
            playArea = cleaningArea;
            dirtSpotTemplate = spotTemplate;
            remainingTimeText = timeLabel;
            scoreText = sessionScoreLabel;
            progressText = sessionProgressLabel;
            resultText = sessionResultLabel;
            cancelButton = sessionCancelButton;
            confirmButton = resultConfirmButton;
            milkroomUi = uiController;
            visualController = tamaVisual;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;

            configured = overlayRoot != null
                && playArea != null
                && dirtSpotTemplate != null
                && cancelButton != null
                && confirmButton != null;

            if (dirtSpotTemplate != null)
            {
                dirtSpotTemplate.gameObject.SetActive(false);
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
            if (configured)
            {
                BindButtons();
            }
        }

        private void OnDisable()
        {
            UnbindButtons();
            AbortSession(false);
            RestoreControls();
        }

        private void OnDestroy()
        {
            DestroySpotPool();
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

            AdvanceSession(Time.unscaledDeltaTime);
        }

        public bool Open()
        {
            if (!configured || !Application.isPlaying || IsBlockingGameplay || IsAnotherModalBlocking())
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();
            if (spotPool.Count == 0)
            {
                PrewarmPool();
            }

            elapsedSeconds = 0f;
            cleanedSpots = 0;
            missedSpots = 0;
            score = 0;
            displayedRemainingSeconds = -1;
            displayedScore = -1;
            displayedCleaned = -1;
            displayedMissed = -1;
            displayedPhase = -1;
            currentPhase = 1;
            nextPhasePending = false;
            currentPhaseInteracted = false;
            completionCommitted = false;
            showingResult = false;
            sessionActive = true;
            activeScrubEntry = null;
            DeactivateAllSpots(false);
            ActivateFixedSpots();
            SetText(resultText, "얼룩을 좌우로 문질러 주세요. 모두 닦으면 다음 얼룩이 나와요!");
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
            return true;
        }

        private void AdvanceSession(float deltaTime)
        {
            if (!sessionActive)
            {
                return;
            }

            elapsedSeconds += Mathf.Max(0f, deltaTime);
            if (CleaningMiniGameRules.IsComplete(elapsedSeconds))
            {
                FinishSession();
                return;
            }

            if (nextPhasePending)
            {
                nextPhasePending = false;
                currentPhase += 1;
                currentPhaseInteracted = false;
                ActivateFixedSpots();
            }

            RefreshSessionLabels();
        }

        public void Cancel()
        {
            if (sessionActive)
            {
                // Completion is intentionally not committed from the cancellation path.
                AbortSession(true);
            }
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
            if (!sessionActive || completionCommitted)
            {
                return;
            }

            sessionActive = false;
            DeactivateAllSpots(currentPhase == 1 || currentPhaseInteracted);
            score = CleaningMiniGameRules.CalculateScore(cleanedSpots);
            completionCommitted = true;

            var manager = GameManager.Instance;
            var completion = manager != null
                ? manager.CompleteCleaningMiniGame(cleanedSpots, missedSpots, score)
                : new CleaningMiniGameCompletionResult(
                    score,
                    cleanedSpots,
                    missedSpots,
                    0,
                    "저장 데이터를 찾지 못해 청소 결과를 반영하지 못했어요.",
                    false);

            cleanedSpots = completion.cleanedSpots;
            missedSpots = completion.missedSpots;
            score = completion.score;
            showingResult = true;

            SetText(resultText, CleaningMiniGameRules.BuildResultSummary(completion));
            SetButtonVisible(cancelButton, false);
            SetButtonVisible(confirmButton, true);
            RefreshSessionLabels();

            if (manager != null)
            {
                milkroomUi?.Bind(manager.CurrentSave);
                milkroomUi?.ShowMessage(completion.message);
                visualController?.Bind(manager.CurrentTama);
            }

            if (completion.success)
            {
                visualController?.ReactAction(CheeseTamaVisualAction.Clean);
            }

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
            completionCommitted = false;
            nextPhasePending = false;
            currentPhaseInteracted = false;
            DeactivateAllSpots(false);
            SetOverlayActive(false);
            RestoreControls();
            RestoreSelection();

            if (showMessage && wasActive)
            {
                milkroomUi?.ShowMessage("청소 게임을 취소했어요. 돌봄과 보상은 반영되지 않았어요.");
            }
        }

        private void RefreshSessionLabels()
        {
            var remaining = Mathf.CeilToInt(CleaningMiniGameRules.GetRemainingSeconds(elapsedSeconds));
            if (remaining != displayedRemainingSeconds)
            {
                displayedRemainingSeconds = remaining;
                SetText(remainingTimeText, $"남은 시간  {remaining}초");
            }

            if (score != displayedScore)
            {
                displayedScore = score;
                SetText(scoreText, $"점수  {score}");
            }

            if (cleanedSpots != displayedCleaned
                || missedSpots != displayedMissed
                || currentPhase != displayedPhase)
            {
                displayedCleaned = cleanedSpots;
                displayedMissed = missedSpots;
                displayedPhase = currentPhase;
                SetText(progressText, $"{currentPhase}단계 · 닦음 {cleanedSpots} · 놓침 {missedSpots}");
            }
        }

        private void ActivateFixedSpots()
        {
            if (!sessionActive || playArea == null)
            {
                return;
            }

            var areaWidth = Mathf.Max(0f, playArea.rect.width);
            var areaHeight = Mathf.Max(0f, playArea.rect.height);
            var count = Mathf.Min(CleaningMiniGameRules.FixedSpotCount, spotPool.Count);
            for (var index = 0; index < count; index += 1)
            {
                var entry = spotPool[index];
                if (entry == null || entry.rect == null || entry.button == null)
                {
                    continue;
                }

                var halfWidth = Mathf.Max(18f, entry.rect.rect.width * 0.5f);
                var halfHeight = Mathf.Max(18f, entry.rect.rect.height * 0.5f);
                var minimumX = halfWidth;
                var maximumX = areaWidth - halfWidth;
                var minimumY = halfHeight;
                var maximumY = areaHeight - halfHeight;
                var slot = CleaningMiniGameRules.GetFixedSlot(index, currentPhase - 1);
                var x = maximumX > minimumX
                    ? Mathf.Lerp(minimumX, maximumX, slot.normalizedX)
                    : areaWidth * 0.5f;
                var distanceFromTop = maximumY > minimumY
                    ? Mathf.Lerp(minimumY, maximumY, slot.normalizedY)
                    : areaHeight * 0.5f;
                entry.rect.anchoredPosition = new Vector2(x, -distanceFromTop);
                entry.rect.localScale = Vector3.one;
                entry.rect.localRotation = Quaternion.identity;
                entry.active = true;
                entry.scrubProgress = 0f;
                entry.gestureStartProgress = 0f;
                entry.gestureActive = false;
                if (entry.graphic != null)
                {
                    entry.graphic.color = entry.baseColor;
                }

                entry.button.gameObject.SetActive(true);
                entry.rect.SetAsLastSibling();
            }
        }

        private void CleanSpot(DirtSpotEntry entry)
        {
            if (!sessionActive || entry == null || !entry.active)
            {
                return;
            }

            if (activeScrubEntry == entry)
            {
                activeScrubEntry = null;
            }

            DeactivateSpot(entry);
            currentPhaseInteracted = true;
            cleanedSpots += 1;
            score = CleaningMiniGameRules.CalculateScore(cleanedSpots);
            if (CountActiveSpots() == 0
                && !CleaningMiniGameRules.IsComplete(elapsedSeconds))
            {
                nextPhasePending = true;
            }

            RefreshSessionLabels();
        }

        private void HandleScrubProgress(
            DirtSpotEntry entry,
            DirectManipulationProgress progress)
        {
            if (entry == null || !entry.active)
            {
                return;
            }

            if (progress.IsActive)
            {
                if (progress.NormalizedProgress > 0f)
                {
                    currentPhaseInteracted = true;
                }

                activeScrubEntry ??= entry;
                if (activeScrubEntry != entry)
                {
                    return;
                }

                if (!entry.gestureActive)
                {
                    entry.gestureActive = true;
                    entry.gestureStartProgress = entry.scrubProgress;
                }

                entry.scrubProgress = Mathf.Clamp01(
                    entry.gestureStartProgress + progress.NormalizedProgress);
            }
            else if (activeScrubEntry == entry)
            {
                activeScrubEntry = null;
                entry.gestureActive = false;
            }

            if (entry.graphic != null)
            {
                var color = entry.baseColor;
                color.a = Mathf.Lerp(
                    entry.baseColor.a,
                    0.12f,
                    entry.scrubProgress);
                entry.graphic.color = color;
            }

            if (entry.scrubProgress >= 0.999f && entry.inputTarget != null)
            {
                entry.inputTarget.TryCompleteFromAlternativeInput();
            }
        }

        private void PrewarmPool()
        {
            while (spotPool.Count < CleaningMiniGameRules.FixedSpotCount)
            {
                if (CreateSpot() == null)
                {
                    break;
                }
            }
        }

        private DirtSpotEntry CreateSpot()
        {
            if (dirtSpotTemplate == null || playArea == null)
            {
                return null;
            }

            var button = Instantiate(dirtSpotTemplate, playArea);
            button.name = $"Cleaning Dirt Pool Item {spotPool.Count + 1}";
            button.onClick.RemoveAllListeners();
            button.transition = Selectable.Transition.None;
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                NormalizeTopLeftAnchor(rect);
                rect.sizeDelta = Vector2.one * CleaningMiniGameRules.SpotSizePixels;
            }

            var graphic = button.targetGraphic;
            var entry = new DirtSpotEntry(
                button,
                rect,
                graphic,
                graphic != null ? graphic.color : Color.white);
            var swipeTarget = button.GetComponent<DirectManipulationInputTarget>()
                ?? button.gameObject.AddComponent<DirectManipulationInputTarget>();
            entry.inputTarget = swipeTarget;
            swipeTarget.ConfigureCleaningSwipe(
                rect,
                () => CleanSpot(entry),
                progress => HandleScrubProgress(entry, progress),
                () => sessionActive
                    && entry.active
                    && (activeScrubEntry == null || activeScrubEntry == entry),
                requiredPathPixels: CleaningMiniGameRules.RequiredScrubPathPixels,
                requiredSpanPixels: CleaningMiniGameRules.RequiredScrubSpanPixels,
                allowTapFallback: false,
                completeOnSubmit: true);
            button.gameObject.SetActive(false);
            spotPool.Add(entry);
            return entry;
        }

        private void DeactivateAllSpots(bool countAsMissed)
        {
            activeScrubEntry = null;
            for (var index = 0; index < spotPool.Count; index += 1)
            {
                var entry = spotPool[index];
                if (entry == null || !entry.active)
                {
                    continue;
                }

                if (countAsMissed)
                {
                    missedSpots += 1;
                }

                DeactivateSpot(entry);
            }
        }

        private static void DeactivateSpot(DirtSpotEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            entry.active = false;
            entry.scrubProgress = 0f;
            entry.gestureStartProgress = 0f;
            entry.gestureActive = false;
            if (entry.graphic != null)
            {
                entry.graphic.color = entry.baseColor;
            }

            if (entry.button != null)
            {
                entry.button.gameObject.SetActive(false);
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

        private int CountActiveSpots()
        {
            var count = 0;
            for (var index = 0; index < spotPool.Count; index += 1)
            {
                if (spotPool[index]?.active == true)
                {
                    count += 1;
                }
            }

            return count;
        }

        private void DestroySpotPool()
        {
            for (var index = 0; index < spotPool.Count; index += 1)
            {
                var entry = spotPool[index];
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

            spotPool.Clear();
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
                if (blocker != null
                    && blocker.gameObject != overlayRoot
                    && blocker.gameObject.activeInHierarchy)
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

        private sealed class DirtSpotEntry
        {
            public DirtSpotEntry(
                Button button,
                RectTransform rect,
                Graphic graphic,
                Color baseColor)
            {
                this.button = button;
                this.rect = rect;
                this.graphic = graphic;
                this.baseColor = baseColor;
            }

            public readonly Button button;
            public readonly RectTransform rect;
            public readonly Graphic graphic;
            public readonly Color baseColor;
            public DirectManipulationInputTarget inputTarget;
            public bool active;
            public bool gestureActive;
            public float gestureStartProgress;
            public float scrubProgress;
        }
    }
}
