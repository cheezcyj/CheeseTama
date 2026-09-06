using System;
using CheeseTama.Audio;
using CheeseTama.Gameplay.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Lightweight, non-modal presentation for CheeseTama dialogue. Attach this
    /// component to the Milkroom canvas and keep the bubble itself as a child.
    /// </summary>
    public sealed class CheeseTamaSpeechBubbleController : MonoBehaviour
    {
        private const float DefaultDurationSeconds = 4f;

        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private RectTransform bubbleRect;
        [SerializeField] private Text messageText;
        [SerializeField] private Canvas hostCanvas;
        [SerializeField] private Transform worldTarget;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.45f, 0f);
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 4f);

        private readonly CheeseTamaDialogueRules dialogueRules = new CheeseTamaDialogueRules();
        private CanvasGroup canvasGroup;
        private Action showSound;
        private bool visible;
        private double visibleUntil;
        private CheeseTamaDialoguePriority currentPriority;
        private string currentLineId = string.Empty;

        public bool IsVisible => visible;
        public string CurrentLineId => currentLineId;
        public CheeseTamaDialoguePriority CurrentPriority => currentPriority;
        public CheeseTamaDialogueRules Rules => dialogueRules;

        public void Configure(
            GameObject root,
            RectTransform rect,
            Text label,
            Canvas canvas,
            Transform target,
            Camera projectionCamera = null,
            Action onShowSound = null)
        {
            bubbleRoot = root != null ? root : rect != null ? rect.gameObject : null;
            bubbleRect = rect != null
                ? rect
                : bubbleRoot != null
                    ? bubbleRoot.GetComponent<RectTransform>()
                    : null;
            messageText = label;
            hostCanvas = canvas;
            worldTarget = target;
            worldCamera = projectionCamera;
            showSound = onShowSound;

            EnsureNonBlockingPresentation();
            Hide();
        }

        public void BindWorldTarget(Transform target, Camera projectionCamera = null)
        {
            worldTarget = target;
            if (projectionCamera != null)
            {
                worldCamera = projectionCamera;
            }

            UpdateBubblePosition();
        }

        public void SetOffsets(Vector3 targetWorldOffset, Vector2 bubbleScreenOffset)
        {
            worldOffset = targetWorldOffset;
            screenOffset = bubbleScreenOffset;
            UpdateBubblePosition();
        }

        public bool Show(CheeseTamaDialogueRequest request, bool playSound = false)
        {
            var now = CurrentUnscaledTime();
            if (!dialogueRules.TrySelect(request, now, out var selection)
                || !CanPresent(selection.Priority))
            {
                return false;
            }

            if (!Present(selection.Text, selection.LineId, selection.Priority, selection.DurationSeconds, playSound))
            {
                return false;
            }

            dialogueRules.Remember(selection, now);
            return true;
        }

        public bool Show(CheeseTamaDialogueSelection selection, bool playSound = false)
        {
            return selection.IsValid
                && Present(
                    selection.Text,
                    selection.LineId,
                    selection.Priority,
                    selection.DurationSeconds,
                    playSound);
        }

        public bool Show(
            string message,
            CheeseTamaDialoguePriority priority = CheeseTamaDialoguePriority.Ambient,
            float durationSeconds = DefaultDurationSeconds,
            bool playSound = false)
        {
            return Present(message, string.Empty, priority, durationSeconds, playSound);
        }

        public void Hide()
        {
            visible = false;
            visibleUntil = 0d;
            currentPriority = default;
            currentLineId = string.Empty;
            SetPresentationAlpha(0f);
            SetBubbleRootActive(false);
        }

        private void Awake()
        {
            EnsureNonBlockingPresentation();
            Hide();
        }

        private void OnEnable()
        {
            EnsureNonBlockingPresentation();
            if (!visible)
            {
                SetPresentationAlpha(0f);
                SetBubbleRootActive(false);
            }
        }

        private void OnDisable()
        {
            Hide();
        }

        private void Update()
        {
            if (visible && CurrentUnscaledTime() >= visibleUntil)
            {
                Hide();
            }
        }

        private void LateUpdate()
        {
            if (visible)
            {
                UpdateBubblePosition();
            }
        }

        private bool Present(
            string message,
            string lineId,
            CheeseTamaDialoguePriority priority,
            float durationSeconds,
            bool playSound)
        {
            if (bubbleRoot == null
                || bubbleRect == null
                || messageText == null
                || string.IsNullOrWhiteSpace(message)
                || !CanPresent(priority))
            {
                return false;
            }

            EnsureNonBlockingPresentation();
            messageText.text = message.Trim();
            visible = true;
            currentLineId = lineId ?? string.Empty;
            currentPriority = priority;
            visibleUntil = CurrentUnscaledTime()
                + CheeseTamaDialogueSelection.ClampDuration(durationSeconds);
            SetBubbleRootActive(true);
            SetPresentationAlpha(1f);
            UpdateBubblePosition();

            if (playSound)
            {
                if (showSound != null)
                {
                    showSound.Invoke();
                }
                else
                {
                    CheeseTamaAudioController.Instance?.PlayUiClick();
                }
            }

            return true;
        }

        private bool CanPresent(CheeseTamaDialoguePriority priority)
        {
            return !visible || (int)priority >= (int)currentPriority;
        }

        private void EnsureNonBlockingPresentation()
        {
            if (bubbleRoot == null && bubbleRect != null)
            {
                bubbleRoot = bubbleRect.gameObject;
            }

            if (bubbleRoot == null)
            {
                return;
            }

            canvasGroup = bubbleRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = bubbleRoot.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            foreach (var graphic in bubbleRoot.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != null)
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        private void UpdateBubblePosition()
        {
            if (!visible)
            {
                SetPresentationAlpha(0f);
                SetBubbleRootActive(false);
                return;
            }

            if (bubbleRect == null || worldTarget == null)
            {
                return;
            }

            var projectionCamera = worldCamera != null ? worldCamera : Camera.main;
            if (projectionCamera == null)
            {
                return;
            }

            var screenPoint = projectionCamera.WorldToScreenPoint(worldTarget.position + worldOffset);
            if (screenPoint.z <= 0f)
            {
                SetPresentationAlpha(0f);
                return;
            }

            var parentRect = bubbleRect.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            var uiCamera = hostCanvas != null && hostCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? hostCanvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPoint,
                    uiCamera,
                    out var localPoint))
            {
                return;
            }

            var desired = localPoint + screenOffset;
            var pivot = bubbleRect.pivot;
            var leftExtent = bubbleRect.rect.width * pivot.x;
            var rightExtent = bubbleRect.rect.width * (1f - pivot.x);
            var bottomExtent = bubbleRect.rect.height * pivot.y;
            var topExtent = bubbleRect.rect.height * (1f - pivot.y);
            if (parentRect.rect.width > bubbleRect.rect.width)
            {
                desired.x = Mathf.Clamp(
                    desired.x,
                    parentRect.rect.xMin + leftExtent,
                    parentRect.rect.xMax - rightExtent);
            }

            if (parentRect.rect.height > bubbleRect.rect.height)
            {
                desired.y = Mathf.Clamp(
                    desired.y,
                    parentRect.rect.yMin + bottomExtent,
                    parentRect.rect.yMax - topExtent);
            }

            var localPosition = bubbleRect.localPosition;
            bubbleRect.localPosition = new Vector3(desired.x, desired.y, localPosition.z);
            SetPresentationAlpha(1f);
        }

        private void SetBubbleRootActive(bool active)
        {
            if (bubbleRoot == null
                || bubbleRoot == gameObject
                || bubbleRoot.activeSelf == active)
            {
                return;
            }

            bubbleRoot.SetActive(active);
        }

        private void SetPresentationAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private static double CurrentUnscaledTime()
        {
            return Time.unscaledTimeAsDouble;
        }
    }
}
