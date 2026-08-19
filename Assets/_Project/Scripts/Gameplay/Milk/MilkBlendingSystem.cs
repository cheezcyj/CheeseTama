using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Milk
{
    public enum MilkBlendStatus
    {
        Applied = 0,
        MissingState = 1,
        MissingTargets = 2,
        InvalidReceipt = 3,
        AlreadyApplied = 4,
        UnknownMilk = 5,
        UnknownIngredient = 6,
        MilkLocked = 7,
        NoMatchingRecipe = 8,
        MissingCatalogResult = 9,
        InsufficientCurrency = 10,
        RewardCapacityFull = 11,
        TrackingCapacityFull = 12,
        InvalidRoll = 13,
        MasteryRewardCapacityFull = 14
    }

    public sealed class MilkBlendUsageView
    {
        public MilkBlendUsageView(
            string ingredientId,
            string resultSnackId,
            int blendCount)
        {
            this.ingredientId = Normalize(ingredientId);
            this.resultSnackId = Normalize(resultSnackId);
            this.blendCount = Math.Max(0, blendCount);
        }

        public string ingredientId { get; }
        public string resultSnackId { get; }
        public int blendCount { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MilkBlendingPanelSnapshot
    {
        private readonly HashSet<string> unlockedMilkIds;
        private readonly HashSet<string> discoveredResultIds;
        private readonly HashSet<string> masteryResearchRecordIds;
        private readonly MilkBlendUsageView[] usageEntries;

        public MilkBlendingPanelSnapshot(
            int milkCoins,
            int milkDrops,
            int collectionFragments,
            IEnumerable<string> unlockedMilks,
            IEnumerable<string> discoveredResults,
            IEnumerable<MilkBlendUsageView> usages,
            IEnumerable<string> masteryResearchRecords = null)
        {
            this.milkCoins = Math.Max(0, milkCoins);
            this.milkDrops = Math.Max(0, milkDrops);
            this.collectionFragments = Math.Max(0, collectionFragments);
            unlockedMilkIds = NormalizeIds(unlockedMilks);
            discoveredResultIds = NormalizeIds(discoveredResults);
            masteryResearchRecordIds = NormalizeIds(masteryResearchRecords);
            usageEntries = NormalizeUsages(usages);
        }

        public int milkCoins { get; }
        public int milkDrops { get; }
        public int collectionFragments { get; }
        public IReadOnlyList<MilkBlendUsageView> UsageEntries => usageEntries;

        public static MilkBlendingPanelSnapshot CreateDefault()
        {
            return new MilkBlendingPanelSnapshot(
                0,
                0,
                0,
                new[] { MilkCatalog.BasicMilkId },
                null,
                null);
        }

        public bool IsMilkUnlocked(string milkId)
        {
            return Contains(unlockedMilkIds, milkId);
        }

        public bool IsDiscovered(string resultSnackId)
        {
            return Contains(discoveredResultIds, resultSnackId);
        }

        public int GetBlendCount(string ingredientId, string resultSnackId)
        {
            var normalizedIngredientId = Normalize(ingredientId);
            var normalizedResultId = Normalize(resultSnackId);
            for (var index = 0; index < usageEntries.Length; index += 1)
            {
                var entry = usageEntries[index];
                if (entry != null
                    && string.Equals(
                        entry.ingredientId,
                        normalizedIngredientId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        entry.resultSnackId,
                        normalizedResultId,
                        StringComparison.Ordinal))
                {
                    return entry.blendCount;
                }
            }

            return 0;
        }

        public int GetIngredientBlendCount(string ingredientId)
        {
            var normalizedIngredientId = Normalize(ingredientId);
            var total = 0;
            for (var index = 0; index < usageEntries.Length; index += 1)
            {
                var entry = usageEntries[index];
                if (entry != null
                    && string.Equals(
                        entry.ingredientId,
                        normalizedIngredientId,
                        StringComparison.Ordinal))
                {
                    total = SaturatingAdd(total, entry.blendCount);
                }
            }

            return total;
        }

        public int GetMasteryStage(string ingredientId)
        {
            return MilkBlendingCatalog.GetMasteryStage(
                GetIngredientBlendCount(ingredientId));
        }

        public int GetMasteryResearchRecordCount(string ingredientId)
        {
            var ingredient = MilkBlendingCatalog.FindIngredient(ingredientId);
            if (ingredient == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0;
                index < MilkBlendingCatalog.AllMasteryMilestones.Length;
                index += 1)
            {
                var milestone = MilkBlendingCatalog.AllMasteryMilestones[index];
                var recordId = MilkBlendingCatalog.BuildMasteryResearchRecordId(
                    ingredient.id,
                    milestone.stage);
                if (Contains(masteryResearchRecordIds, recordId))
                {
                    count += 1;
                }
            }

            return count;
        }

        public MilkBlendMasteryResearchRecord GetLatestMasteryResearchRecord(
            string ingredientId)
        {
            var ingredient = MilkBlendingCatalog.FindIngredient(ingredientId);
            if (ingredient == null)
            {
                return null;
            }

            for (var index = MilkBlendingCatalog.AllMasteryMilestones.Length - 1;
                index >= 0;
                index -= 1)
            {
                var milestone = MilkBlendingCatalog.AllMasteryMilestones[index];
                var recordId = MilkBlendingCatalog.BuildMasteryResearchRecordId(
                    ingredient.id,
                    milestone.stage);
                if (Contains(masteryResearchRecordIds, recordId))
                {
                    return MilkBlendingCatalog.CreateMasteryResearchRecord(
                        ingredient,
                        milestone);
                }
            }

            return null;
        }

        public bool CanAfford(MilkBlendRecipeDefinition recipe)
        {
            return recipe != null
                && milkCoins >= recipe.coinCost
                && milkDrops >= recipe.dropCost
                && collectionFragments >= recipe.fragmentCost;
        }

        private static HashSet<string> NormalizeIds(IEnumerable<string> values)
        {
            var normalized = new HashSet<string>(StringComparer.Ordinal);
            if (values == null)
            {
                return normalized;
            }

            foreach (var value in values)
            {
                var normalizedValue = Normalize(value);
                if (!string.IsNullOrEmpty(normalizedValue))
                {
                    normalized.Add(normalizedValue);
                }
            }

            return normalized;
        }

        private static MilkBlendUsageView[] NormalizeUsages(
            IEnumerable<MilkBlendUsageView> values)
        {
            if (values == null)
            {
                return Array.Empty<MilkBlendUsageView>();
            }

            var normalized = new List<MilkBlendUsageView>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (value == null
                    || string.IsNullOrEmpty(value.ingredientId)
                    || string.IsNullOrEmpty(value.resultSnackId)
                    || value.blendCount <= 0)
                {
                    continue;
                }

                var key = value.ingredientId + "\n" + value.resultSnackId;
                if (seen.Add(key))
                {
                    normalized.Add(value);
                }
            }

            return normalized.ToArray();
        }

        private static bool Contains(HashSet<string> values, string requested)
        {
            var normalized = Normalize(requested);
            return !string.IsNullOrEmpty(normalized) && values.Contains(normalized);
        }

        private static int SaturatingAdd(int current, int amount)
        {
            var result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result > int.MaxValue ? int.MaxValue : (int)result;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MilkBlendMasteryRewardResult
    {
        public static readonly MilkBlendMasteryRewardResult None =
            new MilkBlendMasteryRewardResult(
                string.Empty,
                0,
                0,
                0,
                0,
                Array.Empty<string>(),
                string.Empty);

        public MilkBlendMasteryRewardResult(
            string ingredientId,
            int reachedStage,
            int milkCoins,
            int milkDrops,
            int collectionFragments,
            IReadOnlyList<string> researchRecordIds,
            string message)
        {
            this.ingredientId = Normalize(ingredientId);
            this.reachedStage = Math.Max(0, reachedStage);
            this.milkCoins = Math.Max(0, milkCoins);
            this.milkDrops = Math.Max(0, milkDrops);
            this.collectionFragments = Math.Max(0, collectionFragments);
            this.researchRecordIds = CopyIds(researchRecordIds);
            this.message = Normalize(message);
        }

        public string ingredientId { get; }
        public int reachedStage { get; }
        public int milkCoins { get; }
        public int milkDrops { get; }
        public int collectionFragments { get; }
        public IReadOnlyList<string> researchRecordIds { get; }
        public string message { get; }
        public bool granted => researchRecordIds.Count > 0;

        private static IReadOnlyList<string> CopyIds(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copied = new List<string>(values.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < values.Count; index += 1)
            {
                var normalized = Normalize(values[index]);
                if (!string.IsNullOrEmpty(normalized) && seen.Add(normalized))
                {
                    copied.Add(normalized);
                }
            }

            return copied.ToArray();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MilkBlendResult
    {
        public MilkBlendResult(
            MilkBlendStatus status,
            string receiptKey,
            string milkId,
            string ingredientId,
            string resultSnackId,
            string message,
            bool firstDiscovery,
            int resultSnackQuantity,
            int ingredientBlendCount,
            int milkCoinCost,
            int milkDropCost,
            int collectionFragmentCost,
            string preferredIngredientId,
            bool specialResult = false,
            MilkBlendMasteryRewardResult masteryReward = null)
        {
            this.status = status;
            applied = status == MilkBlendStatus.Applied;
            duplicateReceipt = status == MilkBlendStatus.AlreadyApplied;
            this.receiptKey = Normalize(receiptKey);
            this.milkId = Normalize(milkId);
            this.ingredientId = Normalize(ingredientId);
            this.resultSnackId = applied ? Normalize(resultSnackId) : string.Empty;
            this.message = Normalize(message);
            this.firstDiscovery = applied && firstDiscovery;
            this.resultSnackQuantity = applied ? Math.Max(0, resultSnackQuantity) : 0;
            this.ingredientBlendCount = applied ? Math.Max(0, ingredientBlendCount) : 0;
            this.milkCoinCost = applied ? Math.Max(0, milkCoinCost) : 0;
            this.milkDropCost = applied ? Math.Max(0, milkDropCost) : 0;
            this.collectionFragmentCost = applied ? Math.Max(0, collectionFragmentCost) : 0;
            this.preferredIngredientId = applied
                ? Normalize(preferredIngredientId)
                : string.Empty;
            this.specialResult = applied && specialResult;
            this.masteryReward = applied
                ? masteryReward ?? MilkBlendMasteryRewardResult.None
                : MilkBlendMasteryRewardResult.None;
        }

        public MilkBlendStatus status { get; }
        public bool applied { get; }
        public bool duplicateReceipt { get; }
        public string receiptKey { get; }
        public string milkId { get; }
        public string ingredientId { get; }
        public string resultSnackId { get; }
        public string message { get; }
        public bool firstDiscovery { get; }
        public int resultSnackQuantity { get; }
        public int ingredientBlendCount { get; }
        public int milkCoinCost { get; }
        public int milkDropCost { get; }
        public int collectionFragmentCost { get; }
        public string preferredIngredientId { get; }
        public bool specialResult { get; }
        public MilkBlendMasteryRewardResult masteryReward { get; }
        public IReadOnlyList<string> newMasteryResearchRecordIds =>
            masteryReward.researchRecordIds;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MilkBlendingSystem
    {
        public MilkBlendingPanelSnapshot BuildSnapshot(
            MilkBlendingSaveData state,
            EconomySaveData economy,
            Func<string, bool> isMilkUnlocked)
        {
            state?.EnsureRuntimeDefaults();
            var unlockedMilkIds = new List<string>(MilkBlendingCatalog.AllMilkIds.Length);
            for (var index = 0; index < MilkBlendingCatalog.AllMilkIds.Length; index += 1)
            {
                var milkId = MilkBlendingCatalog.AllMilkIds[index];
                if (ResolveMilkUnlocked(milkId, isMilkUnlocked))
                {
                    unlockedMilkIds.Add(milkId);
                }
            }

            var usages = new List<MilkBlendUsageView>();
            if (state?.ingredientUsage != null)
            {
                for (var index = 0; index < state.ingredientUsage.Count; index += 1)
                {
                    var entry = state.ingredientUsage[index];
                    if (entry == null)
                    {
                        continue;
                    }

                    usages.Add(new MilkBlendUsageView(
                        entry.ingredientId,
                        entry.resultSnackId,
                        entry.blendCount));
                }
            }

            return new MilkBlendingPanelSnapshot(
                economy?.milkCoins ?? 0,
                economy?.milkDrops ?? 0,
                economy?.collectionFragments ?? 0,
                unlockedMilkIds,
                state?.discoveredResultIds,
                usages,
                state?.masteryResearchRecordIds);
        }

        public MilkBlendResult TryBlend(
            MilkBlendingSaveData state,
            CheeseTamaModel tama,
            EconomySaveData economy,
            IList<SnackInventorySaveEntry> snackInventory,
            string milkId,
            string ingredientId,
            Func<string, bool> isMilkUnlocked,
            string receiptKey,
            DateTimeOffset blendedAt)
        {
            return TryBlend(
                state,
                tama,
                economy,
                snackInventory,
                milkId,
                ingredientId,
                isMilkUnlocked,
                receiptKey,
                blendedAt,
                1d);
        }

        public MilkBlendResult TryBlend(
            MilkBlendingSaveData state,
            CheeseTamaModel tama,
            EconomySaveData economy,
            IList<SnackInventorySaveEntry> snackInventory,
            string milkId,
            string ingredientId,
            Func<string, bool> isMilkUnlocked,
            string receiptKey,
            DateTimeOffset blendedAt,
            double specialResultRoll)
        {
            var normalizedMilkId = Normalize(milkId);
            var normalizedIngredientId = Normalize(ingredientId);
            var normalizedReceiptKey = Normalize(receiptKey);
            if (state == null)
            {
                return Failure(
                    MilkBlendStatus.MissingState,
                    normalizedReceiptKey,
                    normalizedMilkId,
                    normalizedIngredientId);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceiptKey))
            {
                return Failure(
                    MilkBlendStatus.InvalidReceipt,
                    string.Empty,
                    normalizedMilkId,
                    normalizedIngredientId);
            }

            if (state.HasAppliedReceipt(normalizedReceiptKey))
            {
                return Failure(
                    MilkBlendStatus.AlreadyApplied,
                    normalizedReceiptKey,
                    normalizedMilkId,
                    normalizedIngredientId);
            }

            var milk = MilkCatalog.Find(normalizedMilkId);
            if (milk == null)
            {
                return Failure(
                    MilkBlendStatus.UnknownMilk,
                    normalizedReceiptKey,
                    normalizedMilkId,
                    normalizedIngredientId);
            }

            var ingredient = MilkBlendingCatalog.FindIngredient(normalizedIngredientId);
            if (ingredient == null)
            {
                return Failure(
                    MilkBlendStatus.UnknownIngredient,
                    normalizedReceiptKey,
                    normalizedMilkId,
                    normalizedIngredientId);
            }

            if (!ResolveMilkUnlocked(milk.id, isMilkUnlocked))
            {
                return Failure(
                    MilkBlendStatus.MilkLocked,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            var recipe = MilkBlendingCatalog.FindRecipe(milk.id, ingredient.id);
            if (recipe == null)
            {
                return Failure(
                    MilkBlendStatus.NoMatchingRecipe,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            var regularResultSnack = recipe.ResultSnack;
            var specialResultSnack = recipe.HasSpecialResult
                ? recipe.SpecialResultSnack
                : null;
            if (regularResultSnack == null
                || (recipe.HasSpecialResult && specialResultSnack == null))
            {
                return Failure(
                    MilkBlendStatus.MissingCatalogResult,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            if (!IsValidRoll(specialResultRoll))
            {
                return Failure(
                    MilkBlendStatus.InvalidRoll,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            var specialResult = recipe.IsSpecialResultRoll(specialResultRoll);
            var resultSnack = specialResult
                ? specialResultSnack
                : regularResultSnack;

            if (tama == null || economy == null || snackInventory == null)
            {
                return Failure(
                    MilkBlendStatus.MissingTargets,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            if (!HasEnoughCurrency(economy, recipe))
            {
                return Failure(
                    MilkBlendStatus.InsufficientCurrency,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            if (!CanAddSnack(snackInventory, resultSnack.id))
            {
                return Failure(
                    MilkBlendStatus.RewardCapacityFull,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            if (!state.CanRecordBlend(ingredient.id, resultSnack.id))
            {
                return Failure(
                    MilkBlendStatus.TrackingCapacityFull,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            var projectedIngredientBlendCount = SaturatingAdd(
                state.GetIngredientBlendCount(ingredient.id),
                1);
            var masteryReward = BuildPendingMasteryReward(
                state,
                ingredient,
                projectedIngredientBlendCount);
            if (!state.CanAddMasteryResearchRecords(masteryReward.researchRecordIds))
            {
                return Failure(
                    MilkBlendStatus.TrackingCapacityFull,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            if (!CanGrantMasteryRewardAfterSpend(economy, recipe, masteryReward))
            {
                return Failure(
                    MilkBlendStatus.MasteryRewardCapacityFull,
                    normalizedReceiptKey,
                    milk.id,
                    ingredient.id);
            }

            // Validation is complete. From here onward every mutation belongs to this receipt.
            var firstDiscovery = !state.HasDiscovered(resultSnack.id);
            SpendCurrency(economy, recipe);
            var grantedQuantity = AddSnack(snackInventory, resultSnack.id);
            state.RecordBlend(ingredient.id, resultSnack.id, blendedAt);
            AddMasteryResearchRecords(state, masteryReward.researchRecordIds);
            GrantMasteryReward(economy, masteryReward);
            state.AddAppliedReceipt(normalizedReceiptKey);
            tama.EnsureRuntimeDefaults();
            var ingredientBlendCount = state.GetIngredientBlendCount(ingredient.id);
            var preferredIngredientId = ReconcileMostUsedIngredient(state, tama);
            var message = specialResult
                ? firstDiscovery
                    ? $"특별한 음식 발견! {resultSnack.displayName} 1개를 보관했습니다."
                    : $"특별한 음식이 완성됐어요! {resultSnack.displayName} 1개를 보관했습니다."
                : firstDiscovery
                    ? $"새 조합을 발견했어요! {resultSnack.displayName} 1개를 보관했습니다."
                    : $"{resultSnack.displayName} 1개를 만들었습니다. 이 재료는 {ingredientBlendCount}회 사용했어요.";
            if (masteryReward.granted)
            {
                message += "\n" + masteryReward.message;
            }

            return new MilkBlendResult(
                MilkBlendStatus.Applied,
                normalizedReceiptKey,
                milk.id,
                ingredient.id,
                resultSnack.id,
                message,
                firstDiscovery,
                grantedQuantity,
                ingredientBlendCount,
                recipe.coinCost,
                recipe.dropCost,
                recipe.fragmentCost,
                preferredIngredientId,
                specialResult,
                masteryReward);
        }

        public string ReconcileMostUsedIngredient(
            MilkBlendingSaveData state,
            CheeseTamaModel tama)
        {
            if (state == null || tama == null)
            {
                return string.Empty;
            }

            state.EnsureRuntimeDefaults();
            tama.EnsureRuntimeDefaults();
            var maximumCount = 0;
            for (var index = 0; index < MilkBlendingCatalog.AllRecipes.Length; index += 1)
            {
                var recipe = MilkBlendingCatalog.AllRecipes[index];
                maximumCount = Math.Max(
                    maximumCount,
                    state.GetIngredientBlendCount(recipe.ingredientId));
            }

            if (maximumCount <= 0)
            {
                return Normalize(tama.growthHistory.mostUsedIngredientId);
            }

            var currentPreference = Normalize(tama.growthHistory.mostUsedIngredientId);
            var currentRecipe = MilkBlendingCatalog.FindByResult(currentPreference);
            if (currentRecipe != null
                && state.GetIngredientBlendCount(currentRecipe.ingredientId) == maximumCount)
            {
                return currentPreference;
            }

            for (var index = 0; index < MilkBlendingCatalog.AllRecipes.Length; index += 1)
            {
                var recipe = MilkBlendingCatalog.AllRecipes[index];
                if (state.GetIngredientBlendCount(recipe.ingredientId) != maximumCount)
                {
                    continue;
                }

                tama.growthHistory.mostUsedIngredientId = recipe.resultSnackId;
                return recipe.resultSnackId;
            }

            return currentPreference;
        }

        private static MilkBlendResult Failure(
            MilkBlendStatus status,
            string receiptKey,
            string milkId,
            string ingredientId)
        {
            return new MilkBlendResult(
                status,
                receiptKey,
                milkId,
                ingredientId,
                string.Empty,
                GetFailureMessage(status),
                firstDiscovery: false,
                resultSnackQuantity: 0,
                ingredientBlendCount: 0,
                milkCoinCost: 0,
                milkDropCost: 0,
                collectionFragmentCost: 0,
                preferredIngredientId: string.Empty);
        }

        private static string GetFailureMessage(MilkBlendStatus status)
        {
            switch (status)
            {
                case MilkBlendStatus.MissingState:
                case MilkBlendStatus.MissingTargets:
                    return "블렌딩 기능이 아직 저장 데이터와 연결되지 않았습니다.";
                case MilkBlendStatus.InvalidReceipt:
                    return "블렌딩 요청을 확인할 수 없습니다.";
                case MilkBlendStatus.AlreadyApplied:
                    return "이미 처리한 블렌딩 요청입니다.";
                case MilkBlendStatus.UnknownMilk:
                    return "알 수 없는 우유입니다.";
                case MilkBlendStatus.UnknownIngredient:
                    return "알 수 없는 재료입니다.";
                case MilkBlendStatus.MilkLocked:
                    return "아직 사용할 수 없는 우유입니다.";
                case MilkBlendStatus.NoMatchingRecipe:
                    return "두 재료가 잘 어울리지 않았어요. 재화는 소비되지 않았습니다.";
                case MilkBlendStatus.MissingCatalogResult:
                    return "완성 결과를 찾지 못했습니다.";
                case MilkBlendStatus.InsufficientCurrency:
                    return "조합에 필요한 재화가 부족합니다.";
                case MilkBlendStatus.RewardCapacityFull:
                    return "간식 보관함이 가득 차 결과를 담을 수 없습니다.";
                case MilkBlendStatus.TrackingCapacityFull:
                    return "새 조합 기록을 더 저장할 수 없습니다.";
                case MilkBlendStatus.InvalidRoll:
                    return "블렌딩 확률 값을 확인할 수 없습니다.";
                case MilkBlendStatus.MasteryRewardCapacityFull:
                    return "숙련 보상을 담을 공간이 부족해 블렌딩을 진행하지 않았습니다.";
                default:
                    return string.Empty;
            }
        }

        private static MilkBlendMasteryRewardResult BuildPendingMasteryReward(
            MilkBlendingSaveData state,
            MilkBlendIngredientDefinition ingredient,
            int projectedIngredientBlendCount)
        {
            if (state == null || ingredient == null)
            {
                return MilkBlendMasteryRewardResult.None;
            }

            var recordIds = new List<string>();
            var recordTitles = new List<string>();
            var reachedStage = 0;
            var milkCoins = 0;
            var milkDrops = 0;
            var collectionFragments = 0;
            for (var index = 0;
                index < MilkBlendingCatalog.AllMasteryMilestones.Length;
                index += 1)
            {
                var milestone = MilkBlendingCatalog.AllMasteryMilestones[index];
                if (milestone == null
                    || projectedIngredientBlendCount < milestone.requiredUseCount)
                {
                    continue;
                }

                var record = MilkBlendingCatalog.CreateMasteryResearchRecord(
                    ingredient,
                    milestone);
                if (record == null || state.HasMasteryResearchRecord(record.recordId))
                {
                    continue;
                }

                recordIds.Add(record.recordId);
                recordTitles.Add(milestone.title);
                reachedStage = Math.Max(reachedStage, milestone.stage);
                milkCoins = SaturatingAdd(milkCoins, milestone.milkCoinReward);
                milkDrops = SaturatingAdd(milkDrops, milestone.milkDropReward);
                collectionFragments = SaturatingAdd(
                    collectionFragments,
                    milestone.collectionFragmentReward);
            }

            if (recordIds.Count == 0)
            {
                return MilkBlendMasteryRewardResult.None;
            }

            var rewardParts = new List<string>(3);
            AddRewardPart(rewardParts, "코인", milkCoins);
            AddRewardPart(rewardParts, "우유방울", milkDrops);
            AddRewardPart(rewardParts, "도감조각", collectionFragments);
            var rewardText = rewardParts.Count > 0
                ? " · " + string.Join(", ", rewardParts)
                : string.Empty;
            var message = $"{ingredient.displayName} 숙련 Lv.{reachedStage}: "
                + string.Join(" · ", recordTitles)
                + $" 연구 기록을 열었어요{rewardText}.";
            return new MilkBlendMasteryRewardResult(
                ingredient.id,
                reachedStage,
                milkCoins,
                milkDrops,
                collectionFragments,
                recordIds,
                message);
        }

        private static void AddRewardPart(List<string> parts, string label, int amount)
        {
            if (parts != null && amount > 0)
            {
                parts.Add($"{label} +{amount}");
            }
        }

        private static bool CanGrantMasteryRewardAfterSpend(
            EconomySaveData economy,
            MilkBlendRecipeDefinition recipe,
            MilkBlendMasteryRewardResult reward)
        {
            if (economy == null || recipe == null || reward == null)
            {
                return false;
            }

            if (!reward.granted)
            {
                return true;
            }

            return CanAddCurrency(
                    Math.Max(0, economy.milkCoins) - recipe.coinCost,
                    reward.milkCoins)
                && CanAddCurrency(
                    Math.Max(0, economy.milkDrops) - recipe.dropCost,
                    reward.milkDrops)
                && CanAddCurrency(
                    Math.Max(0, economy.collectionFragments) - recipe.fragmentCost,
                    reward.collectionFragments);
        }

        private static bool CanAddCurrency(int current, int amount)
        {
            return Math.Max(0, amount) <= int.MaxValue - Math.Max(0, current);
        }

        private static void AddMasteryResearchRecords(
            MilkBlendingSaveData state,
            IReadOnlyList<string> recordIds)
        {
            if (state == null || recordIds == null)
            {
                return;
            }

            for (var index = 0; index < recordIds.Count; index += 1)
            {
                state.AddMasteryResearchRecord(recordIds[index]);
            }
        }

        private static void GrantMasteryReward(
            EconomySaveData economy,
            MilkBlendMasteryRewardResult reward)
        {
            if (economy == null || reward == null || !reward.granted)
            {
                return;
            }

            economy.milkCoins += reward.milkCoins;
            economy.milkDrops += reward.milkDrops;
            economy.collectionFragments += reward.collectionFragments;
        }

        private static bool ResolveMilkUnlocked(
            string milkId,
            Func<string, bool> isMilkUnlocked)
        {
            if (string.IsNullOrWhiteSpace(milkId))
            {
                return false;
            }

            if (isMilkUnlocked != null)
            {
                return isMilkUnlocked(milkId);
            }

            return string.Equals(milkId, MilkCatalog.BasicMilkId, StringComparison.Ordinal);
        }

        private static bool IsValidRoll(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d
                && value <= 1d;
        }

        private static bool HasEnoughCurrency(
            EconomySaveData economy,
            MilkBlendRecipeDefinition recipe)
        {
            return economy != null
                && recipe != null
                && Math.Max(0, economy.milkCoins) >= recipe.coinCost
                && Math.Max(0, economy.milkDrops) >= recipe.dropCost
                && Math.Max(0, economy.collectionFragments) >= recipe.fragmentCost;
        }

        private static void SpendCurrency(
            EconomySaveData economy,
            MilkBlendRecipeDefinition recipe)
        {
            economy.milkCoins = Math.Max(0, economy.milkCoins) - recipe.coinCost;
            economy.milkDrops = Math.Max(0, economy.milkDrops) - recipe.dropCost;
            economy.collectionFragments = Math.Max(0, economy.collectionFragments)
                - recipe.fragmentCost;
        }

        private static bool CanAddSnack(
            IList<SnackInventorySaveEntry> inventory,
            string snackId)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(snackId))
            {
                return false;
            }

            for (var index = 0; index < inventory.Count; index += 1)
            {
                var entry = inventory[index];
                if (entry != null
                    && string.Equals(entry.snackId, snackId, StringComparison.Ordinal))
                {
                    return Math.Max(0, entry.quantity) < int.MaxValue;
                }
            }

            return !inventory.IsReadOnly;
        }

        private static int AddSnack(
            IList<SnackInventorySaveEntry> inventory,
            string snackId)
        {
            for (var index = 0; index < inventory.Count; index += 1)
            {
                var entry = inventory[index];
                if (entry == null
                    || !string.Equals(entry.snackId, snackId, StringComparison.Ordinal))
                {
                    continue;
                }

                entry.quantity = Math.Max(0, entry.quantity) + 1;
                return 1;
            }

            inventory.Add(new SnackInventorySaveEntry
            {
                snackId = snackId,
                quantity = 1
            });
            return 1;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            var result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result > int.MaxValue ? int.MaxValue : (int)result;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
