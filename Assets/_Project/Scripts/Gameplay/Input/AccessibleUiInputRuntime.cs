using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.Gameplay.Input
{
    public static class KeyboardFocusNavigation
    {
        public static bool EnsureFocusWithin(Transform scopeRoot, EventSystem eventSystem)
        {
            if (scopeRoot == null || eventSystem == null)
            {
                return false;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (IsSelectableWithin(selected, scopeRoot))
            {
                return false;
            }

            var candidates = GetCandidates(scopeRoot);
            if (candidates.Count == 0)
            {
                eventSystem.SetSelectedGameObject(null);
                return selected != null;
            }

            eventSystem.SetSelectedGameObject(candidates[0].gameObject);
            return true;
        }

        public static bool TryCycle(Transform scopeRoot, EventSystem eventSystem, bool backwards)
        {
            if (scopeRoot == null || eventSystem == null)
            {
                return false;
            }

            var candidates = GetCandidates(scopeRoot);
            if (candidates.Count == 0)
            {
                eventSystem.SetSelectedGameObject(null);
                return false;
            }

            var current = ResolveSelectable(eventSystem.currentSelectedGameObject);
            var currentIndex = current != null ? candidates.IndexOf(current) : -1;
            var nextIndex = backwards
                ? currentIndex <= 0 ? candidates.Count - 1 : currentIndex - 1
                : currentIndex < 0 || currentIndex >= candidates.Count - 1 ? 0 : currentIndex + 1;
            eventSystem.SetSelectedGameObject(candidates[nextIndex].gameObject);
            return true;
        }

        private static List<Selectable> GetCandidates(Transform scopeRoot)
        {
            var candidates = new List<Selectable>();
            var selectables = scopeRoot.GetComponentsInChildren<Selectable>(false);
            for (var index = 0; index < selectables.Length; index += 1)
            {
                var candidate = selectables[index];
                if (candidate != null
                    && candidate.gameObject.activeInHierarchy
                    && candidate.IsInteractable()
                    && candidate.navigation.mode != Navigation.Mode.None)
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private static bool IsSelectableWithin(GameObject candidate, Transform scopeRoot)
        {
            var selectable = ResolveSelectable(candidate);
            return selectable != null
                && selectable.gameObject.activeInHierarchy
                && selectable.IsInteractable()
                && selectable.navigation.mode != Navigation.Mode.None
                && selectable.transform.IsChildOf(scopeRoot);
        }

        private static Selectable ResolveSelectable(GameObject candidate)
        {
            return candidate != null ? candidate.GetComponentInParent<Selectable>() : null;
        }
    }


    public enum UiPointerGesture
    {
        None,
        Tap,
        LongPress,
        Swipe
    }

    public enum UiSwipeDirection
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// Classifies one pointer sequence so tap, long-press and swipe are mutually exclusive.
    /// Keeping the thresholds outside the MonoBehaviour also makes touch behavior deterministic
    /// in EditMode tests and independent of the active input module.
    /// </summary>
    public sealed class UiPointerGestureTracker
    {
        public const float DefaultLongPressSeconds = 0.55f;
        public const float DefaultHoldSlopPixels = 18f;
        public const float DefaultSwipeDistancePixels = 64f;

        private float longPressSeconds = DefaultLongPressSeconds;
        private float holdSlopPixels = DefaultHoldSlopPixels;
        private float swipeDistancePixels = DefaultSwipeDistancePixels;
        private Vector2 startPosition;
        private Vector2 currentPosition;
        private float startedAt;
        private int pointerId;
        private bool active;
        private bool movedBeyondHoldSlop;
        private bool longPressConsumed;

        public bool IsActive => active;
        public bool LongPressConsumed => longPressConsumed;
        public int PointerId => pointerId;

        public void Configure(
            float requiredLongPressSeconds,
            float allowedHoldSlopPixels,
            float requiredSwipeDistancePixels)
        {
            longPressSeconds = Mathf.Max(0.05f, requiredLongPressSeconds);
            holdSlopPixels = Mathf.Max(1f, allowedHoldSlopPixels);
            swipeDistancePixels = Mathf.Max(holdSlopPixels + 1f, requiredSwipeDistancePixels);
        }

        public void Begin(int activePointerId, Vector2 position, float unscaledTime)
        {
            pointerId = activePointerId;
            startPosition = position;
            currentPosition = position;
            startedAt = unscaledTime;
            active = true;
            movedBeyondHoldSlop = false;
            longPressConsumed = false;
        }

        public bool Track(int activePointerId, Vector2 position)
        {
            if (!active || activePointerId != pointerId)
            {
                return false;
            }

            currentPosition = position;
            if ((currentPosition - startPosition).sqrMagnitude > holdSlopPixels * holdSlopPixels)
            {
                movedBeyondHoldSlop = true;
            }

            return true;
        }

        public void CancelLongPress(int activePointerId)
        {
            if (active && activePointerId == pointerId)
            {
                movedBeyondHoldSlop = true;
            }
        }

        public bool TryConsumeLongPress(float unscaledTime)
        {
            if (!active
                || longPressConsumed
                || movedBeyondHoldSlop
                || unscaledTime - startedAt < longPressSeconds)
            {
                return false;
            }

            longPressConsumed = true;
            return true;
        }

        public UiPointerGesture End(
            int activePointerId,
            Vector2 position,
            out UiSwipeDirection swipeDirection)
        {
            swipeDirection = UiSwipeDirection.None;
            if (!Track(activePointerId, position))
            {
                return UiPointerGesture.None;
            }

            var displacement = currentPosition - startPosition;
            var wasLongPress = longPressConsumed;
            active = false;
            if (wasLongPress)
            {
                return UiPointerGesture.LongPress;
            }

            if (displacement.sqrMagnitude < swipeDistancePixels * swipeDistancePixels)
            {
                return UiPointerGesture.Tap;
            }

            swipeDirection = Mathf.Abs(displacement.x) >= Mathf.Abs(displacement.y)
                ? displacement.x >= 0f ? UiSwipeDirection.Right : UiSwipeDirection.Left
                : displacement.y >= 0f ? UiSwipeDirection.Up : UiSwipeDirection.Down;
            return UiPointerGesture.Swipe;
        }

        public void Cancel()
        {
            active = false;
            movedBeyondHoldSlop = false;
            longPressConsumed = false;
        }
    }

}
