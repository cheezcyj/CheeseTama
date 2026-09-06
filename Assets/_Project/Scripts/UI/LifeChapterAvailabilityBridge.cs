using CheeseTama.Core;
using UnityEngine;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class LifeChapterAvailabilityBridge : MonoBehaviour
    {
        [SerializeField] private LifeChapterPanelController panelController;

        private GameManager manager;

        public void Configure(
            LifeChapterPanelController controller,
            GameManager gameManager)
        {
            UnbindManager();
            panelController = controller;
            manager = gameManager;
            BindManager();
            HandleChanged();
        }

        private void OnEnable()
        {
            if (manager == null)
            {
                manager = GameManager.Instance;
            }

            BindManager();
            HandleChanged();
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
            manager.LifeChapterChanged -= HandleChanged;
            manager.LifeChapterChanged += HandleChanged;
        }

        private void UnbindManager()
        {
            if (manager == null)
            {
                return;
            }

            manager.SaveDataReplaced -= HandleChanged;
            manager.LifeChapterChanged -= HandleChanged;
        }

        private void HandleChanged()
        {
            panelController?.RefreshEntryVisibility(
                manager?.PeekPendingLifeChapterSnapshot());
        }
    }
}
