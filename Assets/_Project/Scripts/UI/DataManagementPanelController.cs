using CheeseTama.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class DataManagementPanelController : MonoBehaviour
    {
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Text statusText;
        [SerializeField] private ConfirmResetDialog resetDialog;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;

        public void Configure(
            Button manualSaveButton,
            Button manualLoadButton,
            Button openResetDialogButton,
            Text dataStatusText,
            ConfirmResetDialog confirmResetDialog,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            saveButton = manualSaveButton;
            loadButton = manualLoadButton;
            resetButton = openResetDialogButton;
            statusText = dataStatusText;
            resetDialog = confirmResetDialog;
            milkroomUi = uiController;
            visualController = cheeseTamaVisual;

            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(Save);
                saveButton.onClick.AddListener(Save);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(Load);
                loadButton.onClick.AddListener(Load);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(OpenResetDialog);
                resetButton.onClick.AddListener(OpenResetDialog);
            }
        }

        private void Save()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.SaveGame();
            RefreshBoundViews(manager, "수동 저장을 완료했습니다.");
        }

        private void Load()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.ReloadGame();
            var message = manager.LastTimeProgression.applied
                ? manager.LastTimeProgression.ToSummary("비운 사이")
                : "저장 데이터를 불러왔습니다.";
            RefreshBoundViews(manager, message);
        }

        private void OpenResetDialog()
        {
            if (resetDialog != null)
            {
                resetDialog.Open();
            }
        }

        private void RefreshBoundViews(GameManager manager, string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            if (milkroomUi != null)
            {
                milkroomUi.Bind(manager.CurrentSave);
                milkroomUi.ShowMessage(message);
            }

            if (visualController != null)
            {
                visualController.Bind(manager.CurrentTama);
                visualController.React(false);
            }

            Debug.Log(message);
        }
    }
}
