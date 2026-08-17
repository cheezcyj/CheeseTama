using System;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class StarGenerationFlowTests
    {
        private static readonly DateTimeOffset FixedNow =
            new DateTimeOffset(2026, 8, 14, 18, 30, 0, TimeSpan.FromHours(9));

        [Test]
        public void DomainRequiresUnlockedRouteAndCreatesANewResetTamaWithoutMutatingTheOldOne()
        {
            var system = new StarEggEmmentalEvolutionSystem();
            var current = CreateCompletedTama();
            var unlocks = new UnlockSaveData();
            var state = CreateUsedGenerationState();

            Assert.That(
                system.EvaluateNewGenerationEligibility(current, unlocks, state),
                Is.EqualTo(StarEggGenerationEligibilityStatus.StarRouteLocked));

            var lockedAttempt = system.TryBeginStarEggGeneration(
                current,
                unlocks,
                state,
                "ct_star_generation_0001",
                FixedNow);

            Assert.That(lockedAttempt.status, Is.EqualTo(StarEggGenerationStartStatus.StarRouteLocked));
            Assert.That(current.id, Is.EqualTo("ct_previous_generation"));
            Assert.That(current.level, Is.EqualTo(UnlockSystem.MaxLevel));
            Assert.That(state.starMilkCareCount, Is.EqualTo(41));
            Assert.That(state.maturationCycle.completedCycles, Is.EqualTo(3));

            unlocks.starEggUnlocked = true;
            unlocks.starMilkUnlocked = true;
            unlocks.fantasyPowderEnabled = true;
            Assert.That(
                system.EvaluateNewGenerationEligibility(current, unlocks, state),
                Is.EqualTo(StarEggGenerationEligibilityStatus.Eligible));

            var result = system.TryBeginStarEggGeneration(
                current,
                unlocks,
                state,
                "ct_star_generation_0001",
                FixedNow);

            Assert.That(result.applied, Is.True);
            Assert.That(result.previousTamaId, Is.EqualTo(current.id));
            Assert.That(result.generationNumber, Is.EqualTo(1));
            Assert.That(result.nextTama, Is.Not.SameAs(current));
            Assert.That(result.nextTama.id, Is.EqualTo("ct_star_generation_0001"));
            Assert.That(result.nextTama.name, Is.EqualTo("모짜"));
            Assert.That(result.nextTama.hasCustomName, Is.True);
            Assert.That(result.nextTama.eggType, Is.EqualTo(StarEggEmmentalEvolutionSystem.StarEggTypeId));
            Assert.That(result.nextTama.isHatched, Is.False);
            Assert.That(result.nextTama.level, Is.EqualTo(1));
            Assert.That(result.nextTama.levelProgress, Is.Zero);
            Assert.That(result.nextTama.form, Is.EqualTo("egg"));
            Assert.That(result.nextTama.evolutionId, Is.Empty);
            Assert.That(result.nextTama.stats.hunger, Is.EqualTo(80));
            Assert.That(result.nextTama.stats.affection, Is.EqualTo(10));
            Assert.That(result.nextTama.growthHistory.sameMilkFeedStreak, Is.Zero);

            Assert.That(current.level, Is.EqualTo(UnlockSystem.MaxLevel));
            Assert.That(current.evolutionId, Is.EqualTo("cream_cheesetama"));
            Assert.That(state.starRoutePermanentlyUnlocked, Is.True);
            Assert.That(state.currentGenerationTamaId, Is.EqualTo(result.nextTama.id));
            Assert.That(state.currentGenerationStartedAtIso, Is.EqualTo(FixedNow.ToString("O")));
            Assert.That(state.starMilkCareCount, Is.Zero);
            Assert.That(state.fantasyResonance, Is.Zero);
            Assert.That(state.emmentalEvolutionUnlocked, Is.False);
            Assert.That(state.emmentalEvolutionAtIso, Is.Empty);
            Assert.That(state.appliedEvolutionReceiptKeys, Is.Empty);
            Assert.That(state.maturationCycle.progress, Is.Zero);
            Assert.That(state.maturationCycle.completedCycles, Is.Zero);
            Assert.That(state.maturationCycle.pendingRewards, Is.Empty);
            Assert.That(unlocks.starEggUnlocked, Is.True);
            Assert.That(unlocks.starMilkUnlocked, Is.True);
            Assert.That(unlocks.fantasyPowderEnabled, Is.True);
            Assert.That(
                system.EvaluateNewGenerationEligibility(result.nextTama, unlocks, state),
                Is.EqualTo(StarEggGenerationEligibilityStatus.AlreadyStarEggGeneration));
        }

        [Test]
        public void GameManagerPreservesAccountDataAndResetsOnlyCurrentTamaAndStarGenerationSignals()
        {
            using var fixture = IsolatedGameManagerFixture.Create("preservation");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            ConfigureUnlockedCompletedSave(save);

            var result = fixture.Manager.BeginStarEggGeneration(
                "ct_star_generation_preserved",
                FixedNow);

            Assert.That(result.applied, Is.True);
            Assert.That(fixture.Manager.CurrentTama, Is.SameAs(result.nextTama));
            Assert.That(fixture.Manager.CurrentTama.name, Is.EqualTo("모짜"));
            Assert.That(fixture.Manager.CurrentTama.level, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentTama.isHatched, Is.False);
            Assert.That(fixture.Manager.CurrentTama.eggType,
                Is.EqualTo(StarEggEmmentalEvolutionSystem.StarEggTypeId));
            Assert.That(save.growthMilestone.acknowledgedStage,
                Is.EqualTo(CheeseTamaGrowthStage.Egg));
            Assert.That(save.evolutionMilestone.acknowledgedEvolutionId, Is.Empty);

            Assert.That(save.economy.milkCoins, Is.EqualTo(321));
            Assert.That(save.economy.milkDrops, Is.EqualTo(87));
            Assert.That(save.economy.starDrops, Is.EqualTo(9));
            Assert.That(save.collections.events, Does.Contain("preserved_collection_event"));
            Assert.That(save.settings.masterVolume, Is.EqualTo(0.4f));
            Assert.That(save.settings.uiScale, Is.EqualTo(1.1f));
            Assert.That(save.milkGrowth, Has.Count.EqualTo(1));
            Assert.That(save.milkGrowth[0].milkId, Is.EqualTo("preserved_milk"));
            Assert.That(save.milkGrowth[0].growthLevel, Is.EqualTo(5));
            Assert.That(save.milkGrowth[0].growthPoints, Is.EqualTo(44));

            Assert.That(save.starLegacy.starRoutePermanentlyUnlocked, Is.True);
            Assert.That(save.starLegacy.starMilkCareCount, Is.Zero);
            Assert.That(save.starLegacy.fantasyResonance, Is.Zero);
            Assert.That(save.starLegacy.emmentalEvolutionUnlocked, Is.False);
            Assert.That(save.starLegacy.appliedEvolutionReceiptKeys, Is.Empty);
            Assert.That(save.starLegacy.maturationCycle.completedCycles, Is.Zero);
            Assert.That(save.unlocks.starEggUnlocked, Is.True);
            Assert.That(save.unlocks.starMilkUnlocked, Is.True);
            Assert.That(save.unlocks.fantasyPowderEnabled, Is.True);

            Assert.That(fixture.Manager.RefreshMilkUnlocks(), Is.False);
            Assert.That(save.unlocks.starEggUnlocked, Is.True);
            Assert.That(save.unlocks.starMilkUnlocked, Is.True);
            Assert.That(fixture.Manager.GetStarRouteProgress().unlocked, Is.True);
        }

        [Test]
        public void NewGenerationAndDurableRouteSurviveManagerRecreationAndRepairTransientUnlockFlags()
        {
            using var fixture = IsolatedGameManagerFixture.Create("reload");
            fixture.Manager.LoadOrCreateGame();
            ConfigureUnlockedCompletedSave(fixture.Manager.CurrentSave);

            Assert.That(
                fixture.Manager.BeginStarEggGeneration("ct_star_generation_reload", FixedNow).applied,
                Is.True);

            fixture.Manager.CurrentSave.unlocks.starEggUnlocked = false;
            fixture.Manager.CurrentSave.unlocks.starMilkUnlocked = false;
            fixture.Manager.CurrentSave.unlocks.fantasyPowderEnabled = false;
            fixture.Manager.SaveGame();

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            var loaded = fixture.Manager.CurrentSave;

            Assert.That(loaded.cheeseTama.id, Is.EqualTo("ct_star_generation_reload"));
            Assert.That(loaded.cheeseTama.name, Is.EqualTo("모짜"));
            Assert.That(loaded.cheeseTama.hasCustomName, Is.True);
            Assert.That(loaded.cheeseTama.eggType,
                Is.EqualTo(StarEggEmmentalEvolutionSystem.StarEggTypeId));
            Assert.That(loaded.cheeseTama.level, Is.EqualTo(1));
            Assert.That(loaded.starLegacy.starRoutePermanentlyUnlocked, Is.True);
            Assert.That(loaded.starLegacy.starEggGenerationCount, Is.EqualTo(1));
            Assert.That(loaded.starLegacy.currentGenerationTamaId,
                Is.EqualTo("ct_star_generation_reload"));
            Assert.That(loaded.starLegacy.starMilkCareCount, Is.Zero);
            Assert.That(loaded.starLegacy.fantasyResonance, Is.Zero);
            Assert.That(loaded.unlocks.starEggUnlocked, Is.True);
            Assert.That(loaded.unlocks.starMilkUnlocked, Is.True);
            Assert.That(loaded.unlocks.fantasyPowderEnabled, Is.True);
            Assert.That(loaded.economy.milkCoins, Is.EqualTo(321));
            Assert.That(loaded.collections.events, Does.Contain("preserved_collection_event"));
            Assert.That(loaded.settings.masterVolume, Is.EqualTo(0.4f));
            Assert.That(loaded.milkGrowth, Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.GetStarRouteProgress().unlocked, Is.True);
        }

        private static CheeseTamaModel CreateCompletedTama()
        {
            return new CheeseTamaModel
            {
                id = "ct_previous_generation",
                name = "모짜",
                hasCustomName = true,
                eggType = "cream_egg",
                isHatched = true,
                level = UnlockSystem.MaxLevel,
                levelProgress = 77,
                maxLevel = UnlockSystem.MaxLevel,
                form = "cream_cheesetama",
                evolutionId = "cream_cheesetama",
                stats = new CheeseTama.Gameplay.Stats.StatBlock
                {
                    hunger = 1,
                    mood = 2,
                    cleanliness = 3,
                    sleepiness = 99,
                    health = 4,
                    maturation = 100,
                    affection = 88,
                    milkSatisfaction = 5,
                    overfullness = 77
                },
                growthHistory = new GrowthHistory
                {
                    mostUsedMilkId = "cream_milk",
                    mostUsedIngredientId = "berry",
                    careStyle = "lively",
                    lastFedMilkId = "cream_milk",
                    sameMilkFeedStreak = 12
                }
            };
        }

        private static StarLegacySaveData CreateUsedGenerationState()
        {
            return new StarLegacySaveData
            {
                starMilkCareCount = 41,
                fantasyResonance = 29,
                emmentalEvolutionUnlocked = true,
                emmentalEvolutionAtIso = FixedNow.AddDays(-1).ToString("O"),
                appliedEvolutionReceiptKeys = new System.Collections.Generic.List<string>
                {
                    "old_evolution_receipt"
                },
                maturationCycle = new FinalMaturationCycleSaveData
                {
                    progress = 72,
                    completedCycles = 3,
                    claimedCycles = 2,
                    pendingRewards = new System.Collections.Generic.List<FinalMaturationRewardSaveEntry>
                    {
                        new FinalMaturationRewardSaveEntry
                        {
                            rewardId = "final_maturation_00000003",
                            cycleNumber = 3,
                            milkCoins = 60,
                            milkDrops = 10
                        }
                    }
                }
            };
        }

        private static void ConfigureUnlockedCompletedSave(CheeseTamaSaveData save)
        {
            save.cheeseTama = CreateCompletedTama();
            save.unlocks.starEggUnlocked = true;
            save.unlocks.starMilkUnlocked = true;
            save.unlocks.fantasyPowderEnabled = true;
            save.starRoute.unlockAcknowledged = true;
            save.starRoute.unlockedAtIso = FixedNow.AddDays(-2).ToString("O");
            save.starLegacy = CreateUsedGenerationState();
            save.economy.milkCoins = 321;
            save.economy.milkDrops = 87;
            save.economy.starDrops = 9;
            save.economy.collectionFragments = 6;
            save.collections.events.Add("preserved_collection_event");
            save.settings.masterVolume = 0.4f;
            save.settings.musicVolume = 0.3f;
            save.settings.effectVolume = 0.2f;
            save.settings.uiScale = 1.1f;
            save.milkGrowth.Clear();
            save.milkGrowth.Add(new MilkGrowthSaveEntry
            {
                milkId = "preserved_milk",
                growthLevel = 5,
                growthPoints = 44
            });
        }

        private sealed class IsolatedGameManagerFixture : IDisposable
        {
            private readonly GameObject root;

            private IsolatedGameManagerFixture(
                GameObject root,
                SaveManager saveManager,
                GameManager manager)
            {
                this.root = root;
                SaveManager = saveManager;
                Manager = manager;
            }

            public SaveManager SaveManager { get; }
            public GameManager Manager { get; private set; }

            public static IsolatedGameManagerFixture Create(string label)
            {
                var root = new GameObject($"{label} Star Generation Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                var manager = root.AddComponent<GameManager>();
                SetPrivateField(
                    saveManager,
                    "saveFileName",
                    $"cheesetama_star_generation_test_{label}_{Guid.NewGuid():N}.json");
                SetPrivateField(manager, "saveManager", saveManager);
                return new IsolatedGameManagerFixture(root, saveManager, manager);
            }

            public void RecreateManager()
            {
                UnityEngine.Object.DestroyImmediate(Manager);
                Manager = root.AddComponent<GameManager>();
                SetPrivateField(Manager, "saveManager", SaveManager);
            }

            public void Dispose()
            {
                SaveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(root);
            }

            private static void SetPrivateField(object target, string fieldName, object value)
            {
                var field = target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, fieldName);
                field.SetValue(target, value);
            }
        }
    }
}
