using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class MilkBlendingFeatureTests
    {
        [Test]
        public void CatalogUsesStableUniqueMilkIngredientPairsAndExistingSnackResults()
        {
            Assert.That(MilkBlendingCatalog.AllMilkIds, Has.Length.EqualTo(8));
            Assert.That(MilkBlendingCatalog.AllIngredients, Has.Length.EqualTo(8));
            Assert.That(MilkBlendingCatalog.AllRecipes, Has.Length.EqualTo(8));

            var milkIds = new HashSet<string>(StringComparer.Ordinal);
            var ingredientIds = new HashSet<string>(StringComparer.Ordinal);
            var pairKeys = new HashSet<string>(StringComparer.Ordinal);
            var resultIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ingredient in MilkBlendingCatalog.AllIngredients)
            {
                Assert.That(ingredient, Is.Not.Null);
                Assert.That(ingredient.id, Is.Not.Empty);
                Assert.That(ingredientIds.Add(ingredient.id), Is.True, ingredient.id);
            }

            foreach (var milkId in MilkBlendingCatalog.AllMilkIds)
            {
                Assert.That(MilkCatalog.Find(milkId), Is.Not.Null, milkId);
                Assert.That(milkIds.Add(milkId), Is.True, milkId);
            }

            foreach (var recipe in MilkBlendingCatalog.AllRecipes)
            {
                Assert.That(recipe, Is.Not.Null);
                Assert.That(MilkCatalog.Find(recipe.milkId), Is.Not.Null, recipe.milkId);
                Assert.That(
                    MilkBlendingCatalog.FindIngredient(recipe.ingredientId),
                    Is.Not.Null,
                    recipe.ingredientId);
                Assert.That(SnackCatalog.Find(recipe.resultSnackId), Is.Not.Null, recipe.resultSnackId);
                var formattedCost = MilkBlendingCatalog.FormatCost(recipe);
                Assert.That(formattedCost, Does.Not.Contain("우유코인"));
                Assert.That(formattedCost, Does.Not.Contain("우유 코인"));
                if (recipe.coinCost > 0)
                {
                    Assert.That(formattedCost, Does.Contain($"코인 {recipe.coinCost}"));
                }
                Assert.That(
                    pairKeys.Add(recipe.milkId + "\n" + recipe.ingredientId),
                    Is.True,
                    recipe.resultSnackId);
                Assert.That(resultIds.Add(recipe.resultSnackId), Is.True, recipe.resultSnackId);
                if (recipe.HasSpecialResult)
                {
                    Assert.That(recipe.SpecialResultSnack, Is.Not.Null, recipe.specialResultSnackId);
                    Assert.That(
                        recipe.specialResultChance,
                        Is.EqualTo(MilkBlendingCatalog.DefaultSpecialResultChance));
                }
            }

            Assert.That(
                MilkBlendingCatalog.AllRecipes.Count(recipe => recipe.HasSpecialResult),
                Is.EqualTo(7));
            Assert.That(
                MilkBlendingCatalog.FindRecipe(
                    MilkCatalog.StarMilkId,
                    MilkBlendingCatalog.StarlightCreamIngredientId).HasSpecialResult,
                Is.False);
            Assert.That(SnackCatalog.Find(SnackCatalog.CreamSoupId), Is.SameAs(SnackCatalog.CreamSoup));
            Assert.That(SnackCatalog.CreamSoup.hunger, Is.EqualTo(25));
            Assert.That(SnackCatalog.CreamSoup.mood, Is.EqualTo(5));
            Assert.That(SnackCatalog.VisibleCookingRecipes.Contains(SnackCatalog.CreamSoup), Is.False);
            Assert.That(SnackCatalog.VisibleSnackItems.Contains(SnackCatalog.CreamSoup), Is.True);
        }

        [Test]
        public void MasteryCatalogUsesStableThreeStageResearchRecordsForEveryIngredient()
        {
            Assert.That(
                MilkBlendingCatalog.AllMasteryMilestones.Select(item => item.requiredUseCount),
                Is.EqualTo(new[]
                {
                    MilkBlendingCatalog.FirstMasteryUseCount,
                    MilkBlendingCatalog.SecondMasteryUseCount,
                    MilkBlendingCatalog.FinalMasteryUseCount
                }));
            Assert.That(
                MilkBlendingCatalog.AllMasteryMilestones.Select(item => item.stage),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(MilkBlendingCatalog.GetMasteryStage(2), Is.Zero);
            Assert.That(MilkBlendingCatalog.GetMasteryStage(3), Is.EqualTo(1));
            Assert.That(MilkBlendingCatalog.GetMasteryStage(7), Is.EqualTo(2));
            Assert.That(MilkBlendingCatalog.GetMasteryStage(12), Is.EqualTo(3));
            Assert.That(MilkBlendingCatalog.GetNextMasteryMilestone(12), Is.Null);

            var recordIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ingredient in MilkBlendingCatalog.AllIngredients)
            {
                foreach (var milestone in MilkBlendingCatalog.AllMasteryMilestones)
                {
                    var record = MilkBlendingCatalog.CreateMasteryResearchRecord(
                        ingredient,
                        milestone);
                    Assert.That(record, Is.Not.Null);
                    Assert.That(recordIds.Add(record.recordId), Is.True, record.recordId);
                    Assert.That(record.title, Does.Contain(ingredient.displayName));
                    Assert.That(record.detail, Does.Contain($"{milestone.requiredUseCount}회"));
                    Assert.That(
                        MilkBlendingCatalog.FindMasteryResearchRecord(record.recordId)?.title,
                        Is.EqualTo(record.title));
                }
            }

            Assert.That(recordIds, Has.Count.EqualTo(24));
        }

        [Test]
        public void SuccessfulBlendChargesExistingRecipeCostDiscoversAndUpdatesEvolutionSignal()
        {
            var system = new MilkBlendingSystem();
            var state = new MilkBlendingSaveData();
            var tama = new CheeseTamaModel();
            var economy = new EconomySaveData
            {
                milkCoins = 12,
                milkDrops = 3,
                collectionFragments = 2
            };
            var inventory = new List<SnackInventorySaveEntry>();
            var blendedAt = new DateTimeOffset(2026, 8, 14, 9, 30, 0, TimeSpan.Zero);

            var result = system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.NuttyMilkId,
                MilkBlendingCatalog.NutCrumbIngredientId,
                _ => true,
                "blend-success-1",
                blendedAt);

            var recipe = MilkBlendingCatalog.FindRecipe(
                MilkCatalog.NuttyMilkId,
                MilkBlendingCatalog.NutCrumbIngredientId);
            Assert.That(result.status, Is.EqualTo(MilkBlendStatus.Applied));
            Assert.That(result.applied, Is.True);
            Assert.That(result.firstDiscovery, Is.True);
            Assert.That(result.resultSnackId, Is.EqualTo(SnackCatalog.NuttyCheeseCrackerId));
            Assert.That(result.resultSnackQuantity, Is.EqualTo(1));
            Assert.That(result.milkCoinCost, Is.EqualTo(recipe.coinCost));
            Assert.That(economy.milkCoins, Is.EqualTo(12 - recipe.coinCost));
            Assert.That(economy.milkDrops, Is.EqualTo(3 - recipe.dropCost));
            Assert.That(economy.collectionFragments, Is.EqualTo(2 - recipe.fragmentCost));
            Assert.That(FindSnackQuantity(inventory, recipe.resultSnackId), Is.EqualTo(1));
            Assert.That(state.HasDiscovered(recipe.resultSnackId), Is.True);
            Assert.That(state.HasAppliedReceipt("blend-success-1"), Is.True);
            Assert.That(
                state.GetBlendCount(recipe.ingredientId, recipe.resultSnackId),
                Is.EqualTo(1));
            Assert.That(
                tama.growthHistory.mostUsedIngredientId,
                Is.EqualTo(SnackCatalog.NuttyCheeseCrackerId));
        }

        [Test]
        public void IngredientMasteryRewardsAndResearchRecordsApplyOnceAndRoundTrip()
        {
            var system = new MilkBlendingSystem();
            var state = new MilkBlendingSaveData();
            var tama = new CheeseTamaModel();
            var economy = new EconomySaveData { milkCoins = 100 };
            var inventory = new List<SnackInventorySaveEntry>();
            var time = new DateTimeOffset(2026, 8, 17, 19, 0, 0, TimeSpan.Zero);
            var rewardsByCount = new Dictionary<int, MilkBlendMasteryRewardResult>();

            for (var count = 1; count <= MilkBlendingCatalog.FinalMasteryUseCount; count += 1)
            {
                var result = BlendSoftDough(
                    system,
                    state,
                    tama,
                    economy,
                    inventory,
                    $"mastery-{count}",
                    time.AddMinutes(count));

                AssertApplied(result);
                Assert.That(result.ingredientBlendCount, Is.EqualTo(count));
                if (result.masteryReward.granted)
                {
                    rewardsByCount.Add(count, result.masteryReward);
                    Assert.That(result.message, Does.Contain("연구 기록"));
                    Assert.That(result.message, Does.Not.Contain("우유코인"));
                }
            }

            Assert.That(
                rewardsByCount.Keys.OrderBy(value => value),
                Is.EqualTo(new[]
                {
                    MilkBlendingCatalog.FirstMasteryUseCount,
                    MilkBlendingCatalog.SecondMasteryUseCount,
                    MilkBlendingCatalog.FinalMasteryUseCount
                }));
            Assert.That(rewardsByCount[3].milkCoins, Is.EqualTo(4));
            Assert.That(rewardsByCount[7].milkDrops, Is.EqualTo(2));
            Assert.That(rewardsByCount[12].milkCoins, Is.EqualTo(8));
            Assert.That(rewardsByCount[12].collectionFragments, Is.EqualTo(1));
            Assert.That(economy.milkCoins, Is.EqualTo(52));
            Assert.That(economy.milkDrops, Is.EqualTo(2));
            Assert.That(economy.collectionFragments, Is.EqualTo(1));
            Assert.That(state.masteryResearchRecordIds, Has.Count.EqualTo(3));

            var coinsAfterMilestones = economy.milkCoins;
            var dropsAfterMilestones = economy.milkDrops;
            var fragmentsAfterMilestones = economy.collectionFragments;
            var duplicate = BlendSoftDough(
                system,
                state,
                tama,
                economy,
                inventory,
                "mastery-12",
                time.AddDays(1));

            Assert.That(duplicate.status, Is.EqualTo(MilkBlendStatus.AlreadyApplied));
            Assert.That(duplicate.masteryReward.granted, Is.False);
            Assert.That(economy.milkCoins, Is.EqualTo(coinsAfterMilestones));
            Assert.That(economy.milkDrops, Is.EqualTo(dropsAfterMilestones));
            Assert.That(economy.collectionFragments, Is.EqualTo(fragmentsAfterMilestones));
            Assert.That(state.masteryResearchRecordIds, Has.Count.EqualTo(3));
            Assert.That(
                state.GetIngredientBlendCount(MilkBlendingCatalog.SoftDoughIngredientId),
                Is.EqualTo(12));

            var json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<MilkBlendingSaveData>(json);
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(restored.masteryResearchRecordIds, Is.EqualTo(state.masteryResearchRecordIds));
            var snapshot = system.BuildSnapshot(restored, economy, _ => true);
            Assert.That(
                snapshot.GetMasteryStage(MilkBlendingCatalog.SoftDoughIngredientId),
                Is.EqualTo(3));
            Assert.That(
                snapshot.GetMasteryResearchRecordCount(
                    MilkBlendingCatalog.SoftDoughIngredientId),
                Is.EqualTo(3));
            Assert.That(
                snapshot.GetLatestMasteryResearchRecord(
                    MilkBlendingCatalog.SoftDoughIngredientId)?.stage,
                Is.EqualTo(3));
        }

        [Test]
        public void SevenPercentBoundaryReplacesNormalResultAndDuplicateReceiptNeverRerolls()
        {
            var system = new MilkBlendingSystem();
            var state = new MilkBlendingSaveData();
            var tama = new CheeseTamaModel();
            var economy = new EconomySaveData { milkCoins = 20 };
            var inventory = new List<SnackInventorySaveEntry>();
            var time = new DateTimeOffset(2026, 8, 17, 18, 0, 0, TimeSpan.Zero);
            var special = system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.BasicMilkId,
                MilkBlendingCatalog.SoftDoughIngredientId,
                _ => true,
                "special-boundary",
                time,
                MilkBlendingCatalog.DefaultSpecialResultChance - 0.000001d);

            Assert.That(special.status, Is.EqualTo(MilkBlendStatus.Applied));
            Assert.That(special.specialResult, Is.True);
            Assert.That(special.resultSnackId, Is.EqualTo(SnackCatalog.CreamSoupId));
            Assert.That(special.firstDiscovery, Is.True);
            Assert.That(special.message, Does.Contain("특별한 음식"));
            Assert.That(special.ingredientBlendCount, Is.EqualTo(1));
            Assert.That(FindSnackQuantity(inventory, SnackCatalog.CreamSoupId), Is.EqualTo(1));
            Assert.That(FindSnackQuantity(inventory, SnackCatalog.SoftSnackDoughId), Is.Zero);
            Assert.That(state.HasDiscovered(SnackCatalog.CreamSoupId), Is.True);
            Assert.That(state.HasDiscovered(SnackCatalog.SoftSnackDoughId), Is.False);
            Assert.That(
                state.GetBlendCount(
                    MilkBlendingCatalog.SoftDoughIngredientId,
                    SnackCatalog.CreamSoupId),
                Is.EqualTo(1));
            Assert.That(
                tama.growthHistory.mostUsedIngredientId,
                Is.EqualTo(SnackCatalog.SoftSnackDoughId));

            var coinsAfterSpecial = economy.milkCoins;
            var duplicate = system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.BasicMilkId,
                MilkBlendingCatalog.SoftDoughIngredientId,
                _ => true,
                "special-boundary",
                time.AddSeconds(1),
                1d);

            Assert.That(duplicate.status, Is.EqualTo(MilkBlendStatus.AlreadyApplied));
            Assert.That(economy.milkCoins, Is.EqualTo(coinsAfterSpecial));
            Assert.That(FindSnackQuantity(inventory, SnackCatalog.CreamSoupId), Is.EqualTo(1));
            Assert.That(FindSnackQuantity(inventory, SnackCatalog.SoftSnackDoughId), Is.Zero);

            var json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<MilkBlendingSaveData>(json);
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(restored.HasDiscovered(SnackCatalog.CreamSoupId), Is.True);
            Assert.That(restored.HasAppliedReceipt("special-boundary"), Is.True);
            Assert.That(
                restored.GetBlendCount(
                    MilkBlendingCatalog.SoftDoughIngredientId,
                    SnackCatalog.CreamSoupId),
                Is.EqualTo(1));

            var boundaryState = new MilkBlendingSaveData();
            var boundaryTama = new CheeseTamaModel();
            var boundaryEconomy = new EconomySaveData { milkCoins = 20 };
            var boundaryInventory = new List<SnackInventorySaveEntry>();
            var boundary = system.TryBlend(
                boundaryState,
                boundaryTama,
                boundaryEconomy,
                boundaryInventory,
                MilkCatalog.BasicMilkId,
                MilkBlendingCatalog.SoftDoughIngredientId,
                _ => true,
                "normal-boundary",
                time,
                MilkBlendingCatalog.DefaultSpecialResultChance);

            Assert.That(boundary.status, Is.EqualTo(MilkBlendStatus.Applied));
            Assert.That(boundary.specialResult, Is.False);
            Assert.That(boundary.resultSnackId, Is.EqualTo(SnackCatalog.SoftSnackDoughId));
            Assert.That(FindSnackQuantity(boundaryInventory, SnackCatalog.CreamSoupId), Is.Zero);
            Assert.That(FindSnackQuantity(boundaryInventory, SnackCatalog.SoftSnackDoughId), Is.EqualTo(1));
        }

        [Test]
        public void InvalidSpecialRollsAndStarMilkNeverApplyGeneralSpecialResult()
        {
            var system = new MilkBlendingSystem();
            var time = new DateTimeOffset(2026, 8, 17, 18, 30, 0, TimeSpan.Zero);
            var invalidRolls = new[]
            {
                double.NaN,
                double.PositiveInfinity,
                -0.000001d,
                1.000001d
            };

            for (var index = 0; index < invalidRolls.Length; index += 1)
            {
                var state = new MilkBlendingSaveData();
                var economy = new EconomySaveData { milkCoins = 20 };
                var inventory = new List<SnackInventorySaveEntry>();
                var result = system.TryBlend(
                    state,
                    new CheeseTamaModel(),
                    economy,
                    inventory,
                    MilkCatalog.BasicMilkId,
                    MilkBlendingCatalog.SoftDoughIngredientId,
                    _ => true,
                    $"invalid-roll-{index}",
                    time,
                    invalidRolls[index]);

                Assert.That(result.status, Is.EqualTo(MilkBlendStatus.InvalidRoll));
                AssertUnchanged(state, economy, inventory, 20, 0);
            }

            var starState = new MilkBlendingSaveData();
            var starEconomy = new EconomySaveData
            {
                milkCoins = 20,
                milkDrops = 10,
                collectionFragments = 5
            };
            var starInventory = new List<SnackInventorySaveEntry>();
            var starResult = system.TryBlend(
                starState,
                new CheeseTamaModel(),
                starEconomy,
                starInventory,
                MilkCatalog.StarMilkId,
                MilkBlendingCatalog.StarlightCreamIngredientId,
                _ => true,
                "star-no-general-special",
                time,
                0d);

            Assert.That(starResult.status, Is.EqualTo(MilkBlendStatus.Applied));
            Assert.That(starResult.specialResult, Is.False);
            Assert.That(starResult.resultSnackId, Is.EqualTo(SnackCatalog.StarCreamId));
            Assert.That(FindSnackQuantity(starInventory, SnackCatalog.CreamSoupId), Is.Zero);
            Assert.That(FindSnackQuantity(starInventory, SnackCatalog.StarCreamId), Is.EqualTo(1));
        }

        [Test]
        public void MostUsedResultKeepsCurrentPreferenceOnTieThenChangesWhenCountLeads()
        {
            var system = new MilkBlendingSystem();
            var state = new MilkBlendingSaveData();
            var tama = new CheeseTamaModel();
            var economy = new EconomySaveData { milkCoins = 100, milkDrops = 100 };
            var inventory = new List<SnackInventorySaveEntry>();
            var time = new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.Zero);

            AssertApplied(BlendSoftDough(system, state, tama, economy, inventory, "tie-1", time));
            Assert.That(
                tama.growthHistory.mostUsedIngredientId,
                Is.EqualTo(SnackCatalog.SoftSnackDoughId));

            AssertApplied(BlendWarmSoup(system, state, tama, economy, inventory, "tie-2", time));
            Assert.That(
                tama.growthHistory.mostUsedIngredientId,
                Is.EqualTo(SnackCatalog.SoftSnackDoughId),
                "The existing preference should remain stable while counts are tied.");

            AssertApplied(BlendWarmSoup(system, state, tama, economy, inventory, "tie-3", time));
            Assert.That(
                tama.growthHistory.mostUsedIngredientId,
                Is.EqualTo(SnackCatalog.WarmMilkSoupId));
            Assert.That(
                state.GetIngredientBlendCount(MilkBlendingCatalog.HoneyPowderIngredientId),
                Is.EqualTo(2));
        }

        [Test]
        public void FailedAndDuplicateAttemptsNeverChargeOrChangeUsage()
        {
            var system = new MilkBlendingSystem();
            var state = new MilkBlendingSaveData();
            var tama = new CheeseTamaModel();
            var economy = new EconomySaveData { milkCoins = 20, milkDrops = 20 };
            var inventory = new List<SnackInventorySaveEntry>();
            var time = new DateTimeOffset(2026, 8, 14, 11, 0, 0, TimeSpan.Zero);

            var wrongPair = system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.BasicMilkId,
                MilkBlendingCatalog.HoneyPowderIngredientId,
                _ => true,
                "wrong-pair",
                time);
            Assert.That(wrongPair.status, Is.EqualTo(MilkBlendStatus.NoMatchingRecipe));
            AssertUnchanged(state, economy, inventory, 20, 20);

            var locked = system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.WarmMilkId,
                MilkBlendingCatalog.HoneyPowderIngredientId,
                milkId => milkId == MilkCatalog.BasicMilkId,
                "locked-pair",
                time);
            Assert.That(locked.status, Is.EqualTo(MilkBlendStatus.MilkLocked));
            AssertUnchanged(state, economy, inventory, 20, 20);

            economy.milkCoins = 4;
            var insufficient = system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.NuttyMilkId,
                MilkBlendingCatalog.NutCrumbIngredientId,
                _ => true,
                "insufficient-pair",
                time);
            Assert.That(insufficient.status, Is.EqualTo(MilkBlendStatus.InsufficientCurrency));
            AssertUnchanged(state, economy, inventory, 4, 20);

            economy.milkCoins = 20;
            var applied = BlendSoftDough(
                system,
                state,
                tama,
                economy,
                inventory,
                "duplicate-pair",
                time);
            AssertApplied(applied);
            var coinsAfterSuccess = economy.milkCoins;
            var quantityAfterSuccess = FindSnackQuantity(
                inventory,
                SnackCatalog.SoftSnackDoughId);
            var duplicate = BlendSoftDough(
                system,
                state,
                tama,
                economy,
                inventory,
                "duplicate-pair",
                time);

            Assert.That(duplicate.status, Is.EqualTo(MilkBlendStatus.AlreadyApplied));
            Assert.That(duplicate.duplicateReceipt, Is.True);
            Assert.That(economy.milkCoins, Is.EqualTo(coinsAfterSuccess));
            Assert.That(
                FindSnackQuantity(inventory, SnackCatalog.SoftSnackDoughId),
                Is.EqualTo(quantityAfterSuccess));
            Assert.That(
                state.GetBlendCount(
                    MilkBlendingCatalog.SoftDoughIngredientId,
                    SnackCatalog.SoftSnackDoughId),
                Is.EqualTo(1));
        }

        [Test]
        public void MasteryRewardCapacityFailureIsAtomicAndRetryBackfillsReachedRecords()
        {
            var system = new MilkBlendingSystem();
            var state = new MilkBlendingSaveData();
            var tama = new CheeseTamaModel();
            var economy = new EconomySaveData
            {
                milkCoins = 20,
                milkDrops = int.MaxValue
            };
            var inventory = new List<SnackInventorySaveEntry>();
            var time = new DateTimeOffset(2026, 8, 17, 20, 0, 0, TimeSpan.Zero);
            for (var index = 0; index < 6; index += 1)
            {
                state.RecordBlend(
                    MilkBlendingCatalog.SoftDoughIngredientId,
                    SnackCatalog.SoftSnackDoughId,
                    time.AddMinutes(index));
            }

            var blocked = BlendSoftDough(
                system,
                state,
                tama,
                economy,
                inventory,
                "mastery-capacity",
                time.AddHours(1));

            Assert.That(
                blocked.status,
                Is.EqualTo(MilkBlendStatus.MasteryRewardCapacityFull));
            Assert.That(economy.milkCoins, Is.EqualTo(20));
            Assert.That(economy.milkDrops, Is.EqualTo(int.MaxValue));
            Assert.That(inventory, Is.Empty);
            Assert.That(state.HasAppliedReceipt("mastery-capacity"), Is.False);
            Assert.That(state.masteryResearchRecordIds, Is.Empty);
            Assert.That(
                state.GetIngredientBlendCount(MilkBlendingCatalog.SoftDoughIngredientId),
                Is.EqualTo(6));

            economy.milkDrops = int.MaxValue - 2;
            var retried = BlendSoftDough(
                system,
                state,
                tama,
                economy,
                inventory,
                "mastery-capacity",
                time.AddHours(2));

            AssertApplied(retried);
            Assert.That(retried.masteryReward.reachedStage, Is.EqualTo(2));
            Assert.That(retried.newMasteryResearchRecordIds.Count, Is.EqualTo(2));
            Assert.That(economy.milkCoins, Is.EqualTo(19));
            Assert.That(economy.milkDrops, Is.EqualTo(int.MaxValue));
            Assert.That(state.masteryResearchRecordIds, Has.Count.EqualTo(2));
            Assert.That(state.HasAppliedReceipt("mastery-capacity"), Is.True);
            Assert.That(
                state.GetIngredientBlendCount(MilkBlendingCatalog.SoftDoughIngredientId),
                Is.EqualTo(7));
        }

        [Test]
        public void MalformedStandaloneSaveNormalizesMergesDuplicatesAndPreservesUnknownFutureData()
        {
            var state = new MilkBlendingSaveData
            {
                schemaVersion = -10,
                ingredientUsage = new List<MilkBlendUsageSaveEntry>
                {
                    null,
                    new MilkBlendUsageSaveEntry
                    {
                        ingredientId = " ingredient_soft_dough ",
                        resultSnackId = " recipe_soft_snack_dough ",
                        blendCount = 2,
                        firstBlendedAtIso = "2026-08-14T12:00:00+09:00",
                        lastBlendedAtIso = "2026-08-14T12:00:00+09:00"
                    },
                    new MilkBlendUsageSaveEntry
                    {
                        ingredientId = MilkBlendingCatalog.SoftDoughIngredientId,
                        resultSnackId = SnackCatalog.SoftSnackDoughId,
                        blendCount = 3,
                        firstBlendedAtIso = "2026-08-13T00:00:00Z",
                        lastBlendedAtIso = "2026-08-15T00:00:00Z"
                    },
                    new MilkBlendUsageSaveEntry
                    {
                        ingredientId = "future_ingredient",
                        resultSnackId = "future_result",
                        blendCount = 4
                    },
                    new MilkBlendUsageSaveEntry
                    {
                        ingredientId = "invalid",
                        resultSnackId = "invalid_result",
                        blendCount = -2
                    }
                },
                discoveredResultIds = null,
                appliedReceiptKeys = new List<string>
                {
                    " receipt-a ",
                    null,
                    "receipt-a",
                    "receipt-b"
                },
                masteryResearchRecordIds = new List<string>
                {
                    " milk_blend_mastery_ingredient_soft_dough_lv_1 ",
                    null,
                    "milk_blend_mastery_ingredient_soft_dough_lv_1",
                    "future_mastery_record"
                }
            };

            Assert.That(state.EnsureRuntimeDefaults(), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(MilkBlendingSaveData.CurrentSchemaVersion));
            Assert.That(state.ingredientUsage, Has.Count.EqualTo(2));
            Assert.That(
                state.GetBlendCount(
                    MilkBlendingCatalog.SoftDoughIngredientId,
                    SnackCatalog.SoftSnackDoughId),
                Is.EqualTo(5));
            Assert.That(state.GetBlendCount("future_ingredient", "future_result"), Is.EqualTo(4));
            Assert.That(state.HasDiscovered(SnackCatalog.SoftSnackDoughId), Is.True);
            Assert.That(state.HasDiscovered("future_result"), Is.True);
            Assert.That(state.appliedReceiptKeys, Is.EqualTo(new[] { "receipt-a", "receipt-b" }));
            Assert.That(
                state.masteryResearchRecordIds,
                Is.EqualTo(new[]
                {
                    "milk_blend_mastery_ingredient_soft_dough_lv_1",
                    "future_mastery_record"
                }));
        }

        [Test]
        public void StandaloneSaveRoundTripsThroughUnityJsonWithSafeDefaults()
        {
            var original = new MilkBlendingSaveData();
            var blendedAt = new DateTimeOffset(2026, 8, 14, 4, 5, 6, TimeSpan.Zero);
            original.RecordBlend(
                MilkBlendingCatalog.CoffeeJellyIngredientId,
                SnackCatalog.CoffeeMilkJellyId,
                blendedAt);
            original.AddAppliedReceipt("json-receipt");
            var masteryRecordId = MilkBlendingCatalog.BuildMasteryResearchRecordId(
                MilkBlendingCatalog.CoffeeJellyIngredientId,
                1);
            original.AddMasteryResearchRecord(masteryRecordId);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<MilkBlendingSaveData>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(restored.HasDiscovered(SnackCatalog.CoffeeMilkJellyId), Is.True);
            Assert.That(restored.HasAppliedReceipt("json-receipt"), Is.True);
            Assert.That(restored.HasMasteryResearchRecord(masteryRecordId), Is.True);
            Assert.That(
                restored.GetBlendCount(
                    MilkBlendingCatalog.CoffeeJellyIngredientId,
                    SnackCatalog.CoffeeMilkJellyId),
                Is.EqualTo(1));
        }

        [Test]
        public void CollectionFormatsSpecialBlendRecordWithoutRawIdOrCookingCopy()
        {
            const string RecordId = "milk_blend_recipe_cream_soup";
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var nameMethod = typeof(CollectionUIController).GetMethod(
                "FormatKnownRecordName",
                flags);
            var categoryMethod = typeof(CollectionUIController).GetMethod(
                "FormatRecordCategory",
                flags);
            var detailMethod = typeof(CollectionUIController).GetMethod(
                "FormatKnownRecordDetail",
                flags);

            Assert.That(nameMethod, Is.Not.Null);
            Assert.That(categoryMethod, Is.Not.Null);
            Assert.That(detailMethod, Is.Not.Null);
            Assert.That(nameMethod.Invoke(null, new object[] { RecordId }), Is.EqualTo("크림 수프"));
            Assert.That(categoryMethod.Invoke(null, new object[] { RecordId }), Is.EqualTo("요리"));
            Assert.That(
                detailMethod.Invoke(null, new object[] { RecordId }),
                Is.EqualTo("우유 블렌딩에서 발견한 음식 기록입니다."));
        }

        [Test]
        public void CallbackPanelSelectsOptionsAndNeverDuplicatesListenersAfterReconfigure()
        {
            var host = new GameObject("Milk Blending Test Host");
            try
            {
                var root = new GameObject("Milk Blending Panel Root");
                root.transform.SetParent(host.transform);
                var controller = host.AddComponent<MilkBlendingPanelController>();
                var balance = CreateText(root.transform, "Balance");
                var detail = CreateText(root.transform, "Detail");
                var resultText = CreateText(root.transform, "Result");
                var status = CreateText(root.transform, "Status");
                var milkNames = CreateTexts(root.transform, "Milk Name", 8);
                var milkStates = CreateTexts(root.transform, "Milk State", 8);
                var milkButtons = CreateButtons(root.transform, "Milk Button", 8);
                var ingredientNames = CreateTexts(root.transform, "Ingredient Name", 8);
                var ingredientStates = CreateTexts(root.transform, "Ingredient State", 8);
                var ingredientButtons = CreateButtons(root.transform, "Ingredient Button", 8);
                var blendButton = CreateButton(root.transform, "Blend");
                var closeButton = CreateButton(root.transform, "Close");
                var callbackCount = 0;
                var closeCount = 0;
                string requestedMilkId = null;
                string requestedIngredientId = null;
                var snapshot = new MilkBlendingPanelSnapshot(
                    50,
                    20,
                    5,
                    new[] { MilkCatalog.BasicMilkId, MilkCatalog.WarmMilkId },
                    new[] { SnackCatalog.WarmMilkSoupId, SnackCatalog.CreamSoupId },
                    new[]
                    {
                        new MilkBlendUsageView(
                            MilkBlendingCatalog.HoneyPowderIngredientId,
                            SnackCatalog.WarmMilkSoupId,
                            2),
                        new MilkBlendUsageView(
                            MilkBlendingCatalog.HoneyPowderIngredientId,
                            SnackCatalog.CreamSoupId,
                            1)
                    },
                    new[]
                    {
                        MilkBlendingCatalog.BuildMasteryResearchRecordId(
                            MilkBlendingCatalog.HoneyPowderIngredientId,
                            1)
                    });
                Func<string, string, MilkBlendResult> command = (milkId, ingredientId) =>
                {
                    callbackCount += 1;
                    requestedMilkId = milkId;
                    requestedIngredientId = ingredientId;
                    return new MilkBlendResult(
                        MilkBlendStatus.Applied,
                        "ui-receipt",
                        milkId,
                        ingredientId,
                        SnackCatalog.WarmMilkSoupId,
                        "완성했습니다.",
                        firstDiscovery: true,
                        resultSnackQuantity: 1,
                        ingredientBlendCount: 1,
                        milkCoinCost: 0,
                        milkDropCost: 0,
                        collectionFragmentCost: 0,
                        preferredIngredientId: SnackCatalog.WarmMilkSoupId);
                };

                ConfigurePanel(
                    controller,
                    root,
                    balance,
                    detail,
                    resultText,
                    status,
                    milkNames,
                    milkStates,
                    milkButtons,
                    ingredientNames,
                    ingredientStates,
                    ingredientButtons,
                    blendButton,
                    closeButton,
                    snapshot,
                    command,
                    () => closeCount += 1);
                ConfigurePanel(
                    controller,
                    root,
                    balance,
                    detail,
                    resultText,
                    status,
                    milkNames,
                    milkStates,
                    milkButtons,
                    ingredientNames,
                    ingredientStates,
                    ingredientButtons,
                    blendButton,
                    closeButton,
                    snapshot,
                    command,
                    () => closeCount += 1);

                Assert.That(controller.Open(), Is.True);
                Assert.That(balance.text, Is.EqualTo("코인 50 · 우유방울 20 · 수집 조각 5"));
                Assert.That(milkButtons[2].interactable, Is.False);
                milkButtons[1].onClick.Invoke();
                ingredientButtons[1].onClick.Invoke();
                blendButton.onClick.Invoke();

                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(requestedMilkId, Is.EqualTo(MilkCatalog.WarmMilkId));
                Assert.That(
                    requestedIngredientId,
                    Is.EqualTo(MilkBlendingCatalog.HoneyPowderIngredientId));
                Assert.That(controller.SelectedMilkId, Is.EqualTo(MilkCatalog.WarmMilkId));
                Assert.That(
                    controller.SelectedIngredientId,
                    Is.EqualTo(MilkBlendingCatalog.HoneyPowderIngredientId));
                Assert.That(status.text, Is.EqualTo("완성했습니다."));
                Assert.That(ingredientStates[1].text, Is.EqualTo("숙련 Lv.1 · 3/7회"));
                Assert.That(detail.text, Does.Contain("숙련 Lv.1 · 다음 연구까지 4회"));
                Assert.That(resultText.text, Does.Contain("따뜻한 우유 수프"));
                Assert.That(resultText.text, Does.Contain("블렌딩 횟수 3회"));
                Assert.That(resultText.text, Does.Contain("특별 결과  크림 수프"));
                Assert.That(resultText.text, Does.Contain("발견 1회"));
                Assert.That(resultText.text, Does.Contain("연구 기록  1/3"));
                Assert.That(resultText.text, Does.Contain("꿀가루 연구 Lv.1"));

                closeButton.onClick.Invoke();
                Assert.That(closeCount, Is.EqualTo(1));
                Assert.That(root.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BuilderCreatesCookingChoiceHubAndRemovesLegacyBlendEntriesIdempotently()
        {
            var canvasObject = new GameObject(
                "Milk Blending Builder Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var cookingPanel = new GameObject(
                    "Cooking Panel",
                    typeof(RectTransform),
                    typeof(Image));
                cookingPanel.transform.SetParent(canvasObject.transform, false);
                var legacyEntry = CreateButton(
                    cookingPanel.transform,
                    "Open Milk Blending Button");
                Assert.That(legacyEntry, Is.Not.Null);
                var utilityBar = new GameObject("Milkroom Utility Bar", typeof(RectTransform));
                utilityBar.transform.SetParent(canvasObject.transform, false);
                CreateButton(utilityBar.transform, "Open Milk Blending Button");

                var cookingController = canvasObject.AddComponent<CookingPanelController>();
                var cookingRootField = typeof(CookingPanelController).GetField(
                    "panelRoot",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(cookingRootField, Is.Not.Null);
                cookingRootField.SetValue(cookingController, cookingPanel);

                var actionBar = new GameObject("Bottom Action Bar", typeof(RectTransform));
                actionBar.transform.SetParent(canvasObject.transform, false);
                var blendEntry = CreateButton(actionBar.transform, "Blend Button");
                var careButton = blendEntry.gameObject.AddComponent<MilkroomCareButton>();
                careButton.Configure(MilkroomCareAction.Blend, null, null, cookingController);

                var ensureBlending = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureMilkBlendingPanel",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var ensureChoice = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureCookingChoicePanel",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(ensureBlending, Is.Not.Null);
                Assert.That(ensureChoice, Is.Not.Null);
                ensureBlending.Invoke(null, new object[] { canvasObject.transform, null, null });
                ensureChoice.Invoke(null, new object[] { canvasObject.transform });
                ensureBlending.Invoke(null, new object[] { canvasObject.transform, null, null });
                ensureChoice.Invoke(null, new object[] { canvasObject.transform });

                Assert.That(cookingPanel.transform.Find("Open Milk Blending Button"), Is.Null);
                Assert.That(utilityBar.transform.Find("Open Milk Blending Button"), Is.Null);
                Assert.That(
                    canvasObject.GetComponentsInChildren<Transform>(true)
                        .Count(item => item.name == "Open Milk Blending Button"),
                    Is.Zero);

                var overlay = canvasObject.transform.Find(CookingChoicePanelController.OverlayObjectName);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(
                    canvasObject.GetComponentsInChildren<Transform>(true)
                        .Count(item => item.name == CookingChoicePanelController.OverlayObjectName),
                    Is.EqualTo(1));
                Assert.That(
                    canvasObject.GetComponents<CookingChoicePanelController>(),
                    Has.Length.EqualTo(1));
                Assert.That(
                    canvasObject.transform
                        .Find("Milk Blending Overlay/Milk Blending Card/Milk Blending Balance Text")
                        ?.GetComponent<Text>()
                        ?.text,
                    Is.EqualTo("코인 0 · 우유방울 0 · 수집 조각 0"));
                var resolveChoice = typeof(MilkroomCareButton).GetMethod(
                    "ResolveCookingChoicePanel",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(resolveChoice, Is.Not.Null);
                Assert.That(
                    resolveChoice.Invoke(careButton, null),
                    Is.SameAs(canvasObject.GetComponent<CookingChoicePanelController>()));

                var card = overlay.Find("Cooking Choice Card");
                Assert.That(card, Is.Not.Null);
                var cookingChoice = card.Find("Cooking Choice Cooking Button")?.GetComponent<Button>();
                var blendingChoice = card.Find("Cooking Choice Milk Blending Button")?.GetComponent<Button>();
                Assert.That(cookingChoice, Is.Not.Null);
                Assert.That(blendingChoice, Is.Not.Null);
                Assert.That(
                    cookingChoice.GetComponentInChildren<Text>(true).text,
                    Is.EqualTo("요리하기"));
                Assert.That(
                    blendingChoice.GetComponentInChildren<Text>(true).text,
                    Is.EqualTo(
                        "<size=21>우유 블렌딩</size>\n"
                        + "<size=14>(낮은 확률로 특별한 음식 등장)</size>"));
                Assert.That(
                    card.GetComponentsInChildren<Button>(true),
                    Has.Length.EqualTo(2));
                Assert.That(card.Find("Cooking Choice Close Button"), Is.Null);

                var overlayRect = overlay.GetComponent<RectTransform>();
                Assert.That(overlayRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(overlayRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True);

                cookingPanel.SetActive(false);
                var openChoice = typeof(MilkroomCareButton).GetMethod(
                    "TryOpenCookingChoice",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(openChoice, Is.Not.Null);
                Assert.That(openChoice.Invoke(careButton, null), Is.True);
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                Assert.That(cookingPanel.activeSelf, Is.False);
                Assert.That(
                    canvasObject.transform.Find("Milk Blending Overlay").gameObject.activeSelf,
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void MilkroomFeatureBuildersContinueWhenOptionalUtilityBarIsMissing()
        {
            var canvasObject = new GameObject(
                "Milkroom Missing Utility Bar Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var cookingPanel = new GameObject(
                    "Cooking Panel",
                    typeof(RectTransform),
                    typeof(Image));
                cookingPanel.transform.SetParent(canvasObject.transform, false);

                var decorateOverlay = new GameObject(
                    "Decorate Overlay",
                    typeof(RectTransform),
                    typeof(Image));
                decorateOverlay.transform.SetParent(canvasObject.transform, false);

                Assert.That(canvasObject.transform.Find("Milkroom Utility Bar"), Is.Null);

                var ensureBlending = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureMilkBlendingPanel",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var ensureChoice = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureCookingChoicePanel",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var ensureDecorationShop = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureDecorationShop",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(ensureBlending, Is.Not.Null);
                Assert.That(ensureChoice, Is.Not.Null);
                Assert.That(ensureDecorationShop, Is.Not.Null);

                Assert.DoesNotThrow(() =>
                    ensureBlending.Invoke(null, new object[] { canvasObject.transform, null, null }));
                Assert.DoesNotThrow(() =>
                    ensureChoice.Invoke(null, new object[] { canvasObject.transform }));
                Assert.DoesNotThrow(() =>
                    ensureDecorationShop.Invoke(null, new object[] { canvasObject.transform }));

                Assert.That(canvasObject.transform.Find("Milk Blending Overlay"), Is.Not.Null);
                Assert.That(
                    canvasObject.transform.Find(CookingChoicePanelController.OverlayObjectName),
                    Is.Not.Null);
                Assert.That(canvasObject.transform.Find("Decoration Shop Overlay"), Is.Not.Null);
                Assert.That(
                    decorateOverlay.transform.Find("Open Decoration Shop Button")?.GetComponent<Button>(),
                    Is.Not.Null);
                Assert.That(canvasObject.GetComponent<MilkBlendingPanelController>(), Is.Not.Null);
                Assert.That(canvasObject.GetComponent<CookingChoicePanelController>(), Is.Not.Null);
                Assert.That(canvasObject.GetComponent<DecorationShopPanelController>(), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CookingChoiceRestoresControlsBeforeOpeningDetailedPanels()
        {
            var host = new GameObject("Cooking Choice Controller Test", typeof(RectTransform));
            try
            {
                var top = host.AddComponent<TopMenuController>();
                var dev = host.AddComponent<DevPanelController>();
                var actionBarObject = new GameObject("Bottom Action Bar", typeof(RectTransform));
                actionBarObject.transform.SetParent(host.transform, false);
                var bottom = actionBarObject.AddComponent<BottomActionBarController>();
                top.enabled = true;
                bottom.enabled = false;
                dev.enabled = true;

                var hubRoot = new GameObject(
                    CookingChoicePanelController.OverlayObjectName,
                    typeof(RectTransform),
                    typeof(Image));
                hubRoot.transform.SetParent(host.transform, false);
                var cookingChoice = CreateButton(hubRoot.transform, "Cooking Choice Cooking Button");
                var blendingChoice = CreateButton(hubRoot.transform, "Cooking Choice Milk Blending Button");

                var blendRoot = new GameObject(
                    "Milk Blending Overlay",
                    typeof(RectTransform),
                    typeof(Image));
                blendRoot.transform.SetParent(host.transform, false);
                var blendClose = CreateButton(blendRoot.transform, "Close Milk Blending Button");
                var blendExecute = CreateButton(blendRoot.transform, "Execute Milk Blending Button");
                var blendingController = host.AddComponent<MilkBlendingPanelController>();
                blendingController.Configure(
                    blendRoot,
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<Text>(),
                    Array.Empty<Text>(),
                    Array.Empty<Button>(),
                    Array.Empty<Text>(),
                    Array.Empty<Text>(),
                    Array.Empty<Button>(),
                    blendExecute,
                    blendClose,
                    MilkBlendingPanelSnapshot.CreateDefault,
                    null,
                    null,
                    top,
                    bottom,
                    dev);

                var cookingOpened = false;
                var cookingSawRestoredControls = false;
                var blendSawRestoredControls = false;
                var controller = host.AddComponent<CookingChoicePanelController>();
                controller.Configure(
                    hubRoot,
                    cookingChoice,
                    blendingChoice,
                    () =>
                    {
                        cookingSawRestoredControls = top.enabled && !bottom.enabled && dev.enabled;
                        cookingOpened = true;
                    },
                    () =>
                    {
                        blendSawRestoredControls = top.enabled && !bottom.enabled && dev.enabled;
                        return blendingController.Open();
                    },
                    top,
                    bottom,
                    dev);

                Assert.That(controller.Open(), Is.True);
                Assert.That(hubRoot.activeSelf, Is.True);
                Assert.That(top.enabled, Is.False);
                Assert.That(bottom.enabled, Is.False);
                Assert.That(dev.enabled, Is.False);

                cookingChoice.onClick.Invoke();
                Assert.That(cookingSawRestoredControls, Is.True);
                Assert.That(cookingOpened, Is.True);
                Assert.That(hubRoot.activeSelf, Is.False);
                Assert.That(top.enabled, Is.True);
                Assert.That(bottom.enabled, Is.False);
                Assert.That(dev.enabled, Is.True);

                Assert.That(controller.Open(), Is.True);
                blendingChoice.onClick.Invoke();
                Assert.That(blendSawRestoredControls, Is.True);
                Assert.That(hubRoot.activeSelf, Is.False);
                Assert.That(blendRoot.activeSelf, Is.True);
                Assert.That(top.enabled, Is.False);
                Assert.That(bottom.enabled, Is.False);
                Assert.That(dev.enabled, Is.False);

                blendingController.Close();
                Assert.That(top.enabled, Is.True);
                Assert.That(bottom.enabled, Is.False);
                Assert.That(dev.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static MilkBlendResult BlendSoftDough(
            MilkBlendingSystem system,
            MilkBlendingSaveData state,
            CheeseTamaModel tama,
            EconomySaveData economy,
            IList<SnackInventorySaveEntry> inventory,
            string receiptKey,
            DateTimeOffset time)
        {
            return system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.BasicMilkId,
                MilkBlendingCatalog.SoftDoughIngredientId,
                _ => true,
                receiptKey,
                time);
        }

        private static MilkBlendResult BlendWarmSoup(
            MilkBlendingSystem system,
            MilkBlendingSaveData state,
            CheeseTamaModel tama,
            EconomySaveData economy,
            IList<SnackInventorySaveEntry> inventory,
            string receiptKey,
            DateTimeOffset time)
        {
            return system.TryBlend(
                state,
                tama,
                economy,
                inventory,
                MilkCatalog.WarmMilkId,
                MilkBlendingCatalog.HoneyPowderIngredientId,
                _ => true,
                receiptKey,
                time);
        }

        private static void AssertApplied(MilkBlendResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.status, Is.EqualTo(MilkBlendStatus.Applied));
            Assert.That(result.applied, Is.True);
        }

        private static void AssertUnchanged(
            MilkBlendingSaveData state,
            EconomySaveData economy,
            ICollection<SnackInventorySaveEntry> inventory,
            int expectedCoins,
            int expectedDrops)
        {
            Assert.That(economy.milkCoins, Is.EqualTo(expectedCoins));
            Assert.That(economy.milkDrops, Is.EqualTo(expectedDrops));
            Assert.That(inventory, Is.Empty);
            Assert.That(state.ingredientUsage, Is.Empty);
            Assert.That(state.discoveredResultIds, Is.Empty);
            Assert.That(state.appliedReceiptKeys, Is.Empty);
            Assert.That(state.masteryResearchRecordIds, Is.Empty);
        }

        private static int FindSnackQuantity(
            IEnumerable<SnackInventorySaveEntry> inventory,
            string snackId)
        {
            foreach (var entry in inventory)
            {
                if (entry != null
                    && string.Equals(entry.snackId, snackId, StringComparison.Ordinal))
                {
                    return entry.quantity;
                }
            }

            return 0;
        }

        private static void ConfigurePanel(
            MilkBlendingPanelController controller,
            GameObject root,
            Text balance,
            Text detail,
            Text resultText,
            Text status,
            Text[] milkNames,
            Text[] milkStates,
            Button[] milkButtons,
            Text[] ingredientNames,
            Text[] ingredientStates,
            Button[] ingredientButtons,
            Button blendButton,
            Button closeButton,
            MilkBlendingPanelSnapshot snapshot,
            Func<string, string, MilkBlendResult> command,
            Action closeAction)
        {
            controller.Configure(
                root,
                balance,
                detail,
                resultText,
                status,
                milkNames,
                milkStates,
                milkButtons,
                ingredientNames,
                ingredientStates,
                ingredientButtons,
                blendButton,
                closeButton,
                () => snapshot,
                command,
                closeAction);
        }

        private static Text[] CreateTexts(Transform parent, string baseName, int count)
        {
            var values = new Text[count];
            for (var index = 0; index < count; index += 1)
            {
                values[index] = CreateText(parent, baseName + " " + index);
            }

            return values;
        }

        private static Button[] CreateButtons(Transform parent, string baseName, int count)
        {
            var values = new Button[count];
            for (var index = 0; index < count; index += 1)
            {
                values[index] = CreateButton(parent, baseName + " " + index);
            }

            return values;
        }

        private static Text CreateText(Transform parent, string objectName)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent);
            return gameObject.GetComponent<Text>();
        }

        private static Button CreateButton(Transform parent, string objectName)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent);
            return gameObject.GetComponent<Button>();
        }
    }
}
