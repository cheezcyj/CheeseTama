using System.Collections.Generic;
using CheeseTama.Core;
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
        private float spawnAccumulator;
        private int cleanedSpots;
        private int missedSpots;
        private int score;
        private int displayedRemainingSeconds = -1;
        private int displayedScore = -1;
        private int displayedCleaned = -1;
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

            var deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            elapsedSeconds += deltaTime;
            spawnAccumulator += deltaTime;
            while (spawnAccumulator >= CleaningMiniGameRules.SpawnIntervalSeconds)
            {
                spawnAccumulator -= CleaningMiniGameRules.SpawnIntervalSeconds;
                SpawnDirtSpot();
            }

            UpdateDirtSpots(deltaTime);
            RefreshSessionLabels();
            if (CleaningMiniGameRules.IsComplete(elapsedSeconds))
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
            if (spotPool.Count == 0)
            {
                PrewarmPool();
            }

            elapsedSeconds = 0f;
            spawnAccumulator = 0f;
            cleanedSpots = 0;
            missedSpots = 0;
            score = 0;
            displayedRemainingSeconds = -1;
            displayedScore = -1;
            displayedCleaned = -1;
            displayedMissed = -1;
            completionCommitted = false;
            showingResult = false;
            sessionActive = true;
            DeactivateAllSpots(false);
            SetText(resultText, "얼룩을 눌러 밀크룸을 반짝이게 닦아 주세요.");
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
            SpawnDirtSpot();
            return true;
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
            DeactivateAllSpots(true);
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

            var grade = CleaningMiniGameRules.GetGrade(cleanedSpots, missedSpots);
            var cleanlinessLine = completion.success
                ? $"청결 +{completion.cleanlinessGain}"
                : "청결 변화 없음";
            SetText(
                resultText,
                $"결과  {grade}\n점수 {score} · 닦은 얼룩 {cleanedSpots}개 · 놓친 얼룩 {missedSpots}개\n{cleanlinessLine}\n{completion.message}");
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

            if (cleanedSpots != displayedCleaned || missedSpots != displayedMissed)
            {
                displayedCleaned = cleanedSpots;
                displayedMissed = missedSpots;
                SetText(progressText, $"닦음 {cleanedSpots}  ·  놓침 {missedSpots}");
            }
        }

        private void SpawnDirtSpot()
        {
            if (!sessionActive || playArea == null)
            {
                return;
            }

            var entry = FindAvailableSpot();
            if (entry == null || entry.rect == null)
            {
                return;
            }

            var scale = Random.Range(
                CleaningMiniGameRules.MinimumSpotScale,
                CleaningMiniGameRules.MaximumSpotScale);
            var areaRect = playArea.rect;
            var halfWidth = Mathf.Max(18f, entry.rect.rect.width * scale * 0.5f);
            var halfHeight = Mathf.Max(18f, entry.rect.rect.height * scale * 0.5f);
            var minimumX = areaRect.xMin + halfWidth;
            var maximumX = areaRect.xMax - halfWidth;
            var minimumY = areaRect.yMin + halfHeight;
            var maximumY = areaRect.yMax - halfHeight;
            entry.rect.anchoredPosition = new Vector2(
                maximumX > minimumX ? Random.Range(minimumX, maximumX) : areaRect.center.x,
                maximumY > minimumY ? Random.Range(minimumY, maximumY) : areaRect.center.y);
            entry.rect.localScale = Vector3.one * scale;
            entry.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
            entry.ageSeconds = 0f;
            entry.baseScale = scale;
            entry.active = true;
            if (entry.graphic != null)
            {
                entry.graphic.color = entry.baseColor;
            }

            entry.button.gameObject.SetActive(true);
            entry.rect.SetAsLastSibling();
        }

        private void UpdateDirtSpots(float deltaTime)
        {
            for (var index = 0; index < spotPool.Count; index += 1)
            {
                var entry = spotPool[index];
                if (entry == null || !entry.active || entry.rect == null)
                {
                    continue;
                }

                entry.ageSeconds += deltaTime;
                var normalizedAge = Mathf.Clamp01(
                    entry.ageSeconds / CleaningMiniGameRules.SpotLifetimeSeconds);
                var pulse = 1f + Mathf.Sin(entry.ageSeconds * 7f) * 0.035f;
                entry.rect.localScale = Vector3.one * (entry.baseScale * pulse);
                if (entry.graphic != null)
                {
                    var color = entry.baseColor;
                    color.a *= Mathf.Lerp(1f, 0.48f, normalizedAge);
                    entry.graphic.color = color;
                }

                if (entry.ageSeconds >= CleaningMiniGameRules.SpotLifetimeSeconds)
                {
                    DeactivateSpot(entry);
                    missedSpots += 1;
                }
            }
        }

        private void CleanSpot(DirtSpotEntry entry)
        {
            if (!sessionActive || entry == null || !entry.active)
            {
                return;
            }

            DeactivateSpot(entry);
            cleanedSpots += 1;
            score = CleaningMiniGameRules.CalculateScore(cleanedSpots);
            RefreshSessionLabels();
        }

        private DirtSpotEntry FindAvailableSpot()
        {
            for (var index = 0; index < spotPool.Count; index += 1)
            {
                var entry = spotPool[index];
                if (entry != null && !entry.active && entry.button != null)
                {
                    return entry;
                }
            }

            return spotPool.Count < CleaningMiniGameRules.MaximumPoolSize
                ? CreateSpot()
                : null;
        }

        private void PrewarmPool()
        {
            while (spotPool.Count < CleaningMiniGameRules.InitialPoolSize)
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
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.one * CleaningMiniGameRules.SpotSizePixels;
            }

            var graphic = button.targetGraphic;
            var entry = new DirtSpotEntry(
                button,
                rect,
                graphic,
                graphic != null ? graphic.color : Color.white);
            button.onClick.AddListener(() => CleanSpot(entry));
            button.gameObject.SetActive(false);
            spotPool.Add(entry);
            return entry;
        }

        private void DeactivateAllSpots(bool countAsMissed)
        {
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
            entry.ageSeconds = 0f;
            if (entry.graphic != null)
            {
                entry.graphic.color = entry.baseColor;
            }

            if (entry.button != null)
            {
                entry.button.gameObject.SetActive(false);
            }
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
            public bool active;
            public float ageSeconds;
            public float baseScale;
        }
    }
}
