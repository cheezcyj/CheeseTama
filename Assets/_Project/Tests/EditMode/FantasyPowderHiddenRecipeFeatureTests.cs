using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.HiddenRecipes;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class FantasyPowderHiddenRecipeFeatureTests
    {
        [Test]
        public void FeatureIsHiddenAndAttemptIsBlockedUntilBothUnlockFlagsAreSet()
        {
            var system = new FantasyPowderHiddenRecipeSystem();
            var state = new FantasyPowderSaveData { powderQuantity = 5 };
            var inventory = new List<SnackInventorySaveEntry>();
            var economy = new EconomySaveData();

            var noUnlock = new UnlockSaveData();
            Assert.That(system.BuildSnapshot(noUnlock, state).visible, Is.False);
            var locked = system.TryAttempt(
                noUnlock,
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "locked-receipt-1",
                0d);

            Assert.That(locked.status, Is.EqualTo(FantasyPowderAttemptStatus.Locked));
            Assert.That(locked.applied, Is.False);
            Assert.That(state.powderQuantity, Is.EqualTo(5));
            Assert.That(state.attemptCount, Is.Zero);
            Assert.That(inventory, Is.Empty);

            var fantasyFlagOnly = new UnlockSaveData
            {
                fantasyPowderEnabled = true,
                starMilkUnlocked = false
            };
            Assert.That(system.BuildSnapshot(fantasyFlagOnly, state).visible, Is.False);

            var unlocked = CreateUnlockedFlags();
            Assert.That(system.BuildSnapshot(unlocked, state).visible, Is.True);
        }

        [Test]
        public void SevenPercentBoundaryUsesStrictLessThanAndPityNeverRaisesChance()
        {
            var system = new FantasyPowderHiddenRecipeSystem();
            var state = new FantasyPowderSaveData
            {
                powderQuantity = 3,
                attemptCount = 100,
                pityHintLevel = FantasyPowderSaveData.MaximumPityHintLevel
            };
            var inventory = new List<SnackInventorySaveEntry>();
            var economy = new EconomySaveData();

            var belowBoundary = system.TryAttempt(
                CreateUnlockedFlags(),
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "boundary-success",
                FantasyPowderHiddenRecipeSystem.SuccessChance - 0.000001d);
            var exactBoundary = system.TryAttempt(
                CreateUnlockedFlags(),
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.QuietAgingBowlId,
                "boundary-byproduct",
                FantasyPowderHiddenRecipeSystem.SuccessChance);

            Assert.That(belowBoundary.success, Is.True);
            Assert.That(exactBoundary.status, Is.EqualTo(FantasyPowderAttemptStatus.AppliedByproduct));
            Assert.That(exactBoundary.success, Is.False);
            Assert.That(exactBoundary.byproductGranted, Is.True);
            Assert.That(exactBoundary.pityHintLevel, Is.EqualTo(FantasyPowderSaveData.MaximumPityHintLevel));
        }

        [Test]
        public void ValidAttemptConsumesExactlyOnePowderAndDuplicateReceiptConsumesNothing()
        {
            var system = new FantasyPowderHiddenRecipeSystem();
            var state = new FantasyPowderSaveData { powderQuantity = 2 };
            var inventory = new List<SnackInventorySaveEntry>();
            var economy = new EconomySaveData();
            var unlocks = CreateUnlockedFlags();

            var invalidRoll = system.TryAttempt(
                unlocks,
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "attempt-receipt-invalid",
                double.NaN);
            Assert.That(invalidRoll.status, Is.EqualTo(FantasyPowderAttemptStatus.InvalidRoll));
            Assert.That(state.powderQuantity, Is.EqualTo(2));
            Assert.That(state.attemptCount, Is.Zero);

            var applied = system.TryAttempt(
                unlocks,
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "attempt-receipt-1",
                0.5d);
            Assert.That(applied.applied, Is.True);
            Assert.That(applied.powderBefore, Is.EqualTo(2));
            Assert.That(applied.powderAfter, Is.EqualTo(1));
            Assert.That(state.powderQuantity, Is.EqualTo(1));
            Assert.That(state.attemptCount, Is.EqualTo(1));

            var duplicate = system.TryAttempt(
                unlocks,
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "attempt-receipt-1",
                0d);
            Assert.That(duplicate.status, Is.EqualTo(FantasyPowderAttemptStatus.AlreadyApplied));
            Assert.That(duplicate.duplicateReceipt, Is.True);
            Assert.That(state.powderQuantity, Is.EqualTo(1));
            Assert.That(state.attemptCount, Is.EqualTo(1));
        }

        [Test]
        public void SuccessDiscoversRecipeAndFailureAlwaysGrantsUsefulByproduct()
        {
            var system = new FantasyPowderHiddenRecipeSystem();
            var unlocks = CreateUnlockedFlags();

            var successState = new FantasyPowderSaveData { powderQuantity = 1 };
            var successInventory = new List<SnackInventorySaveEntry>();
            var successEconomy = new EconomySaveData();
            var success = system.TryAttempt(
                unlocks,
                successState,
                successInventory,
                successEconomy,
                FantasyPowderHiddenRecipeCatalog.MidnightMilkJellyId,
                "success-receipt",
                0d);

            Assert.That(success.status, Is.EqualTo(FantasyPowderAttemptStatus.AppliedSuccess));
            Assert.That(success.newDiscovery, Is.True);
            Assert.That(successState.HasDiscovered(FantasyPowderHiddenRecipeCatalog.MidnightMilkJellyId), Is.True);
            Assert.That(success.rewardSnackId, Is.EqualTo(SnackCatalog.CoffeeMilkJellyId));
            Assert.That(success.rewardSnackQuantity, Is.EqualTo(2));
            Assert.That(successEconomy.starDrops, Is.EqualTo(1));
            Assert.That(successEconomy.milkDrops, Is.Zero);

            var byproductState = new FantasyPowderSaveData { powderQuantity = 1 };
            var byproductInventory = new List<SnackInventorySaveEntry>();
            var byproductEconomy = new EconomySaveData();
            var byproduct = system.TryAttempt(
                unlocks,
                byproductState,
                byproductInventory,
                byproductEconomy,
                FantasyPowderHiddenRecipeCatalog.QuietAgingBowlId,
                "byproduct-receipt",
                0.93d);

            Assert.That(byproduct.status, Is.EqualTo(FantasyPowderAttemptStatus.AppliedByproduct));
            Assert.That(byproduct.newDiscovery, Is.False);
            Assert.That(byproduct.rewardSnackId, Is.EqualTo(SnackCatalog.NuttyCheeseCrackerId));
            Assert.That(byproduct.rewardSnackQuantity, Is.EqualTo(1));
            Assert.That(byproduct.milkDropDelta, Is.EqualTo(2));
            Assert.That(byproductEconomy.milkDrops, Is.EqualTo(2));
            Assert.That(byproductInventory, Has.Count.EqualTo(1));
        }

        [Test]
        public void RepeatedSuccessDoesNotDuplicateDiscoveryButStillProducesRecipeOutput()
        {
            var system = new FantasyPowderHiddenRecipeSystem();
            var state = new FantasyPowderSaveData { powderQuantity = 2 };
            var inventory = new List<SnackInventorySaveEntry>();
            var economy = new EconomySaveData();
            var unlocks = CreateUnlockedFlags();

            var first = system.TryAttempt(
                unlocks,
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "discovery-receipt-1",
                0d);
            var repeated = system.TryAttempt(
                unlocks,
                state,
                inventory,
                economy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "discovery-receipt-2",
                0d);

            Assert.That(first.newDiscovery, Is.True);
            Assert.That(repeated.success, Is.True);
            Assert.That(repeated.newDiscovery, Is.False);
            Assert.That(
                state.discoveredHiddenRecipeIds.FindAll(
                    id => id == FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId),
                Has.Count.EqualTo(1));
            Assert.That(state.appliedReceiptKeys, Has.Count.EqualTo(2));
            Assert.That(FindQuantity(inventory, SnackCatalog.SoftSnackDoughId), Is.EqualTo(4));
        }

        [Test]
        public void MalformedSaveAndPublicDtosNormalizeToSafeBounds()
        {
            var state = new FantasyPowderSaveData
            {
                schemaVersion = -1,
                powderQuantity = -5,
                attemptCount = -9,
                pityHintLevel = 999,
                discoveredHiddenRecipeIds = null,
                appliedReceiptKeys = null
            };

            Assert.That(state.EnsureRuntimeDefaults(), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(FantasyPowderSaveData.CurrentSchemaVersion));
            Assert.That(state.powderQuantity, Is.Zero);
            Assert.That(state.attemptCount, Is.Zero);
            Assert.That(state.pityHintLevel, Is.EqualTo(FantasyPowderSaveData.MaximumPityHintLevel));
            Assert.That(state.discoveredHiddenRecipeIds, Is.Not.Null.And.Empty);
            Assert.That(state.appliedReceiptKeys, Is.Not.Null.And.Empty);

            var result = new FantasyPowderAttemptResult(
                FantasyPowderAttemptStatus.AppliedSuccess,
                null,
                "  ",
                null,
                null,
                powderBefore: -10,
                powderAfter: 99,
                attemptCountAfter: -3,
                pityHintLevel: 99,
                newDiscovery: true,
                rewardSnackId: null,
                rewardSnackQuantity: -1,
                milkCoinDelta: -1,
                milkDropDelta: -1,
                starDropDelta: -1,
                collectionFragmentDelta: -1);

            Assert.That(result.powderBefore, Is.Zero);
            Assert.That(result.powderAfter, Is.Zero);
            Assert.That(result.attemptCountAfter, Is.Zero);
            Assert.That(result.pityHintLevel, Is.EqualTo(FantasyPowderSaveData.MaximumPityHintLevel));
            Assert.That(result.recipeId, Is.Empty);
            Assert.That(result.recipeTitle, Is.Empty);
            Assert.That(result.rewardSnackQuantity, Is.Zero);
            Assert.That(result.starDropDelta, Is.Zero);
        }

        [Test]
        public void LockedSnapshotResultAndPanelContainNoSpoilerStrings()
        {
            var system = new FantasyPowderHiddenRecipeSystem();
            var state = new FantasyPowderSaveData
            {
                powderQuantity = 7,
                attemptCount = 20,
                pityHintLevel = 3
            };
            state.AddDiscoveredRecipe(FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId);
            var lockedSnapshot = system.BuildSnapshot(new UnlockSaveData(), state);
            Assert.That(lockedSnapshot.visible, Is.False);
            Assert.That(lockedSnapshot.RecipeEntries, Is.Empty);
            Assert.That(lockedSnapshot.hintText, Is.Empty);

            var lockedResult = new FantasyPowderAttemptResult(
                FantasyPowderAttemptStatus.Locked,
                "secret-receipt",
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDough.displayName,
                "환상가루 비밀 조리법",
                7,
                6,
                1,
                1,
                true,
                SnackCatalog.SoftSnackDoughId,
                2,
                0,
                0,
                1,
                0);
            Assert.That(lockedResult.receiptKey, Is.Empty);
            Assert.That(lockedResult.recipeId, Is.Empty);
            Assert.That(lockedResult.recipeTitle, Is.Empty);
            Assert.That(lockedResult.message, Is.Empty);
            Assert.That(lockedResult.rewardSnackId, Is.Empty);

            AssertLockedPanelClearsAllPresentation(lockedSnapshot);
        }

        [Test]
        public void CatalogUsesOnlyExistingSnackInventoryDefinitions()
        {
            Assert.That(FantasyPowderHiddenRecipeCatalog.All, Has.Length.EqualTo(3));
            foreach (var recipe in FantasyPowderHiddenRecipeCatalog.All)
            {
                Assert.That(recipe, Is.Not.Null);
                Assert.That(SnackCatalog.Find(recipe.resultSnackId), Is.Not.Null, recipe.id);
                Assert.That(SnackCatalog.Find(recipe.byproductSnackId), Is.Not.Null, recipe.id);
                Assert.That(recipe.resultSnackQuantity, Is.GreaterThan(0));
                Assert.That(recipe.byproductSnackQuantity, Is.GreaterThan(0));
            }
        }

        private static UnlockSaveData CreateUnlockedFlags()
        {
            return new UnlockSaveData
            {
                starEggUnlocked = true,
                starMilkUnlocked = true,
                fantasyPowderEnabled = true
            };
        }

        private static int FindQuantity(
            IEnumerable<SnackInventorySaveEntry> inventory,
            string snackId)
        {
            foreach (var entry in inventory)
            {
                if (entry != null && entry.snackId == snackId)
                {
                    return entry.quantity;
                }
            }

            return 0;
        }

        private static void AssertLockedPanelClearsAllPresentation(
            FantasyPowderPanelSnapshot lockedSnapshot)
        {
            var host = new GameObject("Fantasy Powder Test Host");
            try
            {
                var root = new GameObject("Hidden Recipe Panel Root");
                root.transform.SetParent(host.transform);
                var controller = host.AddComponent<FantasyPowderHiddenRecipePanelController>();
                var powderText = CreateText(root.transform, "Powder");
                var attemptText = CreateText(root.transform, "Attempts");
                var hintText = CreateText(root.transform, "Hint");
                var detailText = CreateText(root.transform, "Detail");
                var statusText = CreateText(root.transform, "Status");
                var recipeNames = new[]
                {
                    CreateText(root.transform, "Recipe 1"),
                    CreateText(root.transform, "Recipe 2"),
                    CreateText(root.transform, "Recipe 3")
                };
                var recipeStates = new[]
                {
                    CreateText(root.transform, "State 1"),
                    CreateText(root.transform, "State 2"),
                    CreateText(root.transform, "State 3")
                };
                var recipeButtons = new[]
                {
                    CreateButton(root.transform, "Button 1"),
                    CreateButton(root.transform, "Button 2"),
                    CreateButton(root.transform, "Button 3")
                };
                var attemptButton = CreateButton(root.transform, "Attempt");
                var closeButton = CreateButton(root.transform, "Close");

                controller.Configure(
                    root,
                    powderText,
                    attemptText,
                    hintText,
                    detailText,
                    statusText,
                    recipeNames,
                    recipeStates,
                    recipeButtons,
                    attemptButton,
                    closeButton,
                    () => lockedSnapshot,
                    _ => null);

                Assert.That(controller.Open(), Is.False);
                Assert.That(root.activeSelf, Is.False);
                Assert.That(powderText.text, Is.Empty);
                Assert.That(attemptText.text, Is.Empty);
                Assert.That(hintText.text, Is.Empty);
                Assert.That(detailText.text, Is.Empty);
                Assert.That(statusText.text, Is.Empty);
                Assert.That(attemptButton.interactable, Is.False);
                foreach (var text in recipeNames)
                {
                    Assert.That(text.text, Is.Empty);
                }

                foreach (var text in recipeStates)
                {
                    Assert.That(text.text, Is.Empty);
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
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
