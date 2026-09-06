using CheeseTama.Core;
using CheeseTama.Gameplay.Story;
using UnityEngine;

namespace CheeseTama.Environment
{
    [DisallowMultipleComponent]
    public sealed class MilkroomInvestigationAvailabilityBridge : MonoBehaviour
    {
        [SerializeField] private MilkroomInvestigationInputController inputController;

        private GameManager manager;

        public void Configure(
            MilkroomInvestigationInputController controller,
            GameManager gameManager)
        {
            UnbindManager();
            inputController = controller;
            manager = gameManager;
            BindManager();
            inputController?.RefreshAvailability();
        }

        private void OnEnable()
        {
            if (manager == null)
            {
                manager = GameManager.Instance;
            }

            BindManager();
            inputController?.RefreshAvailability();
        }

        private void OnDisable()
        {
            UnbindManager();
        }

        private void OnDestroy()
        {
            UnbindManager();
        }

        private void BindManager()
        {
            if (manager == null)
            {
                return;
            }

            manager.SaveDataReplaced -= HandleChanged;
            manager.SaveDataReplaced += HandleChanged;
            manager.DreamStorySeasonChanged -= HandleDreamStoryChanged;
            manager.DreamStorySeasonChanged += HandleDreamStoryChanged;
            manager.MilkroomInvestigationChanged -= HandleChanged;
            manager.MilkroomInvestigationChanged += HandleChanged;
        }

        private void UnbindManager()
        {
            if (manager == null)
            {
                return;
            }

            manager.SaveDataReplaced -= HandleChanged;
            manager.DreamStorySeasonChanged -= HandleDreamStoryChanged;
            manager.MilkroomInvestigationChanged -= HandleChanged;
        }

        private void HandleChanged()
        {
            inputController?.RefreshAvailability();
        }

        private void HandleDreamStoryChanged(DreamStoryChoiceResult result)
        {
            HandleChanged();
        }
    }
}
