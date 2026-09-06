using System;
using CheeseTama.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.Gameplay.Decorations
{
    /// <summary>
    /// Owns the blocking placement overlay while the lower-level placement controller owns
    /// pointer preview and explicit commit semantics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DecorationPlacementOverlayController : MonoBehaviour
    {
        public const string OverlayObjectName = "Decoration Placement Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private DecorationPlacementController placementController;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button accentButton;
        [SerializeField] private Button shelfButton;
        [SerializeField] private Button bedsideButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;
        [SerializeField] private Button nudgeLeftButton;
        [SerializeField] private Button nudgeRightButton;
        [SerializeField] private Button nudgeUpButton;
        [SerializeField] private Button nudgeDownButton;
        [SerializeField] private Button resetButton;

        private Action<bool> blockingChanged;
        private bool blockingNotified;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

        public void Configure(
            GameObject root,
            DecorationPlacementController controller,
            Text status,
            Button open,
            Button close,
            Button accent,
            Button shelf,
            Button bedside,
            Button confirm,
            Button cancel,
            Button rotateLeft,
            Button rotateRight,
            Button nudgeLeft,
            Button nudgeRight,
            Button nudgeUp,
            Button nudgeDown,
            Button reset,
            Action<bool> onBlockingChanged = null)
        {
            UnbindButtons();
            ReleaseBlocking();
            overlayRoot = root;
            placementController = controller;
            statusLabel = status;
            openButton = open;
            closeButton = close;
            accentButton = accent;
            shelfButton = shelf;
            bedsideButton = bedside;
            confirmButton = confirm;
            cancelButton = cancel;
            rotateLeftButton = rotateLeft;
            rotateRightButton = rotateRight;
            nudgeLeftButton = nudgeLeft;
            nudgeRightButton = nudgeRight;
            nudgeUpButton = nudgeUp;
            nudgeDownButton = nudgeDown;
            resetButton = reset;
            blockingChanged = onBlockingChanged;
            if (placementController != null)
            {
                placementController.ExternalCancelRouting = true;
            }
            BindButtons();
            RefreshSlotAvailability();
            SetOverlayActive(false);
            SetStatus("이동할 장식을 고른 뒤 방 안에서 위치를 정해 주세요.");
        }

        public void Open()
        {
            if (overlayRoot == null || placementController == null)
            {
                return;
            }

            placementController.CancelEdit();
            SetOverlayActive(true);
            RefreshSlotAvailability();
            NotifyBlocking(true);
            SetStatus("선반 장식을 선택하세요.");
        }

        public void Close()
        {
            placementController?.CancelEdit();
            SetOverlayActive(false);
            ReleaseBlocking();
        }

        private void Update()
        {
            if (!IsOpen || !GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                return;
            }

            if (placementController != null && placementController.IsEditing)
            {
                CancelEdit();
            }
            else
            {
                Close();
            }
        }

        private void OnDisable()
        {
            if (overlayRoot != null && overlayRoot.activeSelf)
            {
                placementController?.CancelEdit();
                SetOverlayActive(false);
            }

            ReleaseBlocking();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            ReleaseBlocking();
            if (placementController != null)
            {
                placementController.ExternalCancelRouting = false;
            }
            blockingChanged = null;
        }

        private void BeginAccent()
        {
            Begin(DecorationSlot.Accent, "포인트 장식을 이동 중입니다.");
        }

        private void BeginShelf()
        {
            Begin(DecorationSlot.Shelf, "선반 장식을 이동 중입니다.");
        }

        private void BeginBedside()
        {
            Begin(DecorationSlot.Bedside, "침대 장식을 이동 중입니다.");
        }

        private void Begin(DecorationSlot slot, string message)
        {
            if (placementController != null && placementController.BeginEdit(slot))
            {
                EventSystem.current?.SetSelectedGameObject(null);
                SetStatus(message + " 드래그하거나 방향 버튼으로 조정한 뒤 확인하세요.");
                return;
            }

            SetStatus("지금은 이 장식을 이동할 수 없습니다.");
        }

        private void RefreshSlotAvailability()
        {
            SetSlotAvailability(accentButton, DecorationSlot.Accent, "포인트");
            SetSlotAvailability(shelfButton, DecorationSlot.Shelf, "선반");
            SetSlotAvailability(bedsideButton, DecorationSlot.Bedside, "침대");
        }

        private void SetSlotAvailability(Button button, DecorationSlot slot, string label)
        {
            if (button == null)
            {
                return;
            }

            var available = placementController != null
                && placementController.CanBeginEdit(slot);
            button.interactable = available;
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = available ? label : label + " (기본 고정)";
            }
        }

        private void ConfirmEdit()
        {
            if (placementController != null && placementController.ConfirmEdit())
            {
                SetStatus("배치를 저장했습니다. 다른 장식도 계속 조정할 수 있어요.");
                return;
            }

            SetStatus("먼저 이동할 장식을 선택해 주세요.");
        }

        private void CancelEdit()
        {
            if (placementController != null && placementController.CancelEdit())
            {
                SetStatus("변경을 취소하고 저장된 배치로 되돌렸습니다.");
            }
        }

        private void RotateLeft()
        {
            Execute(DecorationPlacementKeyboardCommand.RotateCounterClockwise, "왼쪽으로 15° 회전했습니다.");
        }

        private void RotateRight()
        {
            Execute(DecorationPlacementKeyboardCommand.RotateClockwise, "오른쪽으로 15° 회전했습니다.");
        }

        private void NudgeLeft()
        {
            Execute(DecorationPlacementKeyboardCommand.NudgeLeft, "왼쪽으로 한 칸 이동했습니다.");
        }

        private void NudgeRight()
        {
            Execute(DecorationPlacementKeyboardCommand.NudgeRight, "오른쪽으로 한 칸 이동했습니다.");
        }

        private void NudgeUp()
        {
            Execute(DecorationPlacementKeyboardCommand.NudgeUp, "위로 한 칸 이동했습니다.");
        }

        private void NudgeDown()
        {
            Execute(DecorationPlacementKeyboardCommand.NudgeDown, "아래로 한 칸 이동했습니다.");
        }

        private void ResetPlacement()
        {
            Execute(DecorationPlacementKeyboardCommand.ResetToAnchor, "기본 위치로 되돌렸습니다.");
        }

        private void Execute(DecorationPlacementKeyboardCommand command, string message)
        {
            if (placementController != null
                && placementController.TryHandleKeyboardCommand(command))
            {
                SetStatus(message);
                return;
            }

            SetStatus("먼저 이동할 장식을 선택해 주세요.");
        }

        private void BindButtons()
        {
            Bind(openButton, Open);
            Bind(closeButton, Close);
            Bind(accentButton, BeginAccent);
            Bind(shelfButton, BeginShelf);
            Bind(bedsideButton, BeginBedside);
            Bind(confirmButton, ConfirmEdit);
            Bind(cancelButton, CancelEdit);
            Bind(rotateLeftButton, RotateLeft);
            Bind(rotateRightButton, RotateRight);
            Bind(nudgeLeftButton, NudgeLeft);
            Bind(nudgeRightButton, NudgeRight);
            Bind(nudgeUpButton, NudgeUp);
            Bind(nudgeDownButton, NudgeDown);
            Bind(resetButton, ResetPlacement);
        }

        private void UnbindButtons()
        {
            openButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
            accentButton?.onClick.RemoveListener(BeginAccent);
            shelfButton?.onClick.RemoveListener(BeginShelf);
            bedsideButton?.onClick.RemoveListener(BeginBedside);
            confirmButton?.onClick.RemoveListener(ConfirmEdit);
            cancelButton?.onClick.RemoveListener(CancelEdit);
            rotateLeftButton?.onClick.RemoveListener(RotateLeft);
            rotateRightButton?.onClick.RemoveListener(RotateRight);
            nudgeLeftButton?.onClick.RemoveListener(NudgeLeft);
            nudgeRightButton?.onClick.RemoveListener(NudgeRight);
            nudgeUpButton?.onClick.RemoveListener(NudgeUp);
            nudgeDownButton?.onClick.RemoveListener(NudgeDown);
            resetButton?.onClick.RemoveListener(ResetPlacement);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void NotifyBlocking(bool blocked)
        {
            if (blockingNotified == blocked)
            {
                return;
            }

            blockingNotified = blocked;
            blockingChanged?.Invoke(blocked);
        }

        private void ReleaseBlocking()
        {
            NotifyBlocking(false);
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
            }
        }
    }
}
