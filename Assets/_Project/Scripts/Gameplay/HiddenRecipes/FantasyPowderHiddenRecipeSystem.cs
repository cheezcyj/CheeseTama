using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.HiddenRecipes
{
    public enum FantasyPowderAttemptStatus
    {
        AppliedSuccess = 0,
        AppliedByproduct = 1,
        Locked = 2,
        MissingState = 3,
        MissingTargets = 4,
        InvalidReceipt = 5,
        AlreadyApplied = 6,
        UnknownRecipe = 7,
        InvalidRoll = 8,
        InsufficientPowder = 9,
        RewardCapacityFull = 10
    }

    public sealed class FantasyPowderRecipeView
    {
        public FantasyPowderRecipeView(
            string recipeId,
            string displayName,
            string description,
            bool discovered)
        {
            this.recipeId = Normalize(recipeId);
            this.displayName = Normalize(displayName);
            this.description = Normalize(description);
            this.discovered = discovered;
        }

        public string recipeId { get; }
        public string displayName { get; }
        public string description { get; }
        public bool discovered { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class FantasyPowderPanelSnapshot
    {
        private readonly FantasyPowderRecipeView[] recipeEntries;

        public FantasyPowderPanelSnapshot(
            bool visible,
            int powderQuantity,
            int attemptCount,
            int pityHintLevel,
            string hintText,
            IEnumerable<FantasyPowderRecipeView> recipes)
        {
            this.visible = visible;
            if (!visible)
            {
                this.powderQuantity = 0;
                this.attemptCount = 0;
                this.pityHintLevel = 0;
                this.hintText = string.Empty;
                recipeEntries = Array.Empty<FantasyPowderRecipeView>();
                return;
            }

            this.powderQuantity = Math.Max(0, powderQuantity);
            this.attemptCount = Math.Max(0, attemptCount);
            this.pityHintLevel = Math.Max(
                0,
                Math.Min(FantasyPowderSaveData.MaximumPityHintLevel, pityHintLevel));
            this.hintText = string.IsNullOrWhiteSpace(hintText)
                ? string.Empty
                : hintText.Trim();
            recipeEntries = NormalizeRecipes(recipes);
        }

        public bool visible { get; }
        public int powderQuantity { get; }
        public int attemptCount { get; }
        public int pityHintLevel { get; }
        public string hintText { get; }
        public IReadOnlyList<FantasyPowderRecipeView> RecipeEntries => recipeEntries;
        public bool canAttempt => visible && powderQuantity > 0 && recipeEntries.Length > 0;

        public static FantasyPowderPanelSnapshot CreateHidden()
        {
            return new FantasyPowderPanelSnapshot(false, 0, 0, 0, string.Empty, null);
        }

        public FantasyPowderRecipeView FindRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return null;
            }

            for (var index = 0; index < recipeEntries.Length; index += 1)
            {
                var entry = recipeEntries[index];
                if (entry != null
                    && string.Equals(entry.recipeId, recipeId.Trim(), StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static FantasyPowderRecipeView[] NormalizeRecipes(
            IEnumerable<FantasyPowderRecipeView> recipes)
        {
            if (recipes == null)
            {
                return Array.Empty<FantasyPowderRecipeView>();
            }

            var normalized = new List<FantasyPowderRecipeView>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var recipe in recipes)
            {
                if (recipe == null
                    || string.IsNullOrWhiteSpace(recipe.recipeId)
                    || !seen.Add(recipe.recipeId))
                {
                    continue;
                }

                normalized.Add(recipe);
            }

            return normalized.ToArray();
        }
    }

    public sealed class FantasyPowderAttemptResult
    {
        public FantasyPowderAttemptResult(
            FantasyPowderAttemptStatus status,
            string receiptKey,
            string recipeId,
            string recipeTitle,
            string message,
            int powderBefore,
            int powderAfter,
            int attemptCountAfter,
            int pityHintLevel,
            bool newDiscovery,
            string rewardSnackId,
            int rewardSnackQuantity,
            int milkCoinDelta,
            int milkDropDelta,
            int starDropDelta,
            int collectionFragmentDelta)
        {
            this.status = status;
            applied = status == FantasyPowderAttemptStatus.AppliedSuccess
                || status == FantasyPowderAttemptStatus.AppliedByproduct;
            success = status == FantasyPowderAttemptStatus.AppliedSuccess;
            byproductGranted = status == FantasyPowderAttemptStatus.AppliedByproduct;
            duplicateReceipt = status == FantasyPowderAttemptStatus.AlreadyApplied;

            var locked = status == FantasyPowderAttemptStatus.Locked;
            this.receiptKey = locked ? string.Empty : Normalize(receiptKey);
            this.recipeId = locked ? string.Empty : Normalize(recipeId);
            this.recipeTitle = locked ? string.Empty : Normalize(recipeTitle);
            this.message = locked ? string.Empty : Normalize(message);

            this.powderBefore = Math.Max(0, powderBefore);
            this.powderAfter = applied
                ? Math.Max(0, Math.Min(this.powderBefore, powderAfter))
                : this.powderBefore;
            this.attemptCountAfter = Math.Max(0, attemptCountAfter);
            this.pityHintLevel = Math.Max(
                0,
                Math.Min(FantasyPowderSaveData.MaximumPityHintLevel, pityHintLevel));
            this.newDiscovery = success && newDiscovery;
            this.rewardSnackId = applied ? Normalize(rewardSnackId) : string.Empty;
            this.rewardSnackQuantity = applied ? Math.Max(0, rewardSnackQuantity) : 0;
            this.milkCoinDelta = applied ? Math.Max(0, milkCoinDelta) : 0;
            this.milkDropDelta = applied ? Math.Max(0, milkDropDelta) : 0;
            this.starDropDelta = applied ? Math.Max(0, starDropDelta) : 0;
            this.collectionFragmentDelta = applied
                ? Math.Max(0, collectionFragmentDelta)
                : 0;
        }

        public FantasyPowderAttemptStatus status { get; }
        public bool applied { get; }
        public bool success { get; }
        public bool byproductGranted { get; }
        public bool duplicateReceipt { get; }
        public string receiptKey { get; }
        public string recipeId { get; }
        public string recipeTitle { get; }
        public string message { get; }
        public int powderBefore { get; }
        public int powderAfter { get; }
        public int attemptCountAfter { get; }
        public int pityHintLevel { get; }
        public bool newDiscovery { get; }
        public string rewardSnackId { get; }
        public int rewardSnackQuantity { get; }
        public int milkCoinDelta { get; }
        public int milkDropDelta { get; }
        public int starDropDelta { get; }
        public int collectionFragmentDelta { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class FantasyPowderHiddenRecipeSystem
    {
        public const double SuccessChance = 0.07d;
        public const int PowderCostPerAttempt = 1;

        private const int HintLevelOneAttemptCount = 3;
        private const int HintLevelTwoAttemptCount = 7;
        private const int HintLevelThreeAttemptCount = 12;

        public bool IsFeatureUnlocked(UnlockSaveData unlocks)
        {
            return unlocks != null
                && unlocks.fantasyPowderEnabled
                && unlocks.starMilkUnlocked;
        }

        public FantasyPowderPanelSnapshot BuildSnapshot(
            UnlockSaveData unlocks,
            FantasyPowderSaveData state,
            int recipeHintProgress = 0)
        {
            if (!IsFeatureUnlocked(unlocks) || state == null)
            {
                return FantasyPowderPanelSnapshot.CreateHidden();
            }

            state.EnsureRuntimeDefaults();
            var hintLevel = Math.Min(
                FantasyPowderSaveData.MaximumPityHintLevel,
                Math.Max(
                    state.pityHintLevel,
                    CalculatePityHintLevel(state.attemptCount))
                    + Math.Max(0, Math.Min(
                        FantasyPowderSaveData.MaximumPityHintLevel,
                        recipeHintProgress)));
            var recipes = new FantasyPowderRecipeView[
                FantasyPowderHiddenRecipeCatalog.All.Length];
            for (var index = 0; index < recipes.Length; index += 1)
            {
                var definition = FantasyPowderHiddenRecipeCatalog.All[index];
                var discovered = state.HasDiscovered(definition.id);
                recipes[index] = new FantasyPowderRecipeView(
                    definition.id,
                    discovered ? definition.displayName : $"미지의 조합 {index + 1}",
                    discovered
                        ? definition.description
                        : "아직 형태가 드러나지 않은 조합입니다.",
                    discovered);
            }

            return new FantasyPowderPanelSnapshot(
                true,
                state.powderQuantity,
                state.attemptCount,
                hintLevel,
                GetPityHintText(hintLevel),
                recipes);
        }

        public int GrantPowder(FantasyPowderSaveData state, int requestedAmount)
        {
            if (state == null || requestedAmount <= 0)
            {
                return 0;
            }

            state.EnsureRuntimeDefaults();
            var granted = Math.Min(requestedAmount, int.MaxValue - state.powderQuantity);
            state.powderQuantity += granted;
            return granted;
        }

        public FantasyPowderAttemptResult TryAttempt(
            UnlockSaveData unlocks,
            FantasyPowderSaveData state,
            IList<SnackInventorySaveEntry> snackInventory,
            EconomySaveData economy,
            string recipeId,
            string receiptKey,
            double successRoll,
            int rareByproductWeightPercent = 0)
        {
            if (!IsFeatureUnlocked(unlocks))
            {
                return Failure(FantasyPowderAttemptStatus.Locked);
            }

            if (state == null)
            {
                return Failure(FantasyPowderAttemptStatus.MissingState);
            }

            state.EnsureRuntimeDefaults();
            var normalizedReceiptKey = Normalize(receiptKey);
            if (string.IsNullOrEmpty(normalizedReceiptKey))
            {
                return Failure(
                    FantasyPowderAttemptStatus.InvalidReceipt,
                    state: state);
            }

            if (state.HasAppliedReceipt(normalizedReceiptKey))
            {
                return Failure(
                    FantasyPowderAttemptStatus.AlreadyApplied,
                    receiptKey: normalizedReceiptKey,
                    state: state);
            }

            var definition = FantasyPowderHiddenRecipeCatalog.Find(recipeId);
            if (definition == null)
            {
                return Failure(
                    FantasyPowderAttemptStatus.UnknownRecipe,
                    receiptKey: normalizedReceiptKey,
                    state: state);
            }

            if (double.IsNaN(successRoll)
                || double.IsInfinity(successRoll)
                || successRoll < 0d
                || successRoll > 1d)
            {
                return Failure(
                    FantasyPowderAttemptStatus.InvalidRoll,
                    normalizedReceiptKey,
                    definition.id,
                    state);
            }

            if (snackInventory == null || economy == null)
            {
                return Failure(
                    FantasyPowderAttemptStatus.MissingTargets,
                    normalizedReceiptKey,
                    definition.id,
                    state);
            }

            if (state.powderQuantity < PowderCostPerAttempt)
            {
                return Failure(
                    FantasyPowderAttemptStatus.InsufficientPowder,
                    normalizedReceiptKey,
                    definition.id,
                    state);
            }

            var success = successRoll < CalculateSuccessChance(
                rareByproductWeightPercent);
            var wasDiscovered = state.HasDiscovered(definition.id);
            var canDiscover = !wasDiscovered
                && state.discoveredHiddenRecipeIds.Count
                    < FantasyPowderSaveData.MaximumDiscoveredRecipeIds;
            var rewardSnackId = success
                ? definition.resultSnackId
                : definition.byproductSnackId;
            var rewardSnackQuantity = success
                ? definition.resultSnackQuantity
                : definition.byproductSnackQuantity;
            var currencyReward = success
                ? definition.successStarDrops
                : definition.byproductMilkDrops;
            var hasRewardCapacity = CanAddSnack(
                    snackInventory,
                    rewardSnackId,
                    rewardSnackQuantity)
                || (success
                    ? CanAddCurrency(economy.starDrops, currencyReward)
                    : CanAddCurrency(economy.milkDrops, currencyReward));
            if (!hasRewardCapacity && !canDiscover)
            {
                return Failure(
                    FantasyPowderAttemptStatus.RewardCapacityFull,
                    normalizedReceiptKey,
                    definition.id,
                    state);
            }

            // All validation is complete before any attempt-owned state changes.
            var powderBefore = state.powderQuantity;
            state.powderQuantity -= PowderCostPerAttempt;
            state.attemptCount = SaturatingAdd(state.attemptCount, 1);
            state.pityHintLevel = Math.Max(
                state.pityHintLevel,
                CalculatePityHintLevel(state.attemptCount));

            var newDiscovery = success && state.AddDiscoveredRecipe(definition.id);
            var snackQuantityGranted = AddSnack(
                snackInventory,
                rewardSnackId,
                rewardSnackQuantity);
            var milkDropDelta = 0;
            var starDropDelta = 0;
            if (success)
            {
                starDropDelta = AddCurrency(ref economy.starDrops, currencyReward);
            }
            else
            {
                milkDropDelta = AddCurrency(ref economy.milkDrops, currencyReward);
            }

            state.AddAppliedReceipt(normalizedReceiptKey);
            var rewardSnack = SnackCatalog.Find(rewardSnackId);
            var rewardSnackName = rewardSnack?.displayName ?? "간식";
            var resultMessage = success
                ? $"{definition.displayName} 조리법을 발견했습니다. {rewardSnackName}을 보관했어요."
                : BuildByproductMessage(
                    rewardSnackName,
                    snackQuantityGranted,
                    milkDropDelta);

            return new FantasyPowderAttemptResult(
                success
                    ? FantasyPowderAttemptStatus.AppliedSuccess
                    : FantasyPowderAttemptStatus.AppliedByproduct,
                normalizedReceiptKey,
                definition.id,
                success ? definition.displayName : "조합의 잔향",
                resultMessage,
                powderBefore,
                state.powderQuantity,
                state.attemptCount,
                state.pityHintLevel,
                newDiscovery,
                snackQuantityGranted > 0 ? rewardSnackId : string.Empty,
                snackQuantityGranted,
                milkCoinDelta: 0,
                milkDropDelta: milkDropDelta,
                starDropDelta: starDropDelta,
                collectionFragmentDelta: 0);
        }

        public static int CalculatePityHintLevel(int attemptCount)
        {
            var safeAttemptCount = Math.Max(0, attemptCount);
            if (safeAttemptCount >= HintLevelThreeAttemptCount)
            {
                return FantasyPowderSaveData.MaximumPityHintLevel;
            }

            if (safeAttemptCount >= HintLevelTwoAttemptCount)
            {
                return 2;
            }

            return safeAttemptCount >= HintLevelOneAttemptCount ? 1 : 0;
        }

        public static double CalculateSuccessChance(int rareByproductWeightPercent)
        {
            var bonusPercentagePoints = Math.Max(
                0,
                Math.Min(100, rareByproductWeightPercent));
            return Math.Min(1d, SuccessChance + (bonusPercentagePoints / 100d));
        }

        public static string GetPityHintText(int hintLevel)
        {
            switch (Math.Max(
                0,
                Math.Min(FantasyPowderSaveData.MaximumPityHintLevel, hintLevel)))
            {
                case 1:
                    return "남은 잔향이 우유의 온도에 반응하는 것 같아요.";
                case 2:
                    return "천천히 섞을수록 서로 다른 결이 잠깐 모습을 드러내요.";
                case 3:
                    return "세 조합은 각각 말랑함, 숙성 향, 차분한 밤빛을 품고 있어요.";
                default:
                    return string.Empty;
            }
        }

        private static FantasyPowderAttemptResult Failure(
            FantasyPowderAttemptStatus status,
            string receiptKey = "",
            string recipeId = "",
            FantasyPowderSaveData state = null)
        {
            var powder = Math.Max(0, state?.powderQuantity ?? 0);
            return new FantasyPowderAttemptResult(
                status,
                receiptKey,
                recipeId,
                string.Empty,
                string.Empty,
                powder,
                powder,
                Math.Max(0, state?.attemptCount ?? 0),
                Math.Max(0, state?.pityHintLevel ?? 0),
                newDiscovery: false,
                rewardSnackId: string.Empty,
                rewardSnackQuantity: 0,
                milkCoinDelta: 0,
                milkDropDelta: 0,
                starDropDelta: 0,
                collectionFragmentDelta: 0);
        }

        private static bool CanAddSnack(
            IList<SnackInventorySaveEntry> inventory,
            string snackId,
            int requestedQuantity)
        {
            if (inventory == null
                || string.IsNullOrWhiteSpace(snackId)
                || requestedQuantity <= 0)
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
            string snackId,
            int requestedQuantity)
        {
            if (!CanAddSnack(inventory, snackId, requestedQuantity))
            {
                return 0;
            }

            for (var index = 0; index < inventory.Count; index += 1)
            {
                var entry = inventory[index];
                if (entry == null
                    || !string.Equals(entry.snackId, snackId, StringComparison.Ordinal))
                {
                    continue;
                }

                var current = Math.Max(0, entry.quantity);
                var granted = Math.Min(requestedQuantity, int.MaxValue - current);
                entry.quantity = current + granted;
                return granted;
            }

            inventory.Add(new SnackInventorySaveEntry
            {
                snackId = snackId,
                quantity = requestedQuantity
            });
            return requestedQuantity;
        }

        private static bool CanAddCurrency(int current, int requestedAmount)
        {
            return requestedAmount > 0 && Math.Max(0, current) < int.MaxValue;
        }

        private static int AddCurrency(ref int current, int requestedAmount)
        {
            if (requestedAmount <= 0)
            {
                return 0;
            }

            var safeCurrent = Math.Max(0, current);
            var granted = Math.Min(requestedAmount, int.MaxValue - safeCurrent);
            current = safeCurrent + granted;
            return granted;
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

        private static string BuildByproductMessage(
            string rewardSnackName,
            int snackQuantity,
            int milkDropDelta)
        {
            if (snackQuantity > 0 && milkDropDelta > 0)
            {
                return $"조합의 잔향으로 {rewardSnackName}을 보관하고 우유방울을 얻었습니다.";
            }

            if (snackQuantity > 0)
            {
                return $"조합의 잔향으로 {rewardSnackName}을 보관했습니다.";
            }

            return $"조합의 잔향으로 우유방울 {milkDropDelta}개를 얻었습니다.";
        }
    }
}
