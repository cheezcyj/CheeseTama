using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Feeding;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class ExpandedFeatureDomainTests
    {
        [Test]
        public void SameMilkActivatesAversionOnThirdFeedAndDifferentMilkRecoversIt()
        {
            var tama = new CheeseTamaModel();
            tama.stats.milkSatisfaction = 60;
            var system = new FeedingStatusSystem();

            var first = system.ApplyMilk(tama, "basic_milk", 0, 0);
            Assert.That(tama.growthHistory.sameMilkFeedStreak, Is.EqualTo(1));
            Assert.That(system.IsMilkAversionActive(tama), Is.False);
            Assert.That(first.milkAversionActivated, Is.False);

            var second = system.ApplyMilk(tama, "basic_milk", 0, 0);
            Assert.That(tama.growthHistory.sameMilkFeedStreak, Is.EqualTo(2));
            Assert.That(system.IsMilkAversionActive(tama), Is.False);
            Assert.That(second.milkAversionActivated, Is.False);

            var third = system.ApplyMilk(tama, "basic_milk", 0, 0);
            Assert.That(tama.growthHistory.sameMilkFeedStreak, Is.EqualTo(3));
            Assert.That(system.IsMilkAversionActive(tama), Is.True);
            Assert.That(third.milkAversionActivated, Is.True);
            Assert.That(tama.stats.milkSatisfaction, Is.EqualTo(48));

            var recovered = system.ApplyMilk(tama, "warm_milk", 0, 0);
            Assert.That(recovered.milkAversionRecovered, Is.True);
            Assert.That(system.IsMilkAversionActive(tama), Is.False);
            Assert.That(tama.growthHistory.lastFedMilkId, Is.EqualTo("warm_milk"));
            Assert.That(tama.growthHistory.sameMilkFeedStreak, Is.EqualTo(1));
            Assert.That(tama.stats.milkSatisfaction, Is.EqualTo(60));
        }

        [Test]
        public void OverfullnessBeginsOnlyAboveRawHungerThresholdAndClamps()
        {
            var tama = new CheeseTamaModel();
            var system = new FeedingStatusSystem();

            var atThreshold = system.ApplySnack(
                tama,
                FeedingStatusSystem.OverfullnessRawHungerThreshold - 10,
                10);
            Assert.That(atThreshold.overfullnessActivated, Is.False);
            Assert.That(tama.stats.overfullness, Is.Zero);

            var justAboveThreshold = system.ApplySnack(
                tama,
                FeedingStatusSystem.OverfullnessRawHungerThreshold - 10,
                11);
            Assert.That(justAboveThreshold.overfullnessActivated, Is.True);
            Assert.That(tama.stats.overfullness, Is.EqualTo(1));

            system.ApplySnack(tama, int.MaxValue, int.MaxValue);
            Assert.That(tama.stats.overfullness, Is.EqualTo(FeedingStatusSystem.MaximumOverfullness));
        }

        [Test]
        public void OverfullnessRecoversByPlayAndElapsedHours()
        {
            var tama = new CheeseTamaModel();
            tama.stats.overfullness = 60;
            var system = new FeedingStatusSystem();

            var play = system.RecoverByPlay(tama);
            Assert.That(play.OverfullnessDelta, Is.EqualTo(-FeedingStatusSystem.OverfullnessRecoveryPerPlay));
            Assert.That(play.overfullnessRecovered, Is.False);
            Assert.That(tama.stats.overfullness, Is.EqualTo(35));

            var oneHour = system.RecoverByTime(tama, 1);
            Assert.That(oneHour.OverfullnessDelta, Is.EqualTo(-FeedingStatusSystem.OverfullnessRecoveryPerHour));
            Assert.That(oneHour.overfullnessRecovered, Is.False);
            Assert.That(tama.stats.overfullness, Is.EqualTo(15));

            var finalHour = system.RecoverByTime(tama, 1);
            Assert.That(finalHour.overfullnessRecovered, Is.True);
            Assert.That(tama.stats.overfullness, Is.Zero);
        }

        [TestCase("small_fever", "따뜻한 온기가 필요해요")]
        [TestCase("hungry_peep", "꼬르륵, 작은 신호")]
        [TestCase("dusty_corner", "먼지 낀 구석")]
        [TestCase("sleepy_yawn", "졸음이 한가득")]
        [TestCase("happy_wiggle", "기분 좋은 흔들림")]
        public void DeterministicRandomEventRollReturnsExpectedConditionCard(string eventId, string title)
        {
            var tama = CreateNeutralEventTama();
            SetConditionBeyondThreshold(tama, eventId);

            var result = new RandomEventSystem().RollCareEvent(
                tama,
                conditionChanceRoll: 0f,
                ambientChanceRoll: 1f);

            Assert.That(result.occurred, Is.True);
            Assert.That(result.eventId, Is.EqualTo(eventId));
            Assert.That(result.title, Is.EqualTo(title));
            Assert.That(result.message, Is.Not.Empty);
        }

        [Test]
        public void RandomEventConditionsUseStrictThresholdBoundaries()
        {
            AssertStrictThreshold("small_fever", tama => tama.stats.health = 35, tama => tama.stats.health = 34);
            AssertStrictThreshold("hungry_peep", tama => tama.stats.hunger = 25, tama => tama.stats.hunger = 24);
            AssertStrictThreshold("dusty_corner", tama => tama.stats.cleanliness = 35, tama => tama.stats.cleanliness = 34);
            AssertStrictThreshold("sleepy_yawn", tama => tama.stats.sleepiness = 75, tama => tama.stats.sleepiness = 76);
            AssertStrictThreshold("happy_wiggle", tama => tama.stats.mood = 80, tama => tama.stats.mood = 81);
        }

        [Test]
        public void RandomEventSelectionCanReachEveryMatchingCondition()
        {
            var tama = CreateNeutralEventTama();
            tama.stats.health = RandomEventSystem.LowHealthThreshold - 1;
            tama.stats.hunger = RandomEventSystem.LowHungerThreshold - 1;
            var system = new RandomEventSystem();

            var first = system.RollCareEvent(
                tama,
                conditionSelectionRoll: 0f,
                conditionChanceRoll: 0f,
                ambientChanceRoll: 1f);
            var second = system.RollCareEvent(
                tama,
                conditionSelectionRoll: 0.99f,
                conditionChanceRoll: 0f,
                ambientChanceRoll: 1f);

            Assert.That(first.eventId, Is.EqualTo("small_fever"));
            Assert.That(second.eventId, Is.EqualTo("hungry_peep"));
        }

        [Test]
        public void ChoiceEventCatalogContainsFiveTwoChoiceConditionalEvents()
        {
            Assert.That(RandomEventSystem.ChoiceEvents.Count, Is.EqualTo(5));
            var eventIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in RandomEventSystem.ChoiceEvents)
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.RequiresChoice, Is.True, definition.id);
                Assert.That(definition.Choices.Count, Is.EqualTo(2), definition.id);
                Assert.That(definition.condition, Is.Not.EqualTo(CareEventCondition.Ambient));
                Assert.That(eventIds.Add(definition.id), Is.True, definition.id);
                Assert.That(definition.Choices[0].id, Is.Not.EqualTo(definition.Choices[1].id));
            }
        }

        [Test]
        public void ChoiceEventRollRunsAfterAutomaticConditionCardMisses()
        {
            var tama = CreateNeutralEventTama();
            tama.stats.health = RandomEventSystem.LowHealthThreshold - 1;

            var result = new RandomEventSystem().RollCareEvent(
                tama,
                conditionSelectionRoll: 0f,
                conditionChanceRoll: 1f,
                choiceSelectionRoll: 0f,
                choiceChanceRoll: 0f,
                ambientChanceRoll: 1f);

            Assert.That(result.occurred, Is.True);
            Assert.That(result.eventId, Is.EqualTo("warm_lamp_choice"));
            Assert.That(result.RequiresChoice, Is.True);
        }

        [Test]
        public void ChoiceResultCarriesCurrencyStatsAndFollowUpHint()
        {
            var tama = new CheeseTamaModel();
            tama.stats.health = 30;
            tama.stats.sleepiness = 20;
            var economy = new EconomySaveData();
            var pending = new RandomEventSystem()
                .ForceCareEvent("warm_lamp_choice")
                .WithOccurrence("choice-occurrence-1", true);

            var result = new CareEventChoiceSystem().ApplyChoice(
                pending,
                "light_milk_lamp",
                tama,
                economy);

            Assert.That(result.status, Is.EqualTo(CareEventChoiceResolutionStatus.Applied));
            Assert.That(result.effect.milkDrops, Is.EqualTo(2));
            Assert.That(result.effect.health, Is.EqualTo(5));
            Assert.That(result.effect.affection, Is.EqualTo(3));
            Assert.That(result.effect.followUpAction, Is.EqualTo(CareEventFollowUpAction.FeedMilk));
            Assert.That(result.effect.followUpHint, Is.Not.Empty);
            Assert.That(result.effect.BuildSummary(), Does.Contain("우유방울 +2"));
            Assert.That(result.effect.BuildSummary(), Does.Contain("다음 행동"));
            Assert.That(economy.milkDrops, Is.EqualTo(2));
            Assert.That(tama.stats.health, Is.EqualTo(35));
        }

        [Test]
        public void ApplyingASecondChoiceForSameOccurrenceIsIdempotent()
        {
            var tama = new CheeseTamaModel();
            tama.stats.health = 30;
            tama.stats.sleepiness = 20;
            var economy = new EconomySaveData();
            var pending = new RandomEventSystem()
                .ForceCareEvent("warm_lamp_choice")
                .WithOccurrence("choice-occurrence-2", false);
            var system = new CareEventChoiceSystem();

            var first = system.ApplyChoice(pending, "wrap_blanket", tama, economy);
            var duplicate = system.ApplyChoice(pending, "light_milk_lamp", tama, economy);

            Assert.That(first.applied, Is.True);
            Assert.That(duplicate.status, Is.EqualTo(CareEventChoiceResolutionStatus.AlreadyApplied));
            Assert.That(duplicate.duplicate, Is.True);
            Assert.That(duplicate.choiceId, Is.EqualTo("wrap_blanket"));
            Assert.That(tama.stats.health, Is.EqualTo(40));
            Assert.That(tama.stats.sleepiness, Is.EqualTo(12));
            Assert.That(economy.milkDrops, Is.Zero);
        }

        [Test]
        public void MilkDropScoreAndRewardClampInvalidAndOverflowingInputs()
        {
            Assert.That(MilkDropMiniGameRules.DurationSeconds, Is.EqualTo(30f));
            Assert.That(MilkDropMiniGameRules.DropSizePixels, Is.EqualTo(56f));
            Assert.That(MilkDropMiniGameRules.SpawnIntervalSeconds, Is.EqualTo(0.48f));
            Assert.That(MilkDropMiniGameRules.MinimumFallSpeed, Is.EqualTo(300f));
            Assert.That(MilkDropMiniGameRules.MaximumFallSpeed, Is.EqualTo(500f));
            Assert.That(MilkDropMiniGameRules.CalculateScore(-1), Is.Zero);
            Assert.That(MilkDropMiniGameRules.CalculateScore(0), Is.Zero);
            Assert.That(MilkDropMiniGameRules.CalculateScore(3), Is.EqualTo(300));
            Assert.That(MilkDropMiniGameRules.CalculateScore(int.MaxValue), Is.EqualTo(int.MaxValue));

            var invalid = MilkDropMiniGameRules.CalculateReward(-4, -7, int.MaxValue);
            Assert.That(invalid.score, Is.Zero);
            Assert.That(invalid.caught, Is.Zero);
            Assert.That(invalid.missed, Is.Zero);
            Assert.That(invalid.milkCoins, Is.Zero);
            Assert.That(invalid.milkDrops, Is.Zero);

            var inflated = MilkDropMiniGameRules.CalculateReward(2, -3, 999);
            Assert.That(inflated.score, Is.EqualTo(200));
            Assert.That(inflated.caught, Is.EqualTo(2));
            Assert.That(inflated.missed, Is.Zero);
            Assert.That(inflated.milkCoins, Is.EqualTo(2));
            Assert.That(inflated.milkDrops, Is.EqualTo(1));

            var capped = MilkDropMiniGameRules.CalculateReward(int.MaxValue, 5, int.MaxValue);
            Assert.That(capped.score, Is.EqualTo(int.MaxValue));
            Assert.That(capped.milkCoins, Is.EqualTo(30));
            Assert.That(capped.milkDrops, Is.EqualTo(8));
        }

        [Test]
        public void MilkDropRewardCooldownUsesExactThirtyMinuteBoundary()
        {
            var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

            Assert.That(MilkDropMiniGameRules.EvaluateRewardCooldown(string.Empty, now).isAvailable, Is.True);
            Assert.That(MilkDropMiniGameRules.EvaluateRewardCooldown("not-a-date", now).isAvailable, Is.True);

            var beforeBoundary = MilkDropMiniGameRules.EvaluateRewardCooldown(
                now.AddMinutes(-29).AddSeconds(-59).ToString("O"),
                now);
            Assert.That(beforeBoundary.isAvailable, Is.False);
            Assert.That(beforeBoundary.remainingSeconds, Is.EqualTo(1));

            var atBoundary = MilkDropMiniGameRules.EvaluateRewardCooldown(
                now.AddMinutes(-30).ToString("O"),
                now);
            Assert.That(atBoundary.isAvailable, Is.True);

            var future = MilkDropMiniGameRules.EvaluateRewardCooldown(
                now.AddDays(1).ToString("O"),
                now);
            Assert.That(future.isAvailable, Is.False);
            Assert.That(future.remainingSeconds, Is.EqualTo(MilkDropMiniGameRules.RewardCooldownSeconds));
            Assert.That(future.shouldRepairTimestamp, Is.True);
        }

        [Test]
        public void LegacySaveAcknowledgesCurrentGrowthStageWithoutShowingOldMilestones()
        {
            using var fixture = IsolatedSaveManagerFixture.Create();
            var legacy = new LegacyGrowthSavePayload
            {
                cheeseTama = new CheeseTamaModel
                {
                    isHatched = true,
                    level = 20,
                    form = "grown_cheesetama"
                },
                onboarding = OnboardingSaveData.CreateCompletedForLegacySave()
            };
            fixture.WriteJson(JsonUtility.ToJson(legacy, true));

            var loaded = fixture.SaveManager.LoadOrCreate();

            Assert.That(fixture.SaveManager.LastLoadMigratedData, Is.True);
            Assert.That(loaded.growthMilestone, Is.Not.Null);
            Assert.That(loaded.growthMilestone.acknowledgedStage, Is.EqualTo(CheeseTamaGrowthStage.Grown));
        }

        private static CheeseTamaModel CreateNeutralEventTama()
        {
            var tama = new CheeseTamaModel();
            tama.stats.health = 100;
            tama.stats.hunger = 100;
            tama.stats.cleanliness = 100;
            tama.stats.sleepiness = 0;
            tama.stats.mood = 0;
            return tama;
        }

        private static void SetConditionBeyondThreshold(CheeseTamaModel tama, string eventId)
        {
            switch (eventId)
            {
                case "small_fever":
                    tama.stats.health = RandomEventSystem.LowHealthThreshold - 1;
                    break;
                case "hungry_peep":
                    tama.stats.hunger = RandomEventSystem.LowHungerThreshold - 1;
                    break;
                case "dusty_corner":
                    tama.stats.cleanliness = RandomEventSystem.LowCleanlinessThreshold - 1;
                    break;
                case "sleepy_yawn":
                    tama.stats.sleepiness = RandomEventSystem.HighSleepinessThreshold + 1;
                    break;
                case "happy_wiggle":
                    tama.stats.mood = RandomEventSystem.HighMoodThreshold + 1;
                    break;
                default:
                    Assert.Fail($"Unknown event id: {eventId}");
                    break;
            }
        }

        private static void AssertStrictThreshold(
            string eventId,
            Action<CheeseTamaModel> setAtBoundary,
            Action<CheeseTamaModel> setBeyondBoundary)
        {
            Assert.That(RandomEventSystem.TryGetDefinition(eventId, out var definition), Is.True);

            var tama = CreateNeutralEventTama();
            setAtBoundary(tama);
            Assert.That(definition.Matches(tama), Is.False, $"{eventId} should not match at its threshold.");

            tama = CreateNeutralEventTama();
            setBeyondBoundary(tama);
            Assert.That(definition.Matches(tama), Is.True, $"{eventId} should match beyond its threshold.");
        }

        [Serializable]
        private sealed class LegacyGrowthSavePayload
        {
            public string version = "0.1.0";
            public CheeseTamaModel cheeseTama;
            public OnboardingSaveData onboarding;
        }

        private sealed class IsolatedSaveManagerFixture : IDisposable
        {
            private readonly GameObject root;

            private IsolatedSaveManagerFixture(GameObject root, SaveManager saveManager)
            {
                this.root = root;
                SaveManager = saveManager;
            }

            public SaveManager SaveManager { get; }

            public static IsolatedSaveManagerFixture Create()
            {
                var root = new GameObject("Expanded Feature Save Fixture");
                var saveManager = root.AddComponent<SaveManager>();
                var fileNameField = typeof(SaveManager).GetField(
                    "saveFileName",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(fileNameField, Is.Not.Null);
                fileNameField.SetValue(
                    saveManager,
                    $"cheesetama_expanded_feature_test_{Guid.NewGuid():N}.json");
                return new IsolatedSaveManagerFixture(root, saveManager);
            }

            public void WriteJson(string json)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SaveManager.SaveFilePath));
                File.WriteAllText(SaveManager.SaveFilePath, json);
            }

            public void Dispose()
            {
                SaveManager.DeleteSave();

                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
