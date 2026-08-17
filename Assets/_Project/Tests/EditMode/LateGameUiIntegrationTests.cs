using System;
using System.Collections.Generic;
using System.Reflection;
using CheeseTama.Collections;
using CheeseTama.Collections.HiddenCareers;
using CheeseTama.Core;
using CheeseTama.Gameplay.Bond;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class LateGameUiIntegrationTests
    {
        [Test]
        public void BuilderCreatesLateGameViewsIdempotentlyAndKeepsLockedEntriesHidden()
        {
            var root = new GameObject(
                "Late Game Builder Test",
                typeof(RectTransform),
                typeof(Canvas));
            var visualObject = new GameObject("Visual");
            try
            {
                var roomUi = root.AddComponent<MilkroomUIController>();
                var visual = visualObject.AddComponent<CheeseTamaVisualController>();

                InvokeBuilder(root.transform, roomUi, visual);
                InvokeBuilder(root.transform, roomUi, visual);

                AssertSingleChild(root.transform, StarLegacyPanelController.OverlayObjectName);
                AssertSingleChild(root.transform, "Bond Status Overlay");
                AssertSingleChild(root.transform, "Hidden Career Card Overlay");
                Assert.That(root.GetComponents<StarLegacyPanelController>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponents<BondStatusPanelController>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponents<HiddenCareerCardPanelController>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponents<BondReactionPresenter>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponents<LateGameFeatureBridge>(), Has.Length.EqualTo(1));
                Assert.That(
                    visualObject.GetComponents<EmmentalConstellationPresenter>(),
                    Has.Length.EqualTo(1));

                var statusPanel = root.transform.Find("Status Panel");
                var entryParent = statusPanel != null ? statusPanel : root.transform;
                Assert.That(
                    entryParent.Find("Open Star Legacy Button").gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    entryParent.Find("Open Hidden Career Button").gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    root.transform.Find(StarLegacyPanelController.OverlayObjectName).gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    root.transform.Find("Hidden Career Card Overlay").gameObject.activeSelf,
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(visualObject);
            }
        }

        [Test]
        public void HiddenCareerPanelReceivesOnlyUnlockedViewDataWithoutSecretCountsOrIds()
        {
            var host = new GameObject("Hidden Career Bridge Test");
            try
            {
                var panel = new GameObject("Panel");
                panel.transform.SetParent(host.transform, false);
                var title = CreateText(host.transform, "Title");
                var cards = CreateText(host.transform, "Cards");
                var entry = CreateButton(host.transform, "Entry");
                var close = CreateButton(host.transform, "Close");
                var controller = host.AddComponent<HiddenCareerCardPanelController>();
                controller.Configure(panel, title, cards, entry, close);
                controller.Bind(Array.Empty<HiddenCareerCardViewData>());

                Assert.That(entry.gameObject.activeSelf, Is.False);
                Assert.That(controller.RenderedText, Is.Empty);

                var collections = new CollectionSaveData();
                var unlocked = new HiddenCareerCardSystem().TryUnlockKnownCard(
                    collections,
                    HiddenCareerCardCatalog.ScientistId,
                    new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.FromHours(9)));
                controller.Bind(new List<HiddenCareerCardViewData> { unlocked.Card });

                Assert.That(entry.gameObject.activeSelf, Is.True);
                Assert.That(controller.RenderedText, Does.Contain("과학자치즈타마"));
                Assert.That(controller.RenderedText, Does.Not.Contain(HiddenCareerCardCatalog.ScientistId));
                Assert.That(controller.RenderedText, Does.Not.Contain("조건"));
                Assert.That(controller.RenderedText, Does.Not.Contain("???"));
                Assert.That(controller.RenderedText, Does.Not.Contain("/7"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [TestCase("feed_warm_milk", BondInteraction.Feed, MilkCatalog.WarmMilkId)]
        [TestCase("pet", BondInteraction.Pet, "")]
        [TestCase("play", BondInteraction.Play, "")]
        [TestCase("clean", BondInteraction.Clean, "")]
        [TestCase("rest", BondInteraction.Rest, "")]
        [TestCase("blend", BondInteraction.Cook, "")]
        public void BridgeMapsAuthoritativeCareActionsToBondReactions(
            string actionId,
            BondInteraction expectedInteraction,
            string expectedSubject)
        {
            Assert.That(
                LateGameFeatureBridge.TryMapCareAction(
                    actionId,
                    out var interaction,
                    out var subjectId),
                Is.True);
            Assert.That(interaction, Is.EqualTo(expectedInteraction));
            Assert.That(subjectId, Is.EqualTo(expectedSubject));
        }

        [Test]
        public void UnknownCareActionDoesNotGenerateBondPresentation()
        {
            Assert.That(
                LateGameFeatureBridge.TryMapCareAction(
                    "future_internal_signal",
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void BuilderCreatesOneHiddenSaveRecoveryNoticeAndBridge()
        {
            var root = new GameObject(
                "Save Recovery Notice Builder Test",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                InvokeBuilderMethod("EnsureSaveRecoveryNotice", root.transform);
                InvokeBuilderMethod("EnsureSaveRecoveryNotice", root.transform);

                AssertSingleChild(root.transform, SaveRecoveryNoticeController.OverlayObjectName);
                Assert.That(root.GetComponents<SaveRecoveryNoticeController>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponents<SaveRecoveryNoticeBridge>(), Has.Length.EqualTo(1));
                Assert.That(
                    root.transform.Find(SaveRecoveryNoticeController.OverlayObjectName)
                        .gameObject.activeSelf,
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void InvokeBuilder(
            Transform root,
            MilkroomUIController roomUi,
            CheeseTamaVisualController visual)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                "EnsureLateGameFeatures",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            try
            {
                method.Invoke(null, new object[] { root, roomUi, visual });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void InvokeBuilderMethod(string methodName, params object[] arguments)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, methodName);
            try
            {
                method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static Text CreateText(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Text>();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Button>();
        }

        private static void AssertSingleChild(Transform parent, string name)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                if (string.Equals(parent.GetChild(index).name, name, StringComparison.Ordinal))
                {
                    count += 1;
                }
            }

            Assert.That(count, Is.EqualTo(1), name);
        }
    }
}
