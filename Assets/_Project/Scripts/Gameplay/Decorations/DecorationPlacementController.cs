using System;
using CheeseTama.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.Gameplay.Decorations
{
    public enum DecorationPlacementKeyboardCommand
    {
        NudgeLeft,
        NudgeRight,
        NudgeUp,
        NudgeDown,
        RotateCounterClockwise,
        RotateClockwise,
        ResetToAnchor,
        Confirm,
        Cancel
    }

    /// <summary>
    /// Converts one explicitly opened placement surface into preview commands. Pointer release does
    /// not persist authority; callers must use Confirm/Edit buttons or the keyboard submit route.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DecorationPlacementController : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        ICancelHandler
    {
        [SerializeField] private RectTransform placementArea;
        [SerializeField] private DecorationRoomPresenter roomPresenter;

        private readonly DecorationPlacementEditSession session =
            new DecorationPlacementEditSession();

        private Func<DecorationPlacementSnapshot> snapshotProvider;
        private Func<DecorationPlacementSnapshot, bool> commitPlacement;
        private Func<bool> interactionAllowed;
        private int activePointerId = int.MinValue;

        public bool IsConfigured => placementArea != null
            && snapshotProvider != null
            && commitPlacement != null;
        public bool IsEditing => session.IsActive;
        public bool OwnsPointer => activePointerId != int.MinValue;
        public bool ExternalCancelRouting { get; set; }
        public DecorationSlot EditingSlot => session.Slot;
        public DecorationPlacementSnapshot Preview => session.Preview;

        public bool CanBeginEdit(DecorationSlot slot)
        {
            return IsConfigured
                && !session.IsActive
                && DecorationPlacementSystem.IsMovableSlot(slot)
                && CanInteract()
                && (roomPresenter == null || roomPresenter.GetMovableVisual(slot) != null);
        }

        public void Configure(
            RectTransform configuredPlacementArea,
            DecorationRoomPresenter configuredPresenter,
            Func<DecorationPlacementSnapshot> getSnapshot,
            Func<DecorationPlacementSnapshot, bool> commitSnapshot,
            Func<bool> isInteractionAllowed = null)
        {
            CancelEdit();
            placementArea = configuredPlacementArea != null
                ? configuredPlacementArea
                : transform as RectTransform;
            roomPresenter = configuredPresenter;
            snapshotProvider = getSnapshot;
            commitPlacement = commitSnapshot;
            interactionAllowed = isInteractionAllowed;
        }

        public bool BeginEdit(DecorationSlot slot)
        {
            if (!CanBeginEdit(slot))
            {
                return false;
            }

            var source = snapshotProvider.Invoke();
            if (!session.Begin(source, slot))
            {
                return false;
            }

            activePointerId = int.MinValue;
            ApplyPreview(session.Preview);
            return true;
        }

        public bool ConfirmEdit()
        {
            if (!session.IsActive || !CanInteract())
            {
                return false;
            }

            var original = session.Original;
            var confirmed = session.Confirm();
            activePointerId = int.MinValue;
            if (commitPlacement.Invoke(confirmed))
            {
                ApplyPreview(confirmed);
                return true;
            }

            ApplyPreview(original);
            return false;
        }

        public bool CancelEdit()
        {
            activePointerId = int.MinValue;
            if (!session.IsActive)
            {
                return false;
            }

            ApplyPreview(session.Cancel());
            return true;
        }

        public bool TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand command)
        {
            if (!session.IsActive || !CanInteract())
            {
                return false;
            }

            switch (command)
            {
                case DecorationPlacementKeyboardCommand.NudgeLeft:
                    return ApplyResult(session.Nudge(-1, 0));
                case DecorationPlacementKeyboardCommand.NudgeRight:
                    return ApplyResult(session.Nudge(1, 0));
                case DecorationPlacementKeyboardCommand.NudgeUp:
                    return ApplyResult(session.Nudge(0, 1));
                case DecorationPlacementKeyboardCommand.NudgeDown:
                    return ApplyResult(session.Nudge(0, -1));
                case DecorationPlacementKeyboardCommand.RotateCounterClockwise:
                    return ApplyResult(session.Rotate(-1));
                case DecorationPlacementKeyboardCommand.RotateClockwise:
                    return ApplyResult(session.Rotate(1));
                case DecorationPlacementKeyboardCommand.ResetToAnchor:
                    return ApplyResult(session.ResetToAnchor());
                case DecorationPlacementKeyboardCommand.Confirm:
                    return ConfirmEdit();
                case DecorationPlacementKeyboardCommand.Cancel:
                    return CancelEdit();
                default:
                    return false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != int.MinValue
                || !CanUsePointerEvent(eventData, requireOwnedPointer: false))
            {
                return;
            }

            activePointerId = eventData.pointerId;
            UpdatePointerPreview(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!CanUsePointerEvent(eventData, requireOwnedPointer: true))
            {
                if (eventData != null && eventData.pointerId == activePointerId)
                {
                    CancelEdit();
                }

                return;
            }

            UpdatePointerPreview(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerId != activePointerId)
            {
                return;
            }

            if (CanUsePointerEvent(eventData, requireOwnedPointer: true))
            {
                UpdatePointerPreview(eventData);
            }

            // Keep the edit session alive for an explicit confirm/cancel decision.
            activePointerId = int.MinValue;
        }

        public void OnCancel(BaseEventData eventData)
        {
            CancelEdit();
        }

        private void Update()
        {
            if (!session.IsActive)
            {
                return;
            }

            if (!CanInteract())
            {
                CancelEdit();
                return;
            }

            if (!ExternalCancelRouting
                && GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.Cancel);
                return;
            }

            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            var selectableFocused = selected != null
                && selected.GetComponentInParent<Selectable>() != null;
            if (!ExternalCancelRouting
                && !selectableFocused
                && GameInputRouter.WasSubmitPressed())
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.Confirm);
                return;
            }

            if (selectableFocused)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.NudgeLeft);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.D))
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.NudgeRight);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.W))
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.NudgeUp);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.NudgeDown);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
            {
                TryHandleKeyboardCommand(
                    DecorationPlacementKeyboardCommand.RotateCounterClockwise);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.RotateClockwise);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Home))
            {
                TryHandleKeyboardCommand(DecorationPlacementKeyboardCommand.ResetToAnchor);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelEdit();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelEdit();
            }
        }

        private void OnDisable()
        {
            CancelEdit();
        }

        private void OnDestroy()
        {
            session.Cancel();
            snapshotProvider = null;
            commitPlacement = null;
            interactionAllowed = null;
        }

        private bool ApplyResult(DecorationPlacementResult result)
        {
            if (!result.Succeeded)
            {
                return false;
            }

            ApplyPreview(result.Snapshot);
            return true;
        }

        private void ApplyPreview(DecorationPlacementSnapshot snapshot)
        {
            roomPresenter?.ApplyPlacementSnapshot(snapshot);
        }

        private bool CanUsePointerEvent(
            PointerEventData eventData,
            bool requireOwnedPointer)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left
                || !session.IsActive
                || !CanInteract()
                || (requireOwnedPointer && eventData.pointerId != activePointerId)
                || !RectTransformUtility.RectangleContainsScreenPoint(
                    placementArea,
                    eventData.position,
                    eventData.pressEventCamera))
            {
                return false;
            }

            var raycastTarget = requireOwnedPointer
                ? eventData.pointerCurrentRaycast.gameObject
                : eventData.pointerPressRaycast.gameObject;
            return raycastTarget == null || IsWithinPlacementSurface(raycastTarget.transform);
        }

        private bool IsWithinPlacementSurface(Transform candidate)
        {
            if (candidate == null
                || (candidate != placementArea && !candidate.IsChildOf(placementArea)))
            {
                return false;
            }

            // A child Button/ScrollRect owns its pointer sequence. Treating it as room input would
            // make an accessibility control move a prop underneath the user's finger or cursor.
            var cursor = candidate;
            while (cursor != null)
            {
                if (cursor.GetComponent<Selectable>() != null
                    || cursor.GetComponent<ScrollRect>() != null)
                {
                    return false;
                }

                if (cursor == placementArea)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            return true;
        }

        private bool CanInteract()
        {
            return isActiveAndEnabled
                && gameObject.activeInHierarchy
                && IsConfigured
                && placementArea.gameObject.activeInHierarchy
                && !GameInputRouter.GameplayInputSuppressed
                && KeyboardFocusScope.IsInteractionAllowed(gameObject)
                && (interactionAllowed == null || interactionAllowed.Invoke());
        }

        private void UpdatePointerPreview(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    placementArea,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                return;
            }

            var rect = placementArea.rect;
            if (rect.width <= 0.001f || rect.height <= 0.001f)
            {
                return;
            }

            var normalizedX = ((localPoint.x - rect.xMin) / rect.width) * 2f - 1f;
            var normalizedY = ((localPoint.y - rect.yMin) / rect.height) * 2f - 1f;
            ApplyResult(session.SetPose(normalizedX, normalizedY));
        }
    }
}
