using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class DecorationFeatureTests
    {
        [Test]
        public void CatalogHasFourteenUniqueItemsAndOneDefaultPerSlot()
        {
            Assert.That(DecorationCatalog.All, Has.Length.EqualTo(14));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in DecorationCatalog.All)
            {
                Assert.That(item, Is.Not.Null);
                Assert.That(ids.Add(item.id), Is.True, item.id);
                Assert.That(item.milkCoinCost, Is.GreaterThanOrEqualTo(0));
                Assert.That(item.milkDropCost, Is.GreaterThanOrEqualTo(0));
            }

            foreach (DecorationSlot slot in Enum.GetValues(typeof(DecorationSlot)))
            {
                var defaults = Array.FindAll(
                    DecorationCatalog.All,
                    item => item.slot == slot && item.defaultOwned);
                Assert.That(defaults, Has.Length.EqualTo(1), slot.ToString());
                Assert.That(defaults[0].IsFree, Is.True);
            }
        }

        [Test]
        public void ExpandedFixedSlotsEquipIndependentlyAndRoundTrip()
        {
            var snapshot = new DecorationShopSnapshot(
                0,
                0,
                new[]
                {
                    DecorationCatalog.MoonCurtainId,
                    DecorationCatalog.MemoryFrameId,
                    DecorationCatalog.StarPlushId
                },
                null,
                null,
                null,
                DecorationCatalog.MoonCurtainId,
                DecorationCatalog.MemoryFrameId,
                DecorationCatalog.StarPlushId);

            Assert.That(snapshot.GetEquippedId(DecorationSlot.Window), Is.EqualTo(DecorationCatalog.MoonCurtainId));
            Assert.That(snapshot.GetEquippedId(DecorationSlot.Shelf), Is.EqualTo(DecorationCatalog.MemoryFrameId));
            Assert.That(snapshot.GetEquippedId(DecorationSlot.Bedside), Is.EqualTo(DecorationCatalog.StarPlushId));

            var changed = DecorationShopRules.Equip(
                DecorationCatalog.CreamCurtainId,
                snapshot);
            Assert.That(changed.Succeeded, Is.True);
            Assert.That(changed.snapshot.GetEquippedId(DecorationSlot.Window), Is.EqualTo(DecorationCatalog.CreamCurtainId));
            Assert.That(changed.snapshot.GetEquippedId(DecorationSlot.Shelf), Is.EqualTo(DecorationCatalog.MemoryFrameId));
        }

        [Test]
        public void MissingAndInvalidStateNormalizesToSafeDefaultsWithoutDroppingUnknownIds()
        {
            var snapshot = new DecorationShopSnapshot(
                -5,
                -2,
                new[] { "future_catalog_item", DecorationCatalog.PeachWallId },
                DecorationCatalog.PeachWallId,
                DecorationCatalog.StarLampId,
                "not_a_real_item");

            Assert.That(snapshot.milkCoins, Is.Zero);
            Assert.That(snapshot.milkDrops, Is.Zero);
            Assert.That(snapshot.Owns("future_catalog_item"), Is.True);
            Assert.That(snapshot.Owns(DecorationCatalog.CreamWallId), Is.True);
            Assert.That(snapshot.Owns(DecorationCatalog.CreamRugId), Is.True);
            Assert.That(snapshot.Owns(DecorationCatalog.MilkBottleId), Is.True);
            Assert.That(snapshot.GetEquippedId(DecorationSlot.Wall), Is.EqualTo(DecorationCatalog.PeachWallId));
            Assert.That(snapshot.GetEquippedId(DecorationSlot.Floor), Is.EqualTo(DecorationCatalog.CreamRugId));
            Assert.That(snapshot.GetEquippedId(DecorationSlot.Accent), Is.EqualTo(DecorationCatalog.MilkBottleId));
        }

        [Test]
        public void PurchaseAtExactPriceDeductsCurrencyAndPreservesOriginalSnapshot()
        {
            var item = DecorationCatalog.StarLamp;
            var original = DecorationShopSnapshot.CreateDefault(
                item.milkCoinCost,
                item.milkDropCost);

            var result = DecorationShopRules.Purchase(item.id, original);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.snapshot.milkCoins, Is.Zero);
            Assert.That(result.snapshot.milkDrops, Is.Zero);
            Assert.That(result.snapshot.Owns(item.id), Is.True);
            Assert.That(original.milkCoins, Is.EqualTo(item.milkCoinCost));
            Assert.That(original.milkDrops, Is.EqualTo(item.milkDropCost));
            Assert.That(original.Owns(item.id), Is.False);
        }

        [Test]
        public void DuplicateOrUnaffordablePurchaseDoesNotChangeSnapshot()
        {
            var poorSnapshot = DecorationShopSnapshot.CreateDefault(999, 0);
            var insufficient = DecorationShopRules.Purchase(
                DecorationCatalog.StarLampId,
                poorSnapshot);

            Assert.That(
                insufficient.status,
                Is.EqualTo(DecorationTransactionStatus.InsufficientCurrency));
            Assert.That(insufficient.snapshot, Is.SameAs(poorSnapshot));

            var ownedSnapshot = new DecorationShopSnapshot(
                999,
                999,
                new[] { DecorationCatalog.StarLampId },
                null,
                null,
                null);
            var duplicate = DecorationShopRules.Purchase(
                DecorationCatalog.StarLampId,
                ownedSnapshot);

            Assert.That(duplicate.status, Is.EqualTo(DecorationTransactionStatus.AlreadyOwned));
            Assert.That(duplicate.snapshot, Is.SameAs(ownedSnapshot));
            Assert.That(duplicate.snapshot.milkCoins, Is.EqualTo(999));
            Assert.That(duplicate.snapshot.milkDrops, Is.EqualTo(999));
        }

        [Test]
        public void EquipRequiresOwnershipAndChangesOnlyTheMatchingSlot()
        {
            var initial = DecorationShopSnapshot.CreateDefault(0, 0);
            var denied = DecorationShopRules.Equip(DecorationCatalog.StarlightWallId, initial);
            Assert.That(denied.status, Is.EqualTo(DecorationTransactionStatus.NotOwned));

            var owned = new DecorationShopSnapshot(
                0,
                0,
                new[] { DecorationCatalog.StarlightWallId, DecorationCatalog.CloudMatId },
                null,
                DecorationCatalog.CloudMatId,
                null);
            var equipped = DecorationShopRules.Equip(DecorationCatalog.StarlightWallId, owned);

            Assert.That(equipped.Succeeded, Is.True);
            Assert.That(
                equipped.snapshot.GetEquippedId(DecorationSlot.Wall),
                Is.EqualTo(DecorationCatalog.StarlightWallId));
            Assert.That(
                equipped.snapshot.GetEquippedId(DecorationSlot.Floor),
                Is.EqualTo(DecorationCatalog.CloudMatId));
            Assert.That(
                equipped.snapshot.GetEquippedId(DecorationSlot.Accent),
                Is.EqualTo(DecorationCatalog.MilkBottleId));
        }

        [Test]
        public void PanelUsesConfiguredCommandsAndDoesNotDuplicateButtonListeners()
        {
            var canvasObject = new GameObject("Decoration Test Canvas", typeof(RectTransform), typeof(Canvas));
            var panelRoot = new GameObject("Decoration Test Panel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasObject.transform, false);
            try
            {
                var balance = CreateText(panelRoot.transform, "Balance");
                var detail = CreateText(panelRoot.transform, "Detail");
                var status = CreateText(panelRoot.transform, "Status");
                var names = new Text[DecorationCatalog.All.Length];
                var states = new Text[DecorationCatalog.All.Length];
                var itemButtons = new Button[DecorationCatalog.All.Length];
                for (var index = 0; index < DecorationCatalog.All.Length; index += 1)
                {
                    names[index] = CreateText(panelRoot.transform, $"Name {index}");
                    states[index] = CreateText(panelRoot.transform, $"State {index}");
                    itemButtons[index] = CreateButton(panelRoot.transform, $"Item {index}");
                }

                var purchaseButton = CreateButton(panelRoot.transform, "Purchase");
                var equipButton = CreateButton(panelRoot.transform, "Equip");
                var closeButton = CreateButton(panelRoot.transform, "Close");
                var current = DecorationShopSnapshot.CreateDefault(200, 10);
                var purchaseCalls = 0;
                var equipCalls = 0;
                var controller = canvasObject.AddComponent<DecorationShopPanelController>();
                controller.Configure(
                    panelRoot,
                    balance,
                    detail,
                    status,
                    names,
                    states,
                    itemButtons,
                    purchaseButton,
                    equipButton,
                    closeButton,
                    () => current,
                    itemId =>
                    {
                        purchaseCalls += 1;
                        var result = DecorationShopRules.Purchase(itemId, current);
                        current = result.snapshot;
                        return result;
                    },
                    itemId =>
                    {
                        equipCalls += 1;
                        var result = DecorationShopRules.Equip(itemId, current);
                        current = result.snapshot;
                        return result;
                    });

                controller.Open();
                controller.Open();
                var selectedIndex = Array.FindIndex(
                    DecorationCatalog.All,
                    item => item.id == DecorationCatalog.StarLampId);
                itemButtons[selectedIndex].onClick.Invoke();
                purchaseButton.onClick.Invoke();
                equipButton.onClick.Invoke();

                Assert.That(purchaseCalls, Is.EqualTo(1));
                Assert.That(equipCalls, Is.EqualTo(1));
                Assert.That(current.Owns(DecorationCatalog.StarLampId), Is.True);
                Assert.That(
                    current.GetEquippedId(DecorationSlot.Accent),
                    Is.EqualTo(DecorationCatalog.StarLampId));
                Assert.That(balance.text, Is.EqualTo("코인 20 · 우유방울 2"));
                Assert.That(status.text, Does.Contain("장착"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static Text CreateText(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var label = gameObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return label;
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            var label = CreateText(gameObject.transform, "Label");
            label.text = name;
            return gameObject.GetComponent<Button>();
        }
    }
}
