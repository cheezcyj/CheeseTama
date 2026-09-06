using CheeseTama.Core;
using CheeseTama.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class CheeseTamaNameDialogController : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text statusText;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private bool configured;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;

        public void Configure(
            Button openNameDialogButton,
            GameObject nameDialogRoot,
            InputField tamaNameInput,
            Text feedbackText,
            Button saveNameButton,
            Button cancelNameButton,
            MilkroomUIController uiController,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            RestoreControls();
            UnbindControls();

            openButton = openNameDialogButton;
            dialogRoot = nameDialogRoot;
            nameInput = tamaNameInput;
            statusText = feedbackText;
            saveButton = saveNameButton;
            cancelButton = cancelNameButton;
            milkroomUi = uiController;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = true;

            if (nameInput != null)
            {
                nameInput.characterLimit = CheeseTamaNameSystem.MaximumNameLength;
                nameInput.lineType = InputField.LineType.SingleLine;
            }

            BindControls();
            Close();
        }

        private void OnEnable()
        {
            if (configured)
            {
                BindControls();
            }
        }

        private void OnDisable()
        {
            UnbindControls();
            RestoreControls();
        }

        private void Update()
        {
            if (dialogRoot != null
                && dialogRoot.activeSelf
                && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        private void BindControls()
        {
            UnbindControls();
            openButton?.onClick.AddListener(Open);
            saveButton?.onClick.AddListener(Save);
            cancelButton?.onClick.AddListener(Close);
            nameInput?.onValueChanged.AddListener(HandleNameChanged);
        }

        private void UnbindControls()
        {
            openButton?.onClick.RemoveListener(Open);
            saveButton?.onClick.RemoveListener(Save);
            cancelButton?.onClick.RemoveListener(Close);
            nameInput?.onValueChanged.RemoveListener(HandleNameChanged);
        }

        public void Open()
        {
            if (dialogRoot == null)
            {
                return;
            }

            UnityEngine.Object.FindFirstObjectByType<CheeseTamaProfileMenuController>()?.CloseForChildNavigation();
            dialogRoot.SetActive(true);
            dialogRoot.transform.SetAsLastSibling();
            SuspendControls();
            SetStatus(string.Empty);

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager == null || manager.CurrentTama == null)
            {
                SetStatus("치즈타마 정보를 불러오지 못했습니다.");
                return;
            }

            nameInput?.SetTextWithoutNotify(manager.CurrentTama.name ?? string.Empty);
            if (nameInput != null)
            {
                nameInput.Select();
                nameInput.ActivateInputField();
            }
        }

        public void Save()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager == null)
            {
                SetStatus("치즈타마 정보를 불러오지 못했습니다.");
                return;
            }

            if (!manager.TryRenameCurrentTama(
                    nameInput != null ? nameInput.text : string.Empty,
                    out var errorMessage))
            {
                SetStatus(string.IsNullOrWhiteSpace(errorMessage)
                    ? "이름을 변경하지 못했습니다."
                    : errorMessage);
                return;
            }

            var changedName = manager.CurrentTama.name;
            milkroomUi?.Bind(manager.CurrentSave);
            Close();
            milkroomUi?.ShowMessage($"새 이름은 ‘{changedName}’이에요.");
        }

        public void Close()
        {
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(false);
            }

            SetStatus(string.Empty);
            RestoreControls();
        }

        private void HandleNameChanged(string value)
        {
            SetStatus(string.Empty);
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomActionBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
            if (topMenuController != null)
            {
                topMenuController.enabled = false;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = false;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = false;
            }

            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null)
            {
                topMenuController.enabled = topMenuWasEnabled;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = bottomActionBarWasEnabled;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = devPanelWasEnabled;
            }

            controlsSuspended = false;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
