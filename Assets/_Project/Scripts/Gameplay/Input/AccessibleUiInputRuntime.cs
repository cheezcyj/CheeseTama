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

    [DefaultExecutionOrder(-100)]
    public sealed class KeyboardFocusScope : MonoBehaviour
    {
        private static readonly List<KeyboardFocusScope> ActiveScopes = new List<KeyboardFocusScope>();

        [SerializeField] private Transform focusRoot;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private bool modalScope = true;
        [SerializeField] private bool focusOnActivation = true;

        private GameObject previouslySelected;

        public Transform FocusRoot => focusRoot != null ? focusRoot : transform;
        public bool IsModalScope => modalScope;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ActiveScopes.Clear();
        }

        public void Configure(
            Transform root,
            bool isModalScope = true,
            bool shouldFocusOnActivation = true,
            EventSystem targetEventSystem = null)
        {
            focusRoot = root != null ? root : transform;
            eventSystem = targetEventSystem != null ? targetEventSystem : EventSystem.current;
            modalScope = isModalScope;
            focusOnActivation = shouldFocusOnActivation;
            if (!isActiveAndEnabled)
            {
                return;
            }

            // Configure is also the deterministic registration point for callers that construct
            // scopes before Play Mode (tests and editor tooling do not receive runtime Update).
            Register();
            if (focusOnActivation && CanOwnFocus())
            {
                EnsureFocusWithinScope();
            }
        }

        public bool EnsureFocusWithinScope()
        {
            return CanOwnFocus()
                && KeyboardFocusNavigation.EnsureFocusWithin(FocusRoot, ResolveEventSystem());
        }

        public bool CycleFocus(bool backwards = false)
        {
            return CanOwnFocus()
                && KeyboardFocusNavigation.TryCycle(FocusRoot, ResolveEventSystem(), backwards);
        }

        public static bool IsInteractionAllowed(GameObject candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var topModal = ResolveTopModalScope();
            return topModal == null || candidate.transform.IsChildOf(topModal.FocusRoot);
        }

        private void OnEnable()
        {
            var resolvedEventSystem = ResolveEventSystem();
            previouslySelected = resolvedEventSystem != null
                ? resolvedEventSystem.currentSelectedGameObject
                : null;
            Register();
            if (focusOnActivation && CanOwnFocus())
            {
                EnsureFocusWithinScope();
            }
        }

        private void Start()
        {
            if (focusOnActivation && CanOwnFocus())
            {
                EnsureFocusWithinScope();
            }
        }

        private void Update()
        {
            if (!CanOwnFocus())
            {
                return;
            }

            if (modalScope)
            {
                EnsureFocusWithinScope();
            }

            if (GameInputRouter.WasNextPanelPressed())
            {
                var backwards = UnityEngine.Input.GetKey(KeyCode.LeftShift)
                    || UnityEngine.Input.GetKey(KeyCode.RightShift);
                CycleFocus(backwards);
            }
        }

        private void OnDisable()
        {
            Unregister();
            RestorePreviousSelection();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Register()
        {
            ActiveScopes.Remove(this);
            ActiveScopes.Add(this);
        }

        private void Unregister()
        {
            ActiveScopes.Remove(this);
        }

        private bool CanOwnFocus()
        {
            if (!isActiveAndEnabled || !FocusRoot.gameObject.activeInHierarchy)
            {
                return false;
            }

            var topModal = ResolveTopModalScope();
            if (topModal != null)
            {
                return ReferenceEquals(topModal, this);
            }

            var resolvedEventSystem = ResolveEventSystem();
            var current = resolvedEventSystem != null
                ? resolvedEventSystem.currentSelectedGameObject
                : null;
            for (var index = ActiveScopes.Count - 1; index >= 0; index -= 1)
            {
                var scope = ActiveScopes[index];
                if (!IsUsable(scope) || scope.modalScope)
                {
                    continue;
                }

                if (current == null || current.transform.IsChildOf(scope.FocusRoot))
                {
                    return ReferenceEquals(scope, this);
                }
            }

            return false;
        }

        private void RestorePreviousSelection()
        {
            var resolvedEventSystem = ResolveEventSystem();
            if (resolvedEventSystem != null
                && previouslySelected != null
                && previouslySelected.activeInHierarchy
                && IsInteractionAllowed(previouslySelected))
            {
                resolvedEventSystem.SetSelectedGameObject(previouslySelected);
            }

            previouslySelected = null;
        }

        private EventSystem ResolveEventSystem()
        {
            if (eventSystem == null)
            {
                eventSystem = EventSystem.current;
            }

            return eventSystem;
        }

        private static KeyboardFocusScope ResolveTopModalScope()
        {
            for (var index = ActiveScopes.Count - 1; index >= 0; index -= 1)
            {
                var scope = ActiveScopes[index];
                if (IsUsable(scope) && scope.modalScope)
                {
                    return scope;
                }
            }

            return null;
        }

        private static bool IsUsable(KeyboardFocusScope scope)
        {
            return scope != null
                && scope.isActiveAndEnabled
                && scope.FocusRoot != null
                && scope.FocusRoot.gameObject.activeInHierarchy;
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

    public sealed class ItemDetailsInputTarget : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IPointerMoveHandler
    {
        private readonly UiPointerGestureTracker pointerGesture = new UiPointerGestureTracker();
        private Action<GameObject> detailsRequested;
        private Action<GameObject, UiSwipeDirection> swipeRequested;

        public void Configure(
            Action<GameObject> onDetailsRequested,
            Action<GameObject, UiSwipeDirection> onSwipeRequested = null)
        {
            detailsRequested = onDetailsRequested;
            swipeRequested = onSwipeRequested;
        }

        public void ConfigureGestureThresholds(
            float longPressSeconds,
            float holdSlopPixels,
            float swipeDistancePixels)
        {
            pointerGesture.Configure(longPressSeconds, holdSlopPixels, swipeDistancePixels);
        }

        public bool RequestDetails()
        {
            if (!isActiveAndEnabled
                || detailsRequested == null
                || !KeyboardFocusScope.IsInteractionAllowed(gameObject))
            {
                return false;
            }

            detailsRequested(gameObject);
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                RequestDetails();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left
                || pointerGesture.IsActive
                || (detailsRequested == null && swipeRequested == null)
                || !KeyboardFocusScope.IsInteractionAllowed(gameObject))
            {
                return;
            }

            pointerGesture.Begin(eventData.pointerId, eventData.position, Time.unscaledTime);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (eventData != null)
            {
                pointerGesture.Track(eventData.pointerId, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData != null)
            {
                // Keep tracking a possible swipe for the parent ScrollRect, but a pointer that
                // leaves this target can no longer become a long-press detail request.
                pointerGesture.CancelLongPress(eventData.pointerId);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null)
            {
                pointerGesture.Cancel();
                return;
            }

            var gesture = pointerGesture.End(
                eventData.pointerId,
                eventData.position,
                out var swipeDirection);
            if (gesture != UiPointerGesture.LongPress && gesture != UiPointerGesture.Swipe)
            {
                return;
            }

            // PointerInputModule checks eligibleForClick after dispatching pointer-up. Clearing it
            // prevents a completed long-press or swipe from also activating the existing Button.
            eventData.eligibleForClick = false;
            eventData.Use();
            if (gesture == UiPointerGesture.Swipe
                && swipeRequested != null
                && KeyboardFocusScope.IsInteractionAllowed(gameObject))
            {
                swipeRequested(gameObject, swipeDirection);
            }
        }

        private void Update()
        {
            if (pointerGesture.TryConsumeLongPress(Time.unscaledTime))
            {
                RequestDetails();
            }
        }

        private void OnDisable()
        {
            pointerGesture.Cancel();
        }

        private void OnDestroy()
        {
            pointerGesture.Cancel();
            detailsRequested = null;
            swipeRequested = null;
        }
    }
}
