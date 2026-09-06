using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.MiniGames;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Runs one blue-ball session using the shared rules duration. Pointer and touch input both arrive
    /// through the uGUI Button, while the supplied callback owns the single save commit.
    /// </summary>
    public sealed class BlueBallMiniGameController : MonoBehaviour
    {
        public const string OverlayObjectName = "Blue Ball Mini Game Overlay";
        public const float BallLifetimeSeconds = 1.35f;
        public const float BallEdgePaddingPixels = 14f;

        private static readonly string[] BlockingOverlayNames =
        {
            NewGameSetupController.OverlayObjectName,
            "First Meeting Onboarding Overlay",
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
            InputBindingsPanelController.OverlayObjectName,
            "Milk Blending Overlay",
            CookingChoicePanelController.OverlayObjectName,
            NpcVisitCardController.OverlayObjectName,
            JourneyHubPanelController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName,
            LifeRecordsPanelController.OverlayObjectName
        };

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private Button ballButton;
        [SerializeField] private Text remainingTimeText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text resultText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private Func<int, int, int, int, BlueBallCompletionResult> completionHandler;
        private bool configured;
        private bool sessionActive;
        private bool showingResult;
        private bool completionCommitted;
        private float elapsedSeconds;
        private float ballAgeSeconds;
        private int hits;
        private int misses;
        private int score;
        private int combo;
        private int highestCombo;
        private int displayedRemaining = -1;
        private int displayedScore = -1;
        private int displayedCombo = -1;
        private int displayedHits = -1;
        private int displayedMisses = -1;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomBarWasEnabled;
        private bool devPanelWasEnabled;
        private bool milkroomUiWasEnabled;
        private GameObject previouslySelectedObject;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeSelf;
        public bool IsSessionActive => sessionActive;
        public bool IsShowingResult => showingResult;
        public int Hits => hits;
        public int Misses => misses;
        public int Score => score;
        public int Combo => combo;
        public int HighestCombo => highestCombo;

        public void Configure(
            GameObject root,
            RectTransform ballPlayArea,
            Button ballInputButton,
            Text timeLabel,
            Text sessionScoreLabel,
            Text sessionComboLabel,
            Text sessionResultLabel,
            Button sessionCancelButton,
            Button resultConfirmButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController tamaVisual,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController,
            Func<int, int, int, int, BlueBallCompletionResult> completeSession = null)
        {
            AbortSession();
            RestoreControls();
            UnbindButtons();

            overlayRoot = root;
            playArea = ballPlayArea;
            ballButton = ballInputButton;
            remainingTimeText = timeLabel;
            scoreText = sessionScoreLabel;
            comboText = sessionComboLabel;
            resultText = sessionResultLabel;
            cancelButton = sessionCancelButton;
            confirmButton = resultConfirmButton;
            milkroomUi = uiController;
            visualController = tamaVisual;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            completionHandler = completeSession;
            configured = overlayRoot != null
                && playArea != null
                && ballButton != null
                && cancelButton != null
                && confirmButton != null;

            NormalizeTopLeftAnchor(ballButton != null
                ? ballButton.transform as RectTransform
                : null);

            BindButtons();
            SetOverlayActive(false);
            SetButtonVisible(ballButton, true);
            SetButtonVisible(cancelButton, true);
            SetButtonVisible(confirmButton, false);
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
            AbortSession();
            RestoreControls();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && sessionActive)
            {
                Cancel();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && sessionActive)
            {
                Cancel();
            }
        }

        private void Update()
        {
            if (!configured || !Application.isPlaying || !IsBlockingGameplay)
            {
                return;
            }

            if (CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(
                    CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
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

            AdvanceSession(Mathf.Max(0f, Time.unscaledDeltaTime));
        }

        public bool Open()
        {
            if (!configured
                || !Application.isPlaying
                || IsBlockingGameplay
                || IsAnotherModalBlocking())
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();
            previouslySelectedObject = EventSystem.current?.currentSelectedGameObject;
            elapsedSeconds = 0f;
            ballAgeSeconds = 0f;
            hits = 0;
            misses = 0;
            score = 0;
            combo = 0;
            highestCombo = 0;
            displayedRemaining = -1;
            displayedScore = -1;
            displayedCombo = -1;
            displayedHits = -1;
            displayedMisses = -1;
            completionCommitted = false;
            showingResult = false;
            sessionActive = true;

            SetText(resultText, "파란 공을 빠르게 눌러 콤보를 이어 보세요!");
            SetButtonVisible(ballButton, true);
            SetButtonVisible(cancelButton, true);
            SetButtonVisible(confirmButton, false);
            PositionBall();
            SuspendControls();
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            RefreshLabels();
            EventSystem.current?.SetSelectedGameObject(null);
            return true;
        }

        public void HitBall()
        {
            if (!sessionActive)
            {
                return;
            }

            hits = SaturatingAdd(hits, 1);
            combo = SaturatingAdd(combo, 1);
            highestCombo = Mathf.Max(highestCombo, combo);
            score = SaturatingAdd(score, BlueBallMiniGameRules.CalculateHitScore(combo));
            ballAgeSeconds = 0f;
            PositionBall();
            SetText(resultText, combo >= 3 ? $"콤보 x{combo}!" : "통통! 파란 공을 잡았어요.");
            visualController?.ReactAction(CheeseTamaVisualAction.Play);
            RefreshLabels();
        }

        public void Cancel()
        {
            if (!sessionActive)
            {
                return;
            }

            AbortSession();
            SetOverlayActive(false);
            RestoreControls();
            RestoreSelection();
            milkroomUi?.ShowMessage("파란 공 놀이를 취소했어요.");
        }

        public void CloseResult()
        {
            if (sessionActive || !showingResult)
            {
                return;
            }

            showingResult = false;
            SetOverlayActive(false);
            RestoreControls();
            RestoreSelection();
        }

        private void AdvanceSession(float deltaSeconds)
        {
            elapsedSeconds += deltaSeconds;
            ballAgeSeconds += deltaSeconds;
            while (sessionActive && ballAgeSeconds >= BallLifetimeSeconds)
            {
                ballAgeSeconds -= BallLifetimeSeconds;
                misses = SaturatingAdd(misses, 1);
                combo = 0;
                SetText(resultText, "공을 놓쳤어요. 새 위치를 노려 보세요!");
                PositionBall();
            }

            RefreshLabels();
            if (elapsedSeconds >= BlueBallMiniGameRules.SessionSeconds)
            {
                FinishSession();
            }
        }

        private void FinishSession()
        {
            if (!sessionActive || completionCommitted)
            {
                return;
            }

            completionCommitted = true;
            sessionActive = false;
            showingResult = true;
            var session = BlueBallMiniGameRules.Complete(
                hits,
                misses,
                score,
                highestCombo);
            var result = completionHandler != null
                ? completionHandler(
                    session.hits,
                    session.misses,
                    session.score,
                    session.highestCombo)
                : new BlueBallCompletionResult(
                    false,
                    session.hits,
                    session.misses,
                    session.score,
                    0,
                    "저장 시스템을 찾지 못해 이번 기록은 반영되지 않았어요.");

            hits = result.hits;
            misses = result.misses;
            score = result.score;
            SetText(
                resultText,
                $"성공 {hits} · 놓침 {misses} · 최고 콤보 x{highestCombo} · 점수 {score}\n최고 점수 {result.bestScore}\n{result.message}");
            SetButtonVisible(ballButton, false);
            SetButtonVisible(cancelButton, false);
            SetButtonVisible(confirmButton, true);
            RefreshLabels();
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(confirmButton.gameObject);
        }

        private void PositionBall()
        {
            if (playArea == null || ballButton == null)
            {
                return;
            }

            var ballRect = ballButton.transform as RectTransform;
            if (ballRect == null)
            {
                return;
            }

            NormalizeTopLeftAnchor(ballRect);
            var areaWidth = Mathf.Max(0f, playArea.rect.width);
            var areaHeight = Mathf.Max(0f, playArea.rect.height);
            var halfWidth = Mathf.Max(24f, ballRect.rect.width * 0.5f);
            var halfHeight = Mathf.Max(24f, ballRect.rect.height * 0.5f);
            var minimumX = halfWidth + BallEdgePaddingPixels;
            var maximumX = areaWidth - halfWidth - BallEdgePaddingPixels;
            var minimumY = -areaHeight + halfHeight + BallEdgePaddingPixels;
            var maximumY = -halfHeight - BallEdgePaddingPixels;
            ballRect.anchoredPosition = new Vector2(
                minimumX <= maximumX
                    ? UnityEngine.Random.Range(minimumX, maximumX)
                    : areaWidth * 0.5f,
                minimumY <= maximumY
                    ? UnityEngine.Random.Range(minimumY, maximumY)
                    : -areaHeight * 0.5f);
            ballRect.SetAsLastSibling();
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

        private void RefreshLabels()
        {
            var remaining = Mathf.Max(
                0,
                Mathf.CeilToInt(BlueBallMiniGameRules.SessionSeconds - elapsedSeconds));
            if (remaining != displayedRemaining)
            {
                displayedRemaining = remaining;
                SetText(remainingTimeText, $"남은 시간  {remaining}초");
            }

            if (score != displayedScore
                || hits != displayedHits
                || misses != displayedMisses)
            {
                displayedScore = score;
                displayedHits = hits;
                displayedMisses = misses;
                SetText(scoreText, $"점수  {score} · 성공 {hits} · 놓침 {misses}");
            }

            if (combo != displayedCombo)
            {
                displayedCombo = combo;
                SetText(comboText, combo > 1 ? $"콤보  x{combo}" : "콤보  -");
            }
        }

        private bool IsAnotherModalBlocking()
        {
            var container = overlayRoot != null && overlayRoot.transform.parent != null
                ? overlayRoot.transform.parent
                : transform;
            var onboarding = container.GetComponent<FirstMeetingOnboardingController>();
            if (onboarding != null && onboarding.IsBlockingGameplay)
            {
                var save = GameManager.Instance?.CurrentSave?.onboarding;
                if (save == null || save.currentStep != CheeseTama.Save.FirstMeetingOnboardingStep.Care)
                {
                    return true;
                }
            }

            for (var index = 0; index < BlockingOverlayNames.Length; index += 1)
            {
                var blocker = container.Find(BlockingOverlayNames[index]);
                if (blocker != null
                    && blocker.gameObject != overlayRoot
                    && blocker.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void BindButtons()
        {
            if (ballButton != null)
            {
                ballButton.onClick.RemoveListener(HitBall);
                ballButton.onClick.AddListener(HitBall);
            }

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
            ballButton?.onClick.RemoveListener(HitBall);
            cancelButton?.onClick.RemoveListener(Cancel);
            confirmButton?.onClick.RemoveListener(CloseResult);
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
            milkroomUiWasEnabled = milkroomUi != null && milkroomUi.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (bottomActionBarController != null) bottomActionBarController.enabled = false;
            if (devPanelController != null) devPanelController.enabled = false;
            if (milkroomUi != null) milkroomUi.enabled = false;
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
            if (milkroomUi != null) milkroomUi.enabled = milkroomUiWasEnabled;
            controlsSuspended = false;
        }

        private void AbortSession()
        {
            sessionActive = false;
            showingResult = false;
            completionCommitted = false;
            ballAgeSeconds = 0f;
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
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

        private static int SaturatingAdd(int left, int right)
        {
            var value = (long)left + right;
            return value >= int.MaxValue ? int.MaxValue : (int)value;
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
    }
}
