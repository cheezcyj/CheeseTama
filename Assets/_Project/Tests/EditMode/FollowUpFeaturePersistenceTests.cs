using System;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Deliveries;
using CheeseTama.Gameplay.HiddenRecipes;
using CheeseTama.Gameplay.Journey;
using CheeseTama.Gameplay.Memories;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class FollowUpFeaturePersistenceTests
    {
        private static readonly DateTimeOffset FixedNow =
            new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.FromHours(9));

        [Test]
        public void DefaultSaveStartsFirstDayJourneyPendingForANewPlayer()
        {
            var save = SaveManager.CreateDefaultSave();

            Assert.That(save.firstDayJourney, Is.Not.Null);
            Assert.That(save.firstDayJourney.legacySuppressed, Is.False);
            Assert.That(save.firstDayJourney.introShown, Is.False);
            Assert.That(save.firstDayJourney.completed, Is.False);
            Assert.That(save.firstDayJourney.rewardClaimed, Is.False);
            Assert.That(save.firstDayJourney.completedTaskIds, Is.Empty);
        }

        [Test]
        public void LegacySaveWithoutFirstDayJourneyIsSuppressedAndMigrationSurvivesReload()
        {
            using var fixture = IsolatedGameManagerFixture.Create("legacy_first_day");
            fixture.WriteRawJson("{\"version\":\"0.1.0\",\"playerId\":\"legacy_player\"}");

            fixture.Manager.LoadOrCreateGame();

            AssertLegacyJourneySuppressed(fixture.Manager.CurrentSave.firstDayJourney);
            Assert.That(fixture.SaveManager.LastLoadMigratedData, Is.True);

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            AssertLegacyJourneySuppressed(fixture.Manager.CurrentSave.firstDayJourney);
        }

        [Test]
        public void FirstDayActionsCollectionAndSingleRewardSurviveManagerRecreation()
        {
            using var fixture = IsolatedGameManagerFixture.Create("first_day_reload");
            fixture.Manager.LoadOrCreateGame();

            fixture.Manager.RegisterCareAction("play");
            fixture.Manager.RegisterCareAction("cook");
            fixture.Manager.RegisterCareAction("feed_milk");
            fixture.Manager.RegisterCareAction("clean");
            fixture.Manager.RegisterCareAction("rest");
            Assert.That(fixture.Manager.RecordFirstDayJourneyCollectionOpened(), Is.True);

            var journey = fixture.Manager.CurrentSave.firstDayJourney;
            Assert.That(journey.completed, Is.True);
            Assert.That(
                FirstDayJourneySystem.CountCompletedTasks(journey),
                Is.EqualTo(FirstDayJourneySystem.Tasks.Count));

            var coinsBefore = fixture.Manager.CurrentSave.economy.milkCoins;
            var dropsBefore = fixture.Manager.CurrentSave.economy.milkDrops;
            var fragmentsBefore = fixture.Manager.CurrentSave.economy.collectionFragments;
            var firstClaim = fixture.Manager.ClaimFirstDayJourneyReward();

            Assert.That(firstClaim.Granted, Is.True);
            Assert.That(
                fixture.Manager.CurrentSave.economy.milkCoins,
                Is.EqualTo(coinsBefore + FirstDayJourneySystem.RewardMilkCoins));
            Assert.That(
                fixture.Manager.CurrentSave.economy.milkDrops,
                Is.EqualTo(dropsBefore + FirstDayJourneySystem.RewardMilkDrops));
            Assert.That(
                fixture.Manager.CurrentSave.economy.collectionFragments,
                Is.EqualTo(fragmentsBefore + FirstDayJourneySystem.RewardCollectionFragments));

            var paidCoins = fixture.Manager.CurrentSave.economy.milkCoins;
            var paidDrops = fixture.Manager.CurrentSave.economy.milkDrops;
            var paidFragments = fixture.Manager.CurrentSave.economy.collectionFragments;
            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            journey = fixture.Manager.CurrentSave.firstDayJourney;
            Assert.That(journey.completed, Is.True);
            Assert.That(journey.rewardClaimed, Is.True);
            Assert.That(
                FirstDayJourneySystem.CountCompletedTasks(journey),
                Is.EqualTo(FirstDayJourneySystem.Tasks.Count));

            var duplicateClaim = fixture.Manager.ClaimFirstDayJourneyReward();
            Assert.That(duplicateClaim.Granted, Is.False);
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.EqualTo(paidCoins));
            Assert.That(fixture.Manager.CurrentSave.economy.milkDrops, Is.EqualTo(paidDrops));
            Assert.That(
                fixture.Manager.CurrentSave.economy.collectionFragments,
                Is.EqualTo(paidFragments));
        }

        [Test]
        public void CheeseStarDeliveryClaimSurvivesSaveManagerReloadAndRejectsSameDayDuplicate()
        {
            using var fixture = IsolatedGameManagerFixture.Create("delivery_reload");
            var save = SaveManager.CreateDefaultSave();

            Assert.That(
                CheeseStarDeliverySystem.TryClaim(
                    save.cheeseStarDelivery,
                    false,
                    FixedNow,
                    out var firstClaim),
                Is.True);
            Assert.That(firstClaim.Status, Is.EqualTo(CheeseStarDeliveryClaimStatus.Claimed));
            fixture.SaveManager.Save(save);

            var loaded = fixture.SaveManager.LoadOrCreate();
            Assert.That(loaded.cheeseStarDelivery.lastClaimedDateKey, Is.EqualTo("2026-08-14"));
            Assert.That(loaded.cheeseStarDelivery.currentStreakDays, Is.EqualTo(1));
            Assert.That(loaded.cheeseStarDelivery.totalClaims, Is.EqualTo(1));

            Assert.That(
                CheeseStarDeliverySystem.TryClaim(
                    loaded.cheeseStarDelivery,
                    false,
                    FixedNow.AddHours(8),
                    out var duplicateClaim),
                Is.False);
            Assert.That(
                duplicateClaim.Status,
                Is.EqualTo(CheeseStarDeliveryClaimStatus.AlreadyClaimed));
            Assert.That(loaded.cheeseStarDelivery.totalClaims, Is.EqualTo(1));
        }

        [Test]
        public void MemoryJournalRecordSurvivesReloadAndKeepsItsIdempotencyKey()
        {
            using var fixture = IsolatedGameManagerFixture.Create("memory_reload");
            var save = SaveManager.CreateDefaultSave();
            var system = new MemoryJournalSystem();

            Assert.That(
                system.TryRecordReturn(
                    save.memoryJournal,
                    "return_followup_001",
                    95,
                    FixedNow,
                    "모짜",
                    "soft_cheesetama",
                    out var recorded),
                Is.True);
            fixture.SaveManager.Save(save);

            var loaded = fixture.SaveManager.LoadOrCreate();
            Assert.That(loaded.memoryJournal.entries, Has.Count.EqualTo(1));
            Assert.That(loaded.memoryJournal.entries[0].id, Is.EqualTo(recorded.id));
            Assert.That(
                loaded.memoryJournal.entries[0].idempotencyKey,
                Is.EqualTo(recorded.idempotencyKey));

            Assert.That(
                system.TryRecordReturn(
                    loaded.memoryJournal,
                    "return_followup_001",
                    180,
                    FixedNow.AddHours(3),
                    "모짜",
                    "soft_cheesetama",
                    out _),
                Is.False);
            Assert.That(loaded.memoryJournal.entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void FantasyPowderSuccessAndReceiptSurviveReloadWithoutDuplicateReward()
        {
            using var fixture = IsolatedGameManagerFixture.Create("fantasy_powder_reload");
            var save = SaveManager.CreateDefaultSave();
            var system = new FantasyPowderHiddenRecipeSystem();
            var recipe = FantasyPowderHiddenRecipeCatalog.CreamCloudDough;
            const string receiptKey = "fantasy_followup_receipt_001";

            save.unlocks.starMilkUnlocked = true;
            save.unlocks.fantasyPowderEnabled = true;
            var unlockedSnapshot = system.BuildSnapshot(save.unlocks, save.fantasyPowder);
            Assert.That(unlockedSnapshot.visible, Is.True);
            Assert.That(system.GrantPowder(save.fantasyPowder, 2), Is.EqualTo(2));

            var starDropsBefore = save.economy.starDrops;
            var firstAttempt = system.TryAttempt(
                save.unlocks,
                save.fantasyPowder,
                save.snackInventory,
                save.economy,
                recipe.id,
                receiptKey,
                successRoll: 0d);

            Assert.That(firstAttempt.status, Is.EqualTo(FantasyPowderAttemptStatus.AppliedSuccess));
            Assert.That(firstAttempt.success, Is.True);
            Assert.That(firstAttempt.newDiscovery, Is.True);
            Assert.That(save.fantasyPowder.powderQuantity, Is.EqualTo(1));
            Assert.That(save.fantasyPowder.attemptCount, Is.EqualTo(1));
            Assert.That(save.fantasyPowder.HasDiscovered(recipe.id), Is.True);
            Assert.That(save.fantasyPowder.HasAppliedReceipt(receiptKey), Is.True);
            Assert.That(
                FindSnackQuantity(save, recipe.resultSnackId),
                Is.EqualTo(recipe.resultSnackQuantity));
            Assert.That(
                save.economy.starDrops,
                Is.EqualTo(starDropsBefore + recipe.successStarDrops));
            fixture.SaveManager.Save(save);

            var loaded = fixture.SaveManager.LoadOrCreate();
            Assert.That(system.BuildSnapshot(loaded.unlocks, loaded.fantasyPowder).visible, Is.True);
            Assert.That(loaded.fantasyPowder.powderQuantity, Is.EqualTo(1));
            Assert.That(loaded.fantasyPowder.attemptCount, Is.EqualTo(1));
            Assert.That(loaded.fantasyPowder.HasDiscovered(recipe.id), Is.True);
            Assert.That(loaded.fantasyPowder.HasAppliedReceipt(receiptKey), Is.True);
            Assert.That(
                FindSnackQuantity(loaded, recipe.resultSnackId),
                Is.EqualTo(recipe.resultSnackQuantity));
            Assert.That(
                loaded.economy.starDrops,
                Is.EqualTo(starDropsBefore + recipe.successStarDrops));

            var stateBeforeDuplicate = JsonUtility.ToJson(loaded);
            var duplicateAttempt = system.TryAttempt(
                loaded.unlocks,
                loaded.fantasyPowder,
                loaded.snackInventory,
                loaded.economy,
                recipe.id,
                receiptKey,
                successRoll: 0d);

            Assert.That(
                duplicateAttempt.status,
                Is.EqualTo(FantasyPowderAttemptStatus.AlreadyApplied));
            Assert.That(duplicateAttempt.duplicateReceipt, Is.True);
            Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(stateBeforeDuplicate));
        }

        private static int FindSnackQuantity(CheeseTamaSaveData save, string snackId)
        {
            if (save?.snackInventory == null)
            {
                return 0;
            }

            for (var index = 0; index < save.snackInventory.Count; index += 1)
            {
                var entry = save.snackInventory[index];
                if (entry != null && string.Equals(entry.snackId, snackId, StringComparison.Ordinal))
                {
                    return entry.quantity;
                }
            }

            return 0;
        }

        private static void AssertLegacyJourneySuppressed(FirstDayJourneySaveData state)
        {
            Assert.That(state, Is.Not.Null);
            Assert.That(state.legacySuppressed, Is.True);
            Assert.That(state.introShown, Is.True);
            Assert.That(state.completed, Is.True);
            Assert.That(state.rewardClaimed, Is.True);
            Assert.That(FirstDayJourneySystem.ClaimCompletionReward(state).Granted, Is.False);
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
                var root = new GameObject($"{label} Follow-up Persistence Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                var manager = root.AddComponent<GameManager>();
                SetPrivateField(
                    saveManager,
                    "saveFileName",
                    $"cheesetama_followup_test_{label}_{Guid.NewGuid():N}.json");
                SetPrivateField(manager, "saveManager", saveManager);
                return new IsolatedGameManagerFixture(root, saveManager, manager);
            }

            public void WriteRawJson(string json)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SaveManager.SaveFilePath));
                File.WriteAllText(SaveManager.SaveFilePath, json);
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
