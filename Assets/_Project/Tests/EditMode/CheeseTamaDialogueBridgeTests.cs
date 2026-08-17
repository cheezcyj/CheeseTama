using System;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Dialogue;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Stats;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class CheeseTamaDialogueBridgeTests
    {
        [Test]
        public void ConfigureRepeatedlyKeepsOneSubscriptionAndDisableBalancesIt()
        {
            var managerObject = new GameObject("Dialogue Bridge Manager");
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<GameManager>();
            var harness = new DialogueHarness();
            try
            {
                harness.Bridge.Configure(harness.Controller, manager, harness.Host.transform);
                harness.Bridge.Configure(harness.Controller, manager, harness.Host.transform);

                Assert.That(ListenerCount(manager, "CareActionRegistered"), Is.EqualTo(1));
                Assert.That(ListenerCount(manager, "ReturnSummaryAvailable"), Is.EqualTo(1));
                Assert.That(ListenerCount(manager, "GrowthMilestoneAvailable"), Is.EqualTo(1));
                Assert.That(ListenerCount(manager, "EvolutionMilestoneAvailable"), Is.EqualTo(1));
                Assert.That(ListenerCount(manager, "CareEventAvailable"), Is.EqualTo(1));
                Assert.That(ListenerCount(manager, "MilkGrowthMilestoneRewardGranted"), Is.EqualTo(1));
                Assert.That(ListenerCount(manager, "SaveDataReplaced"), Is.EqualTo(1));

                harness.Bridge.Bind(null);
                Assert.That(ListenerCount(manager, "CareActionRegistered"), Is.Zero);
                Assert.That(ListenerCount(manager, "SaveDataReplaced"), Is.Zero);

                harness.Bridge.Bind(manager);
                harness.Bridge.Bind(manager);
                Assert.That(ListenerCount(manager, "CareActionRegistered"), Is.EqualTo(1));
                Assert.That(ListenerCount(manager, "SaveDataReplaced"), Is.EqualTo(1));

                harness.Bridge.Bind(null);
                Assert.That(ListenerCount(manager, "CareActionRegistered"), Is.Zero);
                Assert.That(ListenerCount(manager, "SaveDataReplaced"), Is.Zero);
            }
            finally
            {
                harness.Dispose();
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void ExplicitFeedAndPetEntryPointsSelectTheExpectedDialogueKinds()
        {
            var memoryHarness = new DialogueHarness();
            try
            {
                memoryHarness.Bridge.Configure(
                    memoryHarness.Controller,
                    null,
                    memoryHarness.Host.transform);

                Assert.That(
                    memoryHarness.Bridge.NotifyFeed(
                        MilkCatalog.WarmMilkId,
                        2,
                        CheeseTamaDialogueTone.Positive,
                        false),
                    Is.True);
                Assert.That(memoryHarness.Controller.CurrentLineId, Is.EqualTo("feed_warm_memory"));
            }
            finally
            {
                memoryHarness.Dispose();
            }

            var negativeHarness = new DialogueHarness();
            try
            {
                negativeHarness.Bridge.Configure(
                    negativeHarness.Controller,
                    null,
                    negativeHarness.Host.transform);

                Assert.That(
                    negativeHarness.Bridge.NotifyFeed(
                        MilkCatalog.BasicMilkId,
                        0,
                        CheeseTamaDialogueTone.Negative,
                        false),
                    Is.True);
                Assert.That(negativeHarness.Controller.CurrentLineId, Does.StartWith("feed_negative_"));

                negativeHarness.Controller.Hide();
                negativeHarness.Controller.Rules.ResetMemory();
                Assert.That(negativeHarness.Bridge.NotifyPet(false), Is.True);
                Assert.That(negativeHarness.Controller.CurrentLineId, Does.StartWith("pet_"));
            }
            finally
            {
                negativeHarness.Dispose();
            }
        }

        [Test]
        public void ActiveModalDefersDialogueUntilItCanBePresentedWithoutOverlap()
        {
            var harness = new DialogueHarness();
            var modal = new GameObject("Settings Modal");
            try
            {
                modal.transform.SetParent(harness.Host.transform, false);
                harness.Bridge.Configure(harness.Controller, null, harness.Host.transform);

                Assert.That(harness.Bridge.IsModalBlocking, Is.True);
                Assert.That(harness.Bridge.NotifyPet(false), Is.False);
                Assert.That(harness.Bridge.HasPendingDialogue, Is.True);
                Assert.That(harness.Controller.IsVisible, Is.False);

                modal.SetActive(false);
                Assert.That(harness.Bridge.IsModalBlocking, Is.False);
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.True);
                Assert.That(harness.Bridge.HasPendingDialogue, Is.False);
                Assert.That(harness.Controller.IsVisible, Is.True);
                Assert.That(harness.Controller.CurrentLineId, Does.StartWith("pet_"));
            }
            finally
            {
                harness.Dispose();
            }
        }

        [Test]
        public void GameplayEventsMapToFeedReturnGrowthEvolutionAndEventRequests()
        {
            var managerObject = new GameObject("Dialogue Event Manager");
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<GameManager>();
            var harness = new DialogueHarness();
            try
            {
                harness.Bridge.Configure(harness.Controller, manager, harness.Host.transform);

                Raise(manager, "CareActionRegistered", "pet");
                Assert.That(harness.Controller.CurrentLineId, Does.StartWith("pet_"));

                ResetBubble(harness);
                Raise(
                    manager,
                    "MilkGrowthMilestoneRewardGranted",
                    new MilkGrowthMilestoneRewardResult(
                        MilkCatalog.WarmMilkId,
                        2,
                        4,
                        0,
                        0,
                        new[] { "warm_milk:growth:2" },
                        "reward"));
                Assert.That(harness.Controller.CurrentLineId, Is.EqualTo("feed_warm_memory"));

                ResetBubble(harness);
                Raise(
                    manager,
                    "ReturnSummaryAvailable",
                    new ReturnSummaryData(
                        "return",
                        30,
                        0,
                        new ReturnSummaryStatsSnapshot(80, 70, 90, 20, 100),
                        new ReturnSummaryStatsSnapshot(78, 69, 88, 22, 100),
                        0,
                        0,
                        0));
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.True);
                Assert.That(harness.Controller.CurrentLineId, Is.EqualTo("return_short"));

                ResetBubble(harness);
                Raise(
                    manager,
                    "GrowthMilestoneAvailable",
                    new GrowthMilestoneData("growth", CheeseTamaGrowthStage.Final, 33));
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.True);
                Assert.That(harness.Controller.CurrentLineId, Does.StartWith("growth_"));
                Assert.That(
                    harness.Controller.CurrentPriority,
                    Is.EqualTo(CheeseTamaDialoguePriority.Growth));

                ResetBubble(harness);
                var profile = EvolutionSystem.NormalEvolutions[0];
                Raise(
                    manager,
                    "EvolutionMilestoneAvailable",
                    new EvolutionMilestoneData(
                        "evolution",
                        new NormalEvolutionResult(profile, 100),
                        EvolutionSystem.NormalEvolutionLevel));
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.True);
                Assert.That(harness.Controller.CurrentLineId, Is.EqualTo("evolution_cream"));

                ResetBubble(harness);
                Raise(
                    manager,
                    "CareEventAvailable",
                    new CareEventResult(
                        true,
                        "event-occurrence",
                        "small_fever",
                        "event",
                        "message"));
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.True);
                Assert.That(harness.Controller.CurrentLineId, Is.EqualTo("event_fever"));

                Raise(manager, "SaveDataReplaced");
                Assert.That(harness.Controller.IsVisible, Is.False);
                Assert.That(harness.Bridge.HasPendingDialogue, Is.False);
            }
            finally
            {
                harness.Dispose();
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void ConfigureSeedsAlreadyPendingMilestonesUsingExistingPriorityOrder()
        {
            var managerObject = new GameObject("Dialogue Pending Manager");
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<GameManager>();
            var harness = new DialogueHarness();
            try
            {
                SetPending(
                    manager,
                    "pendingReturnSummary",
                    new ReturnSummaryData(
                        "restored-return",
                        240,
                        0,
                        new ReturnSummaryStatsSnapshot(80, 70, 90, 20, 100),
                        new ReturnSummaryStatsSnapshot(78, 69, 88, 22, 100),
                        0,
                        0,
                        0));
                SetPending(
                    manager,
                    "pendingGrowthMilestone",
                    new GrowthMilestoneData(
                        "restored-growth",
                        CheeseTamaGrowthStage.Final,
                        33));
                var profile = EvolutionSystem.NormalEvolutions[0];
                SetPending(
                    manager,
                    "pendingEvolutionMilestone",
                    new EvolutionMilestoneData(
                        "restored-evolution",
                        new NormalEvolutionResult(profile, 100),
                        EvolutionSystem.NormalEvolutionLevel));

                harness.Bridge.Configure(
                    harness.Controller,
                    manager,
                    harness.Host.transform);

                Assert.That(harness.Bridge.HasPendingDialogue, Is.True);
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.True);
                Assert.That(harness.Controller.CurrentLineId, Is.EqualTo("evolution_cream"));
                Assert.That(
                    harness.Controller.CurrentPriority,
                    Is.EqualTo(CheeseTamaDialoguePriority.Evolution));

                harness.Controller.Hide();
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.False);
            }
            finally
            {
                harness.Dispose();
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void SaveReplacementReseedsPendingMilestonesThatWerePreparedBeforeNotification()
        {
            var managerObject = new GameObject("Dialogue Reload Manager");
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<GameManager>();
            var harness = new DialogueHarness();
            try
            {
                harness.Bridge.Configure(
                    harness.Controller,
                    manager,
                    harness.Host.transform);
                SetPending(
                    manager,
                    "pendingGrowthMilestone",
                    new GrowthMilestoneData(
                        "reloaded-growth",
                        CheeseTamaGrowthStage.Grown,
                        15));

                Raise(manager, "SaveDataReplaced");

                Assert.That(harness.Bridge.HasPendingDialogue, Is.True);
                Assert.That(harness.Bridge.TryPresentPendingDialogue(), Is.True);
                Assert.That(
                    harness.Controller.CurrentPriority,
                    Is.EqualTo(CheeseTamaDialoguePriority.Growth));
                Assert.That(harness.Controller.CurrentLineId, Does.StartWith("growth_"));
            }
            finally
            {
                harness.Dispose();
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        private static void ResetBubble(DialogueHarness harness)
        {
            harness.Controller.Hide();
            harness.Controller.Rules.ResetMemory();
        }

        private static int ListenerCount(GameManager manager, string eventName)
        {
            var handler = GetEventDelegate(manager, eventName);
            return handler?.GetInvocationList().Length ?? 0;
        }

        private static void Raise(GameManager manager, string eventName, params object[] arguments)
        {
            var handler = GetEventDelegate(manager, eventName);
            Assert.That(handler, Is.Not.Null, $"No listener was bound to {eventName}.");
            handler.DynamicInvoke(arguments);
        }

        private static Delegate GetEventDelegate(GameManager manager, string eventName)
        {
            var field = typeof(GameManager).GetField(
                eventName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing event backing field: {eventName}");
            return field.GetValue(manager) as Delegate;
        }

        private static void SetPending(GameManager manager, string fieldName, object value)
        {
            var field = typeof(GameManager).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing pending field: {fieldName}");
            field.SetValue(manager, value);
        }

        private sealed class DialogueHarness : IDisposable
        {
            public DialogueHarness()
            {
                Host = new GameObject(
                    "Dialogue Bridge Test Canvas",
                    typeof(RectTransform),
                    typeof(Canvas));
                var bubble = new GameObject(
                    "Dialogue Bridge Test Bubble",
                    typeof(RectTransform),
                    typeof(Image));
                var labelObject = new GameObject(
                    "Dialogue Bridge Test Text",
                    typeof(RectTransform),
                    typeof(Text));
                bubble.transform.SetParent(Host.transform, false);
                labelObject.transform.SetParent(bubble.transform, false);

                Controller = Host.AddComponent<CheeseTamaSpeechBubbleController>();
                Controller.Configure(
                    bubble,
                    bubble.GetComponent<RectTransform>(),
                    labelObject.GetComponent<Text>(),
                    Host.GetComponent<Canvas>(),
                    null);
                Bridge = Host.AddComponent<CheeseTamaDialogueBridge>();
            }

            public GameObject Host { get; }
            public CheeseTamaSpeechBubbleController Controller { get; }
            public CheeseTamaDialogueBridge Bridge { get; }

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
