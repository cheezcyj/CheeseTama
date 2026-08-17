using System;
using CheeseTama.Gameplay.Input;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class InputBindingsPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Input Bindings Overlay";

        private static readonly KeyCode[] AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button[] bindingButtons;
        [SerializeField] private Text[] bindingValueLabels;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController actionBarController;
        [SerializeField] private DevPanelController developerPanelController;

        private Func<GameInputBindingSaveData> stateProvider;
        private Action<GameInputBindingSaveData> persistState;
        private Action closed;
        private string listeningActionId = string.Empty;
        private bool controlsSuspended;
        private bool previousTopMenuEnabled;
        private bool previousActionBarEnabled;
        private bool previousDeveloperPanelEnabled;
        private GameObject previousSelectedObject;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsListening => !string.IsNullOrEmpty(listeningActionId);
        public bool IsBlockingGameplay => IsOpen;

        public void Configure(
            GameObject root,
            Text status,
            Button[] actionBindingButtons,
            Text[] valueLabels,
            Button resetAction,
            Button closeAction,
            Func<GameInputBindingSaveData> getState,
            Action<GameInputBindingSaveData> saveState,
            TopMenuController menuController = null,
            BottomActionBarController bottomController = null,
            DevPanelController devController = null,
            Action onClosed = null)
        {
            overlayRoot = root;
            statusLabel = status;
            bindingButtons = actionBindingButtons;
            bindingValueLabels = valueLabels;
            resetButton = resetAction;
            closeButton = closeAction;
            stateProvider = getState;
            persistState = saveState;
            topMenuController = menuController;
            actionBarController = bottomController;
            developerPanelController = devController;
            closed = onClosed;

            WireButtons();
            RefreshPresentation();
            SetVisible(false);
        }

        public void Open()
        {
            if (overlayRoot == null)
            {
                return;
            }

            listeningActionId = string.Empty;
            SuspendControls();
            previousSelectedObject = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            SetVisible(true);
            overlayRoot.transform.SetAsLastSibling();
            RefreshPresentation("바꿀 조작을 선택하세요.");
            Select(bindingButtons != null && bindingButtons.Length > 0 ? bindingButtons[0] : closeButton);
        }

        public void Close()
        {
            var wasOpen = IsOpen;
            listeningActionId = string.Empty;
            SetVisible(false);
            RestoreControls();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(previousSelectedObject);
            }

            previousSelectedObject = null;
            if (wasOpen)
            {
                closed?.Invoke();
            }
        }

        public void BeginRebind(int definitionIndex)
        {
            if (!IsOpen
                || definitionIndex < 0
                || definitionIndex >= GameInputBindingSystem.All.Count)
            {
                return;
            }

            listeningActionId = GameInputBindingSystem.All[definitionIndex].id;
            var displayName = GameInputBindingSystem.All[definitionIndex].displayName;
            RefreshPresentation($"'{displayName}'에 사용할 키를 누르세요. Esc는 취소입니다.");
        }

        public void ResetAll()
        {
            var state = stateProvider?.Invoke();
            if (state == null)
            {
                RefreshPresentation("키 설정을 불러오지 못했습니다.");
                return;
            }

            GameInputBindingSystem.ResetAll(state);
            persistState?.Invoke(state);
            listeningActionId = string.Empty;
            RefreshPresentation("기본 키로 되돌렸습니다.");
        }

        public void RefreshPresentation(string message = null)
        {
            var state = stateProvider?.Invoke();
            if (state != null)
            {
                GameInputBindingSystem.EnsureDefaults(state);
            }

            if (bindingValueLabels != null)
            {
                var count = Math.Min(bindingValueLabels.Length, GameInputBindingSystem.All.Count);
                for (var index = 0; index < count; index += 1)
                {
                    var label = bindingValueLabels[index];
                    if (label != null)
                    {
                        var definition = GameInputBindingSystem.All[index];
                        label.text = string.Equals(listeningActionId, definition.id, StringComparison.Ordinal)
                            ? "키 입력 대기…"
                            : GameInputBindingSystem.FormatBinding(state, definition.id);
                    }
                }
            }

            if (statusLabel != null && message != null)
            {
                statusLabel.text = message;
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (!IsListening)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    Close();
                }

                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                listeningActionId = string.Empty;
                RefreshPresentation("키 변경을 취소했습니다.");
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace))
            {
                var state = stateProvider?.Invoke();
                if (GameInputBindingSystem.ResetAction(state, listeningActionId))
                {
                    persistState?.Invoke(state);
                    listeningActionId = string.Empty;
                    RefreshPresentation("이 조작을 기본 키로 되돌렸습니다.");
                    return;
                }

                listeningActionId = string.Empty;
                RefreshPresentation("기본 키가 다른 동작에 사용 중입니다.");
                return;
            }

            if (!UnityEngine.Input.anyKeyDown)
            {
                return;
            }

            foreach (var keyCode in AllKeyCodes)
            {
                if (!UnityEngine.Input.GetKeyDown(keyCode)
                    || !GameInputBindingSystem.IsBindableKey(keyCode))
                {
                    continue;
                }

                ApplyRebind(keyCode);
                break;
            }
        }

        private void ApplyRebind(KeyCode keyCode)
        {
            var state = stateProvider?.Invoke();
            var actionId = listeningActionId;
            if (GameInputBindingSystem.TryRebind(state, actionId, keyCode, out var error))
            {
                persistState?.Invoke(state);
                listeningActionId = string.Empty;
                RefreshPresentation($"{GameInputBindingSystem.FormatKey(keyCode)} 키로 저장했습니다.");
                return;
            }

            RefreshPresentation(error);
        }

        private void WireButtons()
        {
            if (bindingButtons != null)
            {
                for (var index = 0; index < bindingButtons.Length; index += 1)
                {
                    var button = bindingButtons[index];
                    if (button == null)
                    {
                        continue;
                    }

                    var capturedIndex = index;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => BeginRebind(capturedIndex));
                }
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(ResetAll);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            previousTopMenuEnabled = topMenuController != null && topMenuController.enabled;
            previousActionBarEnabled = actionBarController != null && actionBarController.enabled;
            previousDeveloperPanelEnabled = developerPanelController != null && developerPanelController.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (actionBarController != null) actionBarController.enabled = false;
            if (developerPanelController != null) developerPanelController.enabled = false;
            GameInputRouter.GameplayInputSuppressed = true;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = previousTopMenuEnabled;
            if (actionBarController != null) actionBarController.enabled = previousActionBarEnabled;
            if (developerPanelController != null) developerPanelController.enabled = previousDeveloperPanelEnabled;
            GameInputRouter.GameplayInputSuppressed = false;
            controlsSuspended = false;
        }

        private void OnDisable()
        {
            RestoreControls();
        }

        private void SetVisible(bool visible)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != visible)
            {
                overlayRoot.SetActive(visible);
            }
        }

        private static void Select(Button button)
        {
            if (button != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
    }
}
