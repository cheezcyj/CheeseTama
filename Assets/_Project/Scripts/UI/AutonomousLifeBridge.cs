using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.UI
{
    /// <summary>
    /// Owns the runtime subscriptions and authority callbacks used by the
    /// autonomous-life presenter. The presenter itself remains deterministic and
    /// independent from GameManager and the Milkroom modal hierarchy.
    /// </summary>
    public sealed class AutonomousLifeBridge : MonoBehaviour
    {
        [SerializeField] private AutonomousLifePresenter presenter;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private CheeseTamaDialogueBridge dialogueBridge;

        private GameManager manager;
        private GameManager subscribedManager;

        public bool IsActive => presenter != null && presenter.IsActive;
        public AutonomousLifeDiscoveryItemSnapshot LastObservedDiscovery { get; private set; }

        public event Action<AutonomousLifeDiscoveryItemSnapshot> DiscoveryObserved;

        public AutonomousLifeDiscoveryCollectionSnapshot GetDiscoverySnapshot()
        {
            return AutonomousLifeDiscoveryCatalog.CreateSnapshot(
                manager?.CurrentSave?.autonomousLife);
        }

        public void Configure(
            AutonomousLifePresenter targetPresenter,
            Transform movingCharacterRoot,
            AutonomousLifeAnchorBindings anchors,
            GameManager gameManager,
            CheeseTamaVisualController tamaVisual,
            CheeseTamaDialogueBridge dialogue)
        {
            presenter = targetPresenter;
            visualController = tamaVisual;
            dialogueBridge = dialogue;
            manager = gameManager;

            if (presenter != null)
            {
                presenter.Configure(
                    movingCharacterRoot,
                    anchors,
                    new AutonomousLifePresenterCallbacks(
                        contextProvider: BuildContext,
                        saveProvider: () => manager?.CurrentSave?.autonomousLife,
                        persistFirstDiscovery: PersistFirstDiscovery,
                        interactionBlockedProvider: IsInteractionBlocked,
                        behaviourStarted: HandleBehaviourStarted,
                        discoveryObserved: HandleDiscoveryObserved));
            }

            Subscribe(gameManager);
        }

        public void InterruptForInteraction()
        {
            presenter?.InterruptForInteraction();
        }

        private void OnEnable()
        {
            Subscribe(manager);
        }

        private void OnDisable()
        {
            Subscribe(null);
            presenter?.InterruptForInteraction();
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
                subscribedManager.CareActionRegistered -= HandleCareActionRegistered;
            }

            subscribedManager = target;
            if (subscribedManager != null && isActiveAndEnabled)
            {
                subscribedManager.SaveDataReplaced += HandleSaveDataReplaced;
                subscribedManager.CareActionRegistered += HandleCareActionRegistered;
            }
        }

        private AutonomousLifeContext BuildContext()
        {
            var save = manager?.CurrentSave;
            var tama = manager?.CurrentTama;
            var stats = tama?.stats;
            if (save == null || tama == null || stats == null)
            {
                return AutonomousLifeContext.CreateNeutral(DateTimeOffset.Now.Hour);
            }

            var traitId = save.newGameSetup?.temperamentSeed?.dominantTraitId;
            if (string.IsNullOrWhiteSpace(traitId))
            {
                traitId = tama.growthHistory?.careStyle;
            }

            if (string.IsNullOrWhiteSpace(traitId))
            {
                traitId = NewGameSetupCatalog.BalancedTraitId;
            }

            var decorations = save.decorations;
            return new AutonomousLifeContext(
                DateTimeOffset.Now.Hour,
                tama.isHatched,
                stats.hunger,
                stats.mood,
                stats.cleanliness,
                stats.sleepiness,
                stats.health,
                traitId,
                decorations?.equippedFloorId,
                decorations?.equippedAccentId,
                decorations?.equippedWindowId,
                decorations?.equippedShelfId,
                decorations?.equippedBedsideId);
        }

        private void PersistFirstDiscovery(AutonomousLifeSaveData data)
        {
            if (manager?.CurrentSave == null || data == null)
            {
                return;
            }

            manager.CurrentSave.autonomousLife = data;
            manager.SaveGame();
        }

        private bool IsInteractionBlocked()
        {
            return dialogueBridge?.IsModalBlocking == true
                || JourneyHubPanelController.IsAnyOpen()
                || manager?.IsSleepScheduleActive == true
                || UnityEngine.Input.GetMouseButton(0)
                || UnityEngine.Input.anyKeyDown;
        }

        private void HandleBehaviourStarted(AutonomousLifeBehaviour behaviour)
        {
            visualController?.ReactAction(
                AutonomousLifePresenter.ResolveVisualAction(behaviour));
        }

        private void HandleDiscoveryObserved(AutonomousLifeDiscoveryResult result)
        {
            if (!AutonomousLifeDiscoveryCatalog.TryCreateObservedSnapshot(
                    result,
                    out var snapshot))
            {
                return;
            }

            LastObservedDiscovery = snapshot;
            DiscoveryObserved?.Invoke(snapshot);
        }

        private void HandleSaveDataReplaced()
        {
            LastObservedDiscovery = null;
            presenter?.BeginSession();
        }

        private void HandleCareActionRegistered(string _)
        {
            presenter?.InterruptForInteraction();
        }
    }
}
