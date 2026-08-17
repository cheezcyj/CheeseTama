using System;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class AtmosphereCleaningFeatureTests
    {
        [TestCase(4, MilkroomTimeBand.Night)]
        [TestCase(5, MilkroomTimeBand.Morning)]
        [TestCase(10, MilkroomTimeBand.Morning)]
        [TestCase(11, MilkroomTimeBand.Afternoon)]
        [TestCase(16, MilkroomTimeBand.Afternoon)]
        [TestCase(17, MilkroomTimeBand.Evening)]
        [TestCase(21, MilkroomTimeBand.Evening)]
        [TestCase(22, MilkroomTimeBand.Night)]
        [TestCase(29, MilkroomTimeBand.Morning)]
        public void AtmosphereTimeBandUsesStableLocalHourBoundaries(
            int hour,
            MilkroomTimeBand expected)
        {
            Assert.That(MilkroomAtmosphereLayerRules.ResolveTimeBand(hour), Is.EqualTo(expected));
        }

        [Test]
        public void AtmosphereConditionUsesStrictThresholdsAndCharacterPriority()
        {
            var tama = CreateNeutralTama();
            tama.stats.health = MilkroomAtmosphereLayerRules.SickThreshold;
            tama.stats.hunger = MilkroomAtmosphereLayerRules.HungryThreshold;
            tama.stats.cleanliness = MilkroomAtmosphereLayerRules.MessyThreshold;
            tama.stats.sleepiness = MilkroomAtmosphereLayerRules.SleepyThreshold;
            Assert.That(
                MilkroomAtmosphereLayerRules.ResolveCondition(tama),
                Is.EqualTo(MilkroomAtmosphereCondition.Normal));

            tama.stats.sleepiness = MilkroomAtmosphereLayerRules.SleepyThreshold + 1;
            Assert.That(
                MilkroomAtmosphereLayerRules.ResolveCondition(tama),
                Is.EqualTo(MilkroomAtmosphereCondition.Sleepy));

            tama.stats.cleanliness = MilkroomAtmosphereLayerRules.MessyThreshold - 1;
            Assert.That(
                MilkroomAtmosphereLayerRules.ResolveCondition(tama),
                Is.EqualTo(MilkroomAtmosphereCondition.Messy));

            tama.stats.hunger = MilkroomAtmosphereLayerRules.HungryThreshold - 1;
            Assert.That(
                MilkroomAtmosphereLayerRules.ResolveCondition(tama),
                Is.EqualTo(MilkroomAtmosphereCondition.Hungry));

            tama.stats.health = MilkroomAtmosphereLayerRules.SickThreshold - 1;
            Assert.That(
                MilkroomAtmosphereLayerRules.ResolveCondition(tama),
                Is.EqualTo(MilkroomAtmosphereCondition.Sick));
        }

        [Test]
        public void AtmosphereControllerOnlyWritesOwnedOverlayAndAuxiliaryLight()
        {
            var root = new GameObject("Atmosphere Test Root");
            var overlayObject = new GameObject(
                "Atmosphere Overlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var auxiliaryObject = new GameObject("Atmosphere Auxiliary Light", typeof(Light));
            var themeLightObject = new GameObject("Theme Owned Light", typeof(Light));
            try
            {
                overlayObject.transform.SetParent(root.transform, false);
                auxiliaryObject.transform.SetParent(root.transform, false);
                themeLightObject.transform.SetParent(root.transform, false);

                var overlay = overlayObject.GetComponent<Image>();
                overlay.raycastTarget = true;
                var auxiliaryLight = auxiliaryObject.GetComponent<Light>();
                var themeLight = themeLightObject.GetComponent<Light>();
                var originalThemeColor = new Color(0.15f, 0.35f, 0.75f);
                const float originalThemeIntensity = 1.37f;
                themeLight.color = originalThemeColor;
                themeLight.intensity = originalThemeIntensity;

                var tama = CreateNeutralTama();
                tama.stats.cleanliness = 10;
                var controller = root.AddComponent<MilkroomAtmosphereLayerController>();
                controller.Configure(overlay, auxiliaryLight);
                controller.Bind(tama);
                var localTime = new DateTimeOffset(2026, 8, 13, 23, 0, 0, TimeSpan.FromHours(9));
                controller.Refresh(localTime);

                var expected = MilkroomAtmosphereLayerRules.Evaluate(localTime, tama);
                Assert.That(controller.CurrentLayer.timeBand, Is.EqualTo(MilkroomTimeBand.Night));
                Assert.That(controller.CurrentLayer.condition, Is.EqualTo(MilkroomAtmosphereCondition.Messy));
                Assert.That(overlay.raycastTarget, Is.False);
                Assert.That(overlay.color.r, Is.EqualTo(expected.overlayColor.r).Within(0.0001f));
                Assert.That(overlay.color.g, Is.EqualTo(expected.overlayColor.g).Within(0.0001f));
                Assert.That(overlay.color.b, Is.EqualTo(expected.overlayColor.b).Within(0.0001f));
                Assert.That(overlay.color.a, Is.EqualTo(expected.overlayOpacity).Within(0.0001f));
                Assert.That(auxiliaryLight.intensity, Is.EqualTo(expected.auxiliaryLightIntensity).Within(0.0001f));

                Assert.That(themeLight.color.r, Is.EqualTo(originalThemeColor.r).Within(0.0001f));
                Assert.That(themeLight.color.g, Is.EqualTo(originalThemeColor.g).Within(0.0001f));
                Assert.That(themeLight.color.b, Is.EqualTo(originalThemeColor.b).Within(0.0001f));
                Assert.That(themeLight.intensity, Is.EqualTo(originalThemeIntensity));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AtmosphereLayerStrengthIsAlwaysPresentationSafe()
        {
            var tama = CreateNeutralTama();
            tama.stats.health = 0;
            for (var hour = 0; hour < 24; hour += 1)
            {
                var data = MilkroomAtmosphereLayerRules.Evaluate(
                    new DateTimeOffset(2026, 8, 13, hour, 0, 0, TimeSpan.Zero),
                    tama);
                Assert.That(data.overlayOpacity, Is.InRange(0f, MilkroomAtmosphereLayerRules.MaximumOverlayOpacity));
                Assert.That(data.auxiliaryLightIntensity, Is.InRange(0f, MilkroomAtmosphereLayerRules.MaximumAuxiliaryLightIntensity));
            }
        }

        [Test]
        public void CleaningRulesFitShortSessionAndClampScores()
        {
            Assert.That(CleaningMiniGameRules.DurationSeconds, Is.InRange(20f, 30f));
            Assert.That(CleaningMiniGameRules.SpawnIntervalSeconds, Is.GreaterThan(0f));
            Assert.That(CleaningMiniGameRules.SpotLifetimeSeconds, Is.GreaterThan(CleaningMiniGameRules.SpawnIntervalSeconds));
            Assert.That(CleaningMiniGameRules.InitialPoolSize, Is.GreaterThan(0));
            Assert.That(CleaningMiniGameRules.MaximumPoolSize, Is.GreaterThanOrEqualTo(CleaningMiniGameRules.InitialPoolSize));

            Assert.That(CleaningMiniGameRules.CalculateScore(-1), Is.Zero);
            Assert.That(CleaningMiniGameRules.CalculateScore(3), Is.EqualTo(300));
            Assert.That(CleaningMiniGameRules.CalculateScore(int.MaxValue), Is.EqualTo(int.MaxValue));
            Assert.That(CleaningMiniGameRules.ClampReportedScore(2, 999), Is.EqualTo(200));
            Assert.That(CleaningMiniGameRules.ClampReportedScore(2, -1), Is.Zero);
            Assert.That(
                CleaningMiniGameRules.QualifiesForCareReward(
                    CleaningMiniGameRules.MinimumCleanedSpotsForCareReward - 1),
                Is.False);
            Assert.That(
                CleaningMiniGameRules.QualifiesForCareReward(
                    CleaningMiniGameRules.MinimumCleanedSpotsForCareReward),
                Is.True);
        }

        [Test]
        public void CleaningTimerAndGradeUseExactBoundaries()
        {
            Assert.That(
                CleaningMiniGameRules.GetRemainingSeconds(CleaningMiniGameRules.DurationSeconds - 0.01f),
                Is.EqualTo(0.01f).Within(0.001f));
            Assert.That(CleaningMiniGameRules.IsComplete(CleaningMiniGameRules.DurationSeconds - 0.01f), Is.False);
            Assert.That(CleaningMiniGameRules.IsComplete(CleaningMiniGameRules.DurationSeconds), Is.True);
            Assert.That(CleaningMiniGameRules.GetRemainingSeconds(float.MaxValue), Is.Zero);

            Assert.That(CleaningMiniGameRules.GetGrade(0, 0), Is.EqualTo("연습 필요"));
            Assert.That(CleaningMiniGameRules.GetGrade(12, 6), Is.EqualTo("깨끗해요"));
            Assert.That(CleaningMiniGameRules.GetGrade(20, 3), Is.EqualTo("반짝반짝"));
        }

        [Test]
        public void CleaningCompletionResultRejectsInflatedAndNegativeValues()
        {
            var result = new CleaningMiniGameCompletionResult(
                int.MaxValue,
                2,
                -7,
                -25,
                null,
                true);

            Assert.That(result.score, Is.EqualTo(200));
            Assert.That(result.cleanedSpots, Is.EqualTo(2));
            Assert.That(result.missedSpots, Is.Zero);
            Assert.That(result.cleanlinessGain, Is.Zero);
            Assert.That(result.message, Is.Empty);
            Assert.That(result.success, Is.True);
        }

        [Test]
        public void CleaningCompletionRequiresParticipationBeforeCareIsCommitted()
        {
            using var fixture = GameManagerFixture.Create("cleaning_completion");
            fixture.Manager.LoadOrCreateGame();
            var save = fixture.Manager.CurrentSave;
            save.cheeseTama.stats.cleanliness = 20;
            var historyBefore = save.careHistory.cleanings;
            var dailyBefore = save.dailyCare.cleanings;

            var insufficient = fixture.Manager.CompleteCleaningMiniGame(
                CleaningMiniGameRules.MinimumCleanedSpotsForCareReward - 1,
                4,
                int.MaxValue);

            Assert.That(insufficient.success, Is.False);
            Assert.That(insufficient.cleanlinessGain, Is.Zero);
            Assert.That(save.cheeseTama.stats.cleanliness, Is.EqualTo(20));
            Assert.That(save.careHistory.cleanings, Is.EqualTo(historyBefore));
            Assert.That(save.dailyCare.cleanings, Is.EqualTo(dailyBefore));

            var completed = fixture.Manager.CompleteCleaningMiniGame(
                CleaningMiniGameRules.MinimumCleanedSpotsForCareReward,
                1,
                int.MaxValue);

            Assert.That(completed.success, Is.True);
            Assert.That(completed.score, Is.EqualTo(
                CleaningMiniGameRules.CalculateScore(
                    CleaningMiniGameRules.MinimumCleanedSpotsForCareReward)));
            Assert.That(completed.cleanlinessGain, Is.EqualTo(25));
            Assert.That(save.cheeseTama.stats.cleanliness, Is.EqualTo(45));
            Assert.That(save.careHistory.cleanings, Is.EqualTo(historyBefore + 1));
            Assert.That(save.dailyCare.cleanings, Is.EqualTo(dailyBefore + 1));
        }

        [Test]
        public void CleaningControllerPrewarmsAndReusesFixedPool()
        {
            var host = new GameObject("Cleaning Controller Test Host");
            var overlay = new GameObject("Cleaning Overlay", typeof(RectTransform));
            var playAreaObject = new GameObject("Cleaning Play Area", typeof(RectTransform));
            var template = CreateButton("Dirt Template");
            var cancel = CreateButton("Cancel");
            var confirm = CreateButton("Confirm");
            try
            {
                overlay.transform.SetParent(host.transform, false);
                playAreaObject.transform.SetParent(overlay.transform, false);
                template.transform.SetParent(playAreaObject.transform, false);
                cancel.transform.SetParent(overlay.transform, false);
                confirm.transform.SetParent(overlay.transform, false);

                var controller = host.AddComponent<CleaningMiniGameController>();
                controller.Configure(
                    overlay,
                    playAreaObject.GetComponent<RectTransform>(),
                    template.GetComponent<Button>(),
                    null,
                    null,
                    null,
                    null,
                    cancel.GetComponent<Button>(),
                    confirm.GetComponent<Button>(),
                    null,
                    null,
                    null,
                    null,
                    null);

                InvokePrivate(controller, "PrewarmPool");
                Assert.That(controller.PooledSpotCount, Is.EqualTo(CleaningMiniGameRules.InitialPoolSize));
                Assert.That(controller.ActiveSpotCount, Is.Zero);

                InvokePrivate(controller, "PrewarmPool");
                Assert.That(controller.PooledSpotCount, Is.EqualTo(CleaningMiniGameRules.InitialPoolSize));
                Assert.That(controller.ActiveSpotCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static CheeseTamaModel CreateNeutralTama()
        {
            var tama = new CheeseTamaModel();
            tama.EnsureRuntimeDefaults();
            tama.stats.health = 100;
            tama.stats.hunger = 100;
            tama.stats.cleanliness = 100;
            tama.stats.sleepiness = 0;
            return tama;
        }

        private static GameObject CreateButton(string name)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            var button = gameObject.GetComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            return gameObject;
        }

        private static void InvokePrivate(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
            method.Invoke(instance, null);
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
                    $"cheesetama_atmosphere_cleaning_test_{label}_{Guid.NewGuid():N}.json");
                saveManagerField.SetValue(manager, saveManager);
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
