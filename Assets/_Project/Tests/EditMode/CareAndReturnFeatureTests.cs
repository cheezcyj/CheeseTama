using System;
using System.IO;
using System.Reflection;
using CheeseTama.Audio;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Care;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class CareAndReturnFeatureTests
    {
        [Test]
        public void PetImprovesMoodAndAffectionOnce()
        {
            var tama = new CheeseTamaModel();
            tama.stats.mood = 40;
            tama.stats.affection = 25;
            var progressBefore = tama.levelProgress;

            var result = new CareActionSystem().Pet(tama);

            Assert.That(result.success, Is.True);
            Assert.That(tama.stats.mood, Is.EqualTo(44));
            Assert.That(tama.stats.affection, Is.EqualTo(27));
            Assert.That(tama.levelProgress, Is.EqualTo(progressBefore + 1));
        }

        [Test]
        public void PetClampsStatsAndRejectsMissingTama()
        {
            var system = new CareActionSystem();
            var tama = new CheeseTamaModel();
            tama.stats.mood = 99;
            tama.stats.affection = 100;

            Assert.That(system.Pet(tama).success, Is.True);
            Assert.That(tama.stats.mood, Is.EqualTo(100));
            Assert.That(tama.stats.affection, Is.EqualTo(100));
            Assert.That(system.Pet(null).success, Is.False);
        }

        [Test]
        public void PetHistoryUnlocksFirstAndTenthRecords()
        {
            using var fixture = GameManagerFixture.Create("pet_history");
            fixture.Manager.LoadOrCreateGame();

            fixture.Manager.RegisterCareAction("pet");
            fixture.Manager.RefreshDerivedCollectionRecords();

            Assert.That(fixture.Manager.CurrentSave.careHistory.petSessions, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.collections.events, Does.Contain("pet_first"));

            for (var index = 1; index < 10; index += 1)
            {
                fixture.Manager.RegisterCareAction("pet");
            }

            fixture.Manager.RefreshDerivedCollectionRecords();
            Assert.That(fixture.Manager.CurrentSave.collections.events, Does.Contain("pet_sessions_10"));
        }

        [Test]
        public void OfflineLoadCreatesActualConsumeOnceReturnSummary()
        {
            using var fixture = GameManagerFixture.Create("return_summary");
            var save = SaveManager.CreateDefaultSave();
            save.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            save.cheeseTama.stats.hunger = 6;
            save.cheeseTama.stats.mood = 60;
            save.cheeseTama.stats.cleanliness = 50;
            save.cheeseTama.stats.sleepiness = 95;
            save.cheeseTama.stats.health = 80;
            save.cheeseTama.lastSavedAtIso = DateTimeOffset.Now.AddHours(-2).AddMinutes(-5).ToString("O");
            fixture.WriteSave(save);

            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.LastTimeProgression.applied, Is.True);
            Assert.That(fixture.Manager.TryGetPendingReturnSummary(out var summary), Is.True);
            Assert.That(summary.elapsedMinutes, Is.GreaterThanOrEqualTo(120));
            Assert.That(summary.before.hunger, Is.EqualTo(6));
            Assert.That(summary.after.hunger, Is.EqualTo(0));
            Assert.That(summary.after.hunger - summary.before.hunger, Is.EqualTo(-6));
            Assert.That(summary.before.sleepiness, Is.EqualTo(95));
            Assert.That(summary.after.sleepiness, Is.EqualTo(100));
            Assert.That(summary.after.sleepiness - summary.before.sleepiness, Is.EqualTo(5));

            Assert.That(fixture.Manager.ConsumePendingReturnSummary(summary.id), Is.True);
            Assert.That(fixture.Manager.ConsumePendingReturnSummary(summary.id), Is.False);
            Assert.That(fixture.Manager.TryGetPendingReturnSummary(out _), Is.False);
        }

        [Test]
        public void ManualTimeSkipDoesNotCreateReturnSummary()
        {
            using var fixture = GameManagerFixture.Create("manual_skip");
            fixture.Manager.LoadOrCreateGame();

            fixture.Manager.ApplyTimeSkipHours(2);

            Assert.That(fixture.Manager.LastTimeProgression.applied, Is.True);
            Assert.That(fixture.Manager.TryGetPendingReturnSummary(out _), Is.False);
        }

        [Test]
        public void DailyRoutineGrantsRewardOnceAndRaisesEvent()
        {
            using var fixture = GameManagerFixture.Create("daily_reward");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.dailyCare.dateKey = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            save.dailyCare.milkFeeds = DailyCareSaveData.EatingGoal;
            save.dailyCare.cookings = DailyCareSaveData.CookingGoal;
            save.dailyCare.playSessions = DailyCareSaveData.PlayGoal;
            save.dailyCare.cleanings = DailyCareSaveData.CleanGoal;
            save.dailyCare.rests = DailyCareSaveData.RestGoal - 1;
            save.dailyCare.lastCompletedDateKey = string.Empty;
            save.economy.milkCoins = 7;
            save.economy.milkDrops = 3;
            save.economy.collectionFragments = 2;
            var eventCount = 0;
            fixture.Manager.DailyRoutineCompleted += () => eventCount += 1;

            Assert.That(fixture.Manager.RegisterDailyCareAction("rest"), Is.True);
            Assert.That(save.economy.milkCoins, Is.EqualTo(7 + GameManager.DailyRoutineMilkCoinReward));
            Assert.That(save.economy.milkDrops, Is.EqualTo(3 + GameManager.DailyRoutineMilkDropReward));
            Assert.That(
                save.economy.collectionFragments,
                Is.EqualTo(2 + GameManager.DailyRoutineCollectionFragmentReward));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(save.dailyCare.completedRoutineCount, Is.EqualTo(1));

            Assert.That(fixture.Manager.RegisterDailyCareAction("rest"), Is.False);
            Assert.That(save.economy.milkCoins, Is.EqualTo(7 + GameManager.DailyRoutineMilkCoinReward));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void DailyRewardSaturatesAndUnknownActionCannotTriggerIt()
        {
            using var fixture = GameManagerFixture.Create("daily_saturation");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.dailyCare.dateKey = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            save.dailyCare.milkFeeds = DailyCareSaveData.EatingGoal;
            save.dailyCare.cookings = DailyCareSaveData.CookingGoal;
            save.dailyCare.playSessions = DailyCareSaveData.PlayGoal;
            save.dailyCare.cleanings = DailyCareSaveData.CleanGoal;
            save.dailyCare.rests = DailyCareSaveData.RestGoal;
            save.dailyCare.lastCompletedDateKey = string.Empty;
            save.economy.milkCoins = int.MaxValue - 2;
            save.economy.milkDrops = int.MaxValue - 1;
            save.economy.collectionFragments = int.MaxValue;

            Assert.That(fixture.Manager.RegisterDailyCareAction("unknown"), Is.False);
            Assert.That(save.dailyCare.completedRoutineCount, Is.EqualTo(0));
            Assert.That(fixture.Manager.RegisterDailyCareAction("play"), Is.True);
            Assert.That(save.economy.milkCoins, Is.EqualTo(int.MaxValue));
            Assert.That(save.economy.milkDrops, Is.EqualTo(int.MaxValue));
            Assert.That(save.economy.collectionFragments, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void CareEventCanRepeatAfterItsPerIdCooldownExpires()
        {
            using var fixture = GameManagerFixture.Create("care_event_repeat");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            save.cheeseTama.stats.health = RandomEventSystem.LowHealthThreshold - 1;
            save.cheeseTama.stats.hunger = 100;
            save.cheeseTama.stats.cleanliness = 100;
            save.cheeseTama.stats.sleepiness = 0;
            save.cheeseTama.stats.mood = 0;

            var now = DateTimeOffset.Now;
            save.randomEvents.dateKey = now.ToString("yyyy-MM-dd");
            save.randomEvents.eventsToday = 1;
            save.randomEvents.lastEventId = "small_fever";
            save.randomEvents.nextAllowedAtIso = now.AddMinutes(-1).ToString("O");
            save.randomEvents.history.Clear();
            save.randomEvents.history.Add(new RandomEventHistorySaveEntry
            {
                eventId = "small_fever",
                totalOccurrences = 1,
                lastOccurredAtIso = now.AddMinutes(-31).ToString("O")
            });

            Assert.That(RandomEventSystem.TryGetDefinition("small_fever", out var definition), Is.True);
            var previousRandomState = UnityEngine.Random.state;
            try
            {
                var seed = FindSeedThatPassesConditionChance(definition.chance);
                UnityEngine.Random.InitState(seed);
                var repeated = fixture.Manager.TryRollCareEvent();

                Assert.That(repeated.occurred, Is.True);
                Assert.That(repeated.eventId, Is.EqualTo("small_fever"));
                Assert.That(save.randomEvents.eventsToday, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Random.state = previousRandomState;
            }
        }

        [Test]
        public void MilkDropMiniGameGrantsEconomyOnlyOncePerThirtyMinutes()
        {
            using var fixture = GameManagerFixture.Create("milk_drop_cooldown");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            save.economy.milkCoins = 10;
            save.economy.milkDrops = 4;
            save.milkroomSession.lastRewardAtIso = "presence-reward-marker";
            var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

            var first = fixture.Manager.CompleteMilkDropMiniGame(5, 1, 500, now);
            Assert.That(first.currencyRewardGranted, Is.True);
            Assert.That(first.milkCoins, Is.EqualTo(5));
            Assert.That(first.milkDrops, Is.EqualTo(2));
            Assert.That(save.economy.milkCoins, Is.EqualTo(15));
            Assert.That(save.economy.milkDrops, Is.EqualTo(6));
            Assert.That(save.milkroomSession.lastMilkDropMiniGameRewardAtIso, Is.EqualTo(now.ToString("O")));
            Assert.That(save.milkroomSession.lastRewardAtIso, Is.EqualTo("presence-reward-marker"));

            var careAfterFirst = save.careHistory.playSessions;
            save.dailyCare.dateKey = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            save.dailyCare.milkFeeds = DailyCareSaveData.EatingGoal;
            save.dailyCare.cookings = DailyCareSaveData.CookingGoal;
            save.dailyCare.playSessions = DailyCareSaveData.PlayGoal - 1;
            save.dailyCare.cleanings = DailyCareSaveData.CleanGoal;
            save.dailyCare.rests = DailyCareSaveData.RestGoal;
            save.dailyCare.lastCompletedDateKey = string.Empty;
            var dailyPlayAfterFirst = save.dailyCare.playSessions;
            var second = fixture.Manager.CompleteMilkDropMiniGame(5, 1, 500, now.AddMinutes(29).AddSeconds(59));
            Assert.That(second.currencyRewardGranted, Is.False);
            Assert.That(second.milkCoins, Is.Zero);
            Assert.That(second.milkDrops, Is.Zero);
            Assert.That(second.rewardCooldownRemainingSeconds, Is.EqualTo(1));
            Assert.That(save.economy.milkCoins, Is.EqualTo(15));
            Assert.That(save.economy.milkDrops, Is.EqualTo(6));
            Assert.That(save.careHistory.playSessions, Is.EqualTo(careAfterFirst + 1));
            Assert.That(save.dailyCare.playSessions, Is.EqualTo(dailyPlayAfterFirst + 1));
            Assert.That(save.dailyCare.lastCompletedDateKey, Is.Empty);
            Assert.That(save.milkroomSession.lastMilkDropMiniGameRewardAtIso, Is.EqualTo(now.ToString("O")));

            save.dailyCare.milkFeeds = 0;
            var third = fixture.Manager.CompleteMilkDropMiniGame(5, 1, 500, now.AddMinutes(30));
            Assert.That(third.currencyRewardGranted, Is.True);
            Assert.That(save.economy.milkCoins, Is.EqualTo(20));
            Assert.That(save.economy.milkDrops, Is.EqualTo(8));
        }

        [Test]
        public void ZeroCatchDoesNotConsumeMilkDropRewardAndFutureTimestampIsRepaired()
        {
            using var fixture = GameManagerFixture.Create("milk_drop_zero_future");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

            fixture.Manager.CompleteMilkDropMiniGame(0, 5, 0, now);
            Assert.That(save.milkroomSession.lastMilkDropMiniGameRewardAtIso, Is.Empty);

            save.milkroomSession.lastMilkDropMiniGameRewardAtIso = now.AddDays(1).ToString("O");
            var blocked = fixture.Manager.CompleteMilkDropMiniGame(3, 1, 300, now);
            Assert.That(blocked.currencyRewardGranted, Is.False);
            Assert.That(save.milkroomSession.lastMilkDropMiniGameRewardAtIso, Is.EqualTo(now.ToString("O")));

            var beforeBoundary = fixture.Manager.CompleteMilkDropMiniGame(3, 1, 300, now.AddMinutes(29).AddSeconds(59));
            Assert.That(beforeBoundary.currencyRewardGranted, Is.False);
            var atBoundary = fixture.Manager.CompleteMilkDropMiniGame(3, 1, 300, now.AddMinutes(30));
            Assert.That(atBoundary.currencyRewardGranted, Is.True);
        }

        [Test]
        public void MilkDropRewardCooldownSurvivesSaveReload()
        {
            using var fixture = GameManagerFixture.Create("milk_drop_reload");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

            var first = fixture.Manager.CompleteMilkDropMiniGame(4, 1, 400, now);
            Assert.That(first.currencyRewardGranted, Is.True);
            var coinsAfterFirst = fixture.Manager.CurrentSave.economy.milkCoins;
            var dropsAfterFirst = fixture.Manager.CurrentSave.economy.milkDrops;

            fixture.Manager.ReloadGame();
            var retry = fixture.Manager.CompleteMilkDropMiniGame(4, 1, 400, now.AddMinutes(1));

            Assert.That(retry.currencyRewardGranted, Is.False);
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.EqualTo(coinsAfterFirst));
            Assert.That(fixture.Manager.CurrentSave.economy.milkDrops, Is.EqualTo(dropsAfterFirst));
            Assert.That(
                fixture.Manager.CurrentSave.milkroomSession.lastMilkDropMiniGameRewardAtIso,
                Is.EqualTo(now.ToString("O")));
        }

        [Test]
        public void BuilderCreatesReturnSummaryAndIdempotentPetInteraction()
        {
            var canvasObject = new GameObject("Feature Builder Test Canvas", typeof(RectTransform), typeof(Canvas));
            var characterObject = new GameObject("CheeseTamaRoot");
            try
            {
                var milkroomUi = canvasObject.AddComponent<MilkroomUIController>();
                canvasObject.AddComponent<TopMenuController>();
                var bottomBar = new GameObject("Bottom Action Bar", typeof(RectTransform));
                bottomBar.transform.SetParent(canvasObject.transform, false);
                bottomBar.AddComponent<BottomActionBarController>();
                var visual = characterObject.AddComponent<CheeseTamaVisualController>();

                InvokeBuilder("EnsureReturnSummary", canvasObject.transform);
                InvokeBuilder(
                    "EnsureCheeseTamaPetInteraction",
                    canvasObject.transform,
                    milkroomUi,
                    visual);
                InvokeBuilder(
                    "EnsureCheeseTamaPetInteraction",
                    canvasObject.transform,
                    milkroomUi,
                    visual);

                var overlay = canvasObject.transform.Find("Return Summary Overlay");
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(overlay.GetComponent<Image>()?.raycastTarget, Is.True);
                Assert.That(overlay.GetComponent<CanvasGroup>()?.blocksRaycasts, Is.True);
                Assert.That(canvasObject.GetComponent<ReturnSummaryController>(), Is.Not.Null);
                Assert.That(
                    overlay.Find("Return Summary Card/Return Summary Confirm Button/Label")
                        ?.GetComponent<Text>()?.text,
                    Is.EqualTo("확인"));
                Assert.That(characterObject.GetComponents<BoxCollider>(), Has.Length.EqualTo(1));
                Assert.That(
                    characterObject.GetComponents<CheeseTamaPetInteractionController>(),
                    Has.Length.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(characterObject);
            }
        }

        [Test]
        public void BuilderPlacesCheeseTamaAboveTheRugAndUpdatesItsRestingPosition()
        {
            var parent = new GameObject("Grounding Test Parent");
            var character = new GameObject("Grounding Test Character");
            try
            {
                parent.transform.position = new Vector3(3f, 2f, -4f);
                character.transform.SetParent(parent.transform, false);
                character.transform.position = new Vector3(0f, -1.118f, 0.08f);
                var controller = character.AddComponent<CheeseTamaVisualController>();

                InvokeBuilder("AlignCheeseTamaRestingPosition", controller);

                Assert.That(character.transform.position.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(character.transform.position.y, Is.EqualTo(-1.1f).Within(0.0001f));
                Assert.That(character.transform.position.z, Is.EqualTo(0.08f).Within(0.0001f));
                var restingPosition = (Vector3)GetPrivateField(controller, "restingLocalPosition");
                Assert.That(restingPosition, Is.EqualTo(character.transform.localPosition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void BuilderCreatesExpandedFeatureOverlaysIdempotently()
        {
            var canvasObject = new GameObject("Expanded Builder Test Canvas", typeof(RectTransform), typeof(Canvas));
            var characterObject = new GameObject("Expanded Builder Character");
            try
            {
                var milkroomUi = canvasObject.AddComponent<MilkroomUIController>();
                canvasObject.AddComponent<TopMenuController>();
                var actionBar = new GameObject("Bottom Action Bar", typeof(RectTransform), typeof(Image));
                actionBar.transform.SetParent(canvasObject.transform, false);
                actionBar.AddComponent<BottomActionBarController>();
                var visual = characterObject.AddComponent<CheeseTamaVisualController>();

                InvokeBuilder("EnsureGrowthMilestone", canvasObject.transform, milkroomUi, visual);
                InvokeBuilder("EnsureMilkDropMiniGame", canvasObject.transform, milkroomUi, visual);
                InvokeBuilder("EnsureCareEventCard", canvasObject.transform, visual);
                InvokeBuilder("EnsureGrowthMilestone", canvasObject.transform, milkroomUi, visual);
                InvokeBuilder("EnsureMilkDropMiniGame", canvasObject.transform, milkroomUi, visual);
                InvokeBuilder("EnsureCareEventCard", canvasObject.transform, visual);

                Assert.That(canvasObject.transform.Find("Growth Achievement Overlay"), Is.Not.Null);
                Assert.That(canvasObject.transform.Find("Milk Drop Catch Overlay"), Is.Not.Null);
                Assert.That(canvasObject.transform.Find("Care Event Overlay"), Is.Not.Null);
                Assert.That(canvasObject.GetComponents<GrowthMilestoneController>(), Has.Length.EqualTo(1));
                Assert.That(canvasObject.GetComponents<MilkDropMiniGameController>(), Has.Length.EqualTo(1));
                Assert.That(canvasObject.GetComponents<CareEventCardController>(), Has.Length.EqualTo(1));
                var dropTemplate = canvasObject.transform.Find(
                    "Milk Drop Catch Overlay/Milk Drop Catch Card/Milk Drop Catch Play Area/Milk Drop Template");
                Assert.That(dropTemplate, Is.Not.Null);
                Assert.That(dropTemplate.GetComponent<Image>()?.sprite?.name, Is.EqualTo("milkdrop"));
                Assert.That(dropTemplate.Find("Label")?.gameObject.activeSelf, Is.False);
                var miniGame = canvasObject.GetComponent<MilkDropMiniGameController>();
                InvokePrivate(miniGame, "CreateDrop");
                var pooledDrop = canvasObject.transform.Find(
                    "Milk Drop Catch Overlay/Milk Drop Catch Card/Milk Drop Catch Play Area/Milk Drop Pool Item 1")
                    ?.GetComponent<RectTransform>();
                Assert.That(pooledDrop, Is.Not.Null);
                Assert.That(pooledDrop.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(pooledDrop.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(pooledDrop.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(
                    pooledDrop.sizeDelta,
                    Is.EqualTo(Vector2.one * MilkDropMiniGameRules.DropSizePixels));
                var playArea = pooledDrop.parent.GetComponent<RectTransform>();
                SetPrivateField(miniGame, "sessionActive", true);
                InvokePrivate(miniGame, "SpawnDrop");
                var halfWidth = pooledDrop.rect.width * 0.5f;
                var halfHeight = pooledDrop.rect.height * 0.5f;
                Assert.That(
                    pooledDrop.anchoredPosition.x,
                    Is.InRange(playArea.rect.xMin + halfWidth, playArea.rect.xMax - halfWidth));
                Assert.That(
                    pooledDrop.anchoredPosition.y,
                    Is.EqualTo(playArea.rect.yMax + halfHeight).Within(0.001f));
                Assert.That(
                    canvasObject.transform.Find("Care Event Overlay/Care Event Card/First Discovery Badge/First Discovery Badge Text")
                        ?.GetComponent<Text>()?.text,
                    Is.EqualTo("새 도감 기록"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(characterObject);
            }
        }

        [Test]
        public void AudioCreatesOneMusicAndOneEffectSource()
        {
            var audioObject = new GameObject("Audio Controller Test");
            try
            {
                var controller = audioObject.AddComponent<CheeseTamaAudioController>();
                InvokePrivate(controller, "Awake");
                controller.ReloadAudioAssets();

                Assert.That(audioObject.GetComponents<AudioSource>(), Has.Length.EqualTo(2));
                Assert.That(controller.MusicSource, Is.Not.Null);
                Assert.That(controller.MusicSource.clip, Is.Not.Null);
                Assert.That(controller.MusicSource.loop, Is.True);
                Assert.That(controller.MusicSource.spatialBlend, Is.EqualTo(0f));
                Assert.That(controller.EffectSource, Is.Not.Null);
                Assert.That(controller.EffectSource.loop, Is.False);
                Assert.That(controller.MusicSource.clip.samples, Is.GreaterThan(0));
                Assert.That(controller.MusicSource.clip.frequency, Is.GreaterThan(0));
                Assert.That(controller.UsingAuthoredAudioAssets, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(audioObject);
            }
        }

        [Test]
        public void DailyRoutineSectionFitsInsideRecordPanelWithBottomPadding()
        {
            var canvasObject = new GameObject("Record Layout Test Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var panel = CreateRect(canvasObject.transform, "Status Panel", new Vector2(360f, 510f));
                var identity = CreateRect(panel, "Record Identity Section", new Vector2(336f, 88f));
                identity.anchoredPosition = new Vector2(12f, -58f);
                var growth = CreateRect(panel, "Record Growth Section", new Vector2(336f, 126f));
                growth.anchoredPosition = new Vector2(12f, -158f);
                var care = CreateRect(panel, "Record Care Summary Section", new Vector2(336f, 80f));
                care.anchoredPosition = new Vector2(12f, -296f);
                var daily = CreateRect(panel, "Record Daily Routine Section", new Vector2(336f, 100f));
                daily.anchoredPosition = new Vector2(12f, -388f);

                var careText = CreateText(care, "Care Summary Text", "<b>돌봄 누적</b>\n<size=3> </size>\n쓰다듬기 0  놀이 0  청소 0  휴식 0");
                var dailyText = CreateText(
                    daily,
                    "Daily Routine Text",
                    "<b>오늘 루틴</b>\n<size=3> </size>\n먹기 0/3  요리 0/2\n놀이 0/3  청소 0/2  휴식 0/2\n<size=14>완료 보상  코인 20 · 우유방울 5 · 도감조각 1</size>");
                var controller = canvasObject.AddComponent<MilkroomUIController>();
                SetPrivateField(controller, "careSummaryText", careText);
                SetPrivateField(controller, "dailyRoutineText", dailyText);

                controller.RefreshRecordPanelLayout();

                var dailyBottom = -daily.anchoredPosition.y + daily.rect.height;
                Assert.That(panel.rect.height - dailyBottom, Is.GreaterThanOrEqualTo(12f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static void InvokeBuilder(string methodName, params object[] args)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(null, args);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 size)
        {
            var child = new GameObject(name, typeof(RectTransform));
            var rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(RectTransform parent, string name, string value)
        {
            var rect = CreateRect(parent, name, new Vector2(316f, 74f));
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value;
            label.fontSize = 16;
            label.supportRichText = true;
            return label;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return field.GetValue(target);
        }

        private static int FindSeedThatPassesConditionChance(float chance)
        {
            for (var seed = 0; seed < 10000; seed += 1)
            {
                UnityEngine.Random.InitState(seed);
                _ = UnityEngine.Random.value; // condition selection
                if (RandomEventSystem.PassesChance(UnityEngine.Random.value, chance))
                {
                    return seed;
                }
            }

            Assert.Fail($"Could not find a deterministic event seed for chance {chance}.");
            return 0;
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
                    $"cheesetama_feature_test_{label}_{Guid.NewGuid():N}.json");
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
