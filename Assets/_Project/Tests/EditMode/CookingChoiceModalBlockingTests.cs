using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CheeseTama.Gameplay.Deliveries;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class CookingChoiceModalBlockingTests
    {
        private static readonly object[][] StaticBlockerConsumers =
        {
            new object[] { typeof(BouncyJumpMiniGameController), "BlockingOverlayNames" },
            new object[] { typeof(CareEventCardController), "BlockingUiNames" },
            new object[] { typeof(CheeseStarDeliveryBridge), "BlockingModalNames" },
            new object[] { typeof(CheeseTamaDialogueBridge), "BlockingModalNames" },
            new object[] { typeof(CheeseTamaPetInteractionController), "BlockingUiNames" },
            new object[] { typeof(CheeseTamaProfileMenuController), "BlockingOverlayNames" },
            new object[] { typeof(CleaningMiniGameController), "BlockingOverlayNames" },
            new object[] { typeof(EvolutionMilestoneController), "BlockingNames" },
            new object[] { typeof(GrowthMilestoneController), "BlockingOverlayNames" },
            new object[] { typeof(MemoryJournalRecallBridge), "BlockingModalNames" },
            new object[] { typeof(MilkDropMiniGameController), "BlockingOverlayNames" },
            new object[] { typeof(NpcVisitBridge), "BlockingOverlayNames" },
            new object[] { typeof(SleepScheduleBridge), "BlockingOverlayNames" }
        };

        private static readonly object[][] ComputedBlockerConsumers =
        {
            new object[] { typeof(FirstDayJourneyController), "IsAnotherModalBlocking" },
            new object[] { typeof(GrowthJourneyController), "IsAnotherModalBlocking" },
            new object[] { typeof(PlayChoicePanelController), "IsAnyModalActive" },
            new object[] { typeof(ReturnSummaryController), "IsAnotherModalBlocking" }
        };

        [Test]
        public void ContractTableCoversExactlySeventeenUniqueBlockerConsumers()
        {
            var consumerTypes = StaticBlockerConsumers
                .Concat(ComputedBlockerConsumers)
                .Select(testCase => (Type)testCase[0])
                .ToArray();

            Assert.That(consumerTypes, Has.Length.EqualTo(17));
            Assert.That(new HashSet<Type>(consumerTypes), Has.Count.EqualTo(17));
        }

        [TestCaseSource(nameof(StaticBlockerConsumers))]
        public void StaticBlockerConsumerIncludesChoiceHubExactlyOnce(
            Type consumerType,
            string fieldName)
        {
            var field = consumerType.GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(
                field,
                Is.Not.Null,
                $"{consumerType.Name}.{fieldName} must remain a static blocker contract.");

            var blockerNames = field.GetValue(null) as string[];
            Assert.That(blockerNames, Is.Not.Null, consumerType.Name);
            Assert.That(
                blockerNames.Count(name => string.Equals(
                    name,
                    CookingChoicePanelController.OverlayObjectName,
                    StringComparison.Ordinal)),
                Is.EqualTo(1),
                $"{consumerType.Name}.{fieldName} must include the cooking choice hub exactly once.");
        }

        [TestCaseSource(nameof(ComputedBlockerConsumers))]
        public void ComputedBlockerConsumerRejectsActiveChoiceHub(
            Type consumerType,
            string methodName)
        {
            var host = new GameObject(
                $"{consumerType.Name} Choice Hub Blocking Test",
                typeof(RectTransform));
            try
            {
                var consumer = host.AddComponent(consumerType);
                var overlay = CreateRectChild(
                    host.transform,
                    CookingChoicePanelController.OverlayObjectName);

                overlay.SetActive(true);
                Assert.That(InvokePrivateBool(consumer, methodName), Is.True, consumerType.Name);

                overlay.SetActive(false);
                Assert.That(InvokePrivateBool(consumer, methodName), Is.False, consumerType.Name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DialogueDefersWhileChoiceHubOverlayIsActive()
        {
            var host = new GameObject("Dialogue Choice Hub Blocking Test", typeof(RectTransform));
            try
            {
                var bridge = host.AddComponent<CheeseTamaDialogueBridge>();
                bridge.Configure(null, null, host.transform);
                var overlay = CreateRectChild(
                    host.transform,
                    CookingChoicePanelController.OverlayObjectName);

                overlay.SetActive(true);
                Assert.That(bridge.IsModalBlocking, Is.True);

                overlay.SetActive(false);
                Assert.That(bridge.IsModalBlocking, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DeliveryDefersWhileChoiceHubOverlayIsActive()
        {
            var host = new GameObject("Delivery Choice Hub Blocking Test", typeof(RectTransform));
            try
            {
                var cardRoot = CreateRectChild(host.transform, "Cheese Star Delivery Overlay");
                var card = host.AddComponent<CheeseStarDeliveryCardController>();
                card.Configure(
                    cardRoot,
                    CreateText(cardRoot.transform, "Title"),
                    CreateText(cardRoot.transform, "Streak"),
                    CreateText(cardRoot.transform, "Reward"),
                    CreateText(cardRoot.transform, "Note"),
                    CreateButton(cardRoot.transform, "Claim"),
                    CreateButton(cardRoot.transform, "Later"));
                var bridge = host.AddComponent<CheeseStarDeliveryBridge>();
                bridge.Configure(card, null, null, null, null, host.transform);
                var choiceHub = CreateRectChild(
                    host.transform,
                    CookingChoicePanelController.OverlayObjectName);
                var offer = CheeseStarDeliverySystem.ObserveEntry(
                    new CheeseStarDeliverySaveData(),
                    false,
                    new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.FromHours(9)));

                choiceHub.SetActive(true);
                Assert.That(bridge.TryShowOffer(offer), Is.False);
                Assert.That(card.IsVisible, Is.False);

                choiceHub.SetActive(false);
                Assert.That(bridge.TryShowOffer(offer), Is.True);
                Assert.That(card.IsVisible, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void NpcVisitDefersWhileChoiceHubOverlayIsActive()
        {
            var host = new GameObject("Npc Visit Choice Hub Blocking Test", typeof(RectTransform));
            try
            {
                var bridge = host.AddComponent<NpcVisitBridge>();
                bridge.Configure(null, null, host.transform);
                var overlay = CreateRectChild(
                    host.transform,
                    CookingChoicePanelController.OverlayObjectName);

                overlay.SetActive(true);
                Assert.That(
                    InvokePrivateBool(bridge, "IsAnotherModalBlocking"),
                    Is.True);

                overlay.SetActive(false);
                Assert.That(
                    InvokePrivateBool(bridge, "IsAnotherModalBlocking"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static bool InvokePrivateBool(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing {target.GetType().Name}.{methodName}.");
            return (bool)method.Invoke(target, null);
        }

        private static GameObject CreateRectChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Text CreateText(Transform parent, string name)
        {
            return CreateRectChild(parent, name).AddComponent<Text>();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var child = CreateRectChild(parent, name);
            var image = child.AddComponent<Image>();
            var button = child.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
