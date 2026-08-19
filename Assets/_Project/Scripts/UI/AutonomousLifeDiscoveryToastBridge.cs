using CheeseTama.Gameplay.Autonomy;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class AutonomousLifeDiscoveryToastBridge : MonoBehaviour
    {
        [SerializeField] private AutonomousLifeBridge discoverySource;
        [SerializeField] private MilkroomUIController milkroomUi;

        private AutonomousLifeBridge subscribedSource;

        public string LastPresentedMessage { get; private set; } = string.Empty;

        public void Configure(
            AutonomousLifeBridge source,
            MilkroomUIController uiController)
        {
            discoverySource = source;
            milkroomUi = uiController;
            Subscribe(isActiveAndEnabled ? source : null);
        }

        public bool Present(AutonomousLifeDiscoveryItemSnapshot discovery)
        {
            if (discovery == null || !discovery.IsDiscovered)
            {
                return false;
            }

            LastPresentedMessage = $"새 생활 발견! {discovery.DisplayName} · {discovery.Description}";
            milkroomUi?.ShowEventMessage(LastPresentedMessage);
            return true;
        }

        private void OnEnable()
        {
            Subscribe(discoverySource);
        }

        private void OnDisable()
        {
            Subscribe(null);
        }

        private void Subscribe(AutonomousLifeBridge source)
        {
            if (subscribedSource == source)
            {
                return;
            }

            if (subscribedSource != null)
            {
                subscribedSource.DiscoveryObserved -= HandleDiscoveryObserved;
            }

            subscribedSource = source;
            if (subscribedSource != null)
            {
                subscribedSource.DiscoveryObserved += HandleDiscoveryObserved;
            }
        }

        private void HandleDiscoveryObserved(AutonomousLifeDiscoveryItemSnapshot discovery)
        {
            Present(discovery);
        }
    }
}
