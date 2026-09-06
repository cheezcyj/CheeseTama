using System;
using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;

namespace CheeseTama.Collections.HiddenCareers
{
    /// <summary>
    /// Runtime-only aggregate derived from already-unlocked known cards. It is
    /// intentionally not serialized, so existing saves remain authoritative.
    /// </summary>
    public readonly struct HiddenCareerBenefitSet
    {
        internal HiddenCareerBenefitSet(
            int recipeHintProgress,
            int collectionInterpretation,
            int recoveryEffectPercent,
            int randomEventWeightPercent,
            int negativeEffectMitigationPercent,
            int rareByproductWeightPercent,
            int deepLoreSignal)
        {
            RecipeHintProgress = Math.Max(0, recipeHintProgress);
            CollectionInterpretation = Math.Max(0, collectionInterpretation);
            RecoveryEffectPercent = Math.Max(0, recoveryEffectPercent);
            RandomEventWeightPercent = Math.Max(0, randomEventWeightPercent);
            NegativeEffectMitigationPercent = Math.Max(0, negativeEffectMitigationPercent);
            RareByproductWeightPercent = Math.Max(0, rareByproductWeightPercent);
            DeepLoreSignal = Math.Max(0, deepLoreSignal);
        }

        public int RecipeHintProgress { get; }
        public int CollectionInterpretation { get; }
        public int RecoveryEffectPercent { get; }
        public int RandomEventWeightPercent { get; }
        public int NegativeEffectMitigationPercent { get; }
        public int RareByproductWeightPercent { get; }
        public int DeepLoreSignal { get; }

        public int GetMagnitude(HiddenCareerBenefitKind kind)
        {
            return kind switch
            {
                HiddenCareerBenefitKind.RecipeHintProgress => RecipeHintProgress,
                HiddenCareerBenefitKind.CollectionInterpretation => CollectionInterpretation,
                HiddenCareerBenefitKind.RecoveryEffectPercent => RecoveryEffectPercent,
                HiddenCareerBenefitKind.RandomEventWeightPercent => RandomEventWeightPercent,
                HiddenCareerBenefitKind.NegativeEffectMitigationPercent => NegativeEffectMitigationPercent,
                HiddenCareerBenefitKind.RareByproductWeightPercent => RareByproductWeightPercent,
                HiddenCareerBenefitKind.DeepLoreSignal => DeepLoreSignal,
                _ => 0
            };
        }

        public string BuildCollectionInterpretation(string recordId)
        {
            if (CollectionInterpretation <= 0 || string.IsNullOrWhiteSpace(recordId))
            {
                return string.Empty;
            }

            if (recordId.Contains("growth") || recordId.Contains("level"))
            {
                return "해석 · 반복된 돌봄의 순서가 이 성장 기록에 겹쳐 보여요.";
            }

            if (recordId.Contains("recipe") || recordId.Contains("blend"))
            {
                return "해석 · 재료보다 함께한 돌봄의 흐름이 이 조합을 완성했어요.";
            }

            return "해석 · 이 기록은 앞뒤의 돌봄을 함께 읽을 때 더 또렷해져요.";
        }

        public string BuildDeepLoreSignal(string recordId)
        {
            if (DeepLoreSignal <= 0 || string.IsNullOrWhiteSpace(recordId))
            {
                return string.Empty;
            }

            return "심층 단서 · 기록의 가장자리에서 검은 별빛이 다음 이야기를 가리켜요.";
        }
    }

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

        public HiddenCareerBenefitSet GetBenefitSet(CollectionSaveData collections)
        {
            var values = new int[Enum.GetValues(typeof(HiddenCareerBenefitKind)).Length];
            var benefits = GetUnlockedBenefits(collections);
            for (var index = 0; index < benefits.Count; index += 1)
            {
                var benefit = benefits[index];
                if (benefit == null)
                {
                    continue;
                }

                var kindIndex = (int)benefit.Kind;
                if (kindIndex < 0 || kindIndex >= values.Length)
                {
                    continue;
                }

                values[kindIndex] = SafeSum(values[kindIndex], benefit.Magnitude);
            }

            return new HiddenCareerBenefitSet(
                values[(int)HiddenCareerBenefitKind.RecipeHintProgress],
                values[(int)HiddenCareerBenefitKind.CollectionInterpretation],
                values[(int)HiddenCareerBenefitKind.RecoveryEffectPercent],
                values[(int)HiddenCareerBenefitKind.RandomEventWeightPercent],
                values[(int)HiddenCareerBenefitKind.NegativeEffectMitigationPercent],
                values[(int)HiddenCareerBenefitKind.RareByproductWeightPercent],
                values[(int)HiddenCareerBenefitKind.DeepLoreSignal]);
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
