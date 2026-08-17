using System;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class IntegratedFeaturePersistenceTests
    {
        [Test]
        public void DecorationPurchaseAndEquipSurviveReload()
        {
            using var fixture = GameManagerFixture.Create("decoration_persistence");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.economy.milkCoins = 999;
            fixture.Manager.CurrentSave.economy.milkDrops = 99;
            fixture.Manager.SaveGame();

            Assert.That(fixture.Manager.TryPurchaseDecoration(DecorationCatalog.StarLampId).Succeeded, Is.True);
            Assert.That(fixture.Manager.TryEquipDecoration(DecorationCatalog.StarLampId).Succeeded, Is.True);
            var expectedCoins = fixture.Manager.CurrentSave.economy.milkCoins;
            var expectedDrops = fixture.Manager.CurrentSave.economy.milkDrops;

            fixture.Manager.ReloadGame();
            var snapshot = fixture.Manager.GetDecorationShopSnapshot();
            Assert.That(snapshot.Owns(DecorationCatalog.StarLampId), Is.True);
            Assert.That(snapshot.equippedAccentId, Is.EqualTo(DecorationCatalog.StarLampId));
            Assert.That(snapshot.milkCoins, Is.EqualTo(expectedCoins));
            Assert.That(snapshot.milkDrops, Is.EqualTo(expectedDrops));
        }

        [Test]
        public void MilkGrowthRewardClaimSurvivesReloadAndCannotPayTwice()
        {
            using var fixture = GameManagerFixture.Create("milk_reward_persistence");
            fixture.Manager.LoadOrCreateGame();

            fixture.Manager.RegisterMilkGrowth(MilkCatalog.BasicMilkId, 20);
            var coinsAfterFirst = fixture.Manager.CurrentSave.economy.milkCoins;
            var dropsAfterFirst = fixture.Manager.CurrentSave.economy.milkDrops;
            var keysAfterFirst = fixture.Manager.CurrentSave.claimedMilkGrowthRewardKeys.Count;
            Assert.That(keysAfterFirst, Is.GreaterThan(0));

            fixture.Manager.ReloadGame();
            fixture.Manager.RegisterMilkGrowth(MilkCatalog.BasicMilkId, 0);
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.EqualTo(coinsAfterFirst));
            Assert.That(fixture.Manager.CurrentSave.economy.milkDrops, Is.EqualTo(dropsAfterFirst));
            Assert.That(fixture.Manager.CurrentSave.claimedMilkGrowthRewardKeys.Count, Is.EqualTo(keysAfterFirst));
        }

        [Test]
        public void LegacyEvolutionMilestoneAcknowledgesExistingEvolution()
        {
            using var fixture = GameManagerFixture.Create("legacy_evolution_ack");
            var json = "{\"cheeseTama\":{\"level\":21,\"isHatched\":true,\"form\":\"cream_cheesetama\",\"evolutionId\":\"cream_cheesetama\"}}";
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.SaveManager.SaveFilePath));
            File.WriteAllText(fixture.SaveManager.SaveFilePath, json);

            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.evolutionMilestone.acknowledgedEvolutionId,
                Is.EqualTo("cream_cheesetama"));
            Assert.That(fixture.Manager.HasPendingEvolutionMilestone, Is.False);
        }

        private sealed class GameManagerFixture : IDisposable
        {
            private readonly GameObject root;
            private GameManagerFixture(GameObject root, SaveManager saveManager, GameManager manager)
            {
                this.root = root;
                SaveManager = saveManager;
                Manager = manager;
            }

            public SaveManager SaveManager { get; }
            public GameManager Manager { get; }

            public static GameManagerFixture Create(string label)
            {
                var root = new GameObject($"{label} Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                var manager = root.AddComponent<GameManager>();
                typeof(SaveManager).GetField("saveFileName", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(saveManager, $"cheesetama_integrated_test_{label}_{Guid.NewGuid():N}.json");
                typeof(GameManager).GetField("saveManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(manager, saveManager);
                return new GameManagerFixture(root, saveManager, manager);
            }

            public void Dispose()
            {
                SaveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
