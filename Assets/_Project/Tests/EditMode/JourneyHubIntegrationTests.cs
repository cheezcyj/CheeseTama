using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CheeseTama.Collections;
using CheeseTama.Core;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Gameplay.Weekly;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class JourneyHubIntegrationTests
    {
        private static readonly DateTimeOffset MondayNoon =
            new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.FromHours(9));

        private static DateTimeOffset CurrentWeekNoon =>
            WeeklyCareJourneySystem.GetWeekStart(DateTimeOffset.Now).AddHours(12);

        [Test]
        public void AggregateSaveJsonRoundTripKeepsAllFourJourneyFeatureStates()
        {
            var save = SaveManager.CreateDefaultSave();
            save.EnsureRuntimeDefaults();

            save.npcRelationshipQuests.activeQuest.Set(
                "offer-round-trip",
                NpcVisitSystem.MilkyDoctorId,
                "doctor_warm_soup",
                MondayNoon,
                MondayNoon.AddDays(3),
                MondayNoon.AddDays(5));

            var weeklySystem = new WeeklyCareJourneySystem();
            Assert.That(
                weeklySystem.RecordEvent(
                    save.weeklyCareJourney,
                    WeeklyCareEventIds.Play,
                    2,
                    MondayNoon,
                    "weekly-round-trip").Applied,
                Is.True);

            save.decorationWorkshop.ownedVariantIds.Add(
                DecorationWorkshopCatalog.WallVanillaGlazeId);
            save.decorationWorkshop.appliedCraftReceiptKeys.Add("craft-round-trip");
            save.decorationWorkshop.selectedVariants.Add(
                new DecorationWorkshopSelectionSaveEntry
                {
                    slot = (int)DecorationSlot.Wall,
                    variantId = DecorationWorkshopCatalog.WallVanillaGlazeId
                });
            save.collectionSetAlbum.revealedHiddenSetIds.Add(
                CollectionSetAlbumSystem.NormalEvolutionCircleSetId);
            save.collectionSetAlbum.claimedSetIds.Add(
                CollectionSetAlbumSystem.MilkFirstStepsSetId);
            save.collectionSetAlbum.appliedClaimReceiptKeys.Add("album-round-trip");

            var restored = JsonUtility.FromJson<CheeseTamaSaveData>(
                JsonUtility.ToJson(save));
            restored.EnsureRuntimeDefaults();

            Assert.That(restored.npcRelationshipQuests.activeQuest.offerId,
                Is.EqualTo("offer-round-trip"));
            Assert.That(restored.npcRelationshipQuests.activeQuest.questId,
                Is.EqualTo("doctor_warm_soup"));
            Assert.That(restored.weeklyCareJourney.HasEventReceipt("weekly-round-trip"),
                Is.True);
            Assert.That(
                FindWeeklyProgress(restored.weeklyCareJourney, "weekly_play_3"),
                Is.EqualTo(2));
            Assert.That(
                restored.decorationWorkshop.Owns(
                    DecorationWorkshopCatalog.WallVanillaGlazeId),
                Is.True);
            Assert.That(
                restored.decorationWorkshop.GetSelectedVariantId((int)DecorationSlot.Wall),
                Is.EqualTo(DecorationWorkshopCatalog.WallVanillaGlazeId));
            Assert.That(restored.decorationWorkshop.HasAppliedCraftReceipt("craft-round-trip"),
                Is.True);
            Assert.That(
                restored.collectionSetAlbum.IsHiddenSetRevealed(
                    CollectionSetAlbumSystem.NormalEvolutionCircleSetId),
                Is.True);
            Assert.That(
                restored.collectionSetAlbum.IsRewardClaimed(
                    CollectionSetAlbumSystem.MilkFirstStepsSetId),
                Is.True);
            Assert.That(
                restored.collectionSetAlbum.HasAppliedClaimReceipt("album-round-trip"),
                Is.True);
        }

        [Test]
        public void JourneyHubBuilderIsIdempotentAndCreatesLauncherAndFiveTabs()
        {
            var canvas = new GameObject(
                "Journey Hub Builder Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                Assert.That(Application.isPlaying, Is.False);
                var collectionOverlay = new GameObject("Collection Overlay");
                collectionOverlay.transform.SetParent(canvas.transform, false);
                var decorateOverlay = new GameObject("Decorate Overlay");
                decorateOverlay.transform.SetParent(canvas.transform, false);
                var settingsOverlay = new GameObject("Settings Modal");
                settingsOverlay.transform.SetParent(canvas.transform, false);
                var topMenu = canvas.AddComponent<TopMenuController>();
                topMenu.Configure(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    collectionOverlay,
                    decorateOverlay,
                    settingsOverlay,
                    null);

                InvokeBuilder("EnsureJourneyHub", canvas.transform);
                InvokeBuilder("EnsureJourneyHub", canvas.transform);

                Assert.That(
                    CountRecursively(canvas.transform, "Journey Hub Overlay"),
                    Is.EqualTo(1));
                Assert.That(
                    CountRecursively(canvas.transform, "Open Journey Hub Button"),
                    Is.EqualTo(1));
                Assert.That(
                    canvas.GetComponentsInChildren<JourneyHubPanelController>(true),
                    Has.Length.EqualTo(1));

                var overlay = FindRecursively(canvas.transform, "Journey Hub Overlay");
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(overlay.GetComponent<Image>()?.raycastTarget, Is.True);
                Assert.That(overlay.GetComponent<CanvasGroup>()?.blocksRaycasts, Is.True);

                AssertSingleButtonLabel(canvas.transform, "여정");
                AssertSingleButtonLabel(canvas.transform, "목표");
                AssertSingleButtonLabel(canvas.transform, "주간");
                AssertSingleButtonLabel(canvas.transform, "관계");
                AssertSingleButtonLabel(canvas.transform, "앨범");
                AssertSingleButtonLabel(canvas.transform, "공방");

                decorateOverlay.SetActive(true);
                settingsOverlay.SetActive(true);
                var controller = canvas.GetComponent<JourneyHubPanelController>();
                controller.Open();
                Assert.That(decorateOverlay.activeSelf, Is.False);
                Assert.That(settingsOverlay.activeSelf, Is.False);
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                controller.Close();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void WeeklyClaimThroughGameManagerIsAtomicAndDuplicateDoesNotMutate()
        {
            using var fixture = GameManagerFixture.Create("weekly_claim");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            var weeklySystem = new WeeklyCareJourneySystem();
            var currentWeekNoon = CurrentWeekNoon;
            RecordWeekly(
                weeklySystem,
                save.weeklyCareJourney,
                WeeklyCareEventIds.Feed,
                6,
                "weekly-feed",
                currentWeekNoon);
            RecordWeekly(
                weeklySystem,
                save.weeklyCareJourney,
                WeeklyCareEventIds.Play,
                3,
                "weekly-play",
                currentWeekNoon);
            RecordWeekly(
                weeklySystem,
                save.weeklyCareJourney,
                WeeklyCareEventIds.Discovery,
                2,
                "weekly-discovery",
                currentWeekNoon);
            save.economy.milkCoins = 10;
            save.economy.milkDrops = 20;
            save.economy.collectionFragments = 30;
            var changeCount = 0;
            fixture.Manager.JourneyHubChanged += () => changeCount += 1;

            var applied = fixture.Manager.TryClaimWeeklyCareJourneyReward(
                currentWeekNoon,
                "weekly-manager-claim");

            Assert.That(applied.Status, Is.EqualTo(WeeklyCareClaimStatus.Applied));
            Assert.That(save.economy.milkCoins,
                Is.EqualTo(10 + WeeklyCareJourneySystem.RewardMilkCoins));
            Assert.That(save.economy.milkDrops,
                Is.EqualTo(20 + WeeklyCareJourneySystem.RewardMilkDrops));
            Assert.That(save.economy.collectionFragments,
                Is.EqualTo(30 + WeeklyCareJourneySystem.RewardCollectionFragments));
            Assert.That(changeCount, Is.EqualTo(1));
            var stateAfterApplied = JsonUtility.ToJson(save.weeklyCareJourney);
            var walletAfterApplied = CaptureWallet(save.economy);
            fixture.Manager.ReloadGame();
            save = fixture.Manager.CurrentSave;
            Assert.That(JsonUtility.ToJson(save.weeklyCareJourney), Is.EqualTo(stateAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));

            var duplicate = fixture.Manager.TryClaimWeeklyCareJourneyReward(
                currentWeekNoon,
                "weekly-manager-claim");

            Assert.That(duplicate.Status, Is.EqualTo(WeeklyCareClaimStatus.DuplicateClaim));
            Assert.That(JsonUtility.ToJson(save.weeklyCareJourney),
                Is.EqualTo(stateAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));
            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void NpcDeliveryThroughGameManagerIsAtomicAndDuplicateDoesNotMutate()
        {
            using var fixture = GameManagerFixture.Create("npc_delivery");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.snackInventory.RemoveAll(
                entry => entry != null && entry.snackId == SnackCatalog.WarmMilkSoupId);
            save.snackInventory.Add(new SnackInventorySaveEntry
            {
                snackId = SnackCatalog.WarmMilkSoupId,
                quantity = 2
            });
            var questSystem = new NpcRelationshipQuestSystem();
            var activation = questSystem.TryActivate(
                save.npcRelationshipQuests,
                save.npcVisits,
                NpcVisitSystem.MilkyDoctorId,
                "doctor_warm_soup",
                "npc-manager-offer",
                MondayNoon);
            Assert.That(activation.Applied, Is.True);
            var changeCount = 0;
            fixture.Manager.JourneyHubChanged += () => changeCount += 1;

            var applied = fixture.Manager.TryDeliverNpcRelationshipQuest(
                MondayNoon.AddDays(1),
                "npc-manager-claim");

            Assert.That(applied.Status, Is.EqualTo(NpcQuestDeliveryStatus.Applied));
            Assert.That(FindSnackQuantity(save.snackInventory, SnackCatalog.WarmMilkSoupId),
                Is.EqualTo(1));
            Assert.That(save.economy.milkDrops, Is.EqualTo(2));
            Assert.That(save.economy.collectionFragments, Is.EqualTo(1));
            Assert.That(save.npcRelationshipQuests.activeQuest.HasValue, Is.False);
            Assert.That(changeCount, Is.EqualTo(1));
            var stateAfterApplied = JsonUtility.ToJson(save.npcRelationshipQuests);
            var relationshipsAfterApplied = JsonUtility.ToJson(save.npcVisits);
            var walletAfterApplied = CaptureWallet(save.economy);
            fixture.Manager.ReloadGame();
            save = fixture.Manager.CurrentSave;
            Assert.That(JsonUtility.ToJson(save.npcRelationshipQuests), Is.EqualTo(stateAfterApplied));
            Assert.That(JsonUtility.ToJson(save.npcVisits), Is.EqualTo(relationshipsAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));

            var duplicate = fixture.Manager.TryDeliverNpcRelationshipQuest(
                MondayNoon.AddDays(1),
                "npc-manager-claim");

            Assert.That(duplicate.Status, Is.EqualTo(NpcQuestDeliveryStatus.DuplicateClaim));
            Assert.That(JsonUtility.ToJson(save.npcRelationshipQuests),
                Is.EqualTo(stateAfterApplied));
            Assert.That(JsonUtility.ToJson(save.npcVisits), Is.EqualTo(relationshipsAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));
            Assert.That(FindSnackQuantity(save.snackInventory, SnackCatalog.WarmMilkSoupId),
                Is.EqualTo(1));
            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void WorkshopCraftThroughGameManagerChargesOnceAndDuplicateDoesNotMutate()
        {
            using var fixture = GameManagerFixture.Create("workshop_craft");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            var definition = DecorationWorkshopCatalog.Find(
                DecorationWorkshopCatalog.WallVanillaGlazeId);
            save.economy.milkCoins = definition.CoinCost + 11;
            save.economy.milkDrops = definition.MilkDropCost + 12;
            save.economy.collectionFragments = definition.CollectionFragmentCost + 13;
            var changeCount = 0;
            fixture.Manager.JourneyHubChanged += () => changeCount += 1;

            var applied = fixture.Manager.TryCraftDecorationWorkshopVariant(
                definition.Id,
                "workshop-manager-craft");

            Assert.That(applied.Status, Is.EqualTo(DecorationWorkshopCraftStatus.Applied));
            Assert.That(save.economy.milkCoins, Is.EqualTo(11));
            Assert.That(save.economy.milkDrops, Is.EqualTo(12));
            Assert.That(save.economy.collectionFragments, Is.EqualTo(13));
            Assert.That(save.decorationWorkshop.Owns(definition.Id), Is.True);
            Assert.That(changeCount, Is.EqualTo(1));
            var stateAfterApplied = JsonUtility.ToJson(save.decorationWorkshop);
            var walletAfterApplied = CaptureWallet(save.economy);
            fixture.Manager.ReloadGame();
            save = fixture.Manager.CurrentSave;
            Assert.That(JsonUtility.ToJson(save.decorationWorkshop), Is.EqualTo(stateAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));

            var duplicate = fixture.Manager.TryCraftDecorationWorkshopVariant(
                definition.Id,
                "workshop-manager-craft");

            Assert.That(duplicate.Status,
                Is.EqualTo(DecorationWorkshopCraftStatus.AlreadyApplied));
            Assert.That(JsonUtility.ToJson(save.decorationWorkshop),
                Is.EqualTo(stateAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));
            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void AlbumClaimThroughGameManagerRewardsOnceAndDuplicateDoesNotMutate()
        {
            using var fixture = GameManagerFixture.Create("album_claim");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.collections.milk.Clear();
            save.collections.milk.Add(MilkCatalog.BasicMilkId);
            save.collections.milk.Add(MilkCatalog.WarmMilkId);
            save.collections.milk.Add(MilkCatalog.ColdMilkId);
            save.economy.milkCoins = 10;
            save.economy.milkDrops = 20;
            save.economy.collectionFragments = 30;
            var expectedReward = fixture.Manager.GetCollectionSetAlbumSnapshot()
                .Find(CollectionSetAlbumSystem.MilkFirstStepsSetId)
                .Reward;
            var changeCount = 0;
            fixture.Manager.JourneyHubChanged += () => changeCount += 1;

            var applied = fixture.Manager.TryClaimCollectionSetAlbumReward(
                CollectionSetAlbumSystem.MilkFirstStepsSetId,
                "album-manager-claim");

            Assert.That(applied.Status, Is.EqualTo(CollectionSetAlbumClaimStatus.Applied));
            Assert.That(save.economy.milkCoins, Is.EqualTo(10 + expectedReward.Coins));
            Assert.That(save.economy.milkDrops, Is.EqualTo(20 + expectedReward.MilkDrops));
            Assert.That(save.economy.collectionFragments,
                Is.EqualTo(30 + expectedReward.CollectionFragments));
            Assert.That(changeCount, Is.EqualTo(1));
            var stateAfterApplied = JsonUtility.ToJson(save.collectionSetAlbum);
            var walletAfterApplied = CaptureWallet(save.economy);
            fixture.Manager.ReloadGame();
            save = fixture.Manager.CurrentSave;
            Assert.That(JsonUtility.ToJson(save.collectionSetAlbum), Is.EqualTo(stateAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));

            var duplicate = fixture.Manager.TryClaimCollectionSetAlbumReward(
                CollectionSetAlbumSystem.MilkFirstStepsSetId,
                "album-manager-claim");

            Assert.That(duplicate.Status,
                Is.EqualTo(CollectionSetAlbumClaimStatus.AlreadyApplied));
            Assert.That(JsonUtility.ToJson(save.collectionSetAlbum),
                Is.EqualTo(stateAfterApplied));
            Assert.That(CaptureWallet(save.economy), Is.EqualTo(walletAfterApplied));
            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void GameManagerPublicDiscoveryAndJournalSnapshotsHideUnknownAndLockedData()
        {
            using var fixture = GameManagerFixture.Create("public_snapshots");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.autonomousLife.firstDiscoveries =
                new List<AutonomousLifeDiscoverySaveEntry>
                {
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = AutonomousLifeBehaviourCatalog.DanceId,
                        firstDiscoveredAtIso = MondayNoon.ToString("O")
                    },
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = "future_hidden_behaviour",
                        firstDiscoveredAtIso = MondayNoon.ToString("O")
                    }
                };
            save.randomEvents.history = new List<RandomEventHistorySaveEntry>
            {
                new RandomEventHistorySaveEntry
                {
                    eventId = "quiet_hum",
                    totalOccurrences = 2,
                    lastOccurredAtIso = MondayNoon.AddDays(-1).ToString("O")
                },
                new RandomEventHistorySaveEntry
                {
                    eventId = "future_hidden_event",
                    totalOccurrences = 999,
                    lastOccurredAtIso = MondayNoon.ToString("O")
                }
            };
            var autonomyBefore = JsonUtility.ToJson(save.autonomousLife);
            var eventsBefore = JsonUtility.ToJson(save.randomEvents);

            var discoveries = fixture.Manager.GetAutonomousLifeDiscoverySnapshot();
            var journal = fixture.Manager.GetRandomEventJournalSnapshot(MondayNoon);

            Assert.That(discoveries.TotalCount,
                Is.EqualTo(AutonomousLifeDiscoveryCatalog.TotalDiscoveryCount));
            Assert.That(discoveries.DiscoveredCount, Is.EqualTo(1));
            foreach (var item in discoveries.Items)
            {
                if (item.IsDiscovered)
                {
                    Assert.That(item.BehaviourId,
                        Is.EqualTo(AutonomousLifeBehaviourCatalog.DanceId));
                    continue;
                }

                Assert.That(item.BehaviourId, Is.Empty);
                Assert.That(item.FirstDiscoveredAtIso, Is.Empty);
                Assert.That(item.DisplayName,
                    Is.EqualTo(AutonomousLifeDiscoveryCatalog.HiddenDisplayName));
            }

            Assert.That(journal.Entries.Count, Is.EqualTo(1));
            Assert.That(journal.Entries[0].EventId, Is.EqualTo("quiet_hum"));
            Assert.That(journal.Entries[0].TotalOccurrences, Is.EqualTo(2));
            Assert.That(journal.TotalOccurrences, Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(save.autonomousLife), Is.EqualTo(autonomyBefore));
            Assert.That(JsonUtility.ToJson(save.randomEvents), Is.EqualTo(eventsBefore));
        }

        [Test]
        public void LegacySaveMigratesJourneyStatesAndGraphicsPresetAndPersistsThem()
        {
            using var fixture = GameManagerFixture.Create("journey_migration");
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.SaveManager.SaveFilePath));
            File.WriteAllText(fixture.SaveManager.SaveFilePath, "{}");

            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;

            Assert.That(fixture.SaveManager.LastLoadMigratedData, Is.True);
            Assert.That(save.npcRelationshipQuests, Is.Not.Null);
            Assert.That(save.weeklyCareJourney, Is.Not.Null);
            Assert.That(save.decorationWorkshop, Is.Not.Null);
            Assert.That(save.collectionSetAlbum, Is.Not.Null);
            Assert.That(
                save.settings.graphicsQualityPreset,
                Is.EqualTo((int)CheeseTama.Environment.GraphicsQualityPreset.High));

            var persisted = File.ReadAllText(fixture.SaveManager.SaveFilePath);
            Assert.That(persisted, Does.Contain("\"npcRelationshipQuests\""));
            Assert.That(persisted, Does.Contain("\"weeklyCareJourney\""));
            Assert.That(persisted, Does.Contain("\"decorationWorkshop\""));
            Assert.That(persisted, Does.Contain("\"collectionSetAlbum\""));
            Assert.That(persisted, Does.Contain("\"graphicsQualityPreset\""));
        }

        private static void RecordWeekly(
            WeeklyCareJourneySystem system,
            WeeklyCareJourneySaveData state,
            string eventId,
            int count,
            string receiptPrefix,
            DateTimeOffset? eventTime = null)
        {
            var injectedNow = eventTime ?? MondayNoon;
            for (var index = 0; index < count; index += 1)
            {
                var result = system.RecordEvent(
                    state,
                    eventId,
                    1,
                    injectedNow,
                    $"{receiptPrefix}-{index}");
                Assert.That(result.Applied, Is.True, result.Status.ToString());
            }
        }

        private static int FindWeeklyProgress(
            WeeklyCareJourneySaveData state,
            string objectiveId)
        {
            for (var index = 0; index < (state?.objectives?.Count ?? 0); index += 1)
            {
                var entry = state.objectives[index];
                if (entry != null && entry.objectiveId == objectiveId)
                {
                    return entry.progress;
                }
            }

            return -1;
        }

        private static int FindSnackQuantity(
            IReadOnlyList<SnackInventorySaveEntry> inventory,
            string snackId)
        {
            for (var index = 0; index < (inventory?.Count ?? 0); index += 1)
            {
                var entry = inventory[index];
                if (entry != null && entry.snackId == snackId)
                {
                    return entry.quantity;
                }
            }

            return 0;
        }

        private static string CaptureWallet(EconomySaveData economy)
        {
            return $"{economy.milkCoins}:{economy.milkDrops}:{economy.collectionFragments}";
        }

        private static void InvokeBuilder(string methodName, params object[] arguments)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Builder method not found: {methodName}");
            try
            {
                method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void AssertSingleButtonLabel(Transform root, string expected)
        {
            var count = 0;
            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var index = 0; index < buttons.Length; index += 1)
            {
                var label = buttons[index].GetComponentInChildren<Text>(true);
                if (label != null
                    && (string.Equals(label.text, expected, StringComparison.Ordinal)
                        || string.Equals(label.text, $"● {expected}", StringComparison.Ordinal)))
                {
                    count += 1;
                }
            }

            Assert.That(count, Is.EqualTo(1), $"Button label: {expected}");
        }

        private static Transform FindRecursively(Transform parent, string objectName)
        {
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index);
                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return child;
                }

                var descendant = FindRecursively(child, objectName);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static int CountRecursively(Transform parent, string objectName)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index);
                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    count += 1;
                }

                count += CountRecursively(child, objectName);
            }

            return count;
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
            public GameManager Manager { get; }

            public static GameManagerFixture Create(string label)
            {
                var root = new GameObject($"{label} Journey Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                var manager = root.AddComponent<GameManager>();
                var fileNameField = typeof(SaveManager).GetField(
                    "saveFileName",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var saveManagerField = typeof(GameManager).GetField(
                    "saveManager",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(fileNameField, Is.Not.Null);
                Assert.That(saveManagerField, Is.Not.Null);
                fileNameField.SetValue(
                    saveManager,
                    $"cheesetama_journey_test_{label}_{Guid.NewGuid():N}.json");
                saveManagerField.SetValue(manager, saveManager);
                return new GameManagerFixture(root, saveManager, manager);
            }

            public void WriteSave(CheeseTamaSaveData save)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SaveManager.SaveFilePath));
                File.WriteAllText(SaveManager.SaveFilePath, JsonUtility.ToJson(save, true));
            }

            public void Dispose()
            {
                SaveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
