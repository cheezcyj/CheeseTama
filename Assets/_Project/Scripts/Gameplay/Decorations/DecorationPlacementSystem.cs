using System;
using System.Collections.Generic;

namespace CheeseTama.Gameplay.Decorations
{
    public sealed class DecorationPlacementEntry
    {
        public DecorationPlacementEntry(
            DecorationSlot slot,
            string decorationId,
            float normalizedX,
            float normalizedY,
            int rotationStep)
        {
            Slot = slot;
            DecorationId = decorationId?.Trim() ?? string.Empty;
            NormalizedX = DecorationPlacementSystem.NormalizeSavedCoordinate(normalizedX);
            NormalizedY = DecorationPlacementSystem.NormalizeSavedCoordinate(normalizedY);
            RotationStep = DecorationPlacementSystem.NormalizeRotationStep(rotationStep);
        }

        public DecorationSlot Slot { get; }
        public string DecorationId { get; }
        public float NormalizedX { get; }
        public float NormalizedY { get; }
        public int RotationStep { get; }
        public float RotationDegrees => RotationStep * DecorationPlacementSystem.RotationStepDegrees;
    }

    public sealed class DecorationPlacementSnapshot
    {
        private static readonly DecorationSlot[] MovableSlots =
        {
            DecorationSlot.Accent,
            DecorationSlot.Shelf,
            DecorationSlot.Bedside
        };

        private readonly DecorationPlacementEntry[] entries;

        public DecorationPlacementSnapshot(IEnumerable<DecorationPlacementEntry> source)
        {
            var bySlot = new Dictionary<DecorationSlot, DecorationPlacementEntry>();
            if (source != null)
            {
                foreach (var entry in source)
                {
                    if (entry == null
                        || !DecorationPlacementSystem.IsMovableSlot(entry.Slot)
                        || string.IsNullOrWhiteSpace(entry.DecorationId))
                    {
                        continue;
                    }

                    // Last valid value wins so a repaired save follows the newest serialized entry.
                    bySlot[entry.Slot] = new DecorationPlacementEntry(
                        entry.Slot,
                        entry.DecorationId,
                        entry.NormalizedX,
                        entry.NormalizedY,
                        entry.RotationStep);
                }
            }

            var normalized = new List<DecorationPlacementEntry>(MovableSlots.Length);
            for (var index = 0; index < MovableSlots.Length; index += 1)
            {
                if (bySlot.TryGetValue(MovableSlots[index], out var entry))
                {
                    normalized.Add(entry);
                }
            }

            entries = normalized.ToArray();
        }

        public IReadOnlyList<DecorationPlacementEntry> Entries => entries;

        public DecorationPlacementEntry Find(DecorationSlot slot)
        {
            for (var index = 0; index < entries.Length; index += 1)
            {
                if (entries[index].Slot == slot)
                {
                    return entries[index];
                }
            }

            return null;
        }
    }

    public enum DecorationPlacementStatus
    {
        Updated,
        NoChange,
        UnsupportedSlot,
        MissingPlacement,
        EquippedItemMismatch,
        InvalidCoordinate
    }

    public readonly struct DecorationPlacementResult
    {
        public DecorationPlacementResult(
            DecorationPlacementStatus status,
            DecorationPlacementSnapshot snapshot)
        {
            Status = status;
            Snapshot = snapshot ?? new DecorationPlacementSnapshot(null);
        }

        public DecorationPlacementStatus Status { get; }
        public DecorationPlacementSnapshot Snapshot { get; }
        public bool Succeeded => Status == DecorationPlacementStatus.Updated
            || Status == DecorationPlacementStatus.NoChange;
    }

    public static class DecorationPlacementSystem
    {
        public const float MinimumNormalizedCoordinate = -1f;
        public const float MaximumNormalizedCoordinate = 1f;
        public const float GridStep = 0.1f;
        public const int RotationStepDegrees = 15;
        public const int RotationStepCount = 360 / RotationStepDegrees;

        public static bool IsMovableSlot(DecorationSlot slot)
        {
            return slot == DecorationSlot.Accent
                || slot == DecorationSlot.Shelf
                || slot == DecorationSlot.Bedside;
        }

        public static DecorationPlacementSnapshot MigrateFromLegacySlots(
            string equippedAccentId,
            string equippedShelfId,
            string equippedBedsideId)
        {
            return ReconcileWithEquippedSlots(
                null,
                equippedAccentId,
                equippedShelfId,
                equippedBedsideId);
        }

        public static DecorationPlacementSnapshot ReconcileWithEquippedSlots(
            DecorationPlacementSnapshot source,
            string equippedAccentId,
            string equippedShelfId,
            string equippedBedsideId)
        {
            var reconciled = new List<DecorationPlacementEntry>(3);
            ReconcileSlot(reconciled, source, DecorationSlot.Accent, equippedAccentId);
            ReconcileSlot(reconciled, source, DecorationSlot.Shelf, equippedShelfId);
            ReconcileSlot(reconciled, source, DecorationSlot.Bedside, equippedBedsideId);
            return new DecorationPlacementSnapshot(reconciled);
        }

        public static DecorationPlacementResult SetPose(
            DecorationPlacementSnapshot source,
            DecorationSlot slot,
            string equippedDecorationId,
            float normalizedX,
            float normalizedY,
            int rotationStep)
        {
            source ??= new DecorationPlacementSnapshot(null);
            if (!IsMovableSlot(slot))
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.UnsupportedSlot,
                    source);
            }

            if (!IsFinite(normalizedX) || !IsFinite(normalizedY))
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.InvalidCoordinate,
                    source);
            }

            var current = source.Find(slot);
            if (current == null)
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.MissingPlacement,
                    source);
            }

            var normalizedId = equippedDecorationId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(normalizedId)
                || !string.Equals(current.DecorationId, normalizedId, StringComparison.Ordinal))
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.EquippedItemMismatch,
                    source);
            }

            var replacement = new DecorationPlacementEntry(
                slot,
                normalizedId,
                normalizedX,
                normalizedY,
                rotationStep);
            if (EntriesEqual(current, replacement))
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.NoChange,
                    source);
            }

            return new DecorationPlacementResult(
                DecorationPlacementStatus.Updated,
                ReplaceEntry(source, replacement));
        }

        public static DecorationPlacementResult Nudge(
            DecorationPlacementSnapshot source,
            DecorationSlot slot,
            string equippedDecorationId,
            int horizontalSteps,
            int verticalSteps)
        {
            source ??= new DecorationPlacementSnapshot(null);
            var current = source.Find(slot);
            if (current == null)
            {
                return new DecorationPlacementResult(
                    IsMovableSlot(slot)
                        ? DecorationPlacementStatus.MissingPlacement
                        : DecorationPlacementStatus.UnsupportedSlot,
                    source);
            }

            return SetPose(
                source,
                slot,
                equippedDecorationId,
                current.NormalizedX + horizontalSteps * GridStep,
                current.NormalizedY + verticalSteps * GridStep,
                current.RotationStep);
        }

        public static DecorationPlacementResult Rotate(
            DecorationPlacementSnapshot source,
            DecorationSlot slot,
            string equippedDecorationId,
            int stepDelta)
        {
            source ??= new DecorationPlacementSnapshot(null);
            var current = source.Find(slot);
            if (current == null)
            {
                return new DecorationPlacementResult(
                    IsMovableSlot(slot)
                        ? DecorationPlacementStatus.MissingPlacement
                        : DecorationPlacementStatus.UnsupportedSlot,
                    source);
            }

            return SetPose(
                source,
                slot,
                equippedDecorationId,
                current.NormalizedX,
                current.NormalizedY,
                current.RotationStep + stepDelta);
        }

        public static DecorationPlacementResult ResetToAnchor(
            DecorationPlacementSnapshot source,
            DecorationSlot slot,
            string equippedDecorationId)
        {
            return SetPose(source, slot, equippedDecorationId, 0f, 0f, 0);
        }

        public static bool SnapshotsEqual(
            DecorationPlacementSnapshot left,
            DecorationPlacementSnapshot right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null
                || right == null
                || left.Entries.Count != right.Entries.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Entries.Count; index += 1)
            {
                if (!EntriesEqual(left.Entries[index], right.Entries[index]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static float NormalizeSavedCoordinate(float value)
        {
            if (!IsFinite(value))
            {
                return 0f;
            }

            var clamped = Math.Max(MinimumNormalizedCoordinate,
                Math.Min(MaximumNormalizedCoordinate, value));
            var snapped = (float)(Math.Round(
                clamped / GridStep,
                MidpointRounding.AwayFromZero) * GridStep);
            if (Math.Abs(snapped) < GridStep * 0.5f)
            {
                return 0f;
            }

            return Math.Max(MinimumNormalizedCoordinate,
                Math.Min(MaximumNormalizedCoordinate, snapped));
        }

        internal static int NormalizeRotationStep(int value)
        {
            var normalized = value % RotationStepCount;
            return normalized < 0 ? normalized + RotationStepCount : normalized;
        }

        private static void ReconcileSlot(
            ICollection<DecorationPlacementEntry> destination,
            DecorationPlacementSnapshot source,
            DecorationSlot slot,
            string equippedDecorationId)
        {
            var normalizedId = equippedDecorationId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(normalizedId))
            {
                return;
            }

            var existing = source?.Find(slot);
            destination.Add(existing != null
                && string.Equals(existing.DecorationId, normalizedId, StringComparison.Ordinal)
                    ? existing
                    : new DecorationPlacementEntry(slot, normalizedId, 0f, 0f, 0));
        }

        private static DecorationPlacementSnapshot ReplaceEntry(
            DecorationPlacementSnapshot source,
            DecorationPlacementEntry replacement)
        {
            var entries = new List<DecorationPlacementEntry>(source.Entries.Count);
            var replaced = false;
            for (var index = 0; index < source.Entries.Count; index += 1)
            {
                if (source.Entries[index].Slot == replacement.Slot)
                {
                    entries.Add(replacement);
                    replaced = true;
                }
                else
                {
                    entries.Add(source.Entries[index]);
                }
            }

            if (!replaced)
            {
                entries.Add(replacement);
            }

            return new DecorationPlacementSnapshot(entries);
        }

        private static bool EntriesEqual(
            DecorationPlacementEntry left,
            DecorationPlacementEntry right)
        {
            return left != null
                && right != null
                && left.Slot == right.Slot
                && string.Equals(left.DecorationId, right.DecorationId, StringComparison.Ordinal)
                && Math.Abs(left.NormalizedX - right.NormalizedX) < 0.0001f
                && Math.Abs(left.NormalizedY - right.NormalizedY) < 0.0001f
                && left.RotationStep == right.RotationStep;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class DecorationPlacementEditSession
    {
        private DecorationPlacementSnapshot original;
        private DecorationPlacementSnapshot preview;
        private DecorationSlot slot;

        public bool IsActive { get; private set; }
        public DecorationSlot Slot => slot;
        public DecorationPlacementSnapshot Original => original;
        public DecorationPlacementSnapshot Preview => preview;

        public bool Begin(DecorationPlacementSnapshot snapshot, DecorationSlot requestedSlot)
        {
            if (IsActive
                || snapshot == null
                || !DecorationPlacementSystem.IsMovableSlot(requestedSlot)
                || snapshot.Find(requestedSlot) == null)
            {
                return false;
            }

            original = snapshot;
            preview = snapshot;
            slot = requestedSlot;
            IsActive = true;
            return true;
        }

        public DecorationPlacementResult SetPose(float normalizedX, float normalizedY)
        {
            var current = IsActive ? preview?.Find(slot) : null;
            if (current == null)
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.MissingPlacement,
                    preview);
            }

            var result = DecorationPlacementSystem.SetPose(
                preview,
                slot,
                current.DecorationId,
                normalizedX,
                normalizedY,
                current.RotationStep);
            if (result.Succeeded)
            {
                preview = result.Snapshot;
            }

            return result;
        }

        public DecorationPlacementResult Nudge(int horizontalSteps, int verticalSteps)
        {
            var current = IsActive ? preview?.Find(slot) : null;
            if (current == null)
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.MissingPlacement,
                    preview);
            }

            var result = DecorationPlacementSystem.Nudge(
                preview,
                slot,
                current.DecorationId,
                horizontalSteps,
                verticalSteps);
            if (result.Succeeded)
            {
                preview = result.Snapshot;
            }

            return result;
        }

        public DecorationPlacementResult Rotate(int stepDelta)
        {
            var current = IsActive ? preview?.Find(slot) : null;
            if (current == null)
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.MissingPlacement,
                    preview);
            }

            var result = DecorationPlacementSystem.Rotate(
                preview,
                slot,
                current.DecorationId,
                stepDelta);
            if (result.Succeeded)
            {
                preview = result.Snapshot;
            }

            return result;
        }

        public DecorationPlacementResult ResetToAnchor()
        {
            var current = IsActive ? preview?.Find(slot) : null;
            if (current == null)
            {
                return new DecorationPlacementResult(
                    DecorationPlacementStatus.MissingPlacement,
                    preview);
            }

            var result = DecorationPlacementSystem.ResetToAnchor(
                preview,
                slot,
                current.DecorationId);
            if (result.Succeeded)
            {
                preview = result.Snapshot;
            }

            return result;
        }

        public DecorationPlacementSnapshot Confirm()
        {
            if (!IsActive)
            {
                return preview;
            }

            IsActive = false;
            original = preview;
            return preview;
        }

        public DecorationPlacementSnapshot Cancel()
        {
            if (!IsActive)
            {
                return original;
            }

            IsActive = false;
            preview = original;
            return original;
        }
    }
}
