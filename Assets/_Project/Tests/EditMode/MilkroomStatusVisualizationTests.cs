using System;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class MilkroomStatusVisualizationTests
    {
        private static readonly Color WarningGaugeColor = new Color(0.88f, 0.28f, 0.18f, 1f);
        private static readonly Color WarningTextColor = new Color(0.64f, 0.12f, 0.08f, 1f);
        private static readonly Color CleanlinessGaugeColor = new Color(0.26f, 0.68f, 0.82f, 1f);
        private static readonly Color HealthGaugeColor = new Color(0.30f, 0.70f, 0.38f, 1f);
        private static readonly Color NormalTextColor = new Color(0.22f, 0.17f, 0.12f, 1f);

        [Test]
        public void BindClampsGaugeValuesAndAppliesPerStatWarnings()
        {
            using var fixture = new StatusGaugeFixture();
            var tama = new CheeseTamaModel();
            tama.stats.hunger = -10;
            tama.stats.mood = 44;
            tama.stats.cleanliness = 101;
            tama.stats.sleepiness = 76;
            tama.stats.health = 35;

            fixture.Controller.Bind(tama);

            Assert.That(fixture.HungerFill.fillAmount, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(fixture.MoodFill.fillAmount, Is.EqualTo(0.44f).Within(0.0001f));
            Assert.That(fixture.CleanlinessFill.fillAmount, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(fixture.SleepinessFill.fillAmount, Is.EqualTo(0.76f).Within(0.0001f));
            Assert.That(fixture.HealthFill.fillAmount, Is.EqualTo(0.35f).Within(0.0001f));

            AssertColor(fixture.HungerFill.color, WarningGaugeColor);
            AssertColor(fixture.MoodFill.color, WarningGaugeColor);
            AssertColor(fixture.CleanlinessFill.color, CleanlinessGaugeColor);
            AssertColor(fixture.SleepinessFill.color, WarningGaugeColor);
            AssertColor(fixture.HealthFill.color, HealthGaugeColor);
            AssertColor(fixture.HungerText.color, WarningTextColor);
            AssertColor(fixture.HealthText.color, NormalTextColor);

            Assert.That(fixture.HungerText.text, Does.Contain("0/100"));
            Assert.That(fixture.HungerText.text, Does.Contain("주의"));
            Assert.That(fixture.MoodText.text, Does.Contain("주의"));
            Assert.That(fixture.CleanlinessText.text, Does.Contain("100/100"));
            Assert.That(fixture.CleanlinessText.text, Does.Not.Contain("주의"));
            Assert.That(fixture.SleepinessText.text, Does.Contain("주의"));
            Assert.That(fixture.HealthText.text, Does.Not.Contain("주의"));
        }

        [Test]
        public void GaugeChangesOnlyWhenRefreshedAndConfigureIsIdempotent()
        {
            using var fixture = new StatusGaugeFixture();
            var tama = new CheeseTamaModel();
            tama.stats.hunger = 80;
            fixture.Controller.Bind(tama);
            var initialText = fixture.HungerText.text;

            tama.stats.hunger = 10;

            Assert.That(fixture.HungerFill.fillAmount, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(fixture.HungerText.text, Is.EqualTo(initialText));

            fixture.Controller.Refresh();

            Assert.That(fixture.HungerFill.fillAmount, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(fixture.HungerText.text, Does.Contain("주의"));
            AssertColor(fixture.HungerFill.color, WarningGaugeColor);

            fixture.ConfigureGauges();

            Assert.That(fixture.HungerFill.fillAmount, Is.EqualTo(0.1f).Within(0.0001f));
            AssertColor(fixture.HungerFill.color, WarningGaugeColor);
            AssertGaugeConfiguration(fixture.HungerFill);
            AssertGaugeConfiguration(fixture.MoodFill);
            AssertGaugeConfiguration(fixture.CleanlinessFill);
            AssertGaugeConfiguration(fixture.SleepinessFill);
            AssertGaugeConfiguration(fixture.HealthFill);
        }

        [Test]
        public void BuilderCreatesAndConfiguresFiveGaugesIdempotentlyAfterBind()
        {
            var root = new GameObject("Status Gauge Builder Root", typeof(RectTransform), typeof(Canvas));
            try
            {
                var statBarObject = new GameObject(
                    "Stat Bar",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                statBarObject.transform.SetParent(root.transform, false);
                var statBar = statBarObject.transform;
                var hungerText = CreateText(statBar, "Hunger Text");
                var moodText = CreateText(statBar, "Mood Text");
                var cleanlinessText = CreateText(statBar, "Cleanliness Text");
                var sleepinessText = CreateText(statBar, "Sleepiness Text");
                var healthText = CreateText(statBar, "Health Text");
                var controller = root.AddComponent<MilkroomUIController>();
                ConfigureController(
                    controller,
                    hungerText,
                    moodText,
                    cleanlinessText,
                    sleepinessText,
                    healthText);
                var tama = new CheeseTamaModel();
                tama.stats.hunger = 10;
                controller.Bind(tama);

                InvokeEnsureStatGauges(root.transform, controller);
                InvokeEnsureStatGauges(root.transform, controller);

                AssertBuilderGauge(statBar, "Hunger Gauge", 0.1f);
                AssertBuilderGauge(statBar, "Mood Gauge", 0.7f);
                AssertBuilderGauge(statBar, "Cleanliness Gauge", 0.9f);
                AssertBuilderGauge(statBar, "Sleepiness Gauge", 0.2f);
                AssertBuilderGauge(statBar, "Health Gauge", 1f);
                AssertTextRect(hungerText, -72f);
                AssertTextRect(moodText, -132f);
                AssertTextRect(cleanlinessText, -192f);
                AssertTextRect(sleepinessText, -252f);
                AssertTextRect(healthText, -312f);
                Assert.That(hungerText.text, Does.Contain("주의"));
                AssertColor(
                    statBar.Find("Hunger Gauge/Fill").GetComponent<Image>().color,
                    WarningGaugeColor);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MissingGaugeImagesKeepTextWarningsFunctional()
        {
            using var fixture = new StatusGaugeFixture();
            fixture.Controller.ConfigureStatGauges(null, null, null, null, null);
            var tama = new CheeseTamaModel();
            tama.stats.hunger = 29;

            Assert.DoesNotThrow(() => fixture.Controller.Bind(tama));
            Assert.That(fixture.HungerText.text, Does.Contain("29/100"));
            Assert.That(fixture.HungerText.text, Does.Contain("주의"));
            AssertColor(fixture.HungerText.color, WarningTextColor);
        }

        private static void AssertGaugeConfiguration(Image fill)
        {
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
            Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left));
            Assert.That(fill.preserveAspect, Is.False);
            Assert.That(fill.raycastTarget, Is.False);
        }

        private static void AssertBuilderGauge(Transform statBar, string name, float expectedFill)
        {
            Assert.That(CountDirectChildren(statBar, name), Is.EqualTo(1));
            var track = statBar.Find(name);
            Assert.That(track, Is.Not.Null);
            Assert.That(CountDirectChildren(track, "Fill"), Is.EqualTo(1));
            var trackRect = track.GetComponent<RectTransform>();
            Assert.That(trackRect.sizeDelta, Is.EqualTo(new Vector2(306f, 10f)));
            var fill = track.Find("Fill").GetComponent<Image>();
            Assert.That(fill, Is.Not.Null);
            Assert.That(fill.fillAmount, Is.EqualTo(expectedFill).Within(0.0001f));
            AssertGaugeConfiguration(fill);
        }

        private static void AssertTextRect(Text label, float expectedY)
        {
            Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(22f, expectedY)));
            Assert.That(label.rectTransform.sizeDelta, Is.EqualTo(new Vector2(306f, 30f)));
        }

        private static int CountDirectChildren(Transform parent, string name)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                if (parent.GetChild(index).name == name)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static void InvokeEnsureStatGauges(
            Transform canvasTransform,
            MilkroomUIController controller)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                "EnsureMilkroomStatGauges",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { canvasTransform, controller });
        }

        private static Text CreateText(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Text>();
        }

        private static void ConfigureController(
            MilkroomUIController controller,
            Text hungerText,
            Text moodText,
            Text cleanlinessText,
            Text sleepinessText,
            Text healthText)
        {
            controller.Configure(
                null,
                null,
                null,
                null,
                hungerText,
                moodText,
                cleanlinessText,
                sleepinessText,
                healthText,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }

        private sealed class StatusGaugeFixture : IDisposable
        {
            public StatusGaugeFixture()
            {
                Root = new GameObject("Status Gauge Test Root", typeof(RectTransform), typeof(Canvas));
                Controller = Root.AddComponent<MilkroomUIController>();
                HungerText = CreateText("Hunger Text");
                MoodText = CreateText("Mood Text");
                CleanlinessText = CreateText("Cleanliness Text");
                SleepinessText = CreateText("Sleepiness Text");
                HealthText = CreateText("Health Text");
                HungerFill = CreateImage("Hunger Gauge Fill");
                MoodFill = CreateImage("Mood Gauge Fill");
                CleanlinessFill = CreateImage("Cleanliness Gauge Fill");
                SleepinessFill = CreateImage("Sleepiness Gauge Fill");
                HealthFill = CreateImage("Health Gauge Fill");

                ConfigureController(
                    Controller,
                    HungerText,
                    MoodText,
                    CleanlinessText,
                    SleepinessText,
                    HealthText);
                ConfigureGauges();
            }

            public GameObject Root { get; }
            public MilkroomUIController Controller { get; }
            public Text HungerText { get; }
            public Text MoodText { get; }
            public Text CleanlinessText { get; }
            public Text SleepinessText { get; }
            public Text HealthText { get; }
            public Image HungerFill { get; }
            public Image MoodFill { get; }
            public Image CleanlinessFill { get; }
            public Image SleepinessFill { get; }
            public Image HealthFill { get; }

            public void ConfigureGauges()
            {
                Controller.ConfigureStatGauges(
                    HungerFill,
                    MoodFill,
                    CleanlinessFill,
                    SleepinessFill,
                    HealthFill);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }

            private Text CreateText(string name)
            {
                var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                child.transform.SetParent(Root.transform, false);
                return child.GetComponent<Text>();
            }

            private Image CreateImage(string name)
            {
                var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child.transform.SetParent(Root.transform, false);
                return child.GetComponent<Image>();
            }
        }
    }
}
