using System;
using System.Collections.Generic;
using System.Reflection;
using CheeseTama.Gameplay.Dialogue;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class CheeseTamaDialogueRulesTests
    {
        [Test]
        public void CatalogCoversEveryPlayerFacingContextWithUniqueValidIds()
        {
            var covered = new HashSet<CheeseTamaDialogueContext>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in CheeseTamaDialogueCatalog.All)
            {
                Assert.That(line, Is.Not.Null);
                Assert.That(line.Id, Is.Not.Empty);
                Assert.That(line.Text, Is.Not.Empty);
                Assert.That(ids.Add(line.Id), Is.True, $"Duplicate dialogue id: {line.Id}");
                Assert.That(line.DurationSeconds, Is.InRange(3f, 5f));
                covered.Add(line.Context);
            }

            foreach (CheeseTamaDialogueContext context in Enum.GetValues(typeof(CheeseTamaDialogueContext)))
            {
                Assert.That(covered.Contains(context), Is.True, $"Missing dialogue context: {context}");
            }
        }

        [Test]
        public void RecentThreeLinesAreExcludedBeforeRotationRepeats()
        {
            var catalog = new[]
            {
                TestLine("line_1", CheeseTamaDialoguePriority.Pet),
                TestLine("line_2", CheeseTamaDialoguePriority.Pet),
                TestLine("line_3", CheeseTamaDialoguePriority.Pet),
                TestLine("line_4", CheeseTamaDialoguePriority.Pet)
            };
            var rules = new CheeseTamaDialogueRules(catalog, 3, 0d);
            var request = CheeseTamaDialogueRequest.ForPet();
            var firstFour = new List<string>();

            for (var index = 0; index < 4; index += 1)
            {
                Assert.That(rules.TrySelectAndRemember(request, index, out var selection), Is.True);
                firstFour.Add(selection.LineId);
            }

            Assert.That(new HashSet<string>(firstFour).Count, Is.EqualTo(4));
            Assert.That(rules.RecentLineIds.Count, Is.EqualTo(3));
            Assert.That(rules.TrySelectAndRemember(request, 5d, out var fifth), Is.True);
            Assert.That(fifth.LineId, Is.EqualTo(firstFour[0]));
        }

        [Test]
        public void SmallDialoguePoolResumesAfterCooldownInsteadOfBecomingSilent()
        {
            var catalog = new[]
            {
                new CheeseTamaDialogueLine(
                    "only_line",
                    "다시 이야기할 수 있어.",
                    CheeseTamaDialogueContext.Evolution,
                    CheeseTamaDialoguePriority.Evolution,
                    5f,
                    4f)
            };
            var rules = new CheeseTamaDialogueRules(catalog, 3, 0d);
            var request = CheeseTamaDialogueRequest.ForEvolution("anything");

            Assert.That(rules.TrySelectAndRemember(request, 10d, out _), Is.True);
            Assert.That(rules.TrySelect(request, 14.99d, out _), Is.False);
            Assert.That(rules.TrySelect(request, 15d, out var repeated), Is.True);
            Assert.That(repeated.LineId, Is.EqualTo("only_line"));
        }

        [Test]
        public void PerLineCooldownUsesCallerSuppliedMonotonicTime()
        {
            var catalog = new[]
            {
                new CheeseTamaDialogueLine(
                    "cooldown_line",
                    "기다려 줘.",
                    CheeseTamaDialogueContext.Pet,
                    CheeseTamaDialoguePriority.Pet,
                    5f,
                    4f)
            };
            var rules = new CheeseTamaDialogueRules(catalog, 0, 0d);

            Assert.That(rules.TrySelectAndRemember(CheeseTamaDialogueRequest.ForPet(), 10d, out _), Is.True);
            Assert.That(rules.TrySelect(CheeseTamaDialogueRequest.ForPet(), 14.99d, out _), Is.False);
            Assert.That(rules.TrySelect(CheeseTamaDialogueRequest.ForPet(), 15d, out _), Is.True);
        }

        [Test]
        public void HigherPriorityDialogueMayOverrideGlobalCooldownButLowerPriorityMayNot()
        {
            var catalog = new[]
            {
                new CheeseTamaDialogueLine(
                    "ambient",
                    "조용해.",
                    CheeseTamaDialogueContext.Ambient,
                    CheeseTamaDialoguePriority.Ambient,
                    0f,
                    4f),
                new CheeseTamaDialogueLine(
                    "event",
                    "무슨 일이 생겼어!",
                    CheeseTamaDialogueContext.Event,
                    CheeseTamaDialoguePriority.Event,
                    0f,
                    4f)
            };
            var rules = new CheeseTamaDialogueRules(catalog, 0, 10d);

            Assert.That(
                rules.TrySelectAndRemember(
                    new CheeseTamaDialogueRequest(CheeseTamaDialogueContext.Ambient),
                    10d,
                    out _),
                Is.True);
            Assert.That(
                rules.TrySelectAndRemember(CheeseTamaDialogueRequest.ForEvent("anything"), 11d, out _),
                Is.True);
            Assert.That(
                rules.TrySelect(
                    new CheeseTamaDialogueRequest(CheeseTamaDialogueContext.Ambient),
                    12d,
                    out _),
                Is.False);
        }

        [Test]
        public void MilkSpecificMemoryRequiresGrowthLevelTwo()
        {
            var belowUnlock = new CheeseTamaDialogueRules(
                minimumGlobalCooldownSeconds: 0d);
            var unlocked = new CheeseTamaDialogueRules(
                minimumGlobalCooldownSeconds: 0d);

            Assert.That(
                belowUnlock.TrySelect(
                    CheeseTamaDialogueRequest.ForFeed("warm_milk", 1),
                    0d,
                    out var genericSelection),
                Is.True);
            Assert.That(genericSelection.Priority, Is.EqualTo(CheeseTamaDialoguePriority.Feed));

            Assert.That(
                unlocked.TrySelect(
                    CheeseTamaDialogueRequest.ForFeed("warm_milk", 2),
                    0d,
                    out var memorySelection),
                Is.True);
            Assert.That(memorySelection.LineId, Is.EqualTo("feed_warm_memory"));
            Assert.That(memorySelection.Priority, Is.EqualTo(CheeseTamaDialoguePriority.FeedMemory));
        }

        [TestCase(34, 100, 100, 0, 100, CheeseTamaDialogueState.Sick)]
        [TestCase(100, 24, 100, 0, 100, CheeseTamaDialogueState.Hungry)]
        [TestCase(100, 100, 34, 0, 100, CheeseTamaDialogueState.Messy)]
        [TestCase(100, 100, 100, 76, 100, CheeseTamaDialogueState.Sleepy)]
        [TestCase(100, 100, 100, 0, 81, CheeseTamaDialogueState.Happy)]
        [TestCase(100, 100, 100, 0, 80, CheeseTamaDialogueState.Normal)]
        public void StateResolutionMatchesExistingCarePresentationPriority(
            int health,
            int hunger,
            int cleanliness,
            int sleepiness,
            int mood,
            CheeseTamaDialogueState expected)
        {
            Assert.That(
                CheeseTamaDialogueRules.ResolveState(
                    health,
                    hunger,
                    cleanliness,
                    sleepiness,
                    mood),
                Is.EqualTo(expected));
        }

        [Test]
        public void BubblePresentationIsNonBlockingAndRejectsLowerPriorityReplacement()
        {
            var host = new GameObject("Dialogue Test Canvas", typeof(RectTransform), typeof(Canvas));
            var bubble = new GameObject("Dialogue Test Bubble", typeof(RectTransform), typeof(Image));
            var labelObject = new GameObject("Dialogue Test Text", typeof(RectTransform), typeof(Text));
            try
            {
                bubble.transform.SetParent(host.transform, false);
                labelObject.transform.SetParent(bubble.transform, false);
                var controller = host.AddComponent<CheeseTamaSpeechBubbleController>();
                var bubbleRect = bubble.GetComponent<RectTransform>();
                var image = bubble.GetComponent<Image>();
                var label = labelObject.GetComponent<Text>();
                controller.Configure(
                    bubble,
                    bubbleRect,
                    label,
                    host.GetComponent<Canvas>(),
                    null);

                Assert.That(bubble.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(bubble.GetComponent<CanvasGroup>().interactable, Is.False);
                Assert.That(image.raycastTarget, Is.False);
                Assert.That(label.raycastTarget, Is.False);

                var eventSelection = new CheeseTamaDialogueSelection(
                    new CheeseTamaDialogueLine(
                        "high",
                        "중요한 이야기",
                        CheeseTamaDialogueContext.Event,
                        CheeseTamaDialoguePriority.Event,
                        0f,
                        5f));
                var ambientSelection = new CheeseTamaDialogueSelection(
                    new CheeseTamaDialogueLine(
                        "low",
                        "낮은 우선순위",
                        CheeseTamaDialogueContext.Ambient,
                        CheeseTamaDialoguePriority.Ambient,
                        0f,
                        3f));

                Assert.That(controller.Show(eventSelection), Is.True);
                Assert.That(controller.IsVisible, Is.True);
                Assert.That(label.text, Is.EqualTo("중요한 이야기"));
                Assert.That(controller.Show(ambientSelection), Is.False);
                Assert.That(label.text, Is.EqualTo("중요한 이야기"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BubbleConfigureAndEmptyDialogueKeepRootFullyHidden()
        {
            var fixture = new SpeechBubbleFixture();
            try
            {
                AssertBubbleHidden(fixture, "initial Configure");

                fixture.Controller.SetOffsets(
                    new Vector3(0f, 1.45f, 0f),
                    new Vector2(0f, 4f));
                fixture.Controller.BindWorldTarget(
                    fixture.WorldTarget,
                    fixture.ProjectionCamera);
                AssertBubbleHidden(fixture, "hidden reposition");

                Assert.That(fixture.Controller.Show("   "), Is.False);
                AssertBubbleHidden(fixture, "empty dialogue");

                fixture.ConfigureController();
                fixture.ConfigureController();
                AssertBubbleHidden(fixture, "repeated Configure");
                Assert.That(fixture.Controller.Show("after repeated Configure"), Is.True);
                AssertBubbleVisible(fixture, "show after repeated Configure");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void BubbleExpiryHideAndRepeatedShowKeepRootAndCanvasGroupInSync()
        {
            var fixture = new SpeechBubbleFixture();
            try
            {
                Assert.That(fixture.Controller.Show("first line"), Is.True);
                AssertBubbleVisible(fixture, "first show");

                SetPrivateField(
                    fixture.Controller,
                    "visibleUntil",
                    Time.unscaledTimeAsDouble - 1d);
                InvokePrivate(fixture.Controller, "Update");
                AssertBubbleHidden(fixture, "duration expiry");

                Assert.That(fixture.Controller.Show("second line"), Is.True);
                AssertBubbleVisible(fixture, "repeated show");

                fixture.Controller.Hide();
                AssertBubbleHidden(fixture, "explicit Hide");

                Assert.That(fixture.Controller.Show("third line"), Is.True);
                AssertBubbleVisible(fixture, "show after Hide");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void BubbleDisableEnableCallbacksStayHiddenUntilShownAgain()
        {
            var fixture = new SpeechBubbleFixture();
            try
            {
                Assert.That(fixture.Controller.Show("visible before disable"), Is.True);
                AssertBubbleVisible(fixture, "before OnDisable");

                InvokePrivate(fixture.Controller, "OnDisable");

                AssertBubbleHidden(fixture, "OnDisable");

                InvokePrivate(fixture.Controller, "OnEnable");

                AssertBubbleHidden(fixture, "OnEnable");
                Assert.That(fixture.Controller.Show("visible after re-enable"), Is.True);
                AssertBubbleVisible(fixture, "show after re-enable");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [TestCase(0f, 3f)]
        [TestCase(4f, 4f)]
        [TestCase(99f, 5f)]
        public void BubbleDurationIsClampedToReadableRange(float requested, float expected)
        {
            Assert.That(CheeseTamaDialogueSelection.ClampDuration(requested), Is.EqualTo(expected));
        }

        private static CheeseTamaDialogueLine TestLine(
            string id,
            CheeseTamaDialoguePriority priority)
        {
            return new CheeseTamaDialogueLine(
                id,
                id,
                CheeseTamaDialogueContext.Pet,
                priority,
                0f,
                4f);
        }

        private static void AssertBubbleHidden(SpeechBubbleFixture fixture, string context)
        {
            Assert.That(fixture.Controller.IsVisible, Is.False, $"{context}: controller visibility");
            Assert.That(fixture.Bubble.activeSelf, Is.False, $"{context}: bubble root active state");
            Assert.That(fixture.CanvasGroup.alpha, Is.Zero, $"{context}: alpha");
            Assert.That(fixture.CanvasGroup.interactable, Is.False, $"{context}: interactable");
            Assert.That(fixture.CanvasGroup.blocksRaycasts, Is.False, $"{context}: blocksRaycasts");
        }

        private static void AssertBubbleVisible(SpeechBubbleFixture fixture, string context)
        {
            Assert.That(fixture.Controller.IsVisible, Is.True, $"{context}: controller visibility");
            Assert.That(fixture.Bubble.activeSelf, Is.True, $"{context}: bubble root active state");
            Assert.That(fixture.CanvasGroup.alpha, Is.EqualTo(1f), $"{context}: alpha");
            Assert.That(fixture.CanvasGroup.interactable, Is.False, $"{context}: interactable");
            Assert.That(fixture.CanvasGroup.blocksRaycasts, Is.False, $"{context}: blocksRaycasts");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
            method.Invoke(target, null);
        }

        private sealed class SpeechBubbleFixture : IDisposable
        {
            public SpeechBubbleFixture()
            {
                Host = new GameObject(
                    "Speech Bubble Lifecycle Test Canvas",
                    typeof(RectTransform),
                    typeof(Canvas));
                Bubble = new GameObject(
                    "Speech Bubble Lifecycle Test Root",
                    typeof(RectTransform),
                    typeof(Image));
                var labelObject = new GameObject(
                    "Speech Bubble Lifecycle Test Text",
                    typeof(RectTransform),
                    typeof(Text));
                Bubble.transform.SetParent(Host.transform, false);
                labelObject.transform.SetParent(Bubble.transform, false);
                Host.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 600f);
                Bubble.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 120f);

                var targetObject = new GameObject("Speech Bubble Lifecycle World Target");
                targetObject.transform.SetParent(Host.transform, false);
                WorldTarget = targetObject.transform;

                var cameraObject = new GameObject(
                    "Speech Bubble Lifecycle Projection Camera",
                    typeof(Camera));
                cameraObject.transform.SetParent(Host.transform, false);
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                ProjectionCamera = cameraObject.GetComponent<Camera>();
                ProjectionCamera.orthographic = true;
                ProjectionCamera.enabled = false;

                BubbleRect = Bubble.GetComponent<RectTransform>();
                MessageText = labelObject.GetComponent<Text>();
                HostCanvas = Host.GetComponent<Canvas>();
                Controller = Host.AddComponent<CheeseTamaSpeechBubbleController>();
                ConfigureController();
                CanvasGroup = Bubble.GetComponent<CanvasGroup>();
                Assert.That(CanvasGroup, Is.Not.Null);
            }

            public GameObject Host { get; }

            public GameObject Bubble { get; }

            public CanvasGroup CanvasGroup { get; }

            public CheeseTamaSpeechBubbleController Controller { get; }

            public RectTransform BubbleRect { get; }

            public Text MessageText { get; }

            public Canvas HostCanvas { get; }

            public Transform WorldTarget { get; }

            public Camera ProjectionCamera { get; }

            public void ConfigureController()
            {
                Controller.Configure(
                    Bubble,
                    BubbleRect,
                    MessageText,
                    HostCanvas,
                    WorldTarget,
                    ProjectionCamera);
            }

            public void Dispose()
            {
                if (Host != null)
                {
                    UnityEngine.Object.DestroyImmediate(Host);
                }
            }
        }
    }
}
