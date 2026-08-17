using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class NextFeaturePersistenceIntegrationTests
    {
        [Test]
        public void CompletedVersionTwoSetupAppliesFirstMilkExactlyOnceAcrossReload()
        {
            using var fixture = GameManagerFixture.Create("setup_outcome_once");
            var save = SaveManager.CreateDefaultSave();
            var setup = NewGameSetupSaveData.CreateForNewPlayer();

            Assert.That(NewGameSetupSystem.TrySelectEgg(
                setup,
                NewGameSetupCatalog.ButterEggId,
                out _), Is.True);
            Assert.That(NewGameSetupSystem.TryAdvance(setup, out _), Is.True);
            Assert.That(NewGameSetupSystem.TrySelectFirstMilk(
                setup,
                NewGameSetupCatalog.WarmFirstMilkId,
                out _), Is.True);
            Assert.That(NewGameSetupSystem.TryAdvance(setup, out _), Is.True);
            Assert.That(setup.schemaVersion, Is.EqualTo(NewGameSetupSaveData.CurrentSchemaVersion));
            Assert.That(setup.completed, Is.True);
            Assert.That(setup.outcomeApplied, Is.False);
            var expectedTemperament = setup.temperamentSeed.dominantTraitId;
            Assert.That(expectedTemperament, Is.Not.Empty);

            save.newGameSetup = setup;
            save.cheeseTama.growthHistory.careStyle = "pre_setup_style";
            fixture.WriteSave(save);
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.newGameSetup.outcomeApplied, Is.True);
            Assert.That(fixture.Manager.CurrentTama.eggType,
                Is.EqualTo(NewGameSetupCatalog.ButterEggId));
            Assert.That(fixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo(expectedTemperament));
            Assert.That(fixture.Manager.CurrentTama.stats.mood, Is.EqualTo(74));
            Assert.That(fixture.Manager.CurrentTama.stats.sleepiness, Is.EqualTo(17));
            Assert.That(FindMilkGrowth(
                fixture.Manager.CurrentSave,
                NewGameSetupCatalog.WarmFirstMilkId)?.growthPoints, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.milkGrowth.Count(
                entry => entry != null
                    && entry.milkId == NewGameSetupCatalog.WarmFirstMilkId), Is.EqualTo(1));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.newGameSetup.outcomeApplied, Is.True);
            Assert.That(fixture.Manager.CurrentTama.eggType,
                Is.EqualTo(NewGameSetupCatalog.ButterEggId));
            Assert.That(fixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo(expectedTemperament));
            Assert.That(fixture.Manager.CurrentTama.stats.mood, Is.EqualTo(74));
            Assert.That(fixture.Manager.CurrentTama.stats.sleepiness, Is.EqualTo(17));
            Assert.That(FindMilkGrowth(
                fixture.Manager.CurrentSave,
                NewGameSetupCatalog.WarmFirstMilkId)?.growthPoints, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.milkGrowth.Count(
                entry => entry != null
                    && entry.milkId == NewGameSetupCatalog.WarmFirstMilkId), Is.EqualTo(1));
        }

        [Test]
        public void IncompleteSetupStepAndSelectionSurviveReloadWithoutApplyingOutcome()
        {
            using var fixture = GameManagerFixture.Create("setup_incomplete_reload");
            var save = SaveManager.CreateDefaultSave();
            var setup = NewGameSetupSaveData.CreateForNewPlayer();

            Assert.That(NewGameSetupSystem.TrySelectEgg(
                setup,
                NewGameSetupCatalog.MintEggId,
                out _), Is.True);
            Assert.That(NewGameSetupSystem.TryAdvance(setup, out _), Is.True);
            save.newGameSetup = setup;
            fixture.WriteSave(save);

            fixture.Manager.LoadOrCreateGame();
            AssertIncompleteSetupIsPreserved(fixture.Manager.CurrentSave);
            fixture.Manager.SaveGame();

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            AssertIncompleteSetupIsPreserved(fixture.Manager.CurrentSave);
        }

        [Test]
        public void LegacySaveWithoutSetupFieldIsSuppressedWithoutChangingTamaOrMilkGrowth()
        {
            using var fixture = GameManagerFixture.Create("setup_legacy_missing_field");
            var legacyTama = new CheeseTamaModel
            {
                id = "legacy_tama",
                name = "Legacy Tama",
                hasCustomName = true,
                eggType = "legacy_egg_marker",
                level = 1,
                maxLevel = 33,
                form = "egg",
                createdAtIso = DateTimeOffset.Now.ToString("O"),
                lastSavedAtIso = DateTimeOffset.Now.ToString("O")
            };
            legacyTama.growthHistory.careStyle = "legacy_care_style";
            var legacy = new LegacySavePayload
            {
                version = "0.1.0",
                playerId = "legacy_player",
                cheeseTama = legacyTama,
                milkGrowth = new List<MilkGrowthSaveEntry>()
            };
            fixture.WriteRawJson(JsonUtility.ToJson(legacy, true));

            fixture.Manager.LoadOrCreateGame();

            var migrated = fixture.Manager.CurrentSave;
            Assert.That(migrated.newGameSetup.completed, Is.True);
            Assert.That(migrated.newGameSetup.legacySuppressed, Is.True);
            Assert.That(migrated.newGameSetup.outcomeApplied, Is.True);
            Assert.That(migrated.CurrentEggMarker(), Is.EqualTo("legacy_egg_marker"));
            Assert.That(migrated.cheeseTama.name, Is.EqualTo("Legacy Tama"));
            Assert.That(migrated.cheeseTama.level, Is.EqualTo(1));
            Assert.That(migrated.cheeseTama.growthHistory.careStyle,
                Is.EqualTo("legacy_care_style"));
            Assert.That(migrated.milkGrowth, Is.Empty);

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.newGameSetup.legacySuppressed, Is.True);
            Assert.That(fixture.Manager.CurrentSave.CurrentEggMarker(),
                Is.EqualTo("legacy_egg_marker"));
            Assert.That(fixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo("legacy_care_style"));
            Assert.That(fixture.Manager.CurrentSave.milkGrowth, Is.Empty);
        }

        [Test]
        public void SkippedSetupPreservesExistingCareStyleAcrossReload()
        {
            using var fixture = GameManagerFixture.Create("setup_skip_care_style");
            var save = SaveManager.CreateDefaultSave();
            var setup = NewGameSetupSaveData.CreateForNewPlayer();
            Assert.That(NewGameSetupSystem.TrySkip(setup, out var errorMessage),
                Is.True,
                errorMessage);
            save.newGameSetup = setup;
            save.cheeseTama.growthHistory.careStyle = "patient";
            fixture.WriteSave(save);

            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.newGameSetup.outcomeApplied, Is.True);
            Assert.That(fixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo("patient"));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo("patient"));
        }

        [Test]
        public void DifferentTemperamentSeedsApplyDistinctStatsAndDoNotStackOnReload()
        {
            using var livelyFixture = GameManagerFixture.Create("setup_lively_stats");
            var livelySave = SaveManager.CreateDefaultSave();
            livelySave.cheeseTama.stats.mood = 50;
            livelySave.cheeseTama.stats.sleepiness = 50;
            livelySave.cheeseTama.stats.affection = 50;
            livelySave.newGameSetup = CreateCompletedSetup(
                NewGameSetupCatalog.ButterEggId,
                NewGameSetupCatalog.WarmFirstMilkId);
            livelyFixture.WriteSave(livelySave);

            livelyFixture.Manager.LoadOrCreateGame();
            Assert.That(livelyFixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo(NewGameSetupCatalog.LivelyTraitId));
            Assert.That(livelyFixture.Manager.CurrentTama.stats.mood, Is.EqualTo(54));
            Assert.That(livelyFixture.Manager.CurrentTama.stats.sleepiness, Is.EqualTo(47));
            Assert.That(livelyFixture.Manager.CurrentTama.stats.affection, Is.EqualTo(50));
            var livelyApplied = PersistentStateSnapshot.Capture(livelyFixture.Manager.CurrentSave);

            livelyFixture.RecreateManager();
            livelyFixture.Manager.LoadOrCreateGame();
            livelyApplied.AssertMatches(livelyFixture.Manager.CurrentSave);
            Assert.That(livelyFixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo(NewGameSetupCatalog.LivelyTraitId));

            using var expressiveFixture = GameManagerFixture.Create("setup_expressive_stats");
            var expressiveSave = SaveManager.CreateDefaultSave();
            expressiveSave.cheeseTama.stats.mood = 50;
            expressiveSave.cheeseTama.stats.sleepiness = 50;
            expressiveSave.cheeseTama.stats.affection = 50;
            expressiveSave.newGameSetup = CreateCompletedSetup(
                NewGameSetupCatalog.StrawberryEggId,
                NewGameSetupCatalog.WarmFirstMilkId);
            expressiveFixture.WriteSave(expressiveSave);

            expressiveFixture.Manager.LoadOrCreateGame();
            Assert.That(expressiveFixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo(NewGameSetupCatalog.ExpressiveTraitId));
            Assert.That(expressiveFixture.Manager.CurrentTama.stats.mood, Is.EqualTo(53));
            Assert.That(expressiveFixture.Manager.CurrentTama.stats.sleepiness, Is.EqualTo(50));
            Assert.That(expressiveFixture.Manager.CurrentTama.stats.affection, Is.EqualTo(54));
            var expressiveApplied = PersistentStateSnapshot.Capture(
                expressiveFixture.Manager.CurrentSave);

            expressiveFixture.RecreateManager();
            expressiveFixture.Manager.LoadOrCreateGame();
            expressiveApplied.AssertMatches(expressiveFixture.Manager.CurrentSave);
            Assert.That(expressiveFixture.Manager.CurrentTama.growthHistory.careStyle,
                Is.EqualTo(NewGameSetupCatalog.ExpressiveTraitId));
        }

        [Test]
        public void PendingChoicePersistsAndReceiptRejectsDuplicateAfterManagerRecreation()
        {
            using var fixture = GameManagerFixture.Create("choice_receipt_reload");
            var save = SaveManager.CreateDefaultSave();
            var definition = RandomEventSystem.ChoiceEvents[0];
            var occurrenceId = Guid.NewGuid().ToString("N");
            var pending = new CareEventResult(
                true,
                occurrenceId,
                definition.id,
                definition.title,
                definition.message,
                true);
            save.randomEvents.pendingEvent.Set(pending);
            save.cheeseTama.stats.hunger = 50;
            save.cheeseTama.stats.mood = 50;
            save.cheeseTama.stats.cleanliness = 50;
            save.cheeseTama.stats.sleepiness = 50;
            save.cheeseTama.stats.health = 50;
            save.cheeseTama.stats.maturation = 50;
            save.cheeseTama.stats.affection = 50;
            save.economy.milkCoins = 20;
            save.economy.milkDrops = 20;
            save.economy.starDrops = 20;
            save.economy.collectionFragments = 20;
            fixture.WriteSave(save);

            fixture.Manager.LoadOrCreateGame();
            Assert.That(fixture.Manager.TryGetPendingCareEvent(out var restored), Is.True);
            Assert.That(restored.occurrenceId, Is.EqualTo(occurrenceId));
            Assert.That(restored.eventId, Is.EqualTo(definition.id));

            var beforeInvalidChoice = PersistentStateSnapshot.Capture(fixture.Manager.CurrentSave);
            Assert.That(fixture.Manager.TryResolvePendingCareEventChoice(
                occurrenceId,
                "missing_choice",
                out var invalidResult), Is.False);
            Assert.That(invalidResult.status, Is.EqualTo(CareEventChoiceResolutionStatus.UnknownChoice));
            beforeInvalidChoice.AssertMatches(fixture.Manager.CurrentSave);
            Assert.That(fixture.Manager.HasPendingCareEvent, Is.True);
            Assert.That(fixture.Manager.CurrentSave.randomEvents.choiceReceipts, Is.Empty);

            var selectedChoice = definition.Choices[0];
            Assert.That(fixture.Manager.TryResolvePendingCareEventChoice(
                occurrenceId,
                selectedChoice.id,
                out var appliedResult), Is.True);
            Assert.That(appliedResult.status, Is.EqualTo(CareEventChoiceResolutionStatus.Applied));
            Assert.That(fixture.Manager.HasPendingCareEvent, Is.False);
            Assert.That(fixture.Manager.CurrentSave.randomEvents.choiceReceipts, Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.randomEvents.choiceReceipts[0].occurrenceId,
                Is.EqualTo(occurrenceId));
            var afterAppliedChoice = PersistentStateSnapshot.Capture(fixture.Manager.CurrentSave);

            // Recreate the persisted pending occurrence to model an interrupted UI hand-off.
            // A fresh manager must use the durable receipt, not the in-memory choice cache.
            fixture.Manager.CurrentSave.randomEvents.pendingEvent.Set(pending);
            fixture.Manager.SaveGame();
            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            Assert.That(fixture.Manager.TryGetPendingCareEvent(out restored), Is.True);
            afterAppliedChoice.AssertMatches(fixture.Manager.CurrentSave);

            var alternateChoice = definition.Choices[1];
            Assert.That(fixture.Manager.TryResolvePendingCareEventChoice(
                occurrenceId,
                alternateChoice.id,
                out var duplicateResult), Is.True);
            Assert.That(duplicateResult.status,
                Is.EqualTo(CareEventChoiceResolutionStatus.AlreadyApplied));
            Assert.That(duplicateResult.choiceId, Is.EqualTo(selectedChoice.id));
            afterAppliedChoice.AssertMatches(fixture.Manager.CurrentSave);
            Assert.That(fixture.Manager.CurrentSave.randomEvents.choiceReceipts,
                Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.HasPendingCareEvent, Is.False);
        }

        [Test]
        public void StarRouteUnlockRaisesOneTransitionSignalAndAcknowledgementPersists()
        {
            using var fixture = GameManagerFixture.Create("star_route_transition");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentTama.level = 33;
            fixture.Manager.CurrentSave.milkGrowth.Clear();
            foreach (var milk in MilkCatalog.MainMilks)
            {
                fixture.Manager.CurrentSave.milkGrowth.Add(new MilkGrowthSaveEntry
                {
                    milkId = milk.id,
                    growthPoints = 40,
                    growthLevel = MilkCatalog.MainMilkMaxGrowthLevel
                });
            }

            var signalCount = 0;
            fixture.Manager.StarRouteUnlockAvailable += () => signalCount += 1;
            fixture.Manager.RefreshDerivedCollectionRecords();
            fixture.Manager.RefreshDerivedCollectionRecords();

            Assert.That(signalCount, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.unlocks.starMilkUnlocked, Is.True);
            Assert.That(fixture.Manager.HasPendingStarRouteUnlock, Is.True);
            Assert.That(fixture.Manager.AcknowledgeStarRouteUnlock(), Is.True);
            Assert.That(fixture.Manager.AcknowledgeStarRouteUnlock(), Is.False);

            fixture.RecreateManager();
            fixture.Manager.StarRouteUnlockAvailable += () => signalCount += 1;
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.RefreshDerivedCollectionRecords();

            Assert.That(signalCount, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.unlocks.starMilkUnlocked, Is.True);
            Assert.That(fixture.Manager.HasPendingStarRouteUnlock, Is.False);
        }

        [Test]
        public void BouncyJumpTotalsAndBestScorePersistWithoutNonqualifyingPlayHistory()
        {
            using var fixture = GameManagerFixture.Create("bouncy_jump_reload");
            fixture.Manager.LoadOrCreateGame();
            var initialTotalCare = fixture.Manager.CurrentSave.careHistory.totalCareActions;
            var initialPlayHistory = fixture.Manager.CurrentSave.careHistory.playSessions;
            var initialDailyPlay = fixture.Manager.CurrentSave.dailyCare.playSessions;

            var nonqualifying = fixture.Manager.CompleteBouncyJumpMiniGame(
                BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare - 1,
                4,
                450,
                2);

            Assert.That(nonqualifying.success, Is.False);
            Assert.That(fixture.Manager.CurrentSave.playMiniGames.totalBouncyJumpSessions,
                Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.playMiniGames.totalBouncyJumpSuccesses,
                Is.EqualTo(BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare - 1));
            Assert.That(fixture.Manager.CurrentSave.playMiniGames.highestBouncyJumpScore,
                Is.EqualTo(450));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions,
                Is.EqualTo(initialTotalCare));
            Assert.That(fixture.Manager.CurrentSave.careHistory.playSessions,
                Is.EqualTo(initialPlayHistory));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.playSessions,
                Is.EqualTo(initialDailyPlay));

            var qualifying = fixture.Manager.CompleteBouncyJumpMiniGame(
                BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare + 1,
                1,
                900,
                4);
            Assert.That(qualifying.success, Is.True);
            Assert.That(qualifying.bestScore, Is.EqualTo(900));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.playMiniGames.totalBouncyJumpSessions,
                Is.EqualTo(2));
            Assert.That(fixture.Manager.CurrentSave.playMiniGames.totalBouncyJumpSuccesses,
                Is.EqualTo((BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare - 1)
                    + (BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare + 1)));
            Assert.That(fixture.Manager.CurrentSave.playMiniGames.highestBouncyJumpScore,
                Is.EqualTo(900));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions,
                Is.EqualTo(initialTotalCare + 1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.playSessions,
                Is.EqualTo(initialPlayHistory + 1));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.playSessions,
                Is.EqualTo(initialDailyPlay + 1));
        }

        private static void AssertIncompleteSetupIsPreserved(CheeseTamaSaveData save)
        {
            Assert.That(save.newGameSetup.completed, Is.False);
            Assert.That(save.newGameSetup.outcomeApplied, Is.False);
            Assert.That(save.newGameSetup.currentStep,
                Is.EqualTo(NewGameSetupStep.FirstMilkSelection));
            Assert.That(save.newGameSetup.selectedEggId,
                Is.EqualTo(NewGameSetupCatalog.MintEggId));
            Assert.That(save.newGameSetup.selectedFirstMilkId, Is.Empty);
            Assert.That(save.cheeseTama.eggType, Is.EqualTo("cream_egg"));
            Assert.That(save.milkGrowth, Is.Empty);
        }

        private static MilkGrowthSaveEntry FindMilkGrowth(
            CheeseTamaSaveData save,
            string milkId)
        {
            return save?.milkGrowth?.FirstOrDefault(
                entry => entry != null && entry.milkId == milkId);
        }

        private static NewGameSetupSaveData CreateCompletedSetup(
            string eggId,
            string firstMilkId)
        {
            var setup = NewGameSetupSaveData.CreateForNewPlayer();
            Assert.That(NewGameSetupSystem.TrySelectEgg(setup, eggId, out var errorMessage),
                Is.True,
                errorMessage);
            Assert.That(NewGameSetupSystem.TryAdvance(setup, out errorMessage),
                Is.True,
                errorMessage);
            Assert.That(NewGameSetupSystem.TrySelectFirstMilk(
                    setup,
                    firstMilkId,
                    out errorMessage),
                Is.True,
                errorMessage);
            Assert.That(NewGameSetupSystem.TryAdvance(setup, out errorMessage),
                Is.True,
                errorMessage);
            return setup;
        }

        [Serializable]
        private sealed class LegacySavePayload
        {
            public string version;
            public string playerId;
            public CheeseTamaModel cheeseTama;
            public List<MilkGrowthSaveEntry> milkGrowth;
        }

        private sealed class PersistentStateSnapshot
        {
            private int hunger;
            private int mood;
            private int cleanliness;
            private int sleepiness;
            private int health;
            private int maturation;
            private int affection;
            private int milkCoins;
            private int milkDrops;
            private int starDrops;
            private int collectionFragments;

            public static PersistentStateSnapshot Capture(CheeseTamaSaveData save)
            {
                return new PersistentStateSnapshot
                {
                    hunger = save.cheeseTama.stats.hunger,
                    mood = save.cheeseTama.stats.mood,
                    cleanliness = save.cheeseTama.stats.cleanliness,
                    sleepiness = save.cheeseTama.stats.sleepiness,
                    health = save.cheeseTama.stats.health,
                    maturation = save.cheeseTama.stats.maturation,
                    affection = save.cheeseTama.stats.affection,
                    milkCoins = save.economy.milkCoins,
                    milkDrops = save.economy.milkDrops,
                    starDrops = save.economy.starDrops,
                    collectionFragments = save.economy.collectionFragments
                };
            }

            public void AssertMatches(CheeseTamaSaveData save)
            {
                Assert.That(save.cheeseTama.stats.hunger, Is.EqualTo(hunger));
                Assert.That(save.cheeseTama.stats.mood, Is.EqualTo(mood));
                Assert.That(save.cheeseTama.stats.cleanliness, Is.EqualTo(cleanliness));
                Assert.That(save.cheeseTama.stats.sleepiness, Is.EqualTo(sleepiness));
                Assert.That(save.cheeseTama.stats.health, Is.EqualTo(health));
                Assert.That(save.cheeseTama.stats.maturation, Is.EqualTo(maturation));
                Assert.That(save.cheeseTama.stats.affection, Is.EqualTo(affection));
                Assert.That(save.economy.milkCoins, Is.EqualTo(milkCoins));
                Assert.That(save.economy.milkDrops, Is.EqualTo(milkDrops));
                Assert.That(save.economy.starDrops, Is.EqualTo(starDrops));
                Assert.That(save.economy.collectionFragments,
                    Is.EqualTo(collectionFragments));
            }
        }

        private sealed class GameManagerFixture : IDisposable
        {
            private readonly GameObject root;

            private GameManagerFixture(
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

            public static GameManagerFixture Create(string label)
            {
                var root = new GameObject($"{label} Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                var manager = root.AddComponent<GameManager>();
                SetPrivateField(
                    saveManager,
                    "saveFileName",
                    $"cheesetama_next_feature_test_{label}_{Guid.NewGuid():N}.json");
                SetPrivateField(manager, "saveManager", saveManager);
                return new GameManagerFixture(root, saveManager, manager);
            }

            public void WriteSave(CheeseTamaSaveData save)
            {
                WriteRawJson(JsonUtility.ToJson(save, true));
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

            private static void SetPrivateField(object target, string name, object value)
            {
                var field = target.GetType().GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, name);
                field.SetValue(target, value);
            }
        }
    }

    internal static class PersistenceIntegrationTestExtensions
    {
        public static string CurrentEggMarker(this CheeseTamaSaveData save)
        {
            return save?.cheeseTama?.eggType ?? string.Empty;
        }
    }
}
