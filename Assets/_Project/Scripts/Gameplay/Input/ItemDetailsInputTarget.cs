using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.Gameplay.Input
{
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
