using System;
using CheeseTama.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.Gameplay.Input
{
    /// <summary>
    /// EventSystem bridge shared by mouse and touch. Configure it at scene-build time and keep
    /// the existing Button/keyboard route connected to the same authority callback as the
    /// accessibility fallback; this component never removes or replaces Button listeners.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DirectManipulationInputTarget : MonoBehaviour,
        IPointerDownHandler,
        IPointerMoveHandler,
        IPointerUpHandler,
        IDragHandler,
        ISubmitHandler,
        ICancelHandler
    {
        private readonly DirectManipulationGestureTracker tracker =
            new DirectManipulationGestureTracker();

        private RectTransform gestureArea;
        private RectTransform releaseArea;
        private Action completionCallback;
        private Action<DirectManipulationProgress> progressCallback;
        private Func<bool> interactionAllowed;
        private Vector2 lastReportedPosition;
        private float lastReportedProgress = -1f;
        private bool lastReportedActive;
        private bool completionDispatchInProgress;
        private bool allowPointerTapFallback = true;
        private bool completeFromSubmit;
        private float configuredCleaningPathPixels;
        private float configuredCleaningSpanPixels;

        public DirectManipulationKind Kind => tracker.Kind;
        public bool IsGestureActive => tracker.IsActive;
        public float Progress => tracker.Progress;
        public Vector2 GestureStartScreenPosition => tracker.StartPosition;
        public Vector2 CurrentScreenPosition => tracker.CurrentPosition;
        public bool IsConfigured => tracker.Kind != DirectManipulationKind.None
            && gestureArea != null
            && releaseArea != null
            && completionCallback != null;

        public void ConfigureMilkBottleDrag(
            RectTransform dropArea,
            Action onCompleted,
            Action<DirectManipulationProgress> onProgressChanged = null,
            Func<bool> isInteractionAllowed = null,
            float requiredDistancePixels =
                DirectManipulationGestureTracker.DefaultMilkDragDistancePixels)
        {
            ConfigureCore(
                transform as RectTransform,
                dropArea,
                onCompleted,
                onProgressChanged,
                isInteractionAllowed);
            allowPointerTapFallback = true;
            completeFromSubmit = false;
            tracker.ConfigureMilkBottleDrag(requiredDistancePixels);
        }

        public void ConfigureCircularStir(
            RectTransform stirArea,
            Action onCompleted,
            Action<DirectManipulationProgress> onProgressChanged = null,
            Func<bool> isInteractionAllowed = null,
            float requiredDegrees = DirectManipulationGestureTracker.DefaultStirDegrees,
            float minimumRadiusPixels =
                DirectManipulationGestureTracker.DefaultStirMinimumRadiusPixels,
            float maximumRadiusPixels =
                DirectManipulationGestureTracker.DefaultStirMaximumRadiusPixels)
        {
            var resolvedArea = stirArea != null ? stirArea : transform as RectTransform;
            ConfigureCore(
                resolvedArea,
                resolvedArea,
                onCompleted,
                onProgressChanged,
                isInteractionAllowed);
            allowPointerTapFallback = true;
            completeFromSubmit = false;
            tracker.ConfigureCircularStir(
                requiredDegrees,
                minimumRadiusPixels,
                maximumRadiusPixels);
        }

        public void ConfigureCleaningSwipe(
            RectTransform cleaningArea,
            Action onCompleted,
            Action<DirectManipulationProgress> onProgressChanged = null,
            Func<bool> isInteractionAllowed = null,
            float requiredPathPixels =
                DirectManipulationGestureTracker.DefaultCleaningPathPixels,
            float requiredSpanPixels =
                DirectManipulationGestureTracker.DefaultCleaningSpanPixels,
            bool allowTapFallback = true,
            bool completeOnSubmit = false)
        {
            var resolvedArea = cleaningArea != null ? cleaningArea : transform as RectTransform;
            ConfigureCore(
                resolvedArea,
                resolvedArea,
                onCompleted,
                onProgressChanged,
                isInteractionAllowed);
            allowPointerTapFallback = allowTapFallback;
            completeFromSubmit = completeOnSubmit;
            tracker.ConfigureCleaningSwipe(requiredPathPixels, requiredSpanPixels);
            configuredCleaningPathPixels = Mathf.Max(1f, requiredPathPixels);
            configuredCleaningSpanPixels = Mathf.Max(1f, requiredSpanPixels);
        }

        /// <summary>
        /// Optional bridge for a dedicated keyboard-accessible Button. Existing Buttons may keep
        /// invoking the authority API directly; pointer gestures suppress their click only after
        /// meaningful movement so a normal tap remains available.
        /// </summary>
        public bool TryCompleteFromAlternativeInput()
        {
            if (completionDispatchInProgress || !CanInteract())
            {
                return false;
            }

            var authority = completionCallback;
            completionDispatchInProgress = true;
            try
            {
                CancelActiveGesture();
                authority.Invoke();
                return true;
            }
            finally
            {
                completionDispatchInProgress = false;
            }
        }

        public bool CancelActiveGesture()
        {
            if (!tracker.Cancel())
            {
                return false;
            }

            ReportProgress(false, true);
            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left
                || tracker.IsActive
                || !CanInteract()
                || !ContainsScreenPoint(
                    ResolveEffectiveArea(gestureArea),
                    eventData.position,
                    eventData.pressEventCamera))
            {
                return;
            }

            if (tracker.Kind == DirectManipulationKind.CleaningSwipe)
            {
                ConfigureCleaningRequirementsForCurrentScale(eventData.pressEventCamera);
            }

            var circularCenter = tracker.Kind == DirectManipulationKind.CircularStir
                ? GetScreenCenter(
                    ResolveEffectiveArea(gestureArea),
                    eventData.pressEventCamera)
                : eventData.position;
            if (tracker.Begin(eventData.pointerId, eventData.position, circularCenter))
            {
                ReportProgress(true, true);
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            TrackPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            TrackPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null)
            {
                CancelActiveGesture();
                return;
            }

            if (!tracker.OwnsPointer(eventData.pointerId))
            {
                return;
            }

            if (!CanInteract())
            {
                SuppressFallbackClickIfNeeded(eventData, tracker.HadMeaningfulMovement);
                CancelActiveGesture();
                return;
            }

            tracker.Track(eventData.pointerId, eventData.position);
            var meaningfulMovement = tracker.HadMeaningfulMovement;
            var releasedInsideTarget = ContainsScreenPoint(
                ResolveEffectiveArea(releaseArea),
                eventData.position,
                eventData.pressEventCamera);
            var completed = tracker.End(
                eventData.pointerId,
                eventData.position,
                releasedInsideTarget);
            SuppressFallbackClickIfNeeded(
                eventData,
                meaningfulMovement || !allowPointerTapFallback);

            // Capture authority before the presentation callback. A progress presenter may safely
            // disable or reconfigure this component without losing or duplicating this completion.
            var authority = completed ? completionCallback : null;
            if (authority == null)
            {
                ReportProgress(false, true);
                return;
            }

            completionDispatchInProgress = true;
            try
            {
                ReportProgress(false, true);
                authority.Invoke();
            }
            finally
            {
                completionDispatchInProgress = false;
            }
        }

        public void OnCancel(BaseEventData eventData)
        {
            CancelActiveGesture();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (completeFromSubmit)
            {
                TryCompleteFromAlternativeInput();
            }
        }

        private void Update()
        {
            if (tracker.IsActive && !CanInteract())
            {
                CancelActiveGesture();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelActiveGesture();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelActiveGesture();
            }
        }

        private void OnDisable()
        {
            CancelActiveGesture();
        }

        private void OnDestroy()
        {
            tracker.Cancel();
            completionCallback = null;
            progressCallback = null;
            interactionAllowed = null;
            completeFromSubmit = false;
        }

        private void ConfigureCore(
            RectTransform configuredGestureArea,
            RectTransform configuredReleaseArea,
            Action onCompleted,
            Action<DirectManipulationProgress> onProgressChanged,
            Func<bool> isInteractionAllowed)
        {
            CancelActiveGesture();
            gestureArea = configuredGestureArea;
            releaseArea = configuredReleaseArea;
            completionCallback = onCompleted;
            progressCallback = onProgressChanged;
            interactionAllowed = isInteractionAllowed;
            lastReportedProgress = -1f;
            lastReportedPosition = Vector2.zero;
            lastReportedActive = false;
        }

        private void TrackPointer(PointerEventData eventData)
        {
            if (eventData == null || !tracker.OwnsPointer(eventData.pointerId))
            {
                return;
            }

            if (!CanInteract())
            {
                CancelActiveGesture();
                return;
            }

            var previousPosition = tracker.CurrentPosition;
            var previousProgress = tracker.Progress;
            if (tracker.Track(eventData.pointerId, eventData.position)
                && (previousPosition != tracker.CurrentPosition
                    || !Mathf.Approximately(previousProgress, tracker.Progress)))
            {
                ReportProgress(true, false);
            }
        }

        private bool CanInteract()
        {
            return isActiveAndEnabled
                && gameObject.activeInHierarchy
                && IsConfigured
                && gestureArea.gameObject.activeInHierarchy
                && releaseArea.gameObject.activeInHierarchy
                && !GameInputRouter.GameplayInputSuppressed
                && KeyboardFocusScope.IsInteractionAllowed(gameObject)
                && (interactionAllowed == null || interactionAllowed.Invoke());
        }

        private void ReportProgress(bool activeState, bool force)
        {
            if (progressCallback == null)
            {
                return;
            }

            var currentProgress = tracker.Progress;
            var currentPosition = tracker.CurrentPosition;
            if (!force
                && lastReportedActive == activeState
                && Mathf.Approximately(lastReportedProgress, currentProgress)
                && lastReportedPosition == currentPosition)
            {
                return;
            }

            lastReportedActive = activeState;
            lastReportedProgress = currentProgress;
            lastReportedPosition = currentPosition;
            progressCallback.Invoke(new DirectManipulationProgress(
                tracker.Kind,
                currentProgress,
                currentPosition,
                activeState));
        }

        private static bool ContainsScreenPoint(
            RectTransform target,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            return target != null
                && target.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(
                    target,
                    screenPosition,
                    eventCamera);
        }

        private static RectTransform ResolveEffectiveArea(RectTransform source)
        {
            if (source == null)
            {
                return null;
            }

            var adaptive = source.GetComponent<AdaptiveTouchHitArea>();
            var expanded = adaptive?.HitAreaRect;
            return expanded != null && expanded.gameObject.activeInHierarchy
                ? expanded
                : source;
        }

        private void ConfigureCleaningRequirementsForCurrentScale(Camera eventCamera)
        {
            var effectiveArea = ResolveEffectiveArea(gestureArea);
            if (effectiveArea == null)
            {
                tracker.ConfigureCleaningSwipe(
                    configuredCleaningPathPixels,
                    configuredCleaningSpanPixels);
                return;
            }

            var corners = new Vector3[4];
            effectiveArea.GetWorldCorners(corners);
            var first = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            var second = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[1]);
            var fourth = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[3]);
            var minimumDimension = Mathf.Max(
                1f,
                Mathf.Min(Vector2.Distance(first, second), Vector2.Distance(first, fourth)));
            var scaledSpan = Mathf.Min(
                configuredCleaningSpanPixels,
                Mathf.Max(8f, minimumDimension * 0.65f));
            var scaledPath = Mathf.Min(
                configuredCleaningPathPixels,
                Mathf.Max(30f, minimumDimension * 2.5f));
            tracker.ConfigureCleaningSwipe(scaledPath, scaledSpan);
        }

        private static Vector2 GetScreenCenter(RectTransform target, Camera eventCamera)
        {
            var worldCenter = target.TransformPoint(target.rect.center);
            return RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
        }

        private static void SuppressFallbackClickIfNeeded(
            PointerEventData eventData,
            bool meaningfulMovement)
        {
            if (!meaningfulMovement)
            {
                return;
            }

            eventData.eligibleForClick = false;
            eventData.Use();
        }
    }
}
