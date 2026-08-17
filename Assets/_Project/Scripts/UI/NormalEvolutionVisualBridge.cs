using CheeseTama.Core;
using CheeseTama.Gameplay.Growth;
using UnityEngine;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class NormalEvolutionVisualBridge : MonoBehaviour
    {
        [SerializeField] private NormalEvolutionVisualPresenter presenter;

        private GameManager manager;
        private GameManager subscribedManager;

        public void Configure(
            NormalEvolutionVisualPresenter targetPresenter,
            GameManager gameManager)
        {
            presenter = targetPresenter;
            manager = gameManager;
            presenter?.Bind(gameManager?.CurrentTama);
            Subscribe(gameManager);
        }

        private void OnEnable()
        {
            Subscribe(manager);
        }

        private void OnDisable()
        {
            Subscribe(null);
        }

        private void Subscribe(GameManager target)
        {
            if (subscribedManager == target)
            {
                return;
            }

            if (subscribedManager != null)
            {
                subscribedManager.SaveDataReplaced -= HandleSaveDataReplaced;
                subscribedManager.EvolutionMilestoneAvailable -= HandleEvolutionMilestone;
                subscribedManager.CareActionRegistered -= HandleCareActionRegistered;
            }

            subscribedManager = target;
            if (subscribedManager != null && isActiveAndEnabled)
            {
                subscribedManager.SaveDataReplaced += HandleSaveDataReplaced;
                subscribedManager.EvolutionMilestoneAvailable += HandleEvolutionMilestone;
                subscribedManager.CareActionRegistered += HandleCareActionRegistered;
            }
        }

        private void HandleSaveDataReplaced()
        {
            presenter?.Bind(subscribedManager?.CurrentTama);
        }

        private void HandleEvolutionMilestone(EvolutionMilestoneData _)
        {
            presenter?.Bind(subscribedManager?.CurrentTama);
            presenter?.PlaySignatureReaction();
        }

        private void HandleCareActionRegistered(string _)
        {
            if (presenter != null && presenter.ActiveProfile != null)
            {
                presenter.PlaySignatureReaction();
            }
        }
    }
}
