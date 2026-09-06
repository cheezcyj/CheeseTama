using System;
using System.Collections.Generic;
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
    /// Deterministic XZ visibility-graph planner for the low-frequency
    /// autonomous-life movement. Renderer bounds are copied at configuration
    /// time, projected to XZ, and expanded by the configured character radius.
    /// Equivalent canonical configurations are ignored so a live bounds source
    /// can be sampled without restarting an in-flight route every frame.
    /// </summary>
    internal sealed class AutonomousLifeObstaclePathPlanner
    {
        private const float BoundaryPadding = 0.001f;
        private const float GeometryEpsilon = 0.000001f;

        private readonly List<ObstacleRectangle> obstacles =
            new List<ObstacleRectangle>();
        private readonly List<ObstacleRectangle> candidateObstacles =
            new List<ObstacleRectangle>();
        private readonly List<float> xCandidates = new List<float>();
        private readonly List<float> zCandidates = new List<float>();
        private readonly List<Vector3> visibilityNodes = new List<Vector3>();
        private readonly List<int> reversedPath = new List<int>();

        public int ObstacleCount => obstacles.Count;
        public float CharacterClearanceRadius { get; private set; }

        public bool Configure(
            IReadOnlyList<Bounds> rendererWorldBounds,
            float characterClearanceRadius)
        {
            candidateObstacles.Clear();
            var resolvedClearanceRadius = IsFinite(characterClearanceRadius)
                ? Mathf.Max(0f, characterClearanceRadius)
                : 0f;
            if (rendererWorldBounds != null)
            {
                for (var index = 0; index < rendererWorldBounds.Count; index += 1)
                {
                    var bounds = rendererWorldBounds[index];
                    var min = bounds.min;
                    var max = bounds.max;
                    if (!IsFinite(min.x)
                        || !IsFinite(min.z)
                        || !IsFinite(max.x)
                        || !IsFinite(max.z))
                    {
                        continue;
                    }

                    var rectangle = new ObstacleRectangle(
                        min.x - resolvedClearanceRadius,
                        max.x + resolvedClearanceRadius,
                        min.z - resolvedClearanceRadius,
                        max.z + resolvedClearanceRadius);
                    if (rectangle.Width <= GeometryEpsilon
                        || rectangle.Depth <= GeometryEpsilon)
                    {
                        continue;
                    }

                    candidateObstacles.Add(rectangle);
                }
            }

            candidateObstacles.Sort(CompareRectangles);
            if (HasEquivalentConfiguration(
                    candidateObstacles,
                    resolvedClearanceRadius))
            {
                return false;
            }

            obstacles.Clear();
            obstacles.AddRange(candidateObstacles);
            CharacterClearanceRadius = resolvedClearanceRadius;
            return true;
        }

        private bool HasEquivalentConfiguration(
            IReadOnlyList<ObstacleRectangle> candidates,
            float clearanceRadius)
        {
            if (Mathf.Abs(CharacterClearanceRadius - clearanceRadius) > GeometryEpsilon
                || obstacles.Count != candidates.Count)
            {
                return false;
            }

            for (var index = 0; index < obstacles.Count; index += 1)
            {
                var current = obstacles[index];
                var candidate = candidates[index];
                if (Mathf.Abs(current.MinX - candidate.MinX) > GeometryEpsilon
                    || Mathf.Abs(current.MaxX - candidate.MaxX) > GeometryEpsilon
                    || Mathf.Abs(current.MinZ - candidate.MinZ) > GeometryEpsilon
                    || Mathf.Abs(current.MaxZ - candidate.MaxZ) > GeometryEpsilon)
                {
                    return false;
                }
            }

            return true;
        }

        public Vector3 ResolveSafePosition(Vector3 requested)
        {
            if (obstacles.Count == 0 || !IsInsideAnyObstacle(requested))
            {
                return requested;
            }

            xCandidates.Clear();
            zCandidates.Clear();
            AddUniqueCandidate(xCandidates, requested.x);
            AddUniqueCandidate(zCandidates, requested.z);
            for (var index = 0; index < obstacles.Count; index += 1)
            {
                var obstacle = obstacles[index];
                AddUniqueCandidate(xCandidates, obstacle.MinX - BoundaryPadding);
                AddUniqueCandidate(xCandidates, obstacle.MaxX + BoundaryPadding);
                AddUniqueCandidate(zCandidates, obstacle.MinZ - BoundaryPadding);
                AddUniqueCandidate(zCandidates, obstacle.MaxZ + BoundaryPadding);
            }

            xCandidates.Sort();
            zCandidates.Sort();
            var best = requested;
            var bestDistanceSquared = float.PositiveInfinity;
            var found = false;
            for (var xIndex = 0; xIndex < xCandidates.Count; xIndex += 1)
            {
                for (var zIndex = 0; zIndex < zCandidates.Count; zIndex += 1)
                {
                    var candidate = new Vector3(
                        xCandidates[xIndex],
                        requested.y,
                        zCandidates[zIndex]);
                    if (IsInsideAnyObstacle(candidate))
                    {
                        continue;
                    }

                    var deltaX = candidate.x - requested.x;
                    var deltaZ = candidate.z - requested.z;
                    var distanceSquared = (deltaX * deltaX) + (deltaZ * deltaZ);
                    if (!found
                        || distanceSquared < bestDistanceSquared - GeometryEpsilon
                        || (Mathf.Abs(distanceSquared - bestDistanceSquared) <= GeometryEpsilon
                            && IsLexicographicallyBefore(candidate, best)))
                    {
                        best = candidate;
                        bestDistanceSquared = distanceSquared;
                        found = true;
                    }
                }
            }

            return found ? best : requested;
        }

        public bool TryBuildPath(
            Vector3 safeStart,
            Vector3 safeDestination,
            List<Vector3> outputWaypoints)
        {
            outputWaypoints?.Clear();
            if (outputWaypoints == null)
            {
                return false;
            }

            if (IsSegmentClear(safeStart, safeDestination))
            {
                outputWaypoints.Add(safeDestination);
                return true;
            }

            visibilityNodes.Clear();
            visibilityNodes.Add(safeStart);
            visibilityNodes.Add(safeDestination);
            for (var index = 0; index < obstacles.Count; index += 1)
            {
                var obstacle = obstacles[index];
                AddVisibilityNode(new Vector3(
                    obstacle.MinX - BoundaryPadding,
                    safeStart.y,
                    obstacle.MinZ - BoundaryPadding));
                AddVisibilityNode(new Vector3(
                    obstacle.MinX - BoundaryPadding,
                    safeStart.y,
                    obstacle.MaxZ + BoundaryPadding));
                AddVisibilityNode(new Vector3(
                    obstacle.MaxX + BoundaryPadding,
                    safeStart.y,
                    obstacle.MinZ - BoundaryPadding));
                AddVisibilityNode(new Vector3(
                    obstacle.MaxX + BoundaryPadding,
                    safeStart.y,
                    obstacle.MaxZ + BoundaryPadding));
            }

            var nodeCount = visibilityNodes.Count;
            var distances = new float[nodeCount];
            var previous = new int[nodeCount];
            var visited = new bool[nodeCount];
            for (var index = 0; index < nodeCount; index += 1)
            {
                distances[index] = float.PositiveInfinity;
                previous[index] = -1;
            }

            distances[0] = 0f;
            for (var step = 0; step < nodeCount; step += 1)
            {
                var current = -1;
                var currentDistance = float.PositiveInfinity;
                for (var index = 0; index < nodeCount; index += 1)
                {
                    if (visited[index])
                    {
                        continue;
                    }

                    if (distances[index] < currentDistance - GeometryEpsilon
                        || (Mathf.Abs(distances[index] - currentDistance) <= GeometryEpsilon
                            && (current < 0 || index < current)))
                    {
                        current = index;
                        currentDistance = distances[index];
                    }
                }

                if (current < 0 || float.IsPositiveInfinity(currentDistance))
                {
                    break;
                }

                visited[current] = true;
                if (current == 1)
                {
                    break;
                }

                for (var candidate = 0; candidate < nodeCount; candidate += 1)
                {
                    if (candidate == current
                        || visited[candidate]
                        || !IsSegmentClear(
                            visibilityNodes[current],
                            visibilityNodes[candidate]))
                    {
                        continue;
                    }

                    var candidateDistance = currentDistance
                        + HorizontalDistance(
                            visibilityNodes[current],
                            visibilityNodes[candidate]);
                    if (candidateDistance < distances[candidate] - GeometryEpsilon
                        || (Mathf.Abs(candidateDistance - distances[candidate]) <= GeometryEpsilon
                            && (previous[candidate] < 0 || current < previous[candidate])))
                    {
                        distances[candidate] = candidateDistance;
                        previous[candidate] = current;
                    }
                }
            }

            if (previous[1] < 0)
            {
                return false;
            }

            reversedPath.Clear();
            var pathNode = 1;
            while (pathNode != 0 && pathNode >= 0)
            {
                reversedPath.Add(pathNode);
                pathNode = previous[pathNode];
            }

            if (pathNode != 0)
            {
                return false;
            }

            for (var index = reversedPath.Count - 1; index >= 0; index -= 1)
            {
                outputWaypoints.Add(visibilityNodes[reversedPath[index]]);
            }

            return outputWaypoints.Count > 0;
        }

        public bool IsSegmentClear(Vector3 from, Vector3 to)
        {
            for (var index = 0; index < obstacles.Count; index += 1)
            {
                if (SegmentCrossesInterior(from, to, obstacles[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private void AddVisibilityNode(Vector3 candidate)
        {
            if (IsInsideAnyObstacle(candidate))
            {
                return;
            }

            for (var index = 0; index < visibilityNodes.Count; index += 1)
            {
                var delta = visibilityNodes[index] - candidate;
                delta.y = 0f;
                if (delta.sqrMagnitude <= GeometryEpsilon * GeometryEpsilon)
                {
                    return;
                }
            }

            visibilityNodes.Add(candidate);
        }

        private bool IsInsideAnyObstacle(Vector3 point)
        {
            for (var index = 0; index < obstacles.Count; index += 1)
            {
                if (obstacles[index].ContainsInterior(point))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SegmentCrossesInterior(
            Vector3 from,
            Vector3 to,
            ObstacleRectangle obstacle)
        {
            if (!TryResolveOpenInterval(
                    from.x,
                    to.x,
                    obstacle.MinX,
                    obstacle.MaxX,
                    out var enterX,
                    out var exitX)
                || !TryResolveOpenInterval(
                    from.z,
                    to.z,
                    obstacle.MinZ,
                    obstacle.MaxZ,
                    out var enterZ,
                    out var exitZ))
            {
                return false;
            }

            var enter = Mathf.Max(0f, Mathf.Max(enterX, enterZ));
            var exit = Mathf.Min(1f, Mathf.Min(exitX, exitZ));
            return enter < exit;
        }

        private static bool TryResolveOpenInterval(
            float from,
            float to,
            float minimum,
            float maximum,
            out float enter,
            out float exit)
        {
            var delta = to - from;
            if (Mathf.Abs(delta) <= GeometryEpsilon)
            {
                if (from > minimum && from < maximum)
                {
                    enter = float.NegativeInfinity;
                    exit = float.PositiveInfinity;
                    return true;
                }

                enter = 0f;
                exit = 0f;
                return false;
            }

            var first = (minimum - from) / delta;
            var second = (maximum - from) / delta;
            enter = Mathf.Min(first, second);
            exit = Mathf.Max(first, second);
            return true;
        }

        private static void AddUniqueCandidate(List<float> candidates, float value)
        {
            for (var index = 0; index < candidates.Count; index += 1)
            {
                if (Mathf.Abs(candidates[index] - value) <= GeometryEpsilon)
                {
                    return;
                }
            }

            candidates.Add(value);
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            var deltaX = to.x - from.x;
            var deltaZ = to.z - from.z;
            return Mathf.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
        }

        private static bool IsLexicographicallyBefore(Vector3 candidate, Vector3 current)
        {
            if (candidate.x < current.x - GeometryEpsilon)
            {
                return true;
            }

            return Mathf.Abs(candidate.x - current.x) <= GeometryEpsilon
                && candidate.z < current.z - GeometryEpsilon;
        }

        private static int CompareRectangles(
            ObstacleRectangle first,
            ObstacleRectangle second)
        {
            var comparison = first.MinX.CompareTo(second.MinX);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = first.MinZ.CompareTo(second.MinZ);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = first.MaxX.CompareTo(second.MaxX);
            return comparison != 0
                ? comparison
                : first.MaxZ.CompareTo(second.MaxZ);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private readonly struct ObstacleRectangle
        {
            public ObstacleRectangle(float minX, float maxX, float minZ, float maxZ)
            {
                MinX = minX;
                MaxX = maxX;
                MinZ = minZ;
                MaxZ = maxZ;
            }

            public float MinX { get; }
            public float MaxX { get; }
            public float MinZ { get; }
            public float MaxZ { get; }
            public float Width => MaxX - MinX;
            public float Depth => MaxZ - MinZ;

            public bool ContainsInterior(Vector3 point)
            {
                return point.x > MinX
                    && point.x < MaxX
                    && point.z > MinZ
                    && point.z < MaxZ;
            }
        }
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
        private readonly AutonomousLifeObstaclePathPlanner obstaclePathPlanner =
            new AutonomousLifeObstaclePathPlanner();
        private readonly List<Vector3> tweenWaypoints = new List<Vector3>();

        private AutonomousLifeAnchorBindings anchors;
        private AutonomousLifePresenterCallbacks callbacks;
        private Func<IReadOnlyList<Bounds>> movementObstacleBoundsProvider;
        private float movementObstacleProviderClearanceRadius;
        private bool refreshingMovementObstacleProvider;
        private AutonomousLifePresentationPhase phase =
            AutonomousLifePresentationPhase.Unconfigured;
        private AutonomousLifeSelectionResult currentSelection;
        private Vector3 requestedHomePosition;
        private Vector3 homePosition;
        private float groundedWorldY;
        private Vector3 tweenFrom;
        private Vector3 tweenTo;
        private int tweenWaypointIndex;
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
        public int MovementObstacleCount => obstaclePathPlanner.ObstacleCount;
        public float CharacterClearanceRadius =>
            obstaclePathPlanner.CharacterClearanceRadius;

        public void Configure(
            Transform movingCharacterRoot,
            AutonomousLifeAnchorBindings anchorBindings,
            AutonomousLifePresenterCallbacks integrationCallbacks)
        {
            Configure(
                movingCharacterRoot,
                anchorBindings,
                integrationCallbacks,
                null,
                0f);
        }

        /// <summary>
        /// Configures autonomous movement with world-space renderer bounds for
        /// furniture that must block travel. Bounds are copied and expanded in
        /// XZ by <paramref name="characterClearanceRadius"/>.
        /// </summary>
        public void Configure(
            Transform movingCharacterRoot,
            AutonomousLifeAnchorBindings anchorBindings,
            AutonomousLifePresenterCallbacks integrationCallbacks,
            IReadOnlyList<Bounds> obstacleRendererWorldBounds,
            float characterClearanceRadius)
        {
            var configuredCharacterPosition = movingCharacterRoot != null
                ? movingCharacterRoot.position
                : default;
            CancelWithoutReschedule(true);
            ClearMovementObstacleProvider();

            characterRoot = movingCharacterRoot;
            anchors = anchorBindings ?? new AutonomousLifeAnchorBindings(
                null,
                null,
                null,
                null,
                null,
                null);
            callbacks = integrationCallbacks;
            obstaclePathPlanner.Configure(
                obstacleRendererWorldBounds,
                characterClearanceRadius);
            configured = characterRoot != null;
            if (!configured)
            {
                phase = AutonomousLifePresentationPhase.Unconfigured;
                return;
            }

            groundedWorldY = configuredCharacterPosition.y;
            requestedHomePosition = anchors.Idle != null
                ? anchors.Idle.position
                : configuredCharacterPosition;
            homePosition = ResolveSafeGroundedPosition(requestedHomePosition);
            SnapHome();
            BeginSession();
        }

        /// <summary>
        /// Replaces the copied furniture bounds without changing the public
        /// presenter dependencies. Equivalent repeated configurations are a
        /// no-op; a changed configuration safely replans an in-flight route
        /// while preserving the current behaviour and waiting countdown.
        /// </summary>
        public void ConfigureMovementObstacles(
            IReadOnlyList<Bounds> obstacleRendererWorldBounds,
            float characterClearanceRadius)
        {
            ClearMovementObstacleProvider();
            ApplyMovementObstacleConfiguration(
                obstacleRendererWorldBounds,
                characterClearanceRadius);
        }

        /// <summary>
        /// Registers a live world-bounds source for fixed furniture and movable
        /// decorations. The source is sampled by Tick and LateUpdate; canonical
        /// equality prevents unchanged bounds or list ordering from restarting
        /// movement. Passing null clears both the provider and obstacle bounds.
        /// </summary>
        public void ConfigureMovementObstacleProvider(
            Func<IReadOnlyList<Bounds>> obstacleRendererWorldBoundsProvider,
            float characterClearanceRadius)
        {
            movementObstacleBoundsProvider = obstacleRendererWorldBoundsProvider;
            movementObstacleProviderClearanceRadius = characterClearanceRadius;
            if (movementObstacleBoundsProvider == null)
            {
                ApplyMovementObstacleConfiguration(null, characterClearanceRadius);
                return;
            }

            RefreshMovementObstaclesFromProvider();
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
            RefreshMovementObstaclesFromProvider();
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

        private void LateUpdate()
        {
            // Decoration placement also runs in Update. A late sample guarantees
            // its final preview pose is reconciled before this frame is rendered,
            // regardless of component Update ordering.
            RefreshMovementObstaclesFromProvider();
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
            ClearTweenWaypoints();
            var safeStart = ResolveSafeGroundedPosition(characterRoot.position);
            var safeDestination = ResolveSafeGroundedPosition(destination);
            characterRoot.position = safeStart;

            if (AccessibilityRuntime.ReducedMotion)
            {
                characterRoot.position = safeDestination;
                completeImmediately?.Invoke();
                return;
            }

            if (!obstaclePathPlanner.TryBuildPath(
                    safeStart,
                    safeDestination,
                    tweenWaypoints))
            {
                completeImmediately?.Invoke();
                return;
            }

            tweenWaypointIndex = 0;
            if (!TryConfigureCurrentTweenSegment(tweenPhase))
            {
                ClearTweenWaypoints();
                completeImmediately?.Invoke();
            }
        }

        private bool TryConfigureCurrentTweenSegment(
            AutonomousLifePresentationPhase tweenPhase)
        {
            while (tweenWaypointIndex < tweenWaypoints.Count)
            {
                tweenFrom = ResolveSafeGroundedPosition(characterRoot.position);
                tweenTo = ResolveSafeGroundedPosition(
                    tweenWaypoints[tweenWaypointIndex]);
                characterRoot.position = tweenFrom;
                var horizontalDelta = tweenTo - tweenFrom;
                horizontalDelta.y = 0f;
                var distance = horizontalDelta.magnitude;
                if (distance * distance <= PositionEpsilonSquared)
                {
                    characterRoot.position = tweenTo;
                    tweenWaypointIndex += 1;
                    continue;
                }

                tweenElapsed = 0f;
                tweenDuration = Mathf.Clamp(
                    distance * MoveDurationPerWorldUnit,
                    MinimumMoveDurationSeconds,
                    MaximumMoveDurationSeconds);
                phase = tweenPhase;
                return true;
            }

            return false;
        }

        private void ClearTweenWaypoints()
        {
            tweenWaypoints.Clear();
            tweenWaypointIndex = 0;
            tweenElapsed = 0f;
            tweenDuration = 0f;
        }

        private void ApplyMovementObstacleConfiguration(
            IReadOnlyList<Bounds> obstacleRendererWorldBounds,
            float characterClearanceRadius)
        {
            if (!obstaclePathPlanner.Configure(
                    obstacleRendererWorldBounds,
                    characterClearanceRadius)
                || !IsConfigured)
            {
                return;
            }

            homePosition = ResolveSafeGroundedPosition(requestedHomePosition);
            switch (phase)
            {
                case AutonomousLifePresentationPhase.MovingToAnchor:
                {
                    var requestedAnchor = anchors.Resolve(currentSelection.Behaviour);
                    BeginTween(
                        ResolveGroundedPosition(
                            requestedAnchor != null
                                ? requestedAnchor.position
                                : homePosition),
                        AutonomousLifePresentationPhase.MovingToAnchor,
                        StartPerforming);
                    break;
                }

                case AutonomousLifePresentationPhase.ReturningHome:
                    BeginTween(
                        homePosition,
                        AutonomousLifePresentationPhase.ReturningHome,
                        CompleteCurrentBehaviour);
                    break;

                case AutonomousLifePresentationPhase.Performing:
                    characterRoot.position = ResolveSafeGroundedPosition(
                        characterRoot.position);
                    break;

                default:
                    SnapHome();
                    break;
            }
        }

        private void RefreshMovementObstaclesFromProvider()
        {
            var provider = movementObstacleBoundsProvider;
            if (provider == null || refreshingMovementObstacleProvider)
            {
                return;
            }

            refreshingMovementObstacleProvider = true;
            try
            {
                ApplyMovementObstacleConfiguration(
                    provider.Invoke(),
                    movementObstacleProviderClearanceRadius);
            }
            finally
            {
                refreshingMovementObstacleProvider = false;
            }
        }

        private void ClearMovementObstacleProvider()
        {
            movementObstacleBoundsProvider = null;
            movementObstacleProviderClearanceRadius = 0f;
            refreshingMovementObstacleProvider = false;
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
                tweenWaypointIndex += 1;
                if (TryConfigureCurrentTweenSegment(phase))
                {
                    return;
                }

                ClearTweenWaypoints();
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
            ClearTweenWaypoints();
            if (configured && characterRoot != null)
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
                ClearTweenWaypoints();
                homePosition = ResolveSafeGroundedPosition(homePosition);
                characterRoot.position = homePosition;
            }
        }

        private Vector3 ResolveGroundedPosition(Vector3 requested)
        {
            requested.y = groundedWorldY;
            return requested;
        }

        private Vector3 ResolveSafeGroundedPosition(Vector3 requested)
        {
            return obstaclePathPlanner.ResolveSafePosition(
                ResolveGroundedPosition(requested));
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
