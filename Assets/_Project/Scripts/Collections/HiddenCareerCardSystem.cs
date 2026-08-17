using System;
using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;

namespace CheeseTama.Collections.HiddenCareers
{
    public readonly struct HiddenCareerUnlockResult
    {
        internal HiddenCareerUnlockResult(bool unlocked, HiddenCareerCardViewData card)
        {
            Unlocked = unlocked;
            Card = card;
        }

        public bool Unlocked { get; }
        public HiddenCareerCardViewData Card { get; }

        public static HiddenCareerUnlockResult None =>
            new HiddenCareerUnlockResult(false, null);
    }

    /// <summary>
    /// Owns the seven known hidden career definitions and their internal unlock
    /// evaluation. Only unlocked presentation-safe cards leave this boundary.
    /// </summary>
    public sealed class HiddenCareerCardSystem
    {
        private readonly HiddenCollectionSystem hiddenCollectionSystem =
            new HiddenCollectionSystem();

        /// <summary>
        /// Evaluates in stable catalog order and unlocks at most one card. The
        /// caller remains responsible for saving and announcing the result.
        /// </summary>
        public HiddenCareerUnlockResult TryUnlockNextEligible(
            CheeseTamaSaveData saveData,
            DateTimeOffset acquiredAt)
        {
            if (!IsHiddenRouteAvailable(saveData))
            {
                return HiddenCareerUnlockResult.None;
            }

            saveData.EnsureRuntimeDefaults();
            var cards = HiddenCareerCardCatalog.All;
            for (var index = 0; index < cards.Count; index += 1)
            {
                var definition = cards[index];
                if (FindUnlockedEntry(saveData.collections, definition.Id) != null
                    || !MeetsInternalCondition(saveData, definition.Id))
                {
                    continue;
                }

                if (!hiddenCollectionSystem.Unlock(
                        saveData.collections,
                        definition.Id,
                        acquiredAt))
                {
                    continue;
                }

                return new HiddenCareerUnlockResult(
                    true,
                    new HiddenCareerCardViewData(
                        definition,
                        acquiredAt.ToString("O")));
            }

            return HiddenCareerUnlockResult.None;
        }

        /// <summary>
        /// Explicit authoritative unlock entry point for scripted events. Unknown
        /// IDs and repeated unlocks fail closed.
        /// </summary>
        public HiddenCareerUnlockResult TryUnlockKnownCard(
            CollectionSaveData collections,
            string cardId,
            DateTimeOffset acquiredAt)
        {
            var definition = HiddenCareerCardCatalog.Find(cardId);
            if (collections == null || definition == null)
            {
                return HiddenCareerUnlockResult.None;
            }

            if (!hiddenCollectionSystem.Unlock(collections, definition.Id, acquiredAt))
            {
                return HiddenCareerUnlockResult.None;
            }

            return new HiddenCareerUnlockResult(
                true,
                new HiddenCareerCardViewData(definition, acquiredAt.ToString("O")));
        }

        public IReadOnlyList<HiddenCareerCardViewData> GetVisibleUnlockedCards(
            CollectionSaveData collections)
        {
            var visible = new List<HiddenCareerCardViewData>();
            if (collections == null)
            {
                return visible;
            }

            collections.EnsureRuntimeDefaults();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var definitions = HiddenCareerCardCatalog.All;
            for (var index = 0; index < definitions.Count; index += 1)
            {
                var definition = definitions[index];
                var entry = FindUnlockedEntry(collections, definition.Id);
                if (entry == null || !seen.Add(definition.Id))
                {
                    continue;
                }

                visible.Add(new HiddenCareerCardViewData(
                    definition,
                    entry.acquiredAtIso));
            }

            return visible;
        }

        /// <summary>
        /// Returns only benefits backed by already-unlocked known cards. Applying
        /// a benefit remains the responsibility of the authoritative gameplay
        /// system; this query never changes state.
        /// </summary>
        public IReadOnlyList<HiddenCareerBenefit> GetUnlockedBenefits(
            CollectionSaveData collections)
        {
            var benefits = new List<HiddenCareerBenefit>();
            if (collections == null)
            {
                return benefits;
            }

            var definitions = HiddenCareerCardCatalog.All;
            for (var index = 0; index < definitions.Count; index += 1)
            {
                var definition = definitions[index];
                if (definition.Benefit != null
                    && FindUnlockedEntry(collections, definition.Id) != null)
                {
                    benefits.Add(definition.Benefit);
                }
            }

            return benefits;
        }

        private static bool IsHiddenRouteAvailable(CheeseTamaSaveData saveData)
        {
            return saveData?.cheeseTama != null
                && saveData.unlocks != null
                && saveData.cheeseTama.level >= UnlockSystem.MaxLevel
                && saveData.unlocks.starMilkUnlocked
                && saveData.unlocks.fantasyPowderEnabled;
        }

        private static bool MeetsInternalCondition(CheeseTamaSaveData saveData, string cardId)
        {
            var history = saveData.careHistory;
            var traitId = saveData.newGameSetup?.temperamentSeed?.dominantTraitId
                ?? string.Empty;
            var affection = Math.Max(0, saveData.cheeseTama?.stats?.affection ?? 0);
            var discoveredRecipes = saveData.fantasyPowder?.discoveredHiddenRecipeIds?.Count ?? 0;

            switch (cardId)
            {
                case HiddenCareerCardCatalog.ScientistId:
                    return discoveredRecipes >= 1
                        && history.cookings >= (IsTrait(traitId, NewGameSetupCatalog.FocusedTraitId)
                            ? 10
                            : 16);

                case HiddenCareerCardCatalog.TeacherId:
                    return CountRegularRecords(saveData.collections)
                            >= (IsTrait(traitId, NewGameSetupCatalog.BalancedTraitId) ? 10 : 14)
                        && history.totalCareActions
                            >= (IsTrait(traitId, NewGameSetupCatalog.BalancedTraitId) ? 50 : 75);

                case HiddenCareerCardCatalog.DoctorId:
                    return affection >= 75
                        && SafeSum(history.cleanings, history.rests)
                            >= (IsTrait(traitId, NewGameSetupCatalog.CalmTraitId) ? 16 : 24);

                case HiddenCareerCardCatalog.ExplorerId:
                    return affection >= 70
                        && SafeSum(history.playSessions, CountEventOccurrences(saveData))
                            >= (IsTrait(traitId, NewGameSetupCatalog.LivelyTraitId) ? 20 : 30);

                case HiddenCareerCardCatalog.GuardianId:
                    return affection >= 90
                        && history.petSessions >= 12
                        && history.totalCareActions
                            >= (IsTrait(traitId, NewGameSetupCatalog.ExpressiveTraitId) ? 85 : 120);

                case HiddenCareerCardCatalog.RiftArchitectId:
                    return saveData.fantasyPowder != null
                        && saveData.fantasyPowder.attemptCount >= 21
                        && discoveredRecipes >= 3;

                case HiddenCareerCardCatalog.BlackStarObserverId:
                    return affection >= 95
                        && CountOtherKnownCareerCards(saveData.collections) >= 6;

                default:
                    return false;
            }
        }

        private static bool IsTrait(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.Ordinal);
        }

        private static int CountRegularRecords(CollectionSaveData collections)
        {
            if (collections == null)
            {
                return 0;
            }

            return SafeSum(
                collections.milk?.Count ?? 0,
                collections.evolution?.Count ?? 0,
                collections.events?.Count ?? 0);
        }

        private static int CountEventOccurrences(CheeseTamaSaveData saveData)
        {
            var entries = saveData.randomEvents?.history;
            if (entries == null)
            {
                return 0;
            }

            var total = 0L;
            for (var index = 0; index < entries.Count; index += 1)
            {
                total += Math.Max(0, entries[index]?.totalOccurrences ?? 0);
                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)total;
        }

        private static int CountOtherKnownCareerCards(CollectionSaveData collections)
        {
            if (collections?.hiddenUnlockedOnly == null)
            {
                return 0;
            }

            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < collections.hiddenUnlockedOnly.Count; index += 1)
            {
                var id = collections.hiddenUnlockedOnly[index]?.id;
                if (string.IsNullOrWhiteSpace(id)
                    || string.Equals(
                        id,
                        HiddenCareerCardCatalog.BlackStarObserverId,
                        StringComparison.Ordinal)
                    || HiddenCareerCardCatalog.Find(id) == null)
                {
                    continue;
                }

                known.Add(id);
            }

            return known.Count;
        }

        private static HiddenCollectionSaveEntry FindUnlockedEntry(
            CollectionSaveData collections,
            string cardId)
        {
            if (collections?.hiddenUnlockedOnly == null || string.IsNullOrWhiteSpace(cardId))
            {
                return null;
            }

            for (var index = 0; index < collections.hiddenUnlockedOnly.Count; index += 1)
            {
                var entry = collections.hiddenUnlockedOnly[index];
                if (entry != null && string.Equals(entry.id, cardId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static int SafeSum(params int[] values)
        {
            var sum = 0L;
            if (values != null)
            {
                for (var index = 0; index < values.Length; index += 1)
                {
                    sum += Math.Max(0, values[index]);
                    if (sum >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }
            }

            return (int)sum;
        }
    }
}
