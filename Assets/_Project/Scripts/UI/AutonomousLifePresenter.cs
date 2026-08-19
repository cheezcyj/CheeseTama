using System;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.UI
{
    public enum AutonomousLifePresentationPhase
    {
        Unconfigured = 0,
        Waiting = 1,
        MovingToAnchor = 2,
        Performing = 3,
        ReturningHome = 4,
        Exhausted = 5
    }

    /// <summary>
    /// Fixed scene anchors supplied by StarterSceneBuilder. A missing optional
    /// anchor removes that behaviour from selection; Idle falls back to the
    /// character's configured home position.
    /// </summary>
    public sealed class AutonomousLifeAnchorBindings
    {
        public AutonomousLifeAnchorBindings(
            Transform idle,
            Transform nap,
            Transform window,
            Transform shelf,
            Transform play,
            Transform dance)
        {
            Idle = idle;
            Nap = nap;
            Window = window;
            Shelf = shelf;
            Play = play;
            Dance = dance;
        }

        public Transform Idle { get; }
        public Transform Nap { get; }
        public Transform Window { get; }
        public Transform Shelf { get; }
        public Transform Play { get; }
        public Transform Dance { get; }

        public AutonomousLifeAnchorMask GetAvailableMask(bool hasCharacterRoot)
        {
            var mask = hasCharacterRoot
                ? AutonomousLifeAnchorMask.Idle
                : AutonomousLifeAnchorMask.None;
            if (Nap != null)
            {
                mask |= AutonomousLifeAnchorMask.Nap;
            }

            if (Window != null)
            {
                mask |= AutonomousLifeAnchorMask.Window;
            }

            if (Shelf != null)
            {
                mask |= AutonomousLifeAnchorMask.Shelf;
            }

            if (Play != null)
            {
                mask |= AutonomousLifeAnchorMask.Play;
            }

            if (Dance != null)
            {
                mask |= AutonomousLifeAnchorMask.Dance;
            }

            return mask;
        }

        public Transform Resolve(AutonomousLifeBehaviour behaviour)
        {
            return behaviour switch
            {
                AutonomousLifeBehaviour.Nap => Nap,
                AutonomousLifeBehaviour.Window => Window,
                AutonomousLifeBehaviour.Shelf => Shelf,
                AutonomousLifeBehaviour.Play => Play,
                AutonomousLifeBehaviour.Dance => Dance,
                _ => Idle
            };
        }
    }

    /// <summary>
    /// Explicit integration boundary. No singleton, save path, modal hierarchy,
    /// or scene object name is hidden inside AutonomousLifePresenter.
    /// </summary>
    public sealed class AutonomousLifePresenterCallbacks
    {
        public AutonomousLifePresenterCallbacks(
            Func<AutonomousLifeContext> contextProvider,
            Func<AutonomousLifeSaveData> saveProvider,
            Action<AutonomousLifeSaveData> persistFirstDiscovery,
            Func<bool> interactionBlockedProvider,
            Action<AutonomousLifeBehaviour> behaviourStarted = null,
            Action<AutonomousLifeBehaviour, bool> behaviourEnded = null,
            Action<AutonomousLifeDiscoveryResult> discoveryObserved = null,
            Func<DateTimeOffset> nowProvider = null,
            Func<float> random01Provider = null)
        {
            ContextProvider = contextProvider;
            SaveProvider = saveProvider;
            PersistFirstDiscovery = persistFirstDiscovery;
            InteractionBlockedProvider = interactionBlockedProvider;
            BehaviourStarted = behaviourStarted;
            BehaviourEnded = behaviourEnded;
            DiscoveryObserved = discoveryObserved;
            NowProvider = nowProvider;
            Random01Provider = random01Provider;
        }

        public Func<AutonomousLifeContext> ContextProvider { get; }
        public Func<AutonomousLifeSaveData> SaveProvider { get; }
        public Action<AutonomousLifeSaveData> PersistFirstDiscovery { get; }
        public Func<bool> InteractionBlockedProvider { get; }
        public Action<AutonomousLifeBehaviour> BehaviourStarted { get; }
        public Action<AutonomousLifeBehaviour, bool> BehaviourEnded { get; }
        public Action<AutonomousLifeDiscoveryResult> DiscoveryObserved { get; }
        public Func<DateTimeOffset> NowProvider { get; }
        public Func<float> Random01Provider { get; }
    }

    /// <summary>
    /// Low-frequency transform presenter using fixed anchors and horizontal
    /// smooth-step tweens. It deliberately does not use NavMesh or own save data.
    /// </summary>
    public sealed class AutonomousLifePresenter : MonoBehaviour
    {
        private const float MinimumMoveDurationSeconds = 0.55f;
        private const float MaximumMoveDurationSeconds = 1.25f;
        private const float MoveDurationPerWorldUnit = 0.28f;
        private const float PositionEpsilonSquared = 0.000001f;

        [SerializeField] private Transform characterRoot;

        private readonly AutonomousLifeSystem system = new AutonomousLifeSystem();
        private readonly AutonomousLifeSessionState session =
            new AutonomousLifeSessionState();

        private AutonomousLifeAnchorBindings anchors;
        private AutonomousLifePresenterCallbacks callbacks;
        private AutonomousLifePresentationPhase phase =
            AutonomousLifePresentationPhase.Unconfigured;
        private AutonomousLifeSelectionResult currentSelection;
        private Vector3 homePosition;
        private float groundedWorldY;
        private Vector3 tweenFrom;
        private Vector3 tweenTo;
        private float tweenDuration;
        private float tweenElapsed;
        private float phaseRemainingSeconds;
        private bool configured;

        public bool IsConfigured => configured && characterRoot != null;
        public bool IsActive => phase == AutonomousLifePresentationPhase.MovingToAnchor
            || phase == AutonomousLifePresentationPhase.Performing
            || phase == AutonomousLifePresentationPhase.ReturningHome;
        public AutonomousLifePresentationPhase Phase => phase;
        public AutonomousLifeBehaviour CurrentBehaviour => currentSelection.Behaviour;
        public int SessionStartedBehaviourCount => session.StartedBehaviourCount;
        public float SecondsUntilNextBehaviour => phase == AutonomousLifePresentationPhase.Waiting
            ? Math.Max(0f, phaseRemainingSeconds)
            : 0f;
        public float GroundedWorldY => groundedWorldY;

        public void Configure(
            Transform movingCharacterRoot,
            AutonomousLifeAnchorBindings anchorBindings,
            AutonomousLifePresenterCallbacks integrationCallbacks)
        {
            CancelWithoutReschedule(true);

            characterRoot = movingCharacterRoot;
            anchors = anchorBindings ?? new AutonomousLifeAnchorBindings(
                null,
                null,
                null,
                null,
                null,
                null);
            callbacks = integrationCallbacks;
            configured = characterRoot != null;
            if (!configured)
            {
                phase = AutonomousLifePresentationPhase.Unconfigured;
                return;
            }

            groundedWorldY = characterRoot.position.y;
            var requestedHome = anchors.Idle != null
                ? anchors.Idle.position
                : characterRoot.position;
            homePosition = ResolveGroundedPosition(requestedHome);
            SnapHome();
            BeginSession();
        }

        public void BeginSession()
        {
            if (!IsConfigured)
            {
                phase = AutonomousLifePresentationPhase.Unconfigured;
                return;
            }

            CancelWithoutReschedule(true);
            session.Reset();
            SnapHome();
            ScheduleNext();
        }

        /// <summary>
        /// Direct interaction hook for care, petting, modal open, and minigame
        /// entry. Polling the blocked callback provides a second safety layer.
        /// </summary>
        public void InterruptForInteraction()
        {
            if (!IsConfigured)
            {
                return;
            }

            var hadActiveBehaviour = IsActive;
            var interruptedBehaviour = currentSelection.Behaviour;
            SnapHome();
            if (hadActiveBehaviour)
            {
                callbacks?.BehaviourEnded?.Invoke(interruptedBehaviour, true);
            }

            currentSelection = default;
            if (session.IsExhausted)
            {
                phase = AutonomousLifePresentationPhase.Exhausted;
                phaseRemainingSeconds = 0f;
                return;
            }

            ScheduleNext();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!IsConfigured || phase == AutonomousLifePresentationPhase.Exhausted)
            {
                return;
            }

            if (IsInteractionBlocked())
            {
                if (IsActive)
                {
                    InterruptForInteraction();
                }

                return;
            }

            var delta = Math.Max(0f, unscaledDeltaTime);
            switch (phase)
            {
                case AutonomousLifePresentationPhase.Waiting:
                    phaseRemainingSeconds -= delta;
                    if (phaseRemainingSeconds <= 0f)
                    {
                        TryStartNextBehaviour();
                    }
                    break;

                case AutonomousLifePresentationPhase.MovingToAnchor:
                    AdvanceTween(delta, StartPerforming);
                    break;

                case AutonomousLifePresentationPhase.Performing:
                    phaseRemainingSeconds -= delta;
                    if (phaseRemainingSeconds <= 0f)
                    {
                        BeginReturnHome();
                    }
                    break;

                case AutonomousLifePresentationPhase.ReturningHome:
                    AdvanceTween(delta, CompleteCurrentBehaviour);
                    break;
            }
        }

        public static CheeseTamaVisualAction ResolveVisualAction(
            AutonomousLifeBehaviour behaviour)
        {
            return behaviour switch
            {
                AutonomousLifeBehaviour.Nap => CheeseTamaVisualAction.Rest,
                AutonomousLifeBehaviour.Shelf => CheeseTamaVisualAction.Cook,
                AutonomousLifeBehaviour.Play => CheeseTamaVisualAction.Play,
                AutonomousLifeBehaviour.Dance => CheeseTamaVisualAction.Play,
                AutonomousLifeBehaviour.Window => CheeseTamaVisualAction.Event,
                _ => CheeseTamaVisualAction.Neutral
            };
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnEnable()
        {
            if (IsConfigured
                && phase != AutonomousLifePresentationPhase.Exhausted
                && phaseRemainingSeconds <= 0f)
            {
                ScheduleNext();
            }
        }

        private void OnDisable()
        {
            CancelWithoutReschedule(true);
        }

        private void TryStartNextBehaviour()
        {
            var context = callbacks?.ContextProvider?.Invoke()
                ?? AutonomousLifeContext.CreateNeutral(DateTimeOffset.Now.Hour);
            context = context.WithAvailableAnchors(
                anchors.GetAvailableMask(characterRoot != null));
            var selection = system.TrySelectAndStart(
                context,
                session,
                IsInteractionBlocked(),
                NextRandom01(),
                NextRandom01());

            if (!selection.IsSelected)
            {
                if (selection.Status == AutonomousLifeSelectionStatus.SessionLimitReached)
                {
                    phase = AutonomousLifePresentationPhase.Exhausted;
                    phaseRemainingSeconds = 0f;
                }
                else
                {
                    ScheduleNext();
                }

                return;
            }

            currentSelection = selection;
            callbacks?.BehaviourStarted?.Invoke(selection.Behaviour);
            var requestedAnchor = anchors.Resolve(selection.Behaviour);
            var target = requestedAnchor != null
                ? requestedAnchor.position
                : homePosition;
            BeginTween(
                ResolveGroundedPosition(target),
                AutonomousLifePresentationPhase.MovingToAnchor,
                StartPerforming);
        }

        private void StartPerforming()
        {
            if (!currentSelection.IsSelected)
            {
                ScheduleNext();
                return;
            }

            phase = AutonomousLifePresentationPhase.Performing;
            phaseRemainingSeconds = currentSelection.DurationSeconds;

            var saveData = callbacks?.SaveProvider?.Invoke()
                ?? new AutonomousLifeSaveData();
            var discovery = system.RecordFirstDiscovery(
                saveData,
                currentSelection.Behaviour,
                callbacks?.NowProvider?.Invoke() ?? DateTimeOffset.Now);
            if (discovery.WasRecorded)
            {
                callbacks?.PersistFirstDiscovery?.Invoke(saveData);
            }

            callbacks?.DiscoveryObserved?.Invoke(discovery);
        }

        private void BeginReturnHome()
        {
            BeginTween(
                homePosition,
                AutonomousLifePresentationPhase.ReturningHome,
                CompleteCurrentBehaviour);
        }

        private void CompleteCurrentBehaviour()
        {
            var completedBehaviour = currentSelection.Behaviour;
            var hadSelection = currentSelection.IsSelected;
            SnapHome();
            currentSelection = default;
            if (hadSelection)
            {
                callbacks?.BehaviourEnded?.Invoke(completedBehaviour, false);
            }

            if (session.IsExhausted)
            {
                phase = AutonomousLifePresentationPhase.Exhausted;
                phaseRemainingSeconds = 0f;
            }
            else
            {
                ScheduleNext();
            }
        }

        private void BeginTween(
            Vector3 destination,
            AutonomousLifePresentationPhase tweenPhase,
            Action completeImmediately)
        {
            tweenFrom = ResolveGroundedPosition(characterRoot.position);
            tweenTo = ResolveGroundedPosition(destination);
            var horizontalDelta = tweenTo - tweenFrom;
            horizontalDelta.y = 0f;
            var distance = horizontalDelta.magnitude;
            if (AccessibilityRuntime.ReducedMotion
                || distance * distance <= PositionEpsilonSquared)
            {
                characterRoot.position = tweenTo;
                completeImmediately?.Invoke();
                return;
            }

            tweenElapsed = 0f;
            tweenDuration = Mathf.Clamp(
                distance * MoveDurationPerWorldUnit,
                MinimumMoveDurationSeconds,
                MaximumMoveDurationSeconds);
            phase = tweenPhase;
        }

        private void AdvanceTween(float delta, Action completed)
        {
            tweenElapsed += delta;
            var progress = tweenDuration <= 0f
                ? 1f
                : Mathf.Clamp01(tweenElapsed / tweenDuration);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            characterRoot.position = ResolveGroundedPosition(
                Vector3.LerpUnclamped(tweenFrom, tweenTo, eased));
            if (progress >= 1f)
            {
                characterRoot.position = tweenTo;
                completed?.Invoke();
            }
        }

        private void ScheduleNext()
        {
            if (!IsConfigured)
            {
                phase = AutonomousLifePresentationPhase.Unconfigured;
                phaseRemainingSeconds = 0f;
                return;
            }

            if (session.IsExhausted)
            {
                phase = AutonomousLifePresentationPhase.Exhausted;
                phaseRemainingSeconds = 0f;
                return;
            }

            phase = AutonomousLifePresentationPhase.Waiting;
            phaseRemainingSeconds = system.ResolveIdleDelay(NextRandom01());
        }

        private void CancelWithoutReschedule(bool notify)
        {
            var hadActiveBehaviour = IsActive;
            var interruptedBehaviour = currentSelection.Behaviour;
            if (characterRoot != null)
            {
                SnapHome();
            }

            currentSelection = default;
            phaseRemainingSeconds = 0f;
            phase = configured
                ? AutonomousLifePresentationPhase.Waiting
                : AutonomousLifePresentationPhase.Unconfigured;
            if (notify && hadActiveBehaviour)
            {
                callbacks?.BehaviourEnded?.Invoke(interruptedBehaviour, true);
            }
        }

        private void SnapHome()
        {
            if (characterRoot != null)
            {
                characterRoot.position = ResolveGroundedPosition(homePosition);
            }
        }

        private Vector3 ResolveGroundedPosition(Vector3 requested)
        {
            requested.y = groundedWorldY;
            return requested;
        }

        private bool IsInteractionBlocked()
        {
            return callbacks?.InteractionBlockedProvider?.Invoke() == true;
        }

        private float NextRandom01()
        {
            return Mathf.Clamp01(
                callbacks?.Random01Provider?.Invoke() ?? UnityEngine.Random.value);
        }
    }
}
