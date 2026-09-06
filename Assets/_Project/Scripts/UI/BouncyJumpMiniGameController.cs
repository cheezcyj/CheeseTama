using CheeseTama.Core;
using CheeseTama.Gameplay.MiniGames;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class BouncyJumpMiniGameController : MonoBehaviour
    {
        public const string OverlayObjectName = "Bouncy Jump Overlay";

        private static readonly string[] BlockingOverlayNames =
        {
            NewGameSetupController.OverlayObjectName, "First Meeting Onboarding Overlay", "Return Summary Overlay",
            "Growth Achievement Overlay", "Evolution Achievement Overlay", "Care Event Overlay",
            "Cleaning Mini Game Overlay", "Milk Drop Catch Overlay", "Play Choice Overlay",
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

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private RectTransform tamaMarker;
        [SerializeField] private RectTransform targetZone;
        [SerializeField] private Text remainingTimeText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text resultText;
        [SerializeField] private Button jumpButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private bool configured;
        private bool sessionActive;
        private bool showingResult;
        private bool completionCommitted;
        private float elapsedSeconds;
        private float jumpAnimationSeconds;
        private float markerPhaseOffset;
        private int successes;
        private int misses;
        private int score;
        private int combo;
        private int highestCombo;
        private int displayedRemaining = -1;
        private int displayedScore = -1;
        private int displayedCombo = -1;

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

        public void Configure(
            GameObject root,
            RectTransform jumpPlayArea,
            RectTransform marker,
            RectTransform target,
            Text timeLabel,
            Text sessionScoreLabel,
            Text sessionComboLabel,
            Text sessionResultLabel,
            Button jumpInputButton,
            Button sessionCancelButton,
            Button resultConfirmButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController tamaVisual,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            AbortSession();
            RestoreControls();
            UnbindButtons();
            overlayRoot = root;
            playArea = jumpPlayArea;
            tamaMarker = marker;
            targetZone = target;
            remainingTimeText = timeLabel;
            scoreText = sessionScoreLabel;
            comboText = sessionComboLabel;
            resultText = sessionResultLabel;
            jumpButton = jumpInputButton;
            cancelButton = sessionCancelButton;
            confirmButton = resultConfirmButton;
            milkroomUi = uiController;
            visualController = tamaVisual;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = overlayRoot != null
                && playArea != null
                && tamaMarker != null
                && targetZone != null
                && jumpButton != null
                && cancelButton != null
                && confirmButton != null;
            BindButtons();
            SetOverlayActive(false);
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

        private void Update()
        {
            if (!configured || !Application.isPlaying || !IsBlockingGameplay)
            {
                return;
            }

            if (CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                if (sessionActive) Cancel();
                else CloseResult();
                return;
            }

            if (!sessionActive)
            {
                return;
            }

            var delta = Mathf.Max(0f, Time.unscaledDeltaTime);
            elapsedSeconds += delta;
            jumpAnimationSeconds = Mathf.Max(0f, jumpAnimationSeconds - delta);
            UpdateMarkerPosition();
            RefreshLabels();
            if (elapsedSeconds >= BouncyJumpMiniGameRules.SessionSeconds)
            {
                FinishSession();
            }
        }

        public bool Open()
        {
            if (!configured || !Application.isPlaying || IsAnotherModalBlocking())
            {
                return false;
            }

            previouslySelectedObject = EventSystem.current?.currentSelectedGameObject;
            elapsedSeconds = 0f;
            jumpAnimationSeconds = 0f;
            markerPhaseOffset = Random.value * Mathf.PI * 2f;
            successes = 0;
            misses = 0;
            score = 0;
            combo = 0;
            highestCombo = 0;
            displayedRemaining = -1;
            displayedScore = -1;
            displayedCombo = -1;
            completionCommitted = false;
            sessionActive = true;
            showingResult = false;
            PositionTarget();
            SetText(resultText, "빛나는 착지 구역과 겹칠 때 점프하세요!");
            SetButtonVisible(cancelButton, true);
            SetButtonVisible(confirmButton, false);
            SetButtonVisible(jumpButton, true);
            SuspendControls();
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            RefreshLabels();
            EventSystem.current?.SetSelectedGameObject(jumpButton.gameObject);
            return true;
        }

        public void AttemptJump()
        {
            if (!sessionActive || tamaMarker == null || targetZone == null)
            {
                return;
            }

            var targetHalfWidth = Mathf.Max(1f, targetZone.rect.width * 0.5f);
            var distance = Mathf.Abs(tamaMarker.anchoredPosition.x - targetZone.anchoredPosition.x);
            var normalizedDistance = distance / targetHalfWidth;
            var attemptScore = BouncyJumpMiniGameRules.CalculateAttemptScore(normalizedDistance, combo + 1);
            jumpAnimationSeconds = 0.42f;
            if (attemptScore > 0)
            {
                successes += 1;
                combo += 1;
                highestCombo = Mathf.Max(highestCombo, combo);
                score = SaturatingAdd(score, attemptScore);
                SetText(resultText, normalizedDistance <= 0.35f ? "완벽한 착지!" : "말랑하게 착지했어요!");
                visualController?.ReactAction(CheeseTamaVisualAction.Play);
            }
            else
            {
                misses += 1;
                combo = 0;
                SetText(resultText, "조금 빗나갔어요. 다음 박자를 노려 보세요!");
            }

            markerPhaseOffset += Random.Range(0.35f, 1.25f);
            PositionTarget();
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
        }

        public void CloseResult()
        {
            if (sessionActive)
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

            completionCommitted = true;
            sessionActive = false;
            showingResult = true;
            var manager = GameManager.Instance;
            var result = manager != null
                ? manager.CompleteBouncyJumpMiniGame(successes, misses, score, highestCombo)
                : new BouncyJumpCompletionResult(false, successes, misses, score, 0,
                    "저장 시스템을 찾지 못해 이번 기록은 반영되지 않았어요.");
            SetText(resultText,
                $"성공 {successes} · 놓침 {misses} · 점수 {score}\n{result.message}");
            SetButtonVisible(jumpButton, false);
            SetButtonVisible(cancelButton, false);
            SetButtonVisible(confirmButton, true);
            overlayRoot.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(confirmButton.gameObject);
        }

        private void UpdateMarkerPosition()
        {
            if (playArea == null || tamaMarker == null)
            {
                return;
            }

            var halfWidth = Mathf.Max(0f, (playArea.rect.width - tamaMarker.rect.width) * 0.5f - 12f);
            var phase = elapsedSeconds / BouncyJumpMiniGameRules.MarkerTravelSeconds * Mathf.PI * 2f
                + markerPhaseOffset;
            var x = Mathf.Sin(phase) * halfWidth;
            var jumpProgress = jumpAnimationSeconds <= 0f ? 0f : 1f - jumpAnimationSeconds / 0.42f;
            var y = jumpAnimationSeconds <= 0f ? -112f : -112f + Mathf.Sin(jumpProgress * Mathf.PI) * 96f;
            tamaMarker.anchoredPosition = new Vector2(x, y);
        }

        private void PositionTarget()
        {
            if (playArea == null || targetZone == null)
            {
                return;
            }

            var difficulty = Mathf.Clamp01(elapsedSeconds / BouncyJumpMiniGameRules.SessionSeconds);
            var widthRatio = Mathf.Lerp(
                BouncyJumpMiniGameRules.MaximumTargetWidthRatio,
                BouncyJumpMiniGameRules.MinimumTargetWidthRatio,
                difficulty);
            var width = Mathf.Max(72f, playArea.rect.width * widthRatio);
            targetZone.sizeDelta = new Vector2(width, targetZone.sizeDelta.y);
            var halfRange = Mathf.Max(0f, (playArea.rect.width - width) * 0.5f - 18f);
            targetZone.anchoredPosition = new Vector2(Random.Range(-halfRange, halfRange), -112f);
        }

        private void RefreshLabels()
        {
            var remaining = Mathf.Max(0, Mathf.CeilToInt(BouncyJumpMiniGameRules.SessionSeconds - elapsedSeconds));
            if (remaining != displayedRemaining)
            {
                displayedRemaining = remaining;
                SetText(remainingTimeText, $"남은 시간  {remaining}초");
            }

            if (score != displayedScore)
            {
                displayedScore = score;
                SetText(scoreText, $"점수  {score}");
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
                if (blocker != null && blocker.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void SuspendControls()
        {
            if (controlsSuspended) return;
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
            if (!controlsSuspended) return;
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
        }

        private void BindButtons()
        {
            if (jumpButton != null)
            {
                jumpButton.onClick.RemoveListener(AttemptJump);
                jumpButton.onClick.AddListener(AttemptJump);
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
            jumpButton?.onClick.RemoveListener(AttemptJump);
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

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
