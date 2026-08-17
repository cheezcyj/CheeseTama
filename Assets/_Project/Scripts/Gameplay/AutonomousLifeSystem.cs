using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Autonomy
{
    public enum AutonomousLifeBehaviour
    {
        Idle = 0,
        Nap = 1,
        Window = 2,
        Shelf = 3,
        Play = 4,
        Dance = 5
    }

    [Flags]
    public enum AutonomousLifeAnchorMask
    {
        None = 0,
        Idle = 1 << 0,
        Nap = 1 << 1,
        Window = 1 << 2,
        Shelf = 1 << 3,
        Play = 1 << 4,
        Dance = 1 << 5,
        All = Idle | Nap | Window | Shelf | Play | Dance
    }

    public enum AutonomousLifeSelectionStatus
    {
        Selected = 0,
        InteractionBlocked = 1,
        SessionLimitReached = 2,
        NoAvailableBehaviour = 3,
        MissingSession = 4
    }

    public enum AutonomousLifeDiscoveryStatus
    {
        Recorded = 0,
        AlreadyRecorded = 1,
        MissingSaveData = 2
    }

    public static class AutonomousLifeBehaviourCatalog
    {
        public const string IdleId = "idle";
        public const string NapId = "nap";
        public const string WindowId = "window";
        public const string ShelfId = "shelf";
        public const string PlayId = "play";
        public const string DanceId = "dance";

        private static readonly AutonomousLifeBehaviour[] BehavioursInternal =
        {
            AutonomousLifeBehaviour.Idle,
            AutonomousLifeBehaviour.Nap,
            AutonomousLifeBehaviour.Window,
            AutonomousLifeBehaviour.Shelf,
            AutonomousLifeBehaviour.Play,
            AutonomousLifeBehaviour.Dance
        };

        public static IReadOnlyList<AutonomousLifeBehaviour> All => BehavioursInternal;

        public static string GetId(AutonomousLifeBehaviour behaviour)
        {
            return behaviour switch
            {
                AutonomousLifeBehaviour.Nap => NapId,
                AutonomousLifeBehaviour.Window => WindowId,
                AutonomousLifeBehaviour.Shelf => ShelfId,
                AutonomousLifeBehaviour.Play => PlayId,
                AutonomousLifeBehaviour.Dance => DanceId,
                _ => IdleId
            };
        }

        public static bool TryParseId(string value, out AutonomousLifeBehaviour behaviour)
        {
            switch (value?.Trim())
            {
                case IdleId:
                    behaviour = AutonomousLifeBehaviour.Idle;
                    return true;
                case NapId:
                    behaviour = AutonomousLifeBehaviour.Nap;
                    return true;
                case WindowId:
                    behaviour = AutonomousLifeBehaviour.Window;
                    return true;
                case ShelfId:
                    behaviour = AutonomousLifeBehaviour.Shelf;
                    return true;
                case PlayId:
                    behaviour = AutonomousLifeBehaviour.Play;
                    return true;
                case DanceId:
                    behaviour = AutonomousLifeBehaviour.Dance;
                    return true;
                default:
                    behaviour = AutonomousLifeBehaviour.Idle;
                    return false;
            }
        }

        public static AutonomousLifeAnchorMask GetAnchorMask(AutonomousLifeBehaviour behaviour)
        {
            return behaviour switch
            {
                AutonomousLifeBehaviour.Nap => AutonomousLifeAnchorMask.Nap,
                AutonomousLifeBehaviour.Window => AutonomousLifeAnchorMask.Window,
                AutonomousLifeBehaviour.Shelf => AutonomousLifeAnchorMask.Shelf,
                AutonomousLifeBehaviour.Play => AutonomousLifeAnchorMask.Play,
                AutonomousLifeBehaviour.Dance => AutonomousLifeAnchorMask.Dance,
                _ => AutonomousLifeAnchorMask.Idle
            };
        }
    }

    public readonly struct AutonomousLifeContext
    {
        public AutonomousLifeContext(
            int localHour,
            bool isHatched,
            int hunger,
            int mood,
            int cleanliness,
            int sleepiness,
            int health,
            string dominantTraitId,
            string equippedFloorId,
            string equippedAccentId,
            string equippedWindowId,
            string equippedShelfId,
            string equippedBedsideId,
            AutonomousLifeAnchorMask availableAnchors = AutonomousLifeAnchorMask.All)
        {
            LocalHour = NormalizeHour(localHour);
            IsHatched = isHatched;
            Hunger = ClampStat(hunger);
            Mood = ClampStat(mood);
            Cleanliness = ClampStat(cleanliness);
            Sleepiness = ClampStat(sleepiness);
            Health = ClampStat(health);
            DominantTraitId = dominantTraitId?.Trim() ?? string.Empty;
            EquippedFloorId = equippedFloorId?.Trim() ?? string.Empty;
            EquippedAccentId = equippedAccentId?.Trim() ?? string.Empty;
            EquippedWindowId = equippedWindowId?.Trim() ?? string.Empty;
            EquippedShelfId = equippedShelfId?.Trim() ?? string.Empty;
            EquippedBedsideId = equippedBedsideId?.Trim() ?? string.Empty;
            AvailableAnchors = availableAnchors;
        }

        public int LocalHour { get; }
        public bool IsHatched { get; }
        public int Hunger { get; }
        public int Mood { get; }
        public int Cleanliness { get; }
        public int Sleepiness { get; }
        public int Health { get; }
        public string DominantTraitId { get; }
        public string EquippedFloorId { get; }
        public string EquippedAccentId { get; }
        public string EquippedWindowId { get; }
        public string EquippedShelfId { get; }
        public string EquippedBedsideId { get; }
        public AutonomousLifeAnchorMask AvailableAnchors { get; }

        public static AutonomousLifeContext CreateNeutral(
            int localHour,
            AutonomousLifeAnchorMask availableAnchors = AutonomousLifeAnchorMask.All)
        {
            return new AutonomousLifeContext(
                localHour,
                true,
                80,
                70,
                90,
                20,
                100,
                NewGameSetupCatalog.BalancedTraitId,
                DecorationCatalog.CreamRugId,
                DecorationCatalog.MilkBottleId,
                DecorationCatalog.CreamCurtainId,
                DecorationCatalog.CheeseClockId,
                DecorationCatalog.MilkCushionId,
                availableAnchors);
        }

        public AutonomousLifeContext WithAvailableAnchors(AutonomousLifeAnchorMask value)
        {
            return new AutonomousLifeContext(
                LocalHour,
                IsHatched,
                Hunger,
                Mood,
                Cleanliness,
                Sleepiness,
                Health,
                DominantTraitId,
                EquippedFloorId,
                EquippedAccentId,
                EquippedWindowId,
                EquippedShelfId,
                EquippedBedsideId,
                value);
        }

        private static int NormalizeHour(int hour)
        {
            return ((hour % 24) + 24) % 24;
        }

        private static int ClampStat(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }
    }

    public sealed class AutonomousLifeSessionState
    {
        public const int MaximumBehavioursPerSession = 2;

        public int StartedBehaviourCount { get; private set; }
        public bool HasLastBehaviour { get; private set; }
        public AutonomousLifeBehaviour LastBehaviour { get; private set; }
        public bool IsExhausted => StartedBehaviourCount >= MaximumBehavioursPerSession;

        public void Reset()
        {
            StartedBehaviourCount = 0;
            HasLastBehaviour = false;
            LastBehaviour = AutonomousLifeBehaviour.Idle;
        }

        internal bool TryRecordStarted(AutonomousLifeBehaviour behaviour)
        {
            if (IsExhausted)
            {
                return false;
            }

            StartedBehaviourCount += 1;
            LastBehaviour = behaviour;
            HasLastBehaviour = true;
            return true;
        }
    }

    public readonly struct AutonomousLifeSelectionResult
    {
        internal AutonomousLifeSelectionResult(
            AutonomousLifeSelectionStatus status,
            AutonomousLifeBehaviour behaviour,
            float durationSeconds,
            float selectedWeight,
            float totalWeight)
        {
            Status = status;
            Behaviour = behaviour;
            DurationSeconds = Math.Max(0f, durationSeconds);
            SelectedWeight = Math.Max(0f, selectedWeight);
            TotalWeight = Math.Max(0f, totalWeight);
        }

        public AutonomousLifeSelectionStatus Status { get; }
        public AutonomousLifeBehaviour Behaviour { get; }
        public float DurationSeconds { get; }
        public float SelectedWeight { get; }
        public float TotalWeight { get; }
        public bool IsSelected => Status == AutonomousLifeSelectionStatus.Selected;

        internal static AutonomousLifeSelectionResult Rejected(
            AutonomousLifeSelectionStatus status)
        {
            return new AutonomousLifeSelectionResult(
                status,
                AutonomousLifeBehaviour.Idle,
                0f,
                0f,
                0f);
        }
    }

    public readonly struct AutonomousLifeDiscoveryResult
    {
        internal AutonomousLifeDiscoveryResult(
            AutonomousLifeDiscoveryStatus status,
            AutonomousLifeBehaviour behaviour,
            AutonomousLifeDiscoverySaveEntry entry)
        {
            Status = status;
            Behaviour = behaviour;
            Entry = entry;
        }

        public AutonomousLifeDiscoveryStatus Status { get; }
        public AutonomousLifeBehaviour Behaviour { get; }
        public AutonomousLifeDiscoverySaveEntry Entry { get; }
        public bool WasRecorded => Status == AutonomousLifeDiscoveryStatus.Recorded;
    }

    /// <summary>
    /// Deterministic selection rules for low-frequency autonomous life moments.
    /// Runtime presentation and save ownership remain with the caller.
    /// </summary>
    public sealed class AutonomousLifeSystem
    {
        public const float MinimumIdleDelaySeconds = 45f;
        public const float MaximumIdleDelaySeconds = 90f;
        public const float MinimumBehaviourDurationSeconds = 8f;
        public const float MaximumBehaviourDurationSeconds = 14f;

        public float ResolveIdleDelay(float normalizedRoll)
        {
            return Lerp(
                MinimumIdleDelaySeconds,
                MaximumIdleDelaySeconds,
                Clamp01(normalizedRoll));
        }

        public float ResolveBehaviourDuration(
            AutonomousLifeBehaviour behaviour,
            float normalizedRoll)
        {
            var minimum = behaviour == AutonomousLifeBehaviour.Nap
                ? MinimumBehaviourDurationSeconds + 2f
                : MinimumBehaviourDurationSeconds;
            var maximum = behaviour == AutonomousLifeBehaviour.Nap
                ? MaximumBehaviourDurationSeconds + 4f
                : MaximumBehaviourDurationSeconds;
            return Lerp(minimum, maximum, Clamp01(normalizedRoll));
        }

        public AutonomousLifeSelectionResult TrySelectAndStart(
            AutonomousLifeContext context,
            AutonomousLifeSessionState session,
            bool interactionBlocked,
            float selectionRoll,
            float durationRoll)
        {
            if (interactionBlocked)
            {
                return AutonomousLifeSelectionResult.Rejected(
                    AutonomousLifeSelectionStatus.InteractionBlocked);
            }

            if (session == null)
            {
                return AutonomousLifeSelectionResult.Rejected(
                    AutonomousLifeSelectionStatus.MissingSession);
            }

            if (session.IsExhausted)
            {
                return AutonomousLifeSelectionResult.Rejected(
                    AutonomousLifeSelectionStatus.SessionLimitReached);
            }

            var totalWeight = 0f;
            for (var index = 0; index < AutonomousLifeBehaviourCatalog.All.Count; index += 1)
            {
                var behaviour = AutonomousLifeBehaviourCatalog.All[index];
                totalWeight += GetEligibleWeight(behaviour, context, session);
            }

            if (totalWeight <= 0f)
            {
                return AutonomousLifeSelectionResult.Rejected(
                    AutonomousLifeSelectionStatus.NoAvailableBehaviour);
            }

            var threshold = Math.Min(0.999999f, Clamp01(selectionRoll)) * totalWeight;
            var cumulative = 0f;
            var selected = AutonomousLifeBehaviour.Idle;
            var selectedWeight = 0f;
            for (var index = 0; index < AutonomousLifeBehaviourCatalog.All.Count; index += 1)
            {
                var candidate = AutonomousLifeBehaviourCatalog.All[index];
                var weight = GetEligibleWeight(candidate, context, session);
                if (weight <= 0f)
                {
                    continue;
                }

                selected = candidate;
                selectedWeight = weight;
                cumulative += weight;
                if (threshold < cumulative)
                {
                    break;
                }
            }

            if (!session.TryRecordStarted(selected))
            {
                return AutonomousLifeSelectionResult.Rejected(
                    AutonomousLifeSelectionStatus.SessionLimitReached);
            }

            return new AutonomousLifeSelectionResult(
                AutonomousLifeSelectionStatus.Selected,
                selected,
                ResolveBehaviourDuration(selected, durationRoll),
                selectedWeight,
                totalWeight);
        }

        public float GetWeight(
            AutonomousLifeBehaviour behaviour,
            AutonomousLifeContext context)
        {
            if ((context.AvailableAnchors
                & AutonomousLifeBehaviourCatalog.GetAnchorMask(behaviour)) == 0)
            {
                return 0f;
            }

            var weight = GetBaseWeight(behaviour);
            ApplyLifeStageWeight(ref weight, behaviour, context);
            ApplyStateWeight(ref weight, behaviour, context);
            ApplyTimeWeight(ref weight, behaviour, context.LocalHour);
            ApplyTemperamentWeight(ref weight, behaviour, context.DominantTraitId);
            ApplyDecorationWeight(ref weight, behaviour, context);
            return Math.Max(0f, weight);
        }

        public AutonomousLifeDiscoveryResult RecordFirstDiscovery(
            AutonomousLifeSaveData saveData,
            AutonomousLifeBehaviour behaviour,
            DateTimeOffset now)
        {
            if (saveData == null)
            {
                return new AutonomousLifeDiscoveryResult(
                    AutonomousLifeDiscoveryStatus.MissingSaveData,
                    behaviour,
                    null);
            }

            var recorded = saveData.TryRecordFirstDiscovery(
                AutonomousLifeBehaviourCatalog.GetId(behaviour),
                now,
                out var entry);
            return new AutonomousLifeDiscoveryResult(
                recorded
                    ? AutonomousLifeDiscoveryStatus.Recorded
                    : AutonomousLifeDiscoveryStatus.AlreadyRecorded,
                behaviour,
                entry);
        }

        private float GetEligibleWeight(
            AutonomousLifeBehaviour behaviour,
            AutonomousLifeContext context,
            AutonomousLifeSessionState session)
        {
            if (session.HasLastBehaviour && session.LastBehaviour == behaviour)
            {
                return 0f;
            }

            return GetWeight(behaviour, context);
        }

        private static float GetBaseWeight(AutonomousLifeBehaviour behaviour)
        {
            return behaviour switch
            {
                AutonomousLifeBehaviour.Nap => 1.0f,
                AutonomousLifeBehaviour.Window => 1.0f,
                AutonomousLifeBehaviour.Shelf => 0.9f,
                AutonomousLifeBehaviour.Play => 1.2f,
                AutonomousLifeBehaviour.Dance => 0.7f,
                _ => 1.4f
            };
        }

        private static void ApplyLifeStageWeight(
            ref float weight,
            AutonomousLifeBehaviour behaviour,
            AutonomousLifeContext context)
        {
            if (context.IsHatched)
            {
                return;
            }

            weight *= behaviour switch
            {
                AutonomousLifeBehaviour.Idle => 1.8f,
                AutonomousLifeBehaviour.Nap => 1.35f,
                AutonomousLifeBehaviour.Window => 0.45f,
                _ => 0f
            };
        }

        private static void ApplyStateWeight(
            ref float weight,
            AutonomousLifeBehaviour behaviour,
            AutonomousLifeContext context)
        {
            if (context.Health < 35)
            {
                weight *= behaviour switch
                {
                    AutonomousLifeBehaviour.Idle => 2.2f,
                    AutonomousLifeBehaviour.Nap => 2.4f,
                    AutonomousLifeBehaviour.Window => 0.7f,
                    _ => 0.2f
                };
            }

            if (context.Hunger < 25)
            {
                weight *= behaviour switch
                {
                    AutonomousLifeBehaviour.Shelf => 2.7f,
                    AutonomousLifeBehaviour.Idle => 1.35f,
                    AutonomousLifeBehaviour.Play => 0.55f,
                    AutonomousLifeBehaviour.Dance => 0.45f,
                    _ => 1f
                };
            }

            if (context.Cleanliness < 35
                && (behaviour == AutonomousLifeBehaviour.Play
                    || behaviour == AutonomousLifeBehaviour.Dance))
            {
                weight *= 0.65f;
            }

            if (context.Sleepiness > 75)
            {
                weight *= behaviour switch
                {
                    AutonomousLifeBehaviour.Nap => 4f,
                    AutonomousLifeBehaviour.Idle => 1.35f,
                    AutonomousLifeBehaviour.Window => 0.65f,
                    _ => 0.3f
                };
            }

            if (context.Mood < 35)
            {
                weight *= behaviour switch
                {
                    AutonomousLifeBehaviour.Window => 1.55f,
                    AutonomousLifeBehaviour.Play => 1.25f,
                    AutonomousLifeBehaviour.Dance => 0.5f,
                    _ => 1f
                };
            }
            else if (context.Mood > 75)
            {
                weight *= behaviour switch
                {
                    AutonomousLifeBehaviour.Play => 1.35f,
                    AutonomousLifeBehaviour.Dance => 2.2f,
                    _ => 1f
                };
            }
        }

        private static void ApplyTimeWeight(
            ref float weight,
            AutonomousLifeBehaviour behaviour,
            int hour)
        {
            if (hour >= 22 || hour < 5)
            {
                weight *= behaviour switch
                {
                    AutonomousLifeBehaviour.Nap => 2.5f,
                    AutonomousLifeBehaviour.Window => 1.45f,
                    AutonomousLifeBehaviour.Dance => 0.45f,
                    _ => 1f
                };
                return;
            }

            if (hour < 11)
            {
                weight *= behaviour switch
                {
                    AutonomousLifeBehaviour.Window => 1.4f,
                    AutonomousLifeBehaviour.Shelf => 1.2f,
                    _ => 1f
                };
                return;
            }

            if (hour < 17)
            {
                if (behaviour == AutonomousLifeBehaviour.Play)
                {
                    weight *= 1.3f;
                }

                return;
            }

            weight *= behaviour switch
            {
                AutonomousLifeBehaviour.Window => 1.5f,
                AutonomousLifeBehaviour.Dance => 1.2f,
                _ => 1f
            };
        }

        private static void ApplyTemperamentWeight(
            ref float weight,
            AutonomousLifeBehaviour behaviour,
            string traitId)
        {
            switch (traitId)
            {
                case NewGameSetupCatalog.LivelyTraitId:
                    if (behaviour == AutonomousLifeBehaviour.Play)
                    {
                        weight *= 1.85f;
                    }
                    else if (behaviour == AutonomousLifeBehaviour.Dance)
                    {
                        weight *= 1.55f;
                    }
                    break;

                case NewGameSetupCatalog.ExpressiveTraitId:
                    if (behaviour == AutonomousLifeBehaviour.Dance)
                    {
                        weight *= 1.9f;
                    }
                    else if (behaviour == AutonomousLifeBehaviour.Window)
                    {
                        weight *= 1.2f;
                    }
                    break;

                case NewGameSetupCatalog.CalmTraitId:
                    if (behaviour == AutonomousLifeBehaviour.Nap
                        || behaviour == AutonomousLifeBehaviour.Window)
                    {
                        weight *= 1.45f;
                    }
                    break;

                case NewGameSetupCatalog.FocusedTraitId:
                    if (behaviour == AutonomousLifeBehaviour.Shelf)
                    {
                        weight *= 1.8f;
                    }
                    else if (behaviour == AutonomousLifeBehaviour.Window)
                    {
                        weight *= 1.25f;
                    }
                    break;

                default:
                    if (behaviour == AutonomousLifeBehaviour.Idle)
                    {
                        weight *= 1.2f;
                    }
                    break;
            }
        }

        private static void ApplyDecorationWeight(
            ref float weight,
            AutonomousLifeBehaviour behaviour,
            AutonomousLifeContext context)
        {
            if (behaviour == AutonomousLifeBehaviour.Window
                && !string.IsNullOrEmpty(context.EquippedWindowId))
            {
                weight *= 1.25f;
                if (context.EquippedWindowId == DecorationCatalog.MoonCurtainId)
                {
                    weight *= 1.25f;
                }
            }

            if (behaviour == AutonomousLifeBehaviour.Shelf
                && !string.IsNullOrEmpty(context.EquippedShelfId))
            {
                weight *= context.EquippedShelfId == DecorationCatalog.MemoryFrameId
                    ? 1.5f
                    : 1.25f;
            }

            if (behaviour == AutonomousLifeBehaviour.Nap
                && !string.IsNullOrEmpty(context.EquippedBedsideId))
            {
                weight *= context.EquippedBedsideId == DecorationCatalog.StarPlushId
                    ? 1.45f
                    : 1.3f;
            }

            if (behaviour == AutonomousLifeBehaviour.Dance
                && context.EquippedAccentId == DecorationCatalog.StarLampId)
            {
                weight *= 1.55f;
            }

            if (behaviour == AutonomousLifeBehaviour.Play
                && (context.EquippedFloorId == DecorationCatalog.CloudMatId
                    || context.EquippedFloorId == DecorationCatalog.CheeseTileId))
            {
                weight *= 1.35f;
            }
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float Lerp(float minimum, float maximum, float amount)
        {
            return minimum + ((maximum - minimum) * amount);
        }
    }
}
