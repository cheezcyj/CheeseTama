using System;
using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Collections.HiddenCareers;
using CheeseTama.Data;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class HiddenCareerCardFeatureTests
    {
        [Test]
        public void CatalogContainsExactlySevenStableUniqueCards()
        {
            var cards = HiddenCareerCardCatalog.All;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var benefits = new HashSet<HiddenCareerBenefitKind>();

            Assert.That(cards.Count, Is.EqualTo(7));
            for (var index = 0; index < cards.Count; index += 1)
            {
                Assert.That(cards[index], Is.Not.Null);
                Assert.That(cards[index].Id, Is.Not.Empty);
                Assert.That(ids.Add(cards[index].Id), Is.True, cards[index].Id);
                Assert.That(cards[index].DisplayName, Is.Not.Empty);
                Assert.That(cards[index].Quote, Is.Not.Empty);
                Assert.That(cards[index].DeepText, Is.Not.Empty);
                Assert.That(cards[index].Benefit, Is.Not.Null);
                Assert.That(benefits.Add(cards[index].Benefit.Kind), Is.True);
            }

            Assert.That(
                HiddenCareerCardCatalog.Find(HiddenCareerCardCatalog.GuardianId).Rarity,
                Is.EqualTo(Rarity.Unique));
            Assert.That(
                HiddenCareerCardCatalog.Find(HiddenCareerCardCatalog.BlackStarObserverId).Rarity,
                Is.EqualTo(Rarity.Legendary));
        }

        [Test]
        public void UndiscoveredAndUnknownEntriesNeverCreateVisibleCareerSlots()
        {
            var collections = new CollectionSaveData();
            collections.hiddenUnlockedOnly.Add(new HiddenCollectionSaveEntry
            {
                id = "future_hidden_record",
                acquiredAtIso = "2026-08-14T09:00:00+09:00"
            });
            var system = new HiddenCareerCardSystem();

            var visible = system.GetVisibleUnlockedCards(collections);

            Assert.That(visible, Is.Empty);
            Assert.That(collections.hiddenUnlockedOnly, Has.Count.EqualTo(1));
        }

        [Test]
        public void ExplicitKnownUnlockIsIdempotentAndUsesExistingHiddenCollectionStorage()
        {
            var collections = new CollectionSaveData();
            var acquiredAt = new DateTimeOffset(2026, 8, 14, 12, 30, 0, TimeSpan.FromHours(9));
            var system = new HiddenCareerCardSystem();

            var first = system.TryUnlockKnownCard(
                collections,
                HiddenCareerCardCatalog.ScientistId,
                acquiredAt);
            var repeated = system.TryUnlockKnownCard(
                collections,
                HiddenCareerCardCatalog.ScientistId,
                acquiredAt.AddMinutes(1));
            var unknown = system.TryUnlockKnownCard(
                collections,
                "unknown_career",
                acquiredAt);
            var visible = system.GetVisibleUnlockedCards(collections);

            Assert.That(first.Unlocked, Is.True);
            Assert.That(repeated.Unlocked, Is.False);
            Assert.That(unknown.Unlocked, Is.False);
            Assert.That(collections.hiddenUnlockedOnly, Has.Count.EqualTo(1));
            Assert.That(visible, Has.Count.EqualTo(1));
            Assert.That(visible[0].DisplayName, Is.EqualTo("과학자치즈타마"));
            Assert.That(visible[0].AcquiredDateText, Is.EqualTo("2026.08.14"));
            var benefits = system.GetUnlockedBenefits(collections);
            Assert.That(benefits.Count, Is.EqualTo(1));
            Assert.That(benefits[0].Kind, Is.EqualTo(HiddenCareerBenefitKind.RecipeHintProgress));
        }

        [Test]
        public void AutomaticEvaluationFailsClosedBeforeTheHiddenRoute()
        {
            var save = CreateEndgameSave();
            save.unlocks.fantasyPowderEnabled = false;

            var result = new HiddenCareerCardSystem().TryUnlockNextEligible(
                save,
                DateTimeOffset.Now);

            Assert.That(result.Unlocked, Is.False);
            Assert.That(save.collections.hiddenUnlockedOnly, Is.Empty);
        }

        [Test]
        public void TemperamentOnlyLowersAThresholdAndNeverLocksOtherPathsForever()
        {
            var focused = CreateRouteReadySave();
            focused.newGameSetup.selectedEggId = NewGameSetupCatalog.CoffeeEggId;
            focused.newGameSetup.selectedFirstMilkId = NewGameSetupCatalog.CoffeeFirstMilkId;
            focused.newGameSetup.EnsureRuntimeDefaults();
            focused.careHistory.cookings = 10;
            focused.fantasyPowder.discoveredHiddenRecipeIds.Add("recipe_a");

            var balanced = CreateRouteReadySave();
            balanced.careHistory.cookings = 10;
            balanced.fantasyPowder.discoveredHiddenRecipeIds.Add("recipe_a");

            var system = new HiddenCareerCardSystem();
            var focusedResult = system.TryUnlockNextEligible(focused, DateTimeOffset.Now);
            var balancedResult = system.TryUnlockNextEligible(balanced, DateTimeOffset.Now);

            Assert.That(focusedResult.Unlocked, Is.True);
            Assert.That(focusedResult.Card.Id, Is.EqualTo(HiddenCareerCardCatalog.ScientistId));
            Assert.That(balancedResult.Unlocked, Is.False);
        }

        [Test]
        public void EndgameEvaluationUnlocksAtMostOneCardPerCallInStableOrder()
        {
            var save = CreateEndgameSave();
            var expected = new[]
            {
                HiddenCareerCardCatalog.ScientistId,
                HiddenCareerCardCatalog.TeacherId,
                HiddenCareerCardCatalog.DoctorId,
                HiddenCareerCardCatalog.ExplorerId,
                HiddenCareerCardCatalog.GuardianId,
                HiddenCareerCardCatalog.RiftArchitectId,
                HiddenCareerCardCatalog.BlackStarObserverId
            };
            var system = new HiddenCareerCardSystem();
            var acquiredAt = new DateTimeOffset(2026, 8, 14, 18, 0, 0, TimeSpan.FromHours(9));

            for (var index = 0; index < expected.Length; index += 1)
            {
                var result = system.TryUnlockNextEligible(save, acquiredAt.AddMinutes(index));
                Assert.That(result.Unlocked, Is.True, $"index {index}");
                Assert.That(result.Card.Id, Is.EqualTo(expected[index]));
                Assert.That(
                    save.collections.hiddenUnlockedOnly,
                    Has.Count.EqualTo(index + 1));
            }

            Assert.That(
                system.TryUnlockNextEligible(save, acquiredAt.AddHours(1)).Unlocked,
                Is.False);
            Assert.That(system.GetVisibleUnlockedCards(save.collections), Has.Count.EqualTo(7));
        }

        [Test]
        public void IndependentPanelHidesItsExistenceUntilFirstUnlockAndNeverPrintsSecrets()
        {
            var host = new GameObject("Hidden Career UI Test Host");
            try
            {
                var panel = CreateUiObject("Panel", host.transform);
                var title = CreateText("Title", host.transform);
                var list = CreateText("List", host.transform);
                var entry = CreateButton("Entry", host.transform);
                var close = CreateButton("Close", host.transform);
                var controller = host.AddComponent<HiddenCareerCardPanelController>();
                controller.Configure(panel, title, list, entry, close);
                controller.Bind(Array.Empty<HiddenCareerCardViewData>());

                Assert.That(controller.HasVisibleCards, Is.False);
                Assert.That(controller.Open(), Is.False);
                Assert.That(panel.activeSelf, Is.False);
                Assert.That(entry.gameObject.activeSelf, Is.False);
                Assert.That(controller.RenderedText, Is.Empty);

                var collections = new CollectionSaveData();
                var card = new HiddenCareerCardSystem().TryUnlockKnownCard(
                    collections,
                    HiddenCareerCardCatalog.ScientistId,
                    new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.FromHours(9)));
                controller.Bind(new[] { card.Card });

                Assert.That(entry.gameObject.activeSelf, Is.True);
                Assert.That(controller.Open(), Is.True);
                Assert.That(controller.RenderedText, Does.Contain("과학자치즈타마"));
                Assert.That(controller.RenderedText, Does.Contain("맛은 데이터야"));
                Assert.That(controller.RenderedText, Does.Not.Contain(HiddenCareerCardCatalog.ScientistId));
                Assert.That(controller.RenderedText, Does.Not.Contain("hero"));
                Assert.That(controller.RenderedText, Does.Not.Contain("조건"));
                Assert.That(controller.RenderedText, Does.Not.Contain("???"));
                Assert.That(controller.RenderedText, Does.Not.Contain("/7"));

                close.onClick.Invoke();
                Assert.That(controller.IsOpen, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static CheeseTamaSaveData CreateRouteReadySave()
        {
            var save = new CheeseTamaSaveData();
            save.EnsureRuntimeDefaults();
            save.cheeseTama.level = 33;
            save.unlocks.starMilkUnlocked = true;
            save.unlocks.fantasyPowderEnabled = true;
            save.cheeseTama.stats.affection = 50;
            save.newGameSetup.legacySuppressed = false;
            save.newGameSetup.completed = true;
            save.newGameSetup.skipped = false;
            save.newGameSetup.outcomeApplied = true;
            save.newGameSetup.currentStep = NewGameSetupStep.Complete;
            save.newGameSetup.selectedEggId = NewGameSetupCatalog.CreamEggId;
            save.newGameSetup.selectedFirstMilkId = NewGameSetupCatalog.BasicFirstMilkId;
            save.newGameSetup.EnsureRuntimeDefaults();
            return save;
        }

        private static CheeseTamaSaveData CreateEndgameSave()
        {
            var save = CreateRouteReadySave();
            save.cheeseTama.stats.affection = 100;
            save.careHistory.totalCareActions = 200;
            save.careHistory.cookings = 30;
            save.careHistory.playSessions = 30;
            save.careHistory.petSessions = 20;
            save.careHistory.cleanings = 20;
            save.careHistory.rests = 20;

            for (var index = 0; index < 15; index += 1)
            {
                save.collections.events.Add($"event_{index}");
            }

            save.randomEvents.history.Add(new RandomEventHistorySaveEntry
            {
                eventId = "ambient_history",
                totalOccurrences = 30
            });
            save.fantasyPowder.attemptCount = 30;
            save.fantasyPowder.discoveredHiddenRecipeIds.Add("recipe_a");
            save.fantasyPowder.discoveredHiddenRecipeIds.Add("recipe_b");
            save.fantasyPowder.discoveredHiddenRecipeIds.Add("recipe_c");
            return save;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }

        private static Text CreateText(string name, Transform parent)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            value.transform.SetParent(parent, false);
            return value.GetComponent<Text>();
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            value.transform.SetParent(parent, false);
            return value.GetComponent<Button>();
        }
    }
}
