using System;
using System.Collections.Generic;
using System.Reflection;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class DecorationWorkshopDomainTests
    {
        [Test]
        public void CatalogHasExactlyTwoDeterministicVariantsPerExistingSlot()
        {
            Assert.That(DecorationWorkshopCatalog.All.Count, Is.EqualTo(12));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in DecorationWorkshopCatalog.All)
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(ids.Add(definition.Id), Is.True, definition.Id);
                Assert.That(definition.CoinCost, Is.GreaterThan(0));
                Assert.That(definition.MilkDropCost, Is.GreaterThan(0));
                Assert.That(definition.CollectionFragmentCost, Is.GreaterThan(0));
                Assert.That(definition.MaterialKey, Is.Not.Empty);
                Assert.That(definition.ColorKey, Is.Not.Empty);
                Assert.That(definition.TintHex, Does.Match("^[0-9A-F]{6}$"));
            }

            foreach (DecorationSlot slot in Enum.GetValues(typeof(DecorationSlot)))
            {
                var variants = DecorationWorkshopCatalog.GetForSlot(slot);
                Assert.That(variants.Count, Is.EqualTo(2), slot.ToString());
                Assert.That(variants[0].Slot, Is.EqualTo(slot));
                Assert.That(variants[1].Slot, Is.EqualTo(slot));
            }
        }

        [Test]
        public void QuoteShowsAllThreeCostsAndExactShortfallsBeforeCrafting()
        {
            var definition = DecorationWorkshopCatalog.Find(
                DecorationWorkshopCatalog.WallBerryMilkId);
            var state = new DecorationWorkshopSaveData();
            var system = new DecorationWorkshopSystem();
            var shortWallet = new DecorationWorkshopWalletSnapshot(
                definition.CoinCost - 1,
                definition.MilkDropCost - 1,
                definition.CollectionFragmentCost - 1);

            var shortQuote = system.BuildQuote(state, shortWallet, definition.Id);
            var exactQuote = system.BuildQuote(
                state,
                new DecorationWorkshopWalletSnapshot(
                    definition.CoinCost,
                    definition.MilkDropCost,
                    definition.CollectionFragmentCost),
                definition.Id);

            Assert.That(shortQuote.Status, Is.EqualTo(DecorationWorkshopQuoteStatus.Available));
            Assert.That(shortQuote.CanCraft, Is.False);
            Assert.That(shortQuote.MissingCoins, Is.EqualTo(1));
            Assert.That(shortQuote.MissingMilkDrops, Is.EqualTo(1));
            Assert.That(shortQuote.MissingCollectionFragments, Is.EqualTo(1));
            Assert.That(exactQuote.CanCraft, Is.True);
            Assert.That(
                DecorationWorkshopSystem.FormatCost(definition),
                Is.EqualTo(
                    $"코인 {definition.CoinCost} · 우유방울 {definition.MilkDropCost} · 도감조각 {definition.CollectionFragmentCost}"));
            Assert.That(
                DecorationWorkshopSystem.FormatCost(definition),
                Does.Not.Contain("우유코인"));
        }

        [Test]
        public void CraftAtExactPriceOwnsVariantAndReceiptWithoutMutatingInputWallet()
        {
            var definition = DecorationWorkshopCatalog.Find(
                DecorationWorkshopCatalog.AccentGoldLacquerId);
            var state = new DecorationWorkshopSaveData();
            var wallet = new DecorationWorkshopWalletSnapshot(
                definition.CoinCost,
                definition.MilkDropCost,
                definition.CollectionFragmentCost);

            var result = new DecorationWorkshopSystem().TryCraft(
                state,
                wallet,
                definition.Id,
                " workshop-craft-001 ");

            Assert.That(result.Status, Is.EqualTo(DecorationWorkshopCraftStatus.Applied));
            Assert.That(result.ReceiptKey, Is.EqualTo("workshop-craft-001"));
            Assert.That(result.WalletAfter.Coins, Is.Zero);
            Assert.That(result.WalletAfter.MilkDrops, Is.Zero);
            Assert.That(result.WalletAfter.CollectionFragments, Is.Zero);
            Assert.That(result.SpentCoins, Is.EqualTo(definition.CoinCost));
            Assert.That(result.SpentMilkDrops, Is.EqualTo(definition.MilkDropCost));
            Assert.That(
                result.SpentCollectionFragments,
                Is.EqualTo(definition.CollectionFragmentCost));
            Assert.That(state.Owns(definition.Id), Is.True);
            Assert.That(state.HasAppliedCraftReceipt("workshop-craft-001"), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(definition.CoinCost));
        }

        [Test]
        public void DuplicateReceiptAndAlreadyOwnedVariantNeverChargeAgain()
        {
            var definition = DecorationWorkshopCatalog.Find(
                DecorationWorkshopCatalog.FloorHoneyCheckerId);
            var state = new DecorationWorkshopSaveData();
            var wallet = new DecorationWorkshopWalletSnapshot(999, 999, 999);
            var system = new DecorationWorkshopSystem();
            var first = system.TryCraft(state, wallet, definition.Id, "craft-floor");
            var stateAfterFirst = JsonUtility.ToJson(state);

            var duplicate = system.TryCraft(
                state,
                first.WalletAfter,
                DecorationWorkshopCatalog.WallVanillaGlazeId,
                " craft-floor ");
            var alreadyOwned = system.TryCraft(
                state,
                first.WalletAfter,
                definition.Id,
                "different-receipt");

            Assert.That(duplicate.Status, Is.EqualTo(DecorationWorkshopCraftStatus.AlreadyApplied));
            Assert.That(duplicate.WalletAfter.Coins, Is.EqualTo(first.WalletAfter.Coins));
            Assert.That(alreadyOwned.Status, Is.EqualTo(DecorationWorkshopCraftStatus.AlreadyOwned));
            Assert.That(state.HasAppliedCraftReceipt("different-receipt"), Is.False);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(stateAfterFirst));
        }

        [Test]
        public void InvalidOrUnaffordableCraftLeavesStateAndWalletUntouched()
        {
            var state = new DecorationWorkshopSaveData();
            var wallet = new DecorationWorkshopWalletSnapshot(0, 0, 0);
            var system = new DecorationWorkshopSystem();
            var before = JsonUtility.ToJson(state);

            var invalidReceipt = system.TryCraft(
                state,
                wallet,
                DecorationWorkshopCatalog.WallVanillaGlazeId,
                " ");
            var unknown = system.TryCraft(state, wallet, "future_variant", "unknown-craft");
            var insufficient = system.TryCraft(
                state,
                wallet,
                DecorationWorkshopCatalog.WallVanillaGlazeId,
                "poor-craft");

            Assert.That(invalidReceipt.Status, Is.EqualTo(DecorationWorkshopCraftStatus.InvalidReceipt));
            Assert.That(unknown.Status, Is.EqualTo(DecorationWorkshopCraftStatus.UnknownVariant));
            Assert.That(insufficient.Status, Is.EqualTo(DecorationWorkshopCraftStatus.InsufficientCurrency));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(insufficient.WalletAfter.Coins, Is.Zero);
            Assert.That(state.appliedCraftReceiptKeys, Is.Empty);
        }

        [Test]
        public void SelectionRequiresOwnershipAndMatchingSlotAndCanResetToBase()
        {
            var state = new DecorationWorkshopSaveData();
            var system = new DecorationWorkshopSystem();
            var definition = DecorationWorkshopCatalog.Find(
                DecorationWorkshopCatalog.WindowSunriseSheerId);

            var notOwned = system.TrySelect(state, DecorationSlot.Window, definition.Id);
            state.ownedVariantIds.Add(definition.Id);
            var wrongSlot = system.TrySelect(state, DecorationSlot.Shelf, definition.Id);
            var unknown = system.TrySelect(state, DecorationSlot.Window, "future_window_variant");
            var selected = system.TrySelect(state, DecorationSlot.Window, definition.Id);
            var repeated = system.TrySelect(state, DecorationSlot.Window, definition.Id);
            var reset = system.TrySelect(state, DecorationSlot.Window, null);

            Assert.That(notOwned.Status, Is.EqualTo(DecorationWorkshopSelectionStatus.NotOwned));
            Assert.That(wrongSlot.Status, Is.EqualTo(DecorationWorkshopSelectionStatus.SlotMismatch));
            Assert.That(unknown.Status, Is.EqualTo(DecorationWorkshopSelectionStatus.UnknownVariant));
            Assert.That(selected.Status, Is.EqualTo(DecorationWorkshopSelectionStatus.Applied));
            Assert.That(repeated.Status, Is.EqualTo(DecorationWorkshopSelectionStatus.AlreadySelected));
            Assert.That(reset.Status, Is.EqualTo(DecorationWorkshopSelectionStatus.ResetToBase));
            Assert.That(state.GetSelectedVariantId((int)DecorationSlot.Window), Is.Empty);
        }

        [Test]
        public void NormalizationRepairsNullDuplicateAndUnknownSelectionsDeterministically()
        {
            var state = new DecorationWorkshopSaveData
            {
                schemaVersion = 0,
                ownedVariantIds = new List<string>
                {
                    " " + DecorationWorkshopCatalog.WallVanillaGlazeId + " ",
                    DecorationWorkshopCatalog.WallVanillaGlazeId,
                    DecorationWorkshopCatalog.WallBerryMilkId,
                    "future_owned_variant",
                    null
                },
                appliedCraftReceiptKeys = null,
                selectedVariants = new List<DecorationWorkshopSelectionSaveEntry>
                {
                    null,
                    new DecorationWorkshopSelectionSaveEntry { slot = -1, variantId = "bad" },
                    new DecorationWorkshopSelectionSaveEntry
                    {
                        slot = (int)DecorationSlot.Wall,
                        variantId = DecorationWorkshopCatalog.WallVanillaGlazeId
                    },
                    new DecorationWorkshopSelectionSaveEntry
                    {
                        slot = (int)DecorationSlot.Wall,
                        variantId = DecorationWorkshopCatalog.WallBerryMilkId
                    },
                    new DecorationWorkshopSelectionSaveEntry
                    {
                        slot = (int)DecorationSlot.Floor,
                        variantId = "future_floor_variant"
                    }
                }
            };
            var system = new DecorationWorkshopSystem();

            Assert.That(system.NormalizeState(state), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(DecorationWorkshopSaveData.CurrentSchemaVersion));
            Assert.That(state.ownedVariantIds.Count, Is.EqualTo(3));
            Assert.That(state.Owns("future_owned_variant"), Is.True);
            Assert.That(state.appliedCraftReceiptKeys, Is.Not.Null.And.Empty);
            Assert.That(
                state.GetSelectedVariantId((int)DecorationSlot.Wall),
                Is.EqualTo(DecorationWorkshopCatalog.WallBerryMilkId));
            Assert.That(state.GetSelectedVariantId((int)DecorationSlot.Floor), Is.Empty);
            Assert.That(state.selectedVariants.Count, Is.EqualTo(1));
            Assert.That(system.NormalizeState(state), Is.False);
        }

        [Test]
        public void RenderSnapshotHasAllSixSlotsAndOnlyKnownSelectedRenderKeys()
        {
            var state = new DecorationWorkshopSaveData();
            state.ownedVariantIds.Add(DecorationWorkshopCatalog.ShelfWarmMapleId);
            state.ownedVariantIds.Add(DecorationWorkshopCatalog.BedsideNightFabricId);
            var system = new DecorationWorkshopSystem();
            system.TrySelect(
                state,
                DecorationSlot.Shelf,
                DecorationWorkshopCatalog.ShelfWarmMapleId);
            system.TrySelect(
                state,
                DecorationSlot.Bedside,
                DecorationWorkshopCatalog.BedsideNightFabricId);

            var snapshot = system.BuildRenderSnapshot(state);
            var shelf = snapshot.Find(DecorationSlot.Shelf);
            var bedside = snapshot.Find(DecorationSlot.Bedside);
            var wall = snapshot.Find(DecorationSlot.Wall);

            Assert.That(snapshot.Entries.Count, Is.EqualTo(6));
            Assert.That(shelf.VariantId, Is.EqualTo(DecorationWorkshopCatalog.ShelfWarmMapleId));
            Assert.That(shelf.MaterialKey, Is.EqualTo("decor_shelf_wood"));
            Assert.That(shelf.ColorKey, Is.EqualTo("warm_maple"));
            Assert.That(shelf.TintHex, Is.EqualTo("B87743"));
            Assert.That(bedside.HasVariant, Is.True);
            Assert.That(wall.HasVariant, Is.False);
            Assert.That(wall.MaterialKey, Is.Empty);
            Assert.That(system.BuildRenderSnapshot(null).Entries.Count, Is.EqualTo(6));
        }

        [Test]
        public void SaveDtoRoundTripsWithoutAdditionalRepair()
        {
            var state = new DecorationWorkshopSaveData();
            var definition = DecorationWorkshopCatalog.Find(
                DecorationWorkshopCatalog.BedsidePeachFabricId);
            var system = new DecorationWorkshopSystem();
            system.TryCraft(
                state,
                new DecorationWorkshopWalletSnapshot(999, 999, 999),
                definition.Id,
                "round-trip-craft");
            system.TrySelect(state, definition.Slot, definition.Id);

            var restored = JsonUtility.FromJson<DecorationWorkshopSaveData>(
                JsonUtility.ToJson(state));

            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(system.NormalizeState(restored), Is.False);
            Assert.That(restored.Owns(definition.Id), Is.True);
            Assert.That(restored.HasAppliedCraftReceipt("round-trip-craft"), Is.True);
            Assert.That(
                restored.GetSelectedVariantId((int)DecorationSlot.Bedside),
                Is.EqualTo(definition.Id));
        }

        [Test]
        public void DefaultWindowAndShelfHaveTintableVisualsForWorkshopVariants()
        {
            var root = new GameObject("Workshop Presenter Test Root");
            root.SetActive(false);
            try
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(root.transform, false);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(root.transform, false);
                var authoredWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                authoredWindow.name = "Window_Model";
                authoredWindow.transform.SetParent(root.transform, false);
                var authoredShelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                authoredShelf.name = "MilkShelf_Model";
                authoredShelf.transform.SetParent(root.transform, false);
                var accent = new GameObject("Accent").transform;
                accent.SetParent(root.transform, false);
                var window = new GameObject("Window").transform;
                window.SetParent(root.transform, false);
                var shelf = new GameObject("Shelf").transform;
                shelf.SetParent(root.transform, false);
                var bedside = new GameObject("Bedside").transform;
                bedside.SetParent(root.transform, false);
                var presenter = root.AddComponent<DecorationRoomPresenter>();
                presenter.Configure(
                    wall.GetComponent<Renderer>(),
                    floor.GetComponent<Renderer>(),
                    accent,
                    window,
                    shelf,
                    bedside);

                var defaultWindow = window.Find("Equipped Window Decoration");
                var defaultShelf = shelf.Find("Equipped Shelf Decoration");
                Assert.That(defaultWindow, Is.Null);
                Assert.That(defaultShelf, Is.Null);

                var state = new DecorationWorkshopSaveData();
                state.ownedVariantIds.Add(DecorationWorkshopCatalog.WindowMidnightVelvetId);
                state.ownedVariantIds.Add(DecorationWorkshopCatalog.ShelfWarmMapleId);
                var system = new DecorationWorkshopSystem();
                Assert.That(
                    system.TrySelect(
                        state,
                        DecorationSlot.Window,
                        DecorationWorkshopCatalog.WindowMidnightVelvetId).Changed,
                    Is.True);
                Assert.That(
                    system.TrySelect(
                        state,
                        DecorationSlot.Shelf,
                        DecorationWorkshopCatalog.ShelfWarmMapleId).Changed,
                    Is.True);

                var apply = typeof(DecorationRoomPresenter).GetMethod(
                    "ApplyWorkshopSnapshot",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(apply, Is.Not.Null);
                apply.Invoke(presenter, new object[] { system.BuildRenderSnapshot(state) });

                AssertTint(
                    authoredWindow.GetComponent<Renderer>(),
                    "4D4E85");
                AssertTint(
                    authoredShelf.GetComponent<Renderer>(),
                    "B87743");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AssertTint(Renderer renderer, string tintHex)
        {
            Assert.That(renderer, Is.Not.Null);
            Assert.That(ColorUtility.TryParseHtmlString("#" + tintHex, out var expected), Is.True);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var actual = block.GetColor("_BaseColor");
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
