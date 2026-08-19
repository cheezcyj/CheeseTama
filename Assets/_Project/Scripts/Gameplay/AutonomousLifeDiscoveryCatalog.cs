using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Autonomy
{
    public sealed class AutonomousLifeDiscoveryItemSnapshot
    {
        internal AutonomousLifeDiscoveryItemSnapshot(
            int slotIndex,
            bool isDiscovered,
            string behaviourId,
            string displayName,
            string description,
            string firstDiscoveredAtIso)
        {
            SlotIndex = Math.Max(0, slotIndex);
            IsDiscovered = isDiscovered;
            BehaviourId = isDiscovered ? behaviourId ?? string.Empty : string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            FirstDiscoveredAtIso = isDiscovered
                ? firstDiscoveredAtIso ?? string.Empty
                : string.Empty;
        }

        public int SlotIndex { get; }
        public bool IsDiscovered { get; }
        public bool HasDiscoveryTime => !string.IsNullOrEmpty(FirstDiscoveredAtIso);
        public string BehaviourId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string FirstDiscoveredAtIso { get; }
    }

    public sealed class AutonomousLifeDiscoveryCollectionSnapshot
    {
        internal AutonomousLifeDiscoveryCollectionSnapshot(
            IReadOnlyList<AutonomousLifeDiscoveryItemSnapshot> items,
            int discoveredCount)
        {
            Items = items ?? Array.Empty<AutonomousLifeDiscoveryItemSnapshot>();
            DiscoveredCount = Math.Max(0, Math.Min(Items.Count, discoveredCount));
        }

        public int TotalCount => Items.Count;
        public int DiscoveredCount { get; }
        public IReadOnlyList<AutonomousLifeDiscoveryItemSnapshot> Items { get; }
    }

    /// <summary>
    /// Public presentation catalog for the six autonomous-life moments. A hidden
    /// slot never exposes its behaviour id, name, description, or timestamp.
    /// Snapshot creation is read-only and tolerates unnormalized save data.
    /// </summary>
    public static class AutonomousLifeDiscoveryCatalog
    {
        public const int TotalDiscoveryCount = 6;
        public const string HiddenDisplayName = "???";
        public const string HiddenDescription = "아직 발견하지 못한 생활 순간이에요.";

        private static readonly Definition[] Definitions =
        {
            new Definition(
                AutonomousLifeBehaviour.Idle,
                "느긋한 한때",
                "아무것도 서두르지 않고 밀크룸의 시간을 느껴요."),
            new Definition(
                AutonomousLifeBehaviour.Nap,
                "포근한 낮잠",
                "편안한 자리를 찾아 몸을 말고 잠깐 쉬어요."),
            new Definition(
                AutonomousLifeBehaviour.Window,
                "창가 구경",
                "창밖의 빛과 움직임을 한참 바라봐요."),
            new Definition(
                AutonomousLifeBehaviour.Shelf,
                "선반 살피기",
                "선반 가까이 다가가 익숙한 물건을 하나씩 살펴봐요."),
            new Definition(
                AutonomousLifeBehaviour.Play,
                "혼자 놀기",
                "밀크룸의 소품을 친구 삼아 신나게 놀아요."),
            new Definition(
                AutonomousLifeBehaviour.Dance,
                "작은 춤",
                "기분 좋은 리듬을 타며 짧은 춤을 보여줘요.")
        };

        public static AutonomousLifeDiscoveryCollectionSnapshot CreateSnapshot(
            AutonomousLifeSaveData saveData)
        {
            var items = new AutonomousLifeDiscoveryItemSnapshot[Definitions.Length];
            var discoveredCount = 0;
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var definition = Definitions[index];
                var entry = FindEntryReadOnly(saveData, definition.Behaviour);
                if (entry == null)
                {
                    items[index] = Hidden(index);
                    continue;
                }

                items[index] = Discovered(index, definition, entry.firstDiscoveredAtIso);
                discoveredCount += 1;
            }

            return new AutonomousLifeDiscoveryCollectionSnapshot(items, discoveredCount);
        }

        public static bool TryCreateObservedSnapshot(
            AutonomousLifeDiscoveryResult result,
            out AutonomousLifeDiscoveryItemSnapshot snapshot)
        {
            if (!result.WasRecorded
                || result.Entry == null
                || !TryFindDefinition(result.Behaviour, out var definition))
            {
                snapshot = null;
                return false;
            }

            var expectedId = AutonomousLifeBehaviourCatalog.GetId(definition.Behaviour);
            if (!TryNormalizeKnownId(result.Entry.behaviourId, out var observedId)
                || !string.Equals(expectedId, observedId, StringComparison.Ordinal))
            {
                snapshot = null;
                return false;
            }

            snapshot = Discovered(
                IndexOf(definition.Behaviour),
                definition,
                result.Entry.firstDiscoveredAtIso);
            return true;
        }

        private static AutonomousLifeDiscoveryItemSnapshot Hidden(int index)
        {
            return new AutonomousLifeDiscoveryItemSnapshot(
                index,
                false,
                string.Empty,
                HiddenDisplayName,
                HiddenDescription,
                string.Empty);
        }

        private static AutonomousLifeDiscoveryItemSnapshot Discovered(
            int index,
            Definition definition,
            string firstDiscoveredAtIso)
        {
            return new AutonomousLifeDiscoveryItemSnapshot(
                index,
                true,
                AutonomousLifeBehaviourCatalog.GetId(definition.Behaviour),
                definition.DisplayName,
                definition.Description,
                NormalizeTimestamp(firstDiscoveredAtIso));
        }

        private static AutonomousLifeDiscoverySaveEntry FindEntryReadOnly(
            AutonomousLifeSaveData saveData,
            AutonomousLifeBehaviour behaviour)
        {
            var entries = saveData?.firstDiscoveries;
            if (entries == null)
            {
                return null;
            }

            var expectedId = AutonomousLifeBehaviourCatalog.GetId(behaviour);
            for (var index = 0; index < entries.Count; index += 1)
            {
                var entry = entries[index];
                if (entry != null
                    && TryNormalizeKnownId(entry.behaviourId, out var candidateId)
                    && string.Equals(candidateId, expectedId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool TryNormalizeKnownId(string value, out string canonicalId)
        {
            if (AutonomousLifeBehaviourCatalog.TryParseId(
                    value?.Trim(),
                    out var behaviour))
            {
                canonicalId = AutonomousLifeBehaviourCatalog.GetId(behaviour);
                return true;
            }

            canonicalId = string.Empty;
            return false;
        }

        private static string NormalizeTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value)
                || !DateTimeOffset.TryParse(
                    value.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                return string.Empty;
            }

            return parsed.ToString("O", CultureInfo.InvariantCulture);
        }

        private static bool TryFindDefinition(
            AutonomousLifeBehaviour behaviour,
            out Definition definition)
        {
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (Definitions[index].Behaviour == behaviour)
                {
                    definition = Definitions[index];
                    return true;
                }
            }

            definition = null;
            return false;
        }

        private static int IndexOf(AutonomousLifeBehaviour behaviour)
        {
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (Definitions[index].Behaviour == behaviour)
                {
                    return index;
                }
            }

            return 0;
        }

        private sealed class Definition
        {
            public Definition(
                AutonomousLifeBehaviour behaviour,
                string displayName,
                string description)
            {
                Behaviour = behaviour;
                DisplayName = displayName ?? string.Empty;
                Description = description ?? string.Empty;
            }

            public AutonomousLifeBehaviour Behaviour { get; }
            public string DisplayName { get; }
            public string Description { get; }
        }
    }
}
