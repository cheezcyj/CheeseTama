using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.Gameplay.Input
{
    /// <summary>
    /// Converts a single mouse or touch drag into continuous screen-position updates. The
    /// gameplay owner remains responsible for converting and clamping the supplied position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HorizontalDragInputTarget : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        ICancelHandler
    {
        private Action<Vector2, Camera> positionChanged;
        private Func<bool> interactionAllowed;
        private int pointerId;
        private bool dragging;

        public bool IsConfigured => positionChanged != null;
        public bool IsDragging => dragging;

        public void Configure(
            Action<Vector2, Camera> onPositionChanged,
            Func<bool> isInteractionAllowed = null)
        {
            CancelDrag();
            positionChanged = onPositionChanged;
            interactionAllowed = isInteractionAllowed;
        }

        public void ClearConfiguration()
        {
            CancelDrag();
            positionChanged = null;
            interactionAllowed = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left
                || dragging
                || !CanInteract())
            {
                return;
            }

            pointerId = eventData.pointerId;
            dragging = true;
            Dispatch(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null || !dragging || eventData.pointerId != pointerId)
            {
                return;
            }

            if (!CanInteract())
            {
                CancelDrag();
                return;
            }

            Dispatch(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || !dragging || eventData.pointerId != pointerId)
            {
                return;
            }

            if (CanInteract())
            {
                Dispatch(eventData);
            }

            CancelDrag();
        }

        public void OnCancel(BaseEventData eventData)
        {
            CancelDrag();
        }

        private void Update()
        {
            if (dragging && !CanInteract())
            {
                CancelDrag();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelDrag();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelDrag();
            }
        }

        private void OnDisable()
        {
            CancelDrag();
        }

        private void OnDestroy()
        {
            ClearConfiguration();
        }

        private bool CanInteract()
        {
            return isActiveAndEnabled
                && gameObject.activeInHierarchy
                && positionChanged != null
                && KeyboardFocusScope.IsInteractionAllowed(gameObject)
                && (interactionAllowed == null || interactionAllowed.Invoke());
        }

        private void Dispatch(PointerEventData eventData)
        {
            positionChanged?.Invoke(eventData.position, eventData.pressEventCamera);
        }

        private void CancelDrag()
        {
            dragging = false;
            pointerId = 0;
        }
    }
}
