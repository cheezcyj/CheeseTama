using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.Decorations;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class DecorationShopPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text balanceText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text[] itemNameTexts;
        [SerializeField] private Text[] itemStateTexts;
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button closeButton;

        private Func<DecorationShopSnapshot> snapshotProvider;
        private Func<string, DecorationTransactionResult> purchaseCommand;
        private Func<string, DecorationTransactionResult> equipCommand;
        private Action closeAction;
        private DecorationShopSnapshot snapshot;
        private DecorationDefinition selectedItem;
        private string statusMessage = string.Empty;
        private TopMenuController topMenuController;
        private BottomActionBarController bottomActionBarController;
        private DevPanelController devPanelController;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;

        public DecorationDefinition SelectedItem => selectedItem;

        public void Configure(
            GameObject root,
            Text balanceLabel,
            Text detailLabel,
            Text statusLabel,
            Text[] itemNameLabels,
            Text[] itemStateLabels,
            Button[] selectionButtons,
            Button buyButton,
            Button applyButton,
            Button panelCloseButton,
            Func<DecorationShopSnapshot> getSnapshot,
            Func<string, DecorationTransactionResult> buyCommand,
            Func<string, DecorationTransactionResult> applyCommand,
            Action onClosed = null)
        {
            panelRoot = root;
            balanceText = balanceLabel;
            detailText = detailLabel;
            statusText = statusLabel;
            itemNameTexts = itemNameLabels;
            itemStateTexts = itemStateLabels;
            itemButtons = selectionButtons;
            purchaseButton = buyButton;
            equipButton = applyButton;
            closeButton = panelCloseButton;
            snapshotProvider = getSnapshot;
            purchaseCommand = buyCommand;
            equipCommand = applyCommand;
            closeAction = onClosed;

            selectedItem = DecorationCatalog.All.Length > 0
                ? DecorationCatalog.All[0]
                : null;
            statusMessage = string.Empty;
            BindButtons();
            Refresh();
            Close();
        }

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            BindButtons();
            Refresh();
        }

        private void OnDisable()
        {
            RestoreControls();
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                HandleCloseClicked();
            }
        }

        public void Open()
        {
            BindButtons();
            SuspendControls();
            SetActive(panelRoot, true);
            if (panelRoot != null)
            {
                panelRoot.transform.SetAsLastSibling();
            }

            statusMessage = string.Empty;
            Refresh();
        }

        public void Close()
        {
            SetActive(panelRoot, false);
            RestoreControls();
        }

        public void Refresh()
        {
            var providedSnapshot = snapshotProvider?.Invoke();
            if (providedSnapshot != null)
            {
                snapshot = providedSnapshot;
            }

            snapshot ??= DecorationShopSnapshot.CreateDefault();
            if (selectedItem == null)
            {
                selectedItem = DecorationCatalog.All.Length > 0
                    ? DecorationCatalog.All[0]
                    : null;
            }

            Render();
        }

        public void SelectItem(string itemId)
        {
            var item = DecorationCatalog.Find(itemId);
            if (item == null)
            {
                return;
            }

            selectedItem = item;
            statusMessage = string.Empty;
            Render();
        }

        public void PurchaseSelected()
        {
            if (selectedItem == null)
            {
                statusMessage = "구매할 장식을 선택해 주세요.";
                Render();
                return;
            }

            if (purchaseCommand == null)
            {
                statusMessage = "상점 저장 기능이 연결되지 않았어요.";
                Render();
                return;
            }

            ApplyResult(purchaseCommand(selectedItem.id));
        }

        public void EquipSelected()
        {
            if (selectedItem == null)
            {
                statusMessage = "장착할 장식을 선택해 주세요.";
                Render();
                return;
            }

            if (equipCommand == null)
            {
                statusMessage = "꾸미기 저장 기능이 연결되지 않았어요.";
                Render();
                return;
            }

            ApplyResult(equipCommand(selectedItem.id));
        }

        private void BindButtons()
        {
            var items = DecorationCatalog.All;
            if (itemButtons != null)
            {
                for (var index = 0; index < itemButtons.Length; index += 1)
                {
                    var button = itemButtons[index];
                    if (button == null)
                    {
                        continue;
                    }

                    button.onClick.RemoveAllListeners();
                    if (index < items.Length)
                    {
                        var itemId = items[index].id;
                        button.onClick.AddListener(() => SelectItem(itemId));
                    }
                }
            }

            BindButton(purchaseButton, PurchaseSelected);
            BindButton(equipButton, EquipSelected);
            BindButton(closeButton, HandleCloseClicked);
        }

        private void ApplyResult(DecorationTransactionResult result)
        {
            if (result == null)
            {
                statusMessage = "꾸미기 요청을 처리하지 못했어요.";
                Render();
                return;
            }

            snapshot = result.snapshot ?? snapshot;
            statusMessage = result.message;
            Render();
        }

        private void Render()
        {
            snapshot ??= DecorationShopSnapshot.CreateDefault();
            SetText(balanceText, $"코인 {snapshot.milkCoins} · 우유방울 {snapshot.milkDrops}");

            var items = DecorationCatalog.All;
            for (var index = 0; index < items.Length; index += 1)
            {
                var item = items[index];
                SetText(Get(itemNameTexts, index), item.displayName);
                SetText(Get(itemStateTexts, index), FormatItemState(item, snapshot));
                SetSelected(Get(itemButtons, index), selectedItem == item);
            }

            if (selectedItem == null)
            {
                SetText(detailText, "표시할 장식이 없어요.");
                SetText(statusText, statusMessage);
                SetInteractable(purchaseButton, false);
                SetInteractable(equipButton, false);
                return;
            }

            SetText(
                detailText,
                $"<b>{selectedItem.displayName}</b> · {DecorationShopRules.GetSlotName(selectedItem.slot)}\n"
                + $"{selectedItem.description}\n가격: {DecorationShopRules.FormatPrice(selectedItem)}");

            if (string.IsNullOrWhiteSpace(statusMessage))
            {
                statusMessage = GetSelectionMessage(selectedItem, snapshot);
            }

            SetText(statusText, statusMessage);
            SetInteractable(
                purchaseButton,
                DecorationShopRules.CanPurchase(selectedItem, snapshot));
            SetInteractable(
                equipButton,
                DecorationShopRules.CanEquip(selectedItem, snapshot));
            SetButtonLabel(
                purchaseButton,
                snapshot.Owns(selectedItem.id) ? "보유 중" : "구매");
            SetButtonLabel(
                equipButton,
                snapshot.GetEquippedId(selectedItem.slot) == selectedItem.id ? "장착 중" : "장착");
        }

        private void HandleCloseClicked()
        {
            Close();
            closeAction?.Invoke();
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuController ??= GetComponent<TopMenuController>();
            bottomActionBarController ??= GetComponentInChildren<BottomActionBarController>(true);
            devPanelController ??= GetComponent<DevPanelController>();
            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomActionBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (bottomActionBarController != null) bottomActionBarController.enabled = false;
            if (devPanelController != null) devPanelController.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = topMenuWasEnabled;
            if (bottomActionBarController != null) bottomActionBarController.enabled = bottomActionBarWasEnabled;
            if (devPanelController != null) devPanelController.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }

        private static string FormatItemState(
            DecorationDefinition item,
            DecorationShopSnapshot currentSnapshot)
        {
            if (currentSnapshot.GetEquippedId(item.slot) == item.id)
            {
                return "장착 중";
            }

            if (currentSnapshot.Owns(item.id))
            {
                return item.defaultOwned ? "기본 제공" : "보유";
            }

            return DecorationShopRules.FormatPrice(item);
        }

        private static string GetSelectionMessage(
            DecorationDefinition item,
            DecorationShopSnapshot currentSnapshot)
        {
            if (currentSnapshot.GetEquippedId(item.slot) == item.id)
            {
                return "현재 밀크룸에 적용 중이에요.";
            }

            if (currentSnapshot.Owns(item.id))
            {
                return "가지고 있는 장식이에요. 장착할 수 있어요.";
            }

            return DecorationShopRules.CanPurchase(item, currentSnapshot)
                ? "구매할 수 있어요."
                : "재화가 부족해요.";
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = selected
                ? new Color(1f, 0.83f, 0.42f, 1f)
                : Color.white;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = value;
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static T Get<T>(T[] values, int index) where T : class
        {
            return values != null && index >= 0 && index < values.Length
                ? values[index]
                : null;
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
