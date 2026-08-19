using System;
using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Collections.HiddenCareers;
using CheeseTama.Data;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Care;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.HiddenRecipes;
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
        public void BenefitSetUsesOnlyKnownUnlockedCardsAndIgnoresDuplicateSaveEntries()
        {
            var collections = new CollectionSaveData();
            var acquiredAt = "2026-08-17T09:00:00+09:00";
            for (var index = 0; index < HiddenCareerCardCatalog.All.Count; index += 1)
            {
                collections.hiddenUnlockedOnly.Add(new HiddenCollectionSaveEntry
                {
                    id = HiddenCareerCardCatalog.All[index].Id,
                    acquiredAtIso = acquiredAt
                });
            }

            collections.hiddenUnlockedOnly.Add(new HiddenCollectionSaveEntry
            {
                id = HiddenCareerCardCatalog.ScientistId,
                acquiredAtIso = acquiredAt
            });
            collections.hiddenUnlockedOnly.Add(new HiddenCollectionSaveEntry
            {
                id = "future_hidden_career",
                acquiredAtIso = acquiredAt
            });

            var system = new HiddenCareerCardSystem();
            var benefits = system.GetBenefitSet(collections);

            Assert.That(benefits.RecipeHintProgress, Is.EqualTo(1));
            Assert.That(benefits.CollectionInterpretation, Is.EqualTo(1));
            Assert.That(benefits.RecoveryEffectPercent, Is.EqualTo(10));
            Assert.That(benefits.RandomEventWeightPercent, Is.EqualTo(10));
            Assert.That(benefits.NegativeEffectMitigationPercent, Is.EqualTo(15));
            Assert.That(benefits.RareByproductWeightPercent, Is.EqualTo(7));
            Assert.That(benefits.DeepLoreSignal, Is.EqualTo(1));
            Assert.That(system.GetUnlockedBenefits(collections), Has.Count.EqualTo(7));
            Assert.That(
                system.GetVisibleUnlockedCards(collections),
                Has.Count.EqualTo(7));
        }

        [Test]
        public void RecipeBenefitsUseDeterministicBoundariesWithoutPersistingDisplayHint()
        {
            var system = new FantasyPowderHiddenRecipeSystem();
            var unlocks = new UnlockSaveData
            {
                starMilkUnlocked = true,
                fantasyPowderEnabled = true
            };
            var hintState = new FantasyPowderSaveData
            {
                attemptCount = 2,
                pityHintLevel = 0
            };

            var baselineHint = system.BuildSnapshot(unlocks, hintState);
            var boostedHint = system.BuildSnapshot(unlocks, hintState, recipeHintProgress: 1);

            Assert.That(baselineHint.pityHintLevel, Is.Zero);
            Assert.That(boostedHint.pityHintLevel, Is.EqualTo(1));
            Assert.That(hintState.pityHintLevel, Is.Zero);
            Assert.That(
                FantasyPowderHiddenRecipeSystem.CalculateSuccessChance(7),
                Is.EqualTo(0.14d).Within(0.000001d));

            var baselineState = new FantasyPowderSaveData { powderQuantity = 1 };
            var baseline = system.TryAttempt(
                unlocks,
                baselineState,
                new List<SnackInventorySaveEntry>(),
                new EconomySaveData(),
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "career-baseline",
                0.10d);
            Assert.That(
                baseline.status,
                Is.EqualTo(FantasyPowderAttemptStatus.AppliedByproduct));

            var boostedState = new FantasyPowderSaveData { powderQuantity = 1 };
            var boostedInventory = new List<SnackInventorySaveEntry>();
            var boostedEconomy = new EconomySaveData();
            var boosted = system.TryAttempt(
                unlocks,
                boostedState,
                boostedInventory,
                boostedEconomy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "career-boosted",
                0.10d,
                rareByproductWeightPercent: 7);
            var duplicate = system.TryAttempt(
                unlocks,
                boostedState,
                boostedInventory,
                boostedEconomy,
                FantasyPowderHiddenRecipeCatalog.CreamCloudDoughId,
                "career-boosted",
                0d,
                rareByproductWeightPercent: 7);

            Assert.That(boosted.status, Is.EqualTo(FantasyPowderAttemptStatus.AppliedSuccess));
            Assert.That(duplicate.status, Is.EqualTo(FantasyPowderAttemptStatus.AlreadyApplied));
            Assert.That(boostedState.attemptCount, Is.EqualTo(1));
            Assert.That(boostedState.powderQuantity, Is.Zero);
        }

        [Test]
        public void CareAndEventBenefitsChangeOnlyTheirIntendedDeterministicEffects()
        {
            var baselineTama = new CheeseTamaModel();
            baselineTama.stats.health = 50;
            new CareActionSystem().Rest(baselineTama);

            var recoveryTama = new CheeseTamaModel();
            recoveryTama.stats.health = 50;
            var recoverySystem = new CareActionSystem();
            recoverySystem.ConfigureRecoveryEffectPercent(10);
            recoverySystem.Rest(recoveryTama);

            Assert.That(baselineTama.stats.health, Is.EqualTo(54));
            Assert.That(recoveryTama.stats.health, Is.EqualTo(55));

            var neutralTama = new CheeseTamaModel();
            neutralTama.stats.health = 50;
            neutralTama.stats.hunger = 50;
            neutralTama.stats.cleanliness = 50;
            neutralTama.stats.sleepiness = 50;
            neutralTama.stats.mood = 50;
            var randomEvents = new RandomEventSystem();
            var baselineEvent = randomEvents.RollCareEvent(
                neutralTama,
                conditionChanceRoll: 1f,
                ambientChanceRoll: 0.065f);
            var boostedEvent = randomEvents.RollCareEvent(
                neutralTama,
                conditionChanceRoll: 1f,
                ambientChanceRoll: 0.065f,
                randomEventWeightPercent: 10);

            Assert.That(baselineEvent.occurred, Is.False);
            Assert.That(boostedEvent.eventId, Is.EqualTo("quiet_hum"));

            var choiceTama = new CheeseTamaModel();
            choiceTama.stats.cleanliness = 50;
            var pending = randomEvents
                .ForceCareEvent("moldy_footprints_choice")
                .WithOccurrence("guardian-choice", false);
            var choiceSystem = new CareEventChoiceSystem();
            var mitigated = choiceSystem.ApplyChoice(
                pending,
                "follow_footprints",
                choiceTama,
                new EconomySaveData(),
                negativeEffectMitigationPercent: 15);
            var duplicate = choiceSystem.ApplyChoice(
                pending,
                "clean_footprints",
                choiceTama,
                new EconomySaveData(),
                negativeEffectMitigationPercent: 0);

            Assert.That(mitigated.effect.cleanliness, Is.EqualTo(-3));
            Assert.That(choiceTama.stats.cleanliness, Is.EqualTo(47));
            Assert.That(duplicate.status, Is.EqualTo(CareEventChoiceResolutionStatus.AlreadyApplied));
            Assert.That(duplicate.effect.cleanliness, Is.EqualTo(-3));
            Assert.That(choiceTama.stats.cleanliness, Is.EqualTo(47));
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
                Assert.That(controller.RenderedText, Does.Contain("효과 · 환상가루 조합 단서"));
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

        [Test]
        public void CollectionInsightsAppearOnlyAfterTheirKnownCardsAreUnlocked()
        {
            var host = new GameObject("Hidden Career Collection UI Test Host");
            try
            {
                var milk = CreateText("Milk", host.transform);
                var evolution = CreateText("Evolution", host.transform);
                var events = CreateText("Events", host.transform);
                var hidden = CreateText("Hidden", host.transform);
                var message = CreateText("Message", host.transform);
                var controller = host.AddComponent<CollectionUIController>();
                controller.Configure(milk, evolution, events, hidden, message);

                var save = new CheeseTamaSaveData();
                save.EnsureRuntimeDefaults();
                save.collections.milk.Add("basic_milk");
                save.collections.events.Add("daily_routine_complete");
                controller.Bind(save);

                Assert.That(milk.text, Does.Not.Contain("해석 ·"));
                Assert.That(hidden.text, Does.Not.Contain("심층 단서 ·"));

                var cardSystem = new HiddenCareerCardSystem();
                cardSystem.TryUnlockKnownCard(
                    save.collections,
                    HiddenCareerCardCatalog.TeacherId,
                    new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.FromHours(9)));
                controller.Bind(save);

                Assert.That(milk.text, Does.Contain("해석 ·"));
                Assert.That(hidden.text, Does.Not.Contain("심층 단서 ·"));

                cardSystem.TryUnlockKnownCard(
                    save.collections,
                    HiddenCareerCardCatalog.BlackStarObserverId,
                    new DateTimeOffset(2026, 8, 17, 10, 5, 0, TimeSpan.FromHours(9)));
                controller.Bind(save);

                Assert.That(hidden.text, Does.Contain("심층 단서 ·"));
                Assert.That(hidden.text, Does.Contain("선생님치즈타마"));
                Assert.That(hidden.text, Does.Contain("검은 별 관측자치즈타마"));
                Assert.That(hidden.text, Does.Not.Contain(HiddenCareerCardCatalog.TeacherId));
                Assert.That(hidden.text, Does.Not.Contain(HiddenCareerCardCatalog.BlackStarObserverId));
                Assert.That(hidden.text, Does.Not.Contain("조건"));
                Assert.That(hidden.text, Does.Not.Contain("???"));
                Assert.That(hidden.text, Does.Not.Contain("/7"));
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
