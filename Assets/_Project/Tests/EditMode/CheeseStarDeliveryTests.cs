using System;
using CheeseTama.Gameplay.Deliveries;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class CheeseStarDeliveryTests
    {
        private static readonly TimeSpan KoreaOffset = TimeSpan.FromHours(9);

        [Test]
        public void NewPlayer_ReceivesBaseOfferAndObservationWatermark()
        {
            var saveData = new CheeseStarDeliverySaveData();

            var offer = CheeseStarDeliverySystem.ObserveEntry(
                saveData,
                false,
                At(2026, 8, 14));

            Assert.That(offer.Status, Is.EqualTo(CheeseStarDeliveryOfferStatus.Available));
            Assert.That(offer.StreakDay, Is.EqualTo(1));
            Assert.That(offer.RewardCycleDay, Is.EqualTo(1));
            Assert.That(offer.Reward.MilkCoins, Is.EqualTo(20));
            Assert.That(offer.Reward.MilkDrops, Is.EqualTo(3));
            Assert.That(offer.Reward.StarDrops, Is.Zero);
            Assert.That(offer.StateChanged, Is.True);
            Assert.That(saveData.latestObservedDateKey, Is.EqualTo("2026-08-14"));
            Assert.That(saveData.lastClaimedDateKey, Is.Empty);
        }

        [Test]
        public void SameDayClaim_IsIdempotent()
        {
            var saveData = new CheeseStarDeliverySaveData();
            var now = At(2026, 8, 14);

            var firstSucceeded = CheeseStarDeliverySystem.TryClaim(
                saveData,
                false,
                now,
                out var first);
            var secondSucceeded = CheeseStarDeliverySystem.TryClaim(
                saveData,
                false,
                now.AddHours(5),
                out var second);

            Assert.That(firstSucceeded, Is.True);
            Assert.That(first.Status, Is.EqualTo(CheeseStarDeliveryClaimStatus.Claimed));
            Assert.That(secondSucceeded, Is.False);
            Assert.That(second.Status, Is.EqualTo(CheeseStarDeliveryClaimStatus.AlreadyClaimed));
            Assert.That(second.Reward.IsEmpty, Is.True);
            Assert.That(saveData.totalClaims, Is.EqualTo(1));
            Assert.That(saveData.currentStreakDays, Is.EqualTo(1));
        }

        [Test]
        public void NextDay_ContinuesStreakWithBaseReward()
        {
            var saveData = new CheeseStarDeliverySaveData();
            Claim(saveData, At(2026, 8, 14));

            var offer = CheeseStarDeliverySystem.ObserveEntry(
                saveData,
                false,
                At(2026, 8, 15));

            Assert.That(offer.CanClaim, Is.True);
            Assert.That(offer.StreakDay, Is.EqualTo(2));
            Assert.That(offer.RewardCycleDay, Is.EqualTo(2));
            Assert.That(offer.BonusKind, Is.EqualTo(CheeseStarDeliveryBonusKind.None));
            Assert.That(offer.Reward.MilkCoins, Is.EqualTo(20));
            Assert.That(offer.Reward.MilkDrops, Is.EqualTo(3));
        }

        [Test]
        public void ConsecutiveDays_ReachDayThreeBonus()
        {
            var saveData = new CheeseStarDeliverySaveData();
            Claim(saveData, At(2026, 8, 14));
            Claim(saveData, At(2026, 8, 15));

            var offer = CheeseStarDeliverySystem.ObserveEntry(
                saveData,
                false,
                At(2026, 8, 16));

            Assert.That(offer.StreakDay, Is.EqualTo(3));
            Assert.That(offer.BonusKind, Is.EqualTo(CheeseStarDeliveryBonusKind.DayThree));
            Assert.That(offer.Reward.MilkCoins, Is.EqualTo(40));
            Assert.That(offer.Reward.MilkDrops, Is.EqualTo(5));
        }

        [Test]
        public void MissedDay_ResetsBonusStreakButKeepsNewBaseGift()
        {
            var saveData = new CheeseStarDeliverySaveData();
            Claim(saveData, At(2026, 8, 14));
            Claim(saveData, At(2026, 8, 15));

            var offer = CheeseStarDeliverySystem.ObserveEntry(
                saveData,
                false,
                At(2026, 8, 17));

            Assert.That(offer.CanClaim, Is.True);
            Assert.That(offer.StreakDay, Is.EqualTo(1));
            Assert.That(offer.BonusKind, Is.EqualTo(CheeseStarDeliveryBonusKind.None));
            Assert.That(offer.Reward.MilkCoins, Is.EqualTo(20));
            Assert.That(offer.Reward.MilkDrops, Is.EqualTo(3));
            Assert.That(saveData.totalClaims, Is.EqualTo(2));
        }

        [Test]
        public void DaySeven_OnlyRevealsStarRewardAfterRouteUnlock()
        {
            var hiddenSave = new CheeseStarDeliverySaveData();
            var revealedSave = new CheeseStarDeliverySaveData();
            for (var day = 14; day <= 19; day += 1)
            {
                Claim(hiddenSave, At(2026, 8, day), false);
                Claim(revealedSave, At(2026, 8, day), true);
            }

            var hiddenOffer = CheeseStarDeliverySystem.ObserveEntry(
                hiddenSave,
                false,
                At(2026, 8, 20));
            var revealedOffer = CheeseStarDeliverySystem.ObserveEntry(
                revealedSave,
                true,
                At(2026, 8, 20));

            Assert.That(hiddenOffer.BonusKind, Is.EqualTo(CheeseStarDeliveryBonusKind.DaySeven));
            Assert.That(hiddenOffer.Reward.StarDrops, Is.Zero);
            Assert.That(hiddenOffer.RevealStarRoute, Is.False);
            Assert.That(revealedOffer.Reward.StarDrops, Is.EqualTo(1));
            Assert.That(revealedOffer.RevealStarRoute, Is.True);
        }

        [Test]
        public void ClockRollback_FailsClosedWithoutMovingWatermarkBackwards()
        {
            var saveData = new CheeseStarDeliverySaveData();
            var first = CheeseStarDeliverySystem.ObserveEntry(
                saveData,
                false,
                At(2026, 8, 14));

            var rolledBack = CheeseStarDeliverySystem.ObserveEntry(
                saveData,
                false,
                At(2026, 8, 13));
            var claimSucceeded = CheeseStarDeliverySystem.TryClaim(
                saveData,
                false,
                At(2026, 8, 13),
                out var claim);

            Assert.That(first.CanClaim, Is.True);
            Assert.That(rolledBack.Status, Is.EqualTo(CheeseStarDeliveryOfferStatus.ClockRollback));
            Assert.That(claimSucceeded, Is.False);
            Assert.That(claim.Status, Is.EqualTo(CheeseStarDeliveryClaimStatus.ClockRollback));
            Assert.That(saveData.latestObservedDateKey, Is.EqualTo("2026-08-14"));
            Assert.That(saveData.totalClaims, Is.Zero);
        }

        [Test]
        public void RuntimeDefaults_NormalizeLegacyAndCorruptedFieldsIdempotently()
        {
            var saveData = new CheeseStarDeliverySaveData
            {
                schemaVersion = 0,
                latestObservedDateKey = null,
                lastClaimedDateKey = null,
                lastClaimedAtIso = "not-a-time",
                currentStreakDays = -8,
                totalClaims = -3
            };

            var firstChanged = saveData.EnsureRuntimeDefaults();
            var secondChanged = saveData.EnsureRuntimeDefaults();

            Assert.That(firstChanged, Is.True);
            Assert.That(secondChanged, Is.False);
            Assert.That(saveData.schemaVersion, Is.EqualTo(CheeseStarDeliverySaveData.CurrentSchemaVersion));
            Assert.That(saveData.latestObservedDateKey, Is.Empty);
            Assert.That(saveData.lastClaimedDateKey, Is.Empty);
            Assert.That(saveData.lastClaimedAtIso, Is.Empty);
            Assert.That(saveData.currentStreakDays, Is.Zero);
            Assert.That(saveData.totalClaims, Is.Zero);
        }

        [Test]
        public void JsonReload_PreservesClaimReceiptAndBlocksDuplicateClaim()
        {
            var saveData = new CheeseStarDeliverySaveData();
            var now = At(2026, 8, 14);
            Claim(saveData, now);

            var json = JsonUtility.ToJson(saveData);
            var reloaded = JsonUtility.FromJson<CheeseStarDeliverySaveData>(json);
            var defaultsChanged = reloaded.EnsureRuntimeDefaults();
            var claimedAgain = CheeseStarDeliverySystem.TryClaim(
                reloaded,
                false,
                now.AddHours(2),
                out var result);

            Assert.That(defaultsChanged, Is.False);
            Assert.That(claimedAgain, Is.False);
            Assert.That(result.Status, Is.EqualTo(CheeseStarDeliveryClaimStatus.AlreadyClaimed));
            Assert.That(reloaded.totalClaims, Is.EqualTo(1));
        }

        [Test]
        public void CardController_UsesCallbacksOnceAndHidesStarCopyBeforeUnlock()
        {
            var host = new GameObject("Delivery Card Host");
            var overlay = CreateRectChild(host.transform, "Overlay");
            var title = CreateText(overlay.transform, "Title");
            var streak = CreateText(overlay.transform, "Streak");
            var reward = CreateText(overlay.transform, "Reward");
            var note = CreateText(overlay.transform, "Note");
            var claimButton = CreateButton(overlay.transform, "Claim");
            var laterButton = CreateButton(overlay.transform, "Later");
            var controller = host.AddComponent<CheeseStarDeliveryCardController>();
            controller.Configure(
                overlay,
                title,
                streak,
                reward,
                note,
                claimButton,
                laterButton);

            var offer = CheeseStarDeliverySystem.ObserveEntry(
                new CheeseStarDeliverySaveData(),
                false,
                At(2026, 8, 14));
            var claimRequests = 0;
            var laterRequests = 0;

            Assert.That(controller.Show(offer, () => claimRequests += 1, () => laterRequests += 1), Is.True);
            Assert.That(controller.IsBlockingGameplay, Is.True);
            Assert.That(title.text, Does.Not.Contain("별"));
            Assert.That(reward.text, Does.Not.Contain("별"));

            claimButton.onClick.Invoke();
            claimButton.onClick.Invoke();
            Assert.That(claimRequests, Is.EqualTo(1));

            controller.Hide();
            Assert.That(controller.Show(offer, () => claimRequests += 1, () => laterRequests += 1), Is.True);
            laterButton.onClick.Invoke();
            Assert.That(laterRequests, Is.EqualTo(1));
            Assert.That(controller.IsBlockingGameplay, Is.False);

            UnityEngine.Object.DestroyImmediate(host);
        }

        private static void Claim(
            CheeseStarDeliverySaveData saveData,
            DateTimeOffset now,
            bool starRouteUnlocked = false)
        {
            var succeeded = CheeseStarDeliverySystem.TryClaim(
                saveData,
                starRouteUnlocked,
                now,
                out var result);

            Assert.That(succeeded, Is.True, $"Claim failed with {result.Status}.");
        }

        private static DateTimeOffset At(int year, int month, int day)
        {
            return new DateTimeOffset(year, month, day, 9, 0, 0, KoreaOffset);
        }

        private static GameObject CreateRectChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Text CreateText(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.AddComponent<Text>();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var image = child.AddComponent<Image>();
            var button = child.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
