using System;
using System.Globalization;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Deliveries;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class CheeseStarDeliveryBridgeTests
    {
        private GameObject host;
        private GameObject managerHost;
        private GameObject overlay;
        private Button claimButton;
        private Button laterButton;
        private CheeseStarDeliveryCardController card;
        private TopMenuController topMenu;
        private BottomActionBarController actionBar;
        private DevPanelController devPanel;
        private CheeseStarDeliveryBridge bridge;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Delivery Bridge Host", typeof(RectTransform));
            overlay = CreateRectChild(host.transform, "Cheese Star Delivery Overlay");
            var title = CreateText(overlay.transform, "Title");
            var streak = CreateText(overlay.transform, "Streak");
            var reward = CreateText(overlay.transform, "Reward");
            var note = CreateText(overlay.transform, "Note");
            claimButton = CreateButton(overlay.transform, "Claim");
            laterButton = CreateButton(overlay.transform, "Later");

            card = host.AddComponent<CheeseStarDeliveryCardController>();
            card.Configure(
                overlay,
                title,
                streak,
                reward,
                note,
                claimButton,
                laterButton);

            topMenu = host.AddComponent<TopMenuController>();
            actionBar = host.AddComponent<BottomActionBarController>();
            devPanel = host.AddComponent<DevPanelController>();
            bridge = host.AddComponent<CheeseStarDeliveryBridge>();
            bridge.Configure(
                card,
                null,
                topMenu,
                actionBar,
                devPanel,
                host.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (managerHost != null)
            {
                UnityEngine.Object.DestroyImmediate(managerHost);
            }

            if (host != null)
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Later_RestoresExactControlStatesAndAllowsSameDateManualReopen()
        {
            topMenu.enabled = true;
            actionBar.enabled = false;
            devPanel.enabled = true;
            var offer = CreateOffer(2026, 8, 14);

            var shown = bridge.TryShowOffer(offer);

            Assert.That(shown, Is.True);
            Assert.That(overlay.activeSelf, Is.True);
            Assert.That(topMenu.enabled, Is.False);
            Assert.That(actionBar.enabled, Is.False);
            Assert.That(devPanel.enabled, Is.False);

            laterButton.onClick.Invoke();

            Assert.That(overlay.activeSelf, Is.False);
            Assert.That(topMenu.enabled, Is.True);
            Assert.That(actionBar.enabled, Is.False);
            Assert.That(devPanel.enabled, Is.True);
            Assert.That(bridge.TryShowOffer(offer), Is.True);
            Assert.That(overlay.activeSelf, Is.True);
        }

        [Test]
        public void NextDate_CanAutoShowAfterPreviousDateWasHandled()
        {
            var firstOffer = CreateOffer(2026, 8, 14);
            var nextOffer = CreateOffer(2026, 8, 15);
            Assert.That(bridge.TryShowOffer(firstOffer), Is.True);
            laterButton.onClick.Invoke();

            Assert.That(bridge.TryShowOffer(nextOffer), Is.True);
            Assert.That(bridge.LastAutoHandledDateKey, Is.EqualTo("2026-08-15"));
        }

        [TestCase(NewGameSetupController.OverlayObjectName)]
        [TestCase("First Meeting Onboarding Overlay")]
        [TestCase("Return Summary Overlay")]
        [TestCase("Growth Achievement Overlay")]
        [TestCase("Evolution Achievement Overlay")]
        [TestCase(FirstDayJourneyController.OverlayObjectName)]
        [TestCase("Settings Modal")]
        public void ActivePriorityModal_DefersWithoutConsumingDailyDisplay(string modalName)
        {
            var blocker = CreateRectChild(host.transform, modalName);
            blocker.SetActive(true);
            var offer = CreateOffer(2026, 8, 14);

            Assert.That(bridge.TryShowOffer(offer), Is.False);
            Assert.That(bridge.LastAutoHandledDateKey, Is.Empty);

            blocker.SetActive(false);
            Assert.That(bridge.TryShowOffer(offer), Is.True);
        }

        [Test]
        public void SaveReplacementReset_AllowsCurrentDateToBeEvaluatedAgain()
        {
            var offer = CreateOffer(2026, 8, 14);
            Assert.That(bridge.TryShowOffer(offer), Is.True);
            laterButton.onClick.Invoke();
            Assert.That(bridge.LastAutoHandledDateKey, Is.EqualTo("2026-08-14"));

            bridge.ResetAutoDisplayForCurrentSave();

            Assert.That(bridge.LastAutoHandledDateKey, Is.Empty);
            Assert.That(bridge.TryShowOffer(offer), Is.True);
        }

        [Test]
        public void ManualEntryButton_OpensAvailableDeliveryWhileAutomaticPriorityFlowIsIncomplete()
        {
            var saveData = CreateSave(alreadyClaimed: false);
            Assert.That(saveData.newGameSetup.completed, Is.False);
            Assert.That(saveData.onboarding.completed, Is.False);
            var manager = CreateManager(saveData);
            var entry = CreateEntryButton(host.transform, out _);
            bridge.Configure(card, manager, topMenu, actionBar, devPanel, host.transform);
            entry.onClick.AddListener(() =>
            {
                bridge.TryShowOffer(manager.ObserveCheeseStarDelivery());
            });
            bridge.BindEntryButton(entry);

            entry.onClick.Invoke();

            Assert.That(card.IsVisible, Is.True);
            Assert.That(overlay.activeSelf, Is.True);
        }

        [Test]
        public void Later_SuppressesAutomaticRedisplayButManualEntryReopensSameDate()
        {
            var saveData = CreateSave(alreadyClaimed: false);
            saveData.newGameSetup = NewGameSetupSaveData.CreateCompletedForLegacySave();
            saveData.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            saveData.firstDayJourney = FirstDayJourneySaveData.CreateCompletedForLegacySave();
            saveData.cheeseStarDelivery.latestObservedDateKey = string.Empty;
            var manager = CreateManager(saveData);
            bridge.Configure(card, manager, topMenu, actionBar, devPanel, host.transform);
            var offer = manager.ObserveCheeseStarDelivery();

            Assert.That(bridge.TryShowOffer(offer), Is.True);
            laterButton.onClick.Invoke();

            Assert.That(bridge.TryShowPendingDelivery(), Is.False);
            Assert.That(bridge.TryShowOffer(manager.ObserveCheeseStarDelivery()), Is.True);
        }

        [Test]
        public void Reconfigure_DoesNotDuplicateCardCallbacks()
        {
            bridge.Configure(card, null, topMenu, actionBar, devPanel, host.transform);
            bridge.Configure(card, null, topMenu, actionBar, devPanel, host.transform);
            var offer = CreateOffer(2026, 8, 14);

            Assert.That(bridge.TryShowOffer(offer), Is.True);
            laterButton.onClick.Invoke();

            Assert.That(overlay.activeSelf, Is.False);
            Assert.That(topMenu.enabled, Is.True);
        }

        [Test]
        public void AvailableDelivery_ShowsPendingLabelAndNotificationBadge()
        {
            var manager = CreateManager(CreateSave(alreadyClaimed: false));
            var entry = CreateEntryButton(host.transform, out var label);
            bridge.Configure(card, manager, topMenu, actionBar, devPanel, host.transform);

            bridge.BindEntryButton(entry);

            var badge = entry.transform.Find(
                CheeseStarDeliveryBridge.EntryNotificationBadgeObjectName);
            Assert.That(label.text, Is.EqualTo("오늘배달"));
            Assert.That(entry.interactable, Is.True);
            Assert.That(badge, Is.Not.Null);
            Assert.That(badge.gameObject.activeSelf, Is.True);
            Assert.That(badge.GetComponent<Image>().raycastTarget, Is.False);
        }

        [Test]
        public void ClaimedDelivery_ShowsClaimedLabelAndHidesNotificationBadge()
        {
            var manager = CreateManager(CreateSave(alreadyClaimed: true));
            var entry = CreateEntryButton(host.transform, out var label);
            bridge.Configure(card, manager, topMenu, actionBar, devPanel, host.transform);

            bridge.BindEntryButton(entry);

            var badge = entry.transform.Find(
                CheeseStarDeliveryBridge.EntryNotificationBadgeObjectName);
            Assert.That(label.text, Is.EqualTo("오늘배달 받음"));
            Assert.That(entry.interactable, Is.False);
            Assert.That(badge, Is.Not.Null);
            Assert.That(badge.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ExternalClaimEvent_RefreshesBoundEntryPresentation()
        {
            var manager = CreateManager(CreateSave(alreadyClaimed: false));
            var entry = CreateEntryButton(host.transform, out var label);
            bridge.Configure(card, manager, topMenu, actionBar, devPanel, host.transform);
            bridge.BindEntryButton(entry);

            var result = manager.ClaimCheeseStarDelivery();

            var badge = entry.transform.Find(
                CheeseStarDeliveryBridge.EntryNotificationBadgeObjectName);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Claimed, Is.True);
            Assert.That(label.text, Is.EqualTo("오늘배달 받음"));
            Assert.That(entry.interactable, Is.False);
            Assert.That(badge.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void BindEntryButton_ReusesBadgeAndPreservesExistingClickListener()
        {
            var manager = CreateManager(CreateSave(alreadyClaimed: false));
            var entry = CreateEntryButton(host.transform, out _);
            var clickCount = 0;
            entry.onClick.AddListener(() => clickCount += 1);
            bridge.Configure(card, manager, topMenu, actionBar, devPanel, host.transform);

            bridge.BindEntryButton(entry);
            bridge.BindEntryButton(entry);
            entry.onClick.Invoke();

            var badgeCount = 0;
            for (var index = 0; index < entry.transform.childCount; index += 1)
            {
                if (entry.transform.GetChild(index).name
                    == CheeseStarDeliveryBridge.EntryNotificationBadgeObjectName)
                {
                    badgeCount += 1;
                }
            }

            Assert.That(badgeCount, Is.EqualTo(1));
            Assert.That(clickCount, Is.EqualTo(1));
        }

        private static CheeseStarDeliveryOffer CreateOffer(int year, int month, int day)
        {
            return CheeseStarDeliverySystem.ObserveEntry(
                new CheeseStarDeliverySaveData(),
                false,
                new DateTimeOffset(
                    year,
                    month,
                    day,
                    9,
                    0,
                    0,
                    TimeSpan.FromHours(9)));
        }

        private GameManager CreateManager(CheeseTamaSaveData saveData)
        {
            managerHost = new GameObject("Delivery Manager Host");
            managerHost.SetActive(false);
            var manager = managerHost.AddComponent<GameManager>();
            var currentSaveField = typeof(GameManager).GetField(
                "<CurrentSave>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(currentSaveField, Is.Not.Null);
            currentSaveField.SetValue(manager, saveData);
            return manager;
        }

        private static CheeseTamaSaveData CreateSave(bool alreadyClaimed)
        {
            var saveData = SaveManager.CreateDefaultSave();
            var todayKey = DateTime.Now.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);
            saveData.cheeseStarDelivery.latestObservedDateKey = todayKey;
            if (alreadyClaimed)
            {
                saveData.cheeseStarDelivery.lastClaimedDateKey = todayKey;
                saveData.cheeseStarDelivery.currentStreakDays = 1;
                saveData.cheeseStarDelivery.totalClaims = 1;
            }

            saveData.EnsureRuntimeDefaults();
            return saveData;
        }

        private static Button CreateEntryButton(Transform parent, out Text label)
        {
            var button = CreateButton(parent, "Open Delivery Button");
            label = CreateText(button.transform, "Label");
            label.text = CheeseStarDeliveryBridge.PendingEntryLabel;
            return button;
        }

        private static GameObject CreateRectChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Text CreateText(Transform parent, string name)
        {
            var child = CreateRectChild(parent, name);
            return child.AddComponent<Text>();
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
