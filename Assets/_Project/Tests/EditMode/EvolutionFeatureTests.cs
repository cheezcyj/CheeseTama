using System;
using System.Collections.Generic;
using CheeseTama.Data;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class EvolutionFeatureTests
    {
        [Test]
        public void NormalEvolutionCatalogExposesSixStableVisitorFacingEntries()
        {
            Assert.That(EvolutionSystem.NormalEvolutions.Count, Is.EqualTo(6));

            var ids = new HashSet<string>();
            foreach (var profile in EvolutionSystem.NormalEvolutions)
            {
                Assert.That(ids.Add(profile.Id), Is.True, $"Duplicate evolution id: {profile.Id}");
                Assert.That(profile.DisplayName, Is.Not.Empty);
                Assert.That(profile.Description, Is.Not.Empty);
                Assert.That(profile.TendencyHint, Is.Not.Empty);
                Assert.That(profile.PrimaryMilkId, Is.Not.Empty);
                Assert.That(EvolutionSystem.FindNormalEvolution(profile.Id), Is.SameAs(profile));
            }

            Assert.That(EvolutionSystem.FindNormalEvolution("missing"), Is.Null);
        }

        [TestCase(EvolutionSystem.CreamEvolutionId)]
        [TestCase(EvolutionSystem.CheddarEvolutionId)]
        [TestCase(EvolutionSystem.RicottaEvolutionId)]
        [TestCase(EvolutionSystem.MozzarellaEvolutionId)]
        [TestCase(EvolutionSystem.BlueEvolutionId)]
        [TestCase(EvolutionSystem.CoffeeEvolutionId)]
        public void PlanSignalsResolveToTheirMatchingEvolution(string expectedEvolutionId)
        {
            var tama = CreateLevelTwentyOneTama();
            var growth = new List<MilkGrowthSaveEntry>();
            var history = new CareHistorySaveData();
            ConfigureStrongSignal(expectedEvolutionId, tama, growth, history);

            var result = new EvolutionSystem().ResolveNormalEvolution(tama, growth, history);

            Assert.That(result.HasEvolution, Is.True);
            Assert.That(result.EvolutionId, Is.EqualTo(expectedEvolutionId));
            Assert.That(result.DisplayName, Is.Not.Empty);
            Assert.That(result.Score, Is.GreaterThan(0));
        }

        [Test]
        public void TendencyCanBePreviewedBeforeLevelGateButCannotBeApplied()
        {
            var tama = CreateLevelTwentyOneTama();
            tama.level = EvolutionSystem.NormalEvolutionLevel - 1;
            var growth = MaxGrowth(MilkCatalog.NuttyMilkId);
            var system = new EvolutionSystem();

            var preview = system.EvaluateNormalEvolution(tama, growth, new CareHistorySaveData());
            var resolved = system.ResolveNormalEvolution(tama, growth, new CareHistorySaveData());
            var applied = system.TryApplyNormalEvolution(tama, growth, new CareHistorySaveData(), out _);

            Assert.That(preview.EvolutionId, Is.EqualTo(EvolutionSystem.CheddarEvolutionId));
            Assert.That(preview.TendencyHint, Is.Not.Empty);
            Assert.That(resolved.HasEvolution, Is.False);
            Assert.That(applied, Is.False);
            Assert.That(tama.evolutionId, Is.Empty);
        }

        [Test]
        public void LevelTwentyOneAppliesOnceAndPreservesExistingEvolution()
        {
            var tama = CreateLevelTwentyOneTama();
            var growth = MaxGrowth(MilkCatalog.CoffeeMilkId);
            var system = new EvolutionSystem();

            var firstApplied = system.TryApplyNormalEvolution(
                tama,
                growth,
                new CareHistorySaveData(),
                out var firstResult);

            Assert.That(firstApplied, Is.True);
            Assert.That(firstResult.EvolutionId, Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));
            Assert.That(tama.evolutionId, Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));
            Assert.That(tama.form, Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));

            var secondApplied = system.TryApplyNormalEvolution(
                tama,
                MaxGrowth(MilkCatalog.FermentedMilkId),
                new CareHistorySaveData { cleanings = 100 },
                out var secondResult);

            Assert.That(secondApplied, Is.False);
            Assert.That(secondResult.HasEvolution, Is.False);
            Assert.That(tama.evolutionId, Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));
            Assert.That(tama.form, Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));
        }

        [Test]
        public void ExactScoreTieUsesStableCatalogOrderAndIgnoresGrowthListOrder()
        {
            var tama = CreateLevelTwentyOneTama();
            tama.stats = null;
            tama.growthHistory = null;
            var firstOrder = new List<MilkGrowthSaveEntry>
            {
                new MilkGrowthSaveEntry { milkId = MilkCatalog.WarmMilkId },
                new MilkGrowthSaveEntry { milkId = MilkCatalog.NuttyMilkId }
            };
            var reverseOrder = new List<MilkGrowthSaveEntry>(firstOrder);
            reverseOrder.Reverse();
            var system = new EvolutionSystem();

            var first = system.ResolveNormalEvolution(tama, firstOrder, null);
            var second = system.ResolveNormalEvolution(tama, reverseOrder, null);

            Assert.That(first.Score, Is.Zero);
            Assert.That(first.EvolutionId, Is.EqualTo(EvolutionSystem.CreamEvolutionId));
            Assert.That(second.EvolutionId, Is.EqualTo(first.EvolutionId));
        }

        [Test]
        public void MalformedAndOversizedSavedValuesAreBoundedAndDoNotMutateInput()
        {
            var tama = CreateLevelTwentyOneTama();
            tama.stats.affection = int.MaxValue;
            var entry = new MilkGrowthSaveEntry
            {
                milkId = MilkCatalog.WarmMilkId,
                growthLevel = int.MaxValue,
                growthPoints = int.MaxValue
            };
            var history = new CareHistorySaveData { petSessions = int.MaxValue };

            var result = new EvolutionSystem().ResolveNormalEvolution(
                tama,
                new List<MilkGrowthSaveEntry> { null, entry },
                history);

            Assert.That(result.EvolutionId, Is.EqualTo(EvolutionSystem.CreamEvolutionId));
            Assert.That(result.Score, Is.LessThan(1000));
            Assert.That(entry.growthLevel, Is.EqualTo(int.MaxValue));
            Assert.That(entry.growthPoints, Is.EqualTo(int.MaxValue));
            Assert.That(history.petSessions, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void LegacyEvolutionDefinitionApiRemainsUsable()
        {
            var definition = ScriptableObject.CreateInstance<EvolutionDefinition>();
            try
            {
                definition.id = "legacy_evolution";
                definition.requirements = new EvolutionRequirement { cheeseTamaLevel = 3 };
                var tama = new CheeseTamaModel { level = 3 };
                var system = new EvolutionSystem();

                Assert.That(system.CanUseEvolution(tama, new UnlockSaveData(), definition), Is.True);
                Assert.That(system.TryApplyEvolution(tama, new UnlockSaveData(), definition), Is.True);
                Assert.That(tama.evolutionId, Is.EqualTo("legacy_evolution"));
                Assert.That(tama.form, Is.EqualTo("legacy_evolution"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static CheeseTamaModel CreateLevelTwentyOneTama()
        {
            var tama = new CheeseTamaModel
            {
                level = EvolutionSystem.NormalEvolutionLevel,
                isHatched = true,
                evolutionId = string.Empty
            };
            tama.growthHistory.mostUsedMilkId = string.Empty;
            tama.growthHistory.mostUsedIngredientId = string.Empty;
            return tama;
        }

        private static List<MilkGrowthSaveEntry> MaxGrowth(string milkId)
        {
            return new List<MilkGrowthSaveEntry>
            {
                new MilkGrowthSaveEntry
                {
                    milkId = milkId,
                    growthLevel = MilkCatalog.MainMilkMaxGrowthLevel,
                    growthPoints = 100
                }
            };
        }

        private static void ConfigureStrongSignal(
            string evolutionId,
            CheeseTamaModel tama,
            List<MilkGrowthSaveEntry> growth,
            CareHistorySaveData history)
        {
            switch (evolutionId)
            {
                case EvolutionSystem.CreamEvolutionId:
                    growth.AddRange(MaxGrowth(MilkCatalog.WarmMilkId));
                    tama.growthHistory.mostUsedMilkId = MilkCatalog.WarmMilkId;
                    tama.stats.affection = 100;
                    history.petSessions = 100;
                    break;
                case EvolutionSystem.CheddarEvolutionId:
                    growth.AddRange(MaxGrowth(MilkCatalog.NuttyMilkId));
                    tama.growthHistory.mostUsedMilkId = MilkCatalog.NuttyMilkId;
                    tama.stats.mood = 100;
                    history.playSessions = 100;
                    break;
                case EvolutionSystem.RicottaEvolutionId:
                    growth.AddRange(MaxGrowth(MilkCatalog.BasicMilkId));
                    tama.growthHistory.mostUsedMilkId = MilkCatalog.BasicMilkId;
                    tama.growthHistory.mostUsedIngredientId = "recipe_fermented_yogurt_bowl";
                    history.cookings = 100;
                    history.snacksFed = 100;
                    break;
                case EvolutionSystem.MozzarellaEvolutionId:
                    growth.AddRange(MaxGrowth(MilkCatalog.BasicMilkId));
                    tama.growthHistory.mostUsedMilkId = MilkCatalog.BasicMilkId;
                    tama.stats.hunger = 80;
                    tama.stats.mood = 75;
                    tama.stats.cleanliness = 85;
                    tama.stats.sleepiness = 20;
                    tama.stats.health = 90;
                    break;
                case EvolutionSystem.BlueEvolutionId:
                    growth.AddRange(MaxGrowth(MilkCatalog.FermentedMilkId));
                    tama.growthHistory.mostUsedMilkId = MilkCatalog.FermentedMilkId;
                    tama.stats.maturation = 100;
                    history.cleanings = 100;
                    break;
                case EvolutionSystem.CoffeeEvolutionId:
                    growth.AddRange(MaxGrowth(MilkCatalog.CoffeeMilkId));
                    tama.growthHistory.mostUsedMilkId = MilkCatalog.CoffeeMilkId;
                    tama.growthHistory.mostUsedIngredientId = "recipe_coffee_milk_jelly";
                    tama.stats.sleepiness = 0;
                    history.rests = 100;
                    history.waitHours = 100;
                    history.lastCareActionAtIso = new DateTimeOffset(2026, 8, 13, 23, 0, 0, TimeSpan.FromHours(9)).ToString("O");
                    break;
                default:
                    Assert.Fail($"Unknown evolution id: {evolutionId}");
                    break;
            }
        }
    }
}
