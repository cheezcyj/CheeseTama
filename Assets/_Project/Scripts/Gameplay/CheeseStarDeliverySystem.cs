using System;
using System.Globalization;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Deliveries
{
    public enum CheeseStarDeliveryOfferStatus
    {
        Available = 0,
        AlreadyClaimed = 1,
        ClockRollback = 2,
        InvalidSaveData = 3
    }

    public enum CheeseStarDeliveryBonusKind
    {
        None = 0,
        DayThree = 1,
        DaySeven = 2
    }

    public enum CheeseStarDeliveryClaimStatus
    {
        Claimed = 0,
        AlreadyClaimed = 1,
        ClockRollback = 2,
        InvalidSaveData = 3
    }

    public sealed class CheeseStarDeliveryReward
    {
        public CheeseStarDeliveryReward(
            int milkCoins,
            int milkDrops,
            int starDrops,
            int fantasyPowder = 0)
        {
            MilkCoins = Math.Max(0, milkCoins);
            MilkDrops = Math.Max(0, milkDrops);
            StarDrops = Math.Max(0, starDrops);
            FantasyPowder = Math.Max(0, fantasyPowder);
        }

        public int MilkCoins { get; }

        public int MilkDrops { get; }

        public int StarDrops { get; }

        public int FantasyPowder { get; }

        public bool IsEmpty => MilkCoins == 0
            && MilkDrops == 0
            && StarDrops == 0
            && FantasyPowder == 0;

        public static CheeseStarDeliveryReward None()
        {
            return new CheeseStarDeliveryReward(0, 0, 0);
        }
    }

    public sealed class CheeseStarDeliveryOffer
    {
        internal CheeseStarDeliveryOffer(
            CheeseStarDeliveryOfferStatus status,
            string dateKey,
            int streakDay,
            int rewardCycleDay,
            CheeseStarDeliveryBonusKind bonusKind,
            CheeseStarDeliveryReward reward,
            bool revealStarRoute,
            bool stateChanged)
        {
            Status = status;
            DateKey = dateKey ?? string.Empty;
            StreakDay = Math.Max(0, streakDay);
            RewardCycleDay = Math.Max(0, rewardCycleDay);
            BonusKind = bonusKind;
            Reward = reward ?? CheeseStarDeliveryReward.None();
            RevealStarRoute = revealStarRoute;
            StateChanged = stateChanged;
        }

        public CheeseStarDeliveryOfferStatus Status { get; }

        public string DateKey { get; }

        public int StreakDay { get; }

        public int RewardCycleDay { get; }

        public CheeseStarDeliveryBonusKind BonusKind { get; }

        public CheeseStarDeliveryReward Reward { get; }

        public bool RevealStarRoute { get; }

        public bool StateChanged { get; }

        public bool CanClaim => Status == CheeseStarDeliveryOfferStatus.Available;
    }

    public sealed class CheeseStarDeliveryClaimResult
    {
        internal CheeseStarDeliveryClaimResult(
            CheeseStarDeliveryClaimStatus status,
            CheeseStarDeliveryOffer offer,
            bool stateChanged)
        {
            Status = status;
            Offer = offer;
            StateChanged = stateChanged;
        }

        public CheeseStarDeliveryClaimStatus Status { get; }

        public CheeseStarDeliveryOffer Offer { get; }

        public CheeseStarDeliveryReward Reward => Status == CheeseStarDeliveryClaimStatus.Claimed
            ? Offer.Reward
            : CheeseStarDeliveryReward.None();

        public bool StateChanged { get; }

        public bool Claimed => Status == CheeseStarDeliveryClaimStatus.Claimed;
    }

    public static class CheeseStarDeliverySystem
    {
        public const int RewardCycleLength = 7;
        public const int BaseMilkCoins = 20;
        public const int BaseMilkDrops = 3;
        public const int DayThreeBonusMilkCoins = 20;
        public const int DayThreeBonusMilkDrops = 2;
        public const int DaySevenBonusMilkCoins = 50;
        public const int DaySevenBonusMilkDrops = 5;
        public const int DaySevenBonusStarDrops = 1;
        public const int DaySevenBonusFantasyPowder = 1;

        public static CheeseStarDeliveryOffer ObserveEntry(
            CheeseStarDeliverySaveData saveData,
            bool starRouteUnlocked)
        {
            return ObserveEntry(saveData, starRouteUnlocked, DateTimeOffset.Now);
        }

        public static CheeseStarDeliveryOffer ObserveEntry(
            CheeseStarDeliverySaveData saveData,
            bool starRouteUnlocked,
            DateTimeOffset now)
        {
            var dateKey = ToDateKey(now);
            if (saveData == null)
            {
                return CreateBlockedOffer(
                    CheeseStarDeliveryOfferStatus.InvalidSaveData,
                    dateKey,
                    starRouteUnlocked,
                    false);
            }

            var stateChanged = saveData.EnsureRuntimeDefaults();
            var today = now.Date;
            if (TryParseDateKey(saveData.latestObservedDateKey, out var latestObserved)
                && today < latestObserved)
            {
                return CreateBlockedOffer(
                    CheeseStarDeliveryOfferStatus.ClockRollback,
                    dateKey,
                    starRouteUnlocked,
                    stateChanged);
            }

            if (!TryParseDateKey(saveData.latestObservedDateKey, out latestObserved)
                || today > latestObserved)
            {
                saveData.latestObservedDateKey = dateKey;
                stateChanged = true;
            }

            if (string.Equals(
                    saveData.lastClaimedDateKey,
                    dateKey,
                    StringComparison.Ordinal))
            {
                return CreateBlockedOffer(
                    CheeseStarDeliveryOfferStatus.AlreadyClaimed,
                    dateKey,
                    starRouteUnlocked,
                    stateChanged);
            }

            var streakDay = ResolveNextStreakDay(saveData, today);
            var rewardCycleDay = ((streakDay - 1) % RewardCycleLength) + 1;
            var bonusKind = ResolveBonusKind(rewardCycleDay);
            var reward = CreateReward(bonusKind, starRouteUnlocked);
            return new CheeseStarDeliveryOffer(
                CheeseStarDeliveryOfferStatus.Available,
                dateKey,
                streakDay,
                rewardCycleDay,
                bonusKind,
                reward,
                starRouteUnlocked,
                stateChanged);
        }

        public static bool TryClaim(
            CheeseStarDeliverySaveData saveData,
            bool starRouteUnlocked,
            out CheeseStarDeliveryClaimResult result)
        {
            return TryClaim(saveData, starRouteUnlocked, DateTimeOffset.Now, out result);
        }

        public static bool TryClaim(
            CheeseStarDeliverySaveData saveData,
            bool starRouteUnlocked,
            DateTimeOffset now,
            out CheeseStarDeliveryClaimResult result)
        {
            var offer = ObserveEntry(saveData, starRouteUnlocked, now);
            if (!offer.CanClaim)
            {
                result = new CheeseStarDeliveryClaimResult(
                    MapClaimStatus(offer.Status),
                    offer,
                    offer.StateChanged);
                return false;
            }

            saveData.lastClaimedDateKey = offer.DateKey;
            saveData.lastClaimedAtIso = now
                .ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture);
            saveData.currentStreakDays = offer.StreakDay;
            if (saveData.totalClaims < int.MaxValue)
            {
                saveData.totalClaims += 1;
            }

            result = new CheeseStarDeliveryClaimResult(
                CheeseStarDeliveryClaimStatus.Claimed,
                offer,
                true);
            return true;
        }

        private static CheeseStarDeliveryOffer CreateBlockedOffer(
            CheeseStarDeliveryOfferStatus status,
            string dateKey,
            bool starRouteUnlocked,
            bool stateChanged)
        {
            return new CheeseStarDeliveryOffer(
                status,
                dateKey,
                0,
                0,
                CheeseStarDeliveryBonusKind.None,
                CheeseStarDeliveryReward.None(),
                starRouteUnlocked,
                stateChanged);
        }

        private static int ResolveNextStreakDay(
            CheeseStarDeliverySaveData saveData,
            DateTime today)
        {
            if (!TryParseDateKey(saveData.lastClaimedDateKey, out var lastClaimed)
                || (today - lastClaimed).Days != 1)
            {
                return 1;
            }

            return saveData.currentStreakDays >= int.MaxValue
                ? int.MaxValue
                : Math.Max(1, saveData.currentStreakDays) + 1;
        }

        private static CheeseStarDeliveryBonusKind ResolveBonusKind(int rewardCycleDay)
        {
            return rewardCycleDay switch
            {
                3 => CheeseStarDeliveryBonusKind.DayThree,
                7 => CheeseStarDeliveryBonusKind.DaySeven,
                _ => CheeseStarDeliveryBonusKind.None
            };
        }

        private static CheeseStarDeliveryReward CreateReward(
            CheeseStarDeliveryBonusKind bonusKind,
            bool starRouteUnlocked)
        {
            var milkCoins = BaseMilkCoins;
            var milkDrops = BaseMilkDrops;
            var starDrops = 0;
            var fantasyPowder = 0;

            switch (bonusKind)
            {
                case CheeseStarDeliveryBonusKind.DayThree:
                    milkCoins += DayThreeBonusMilkCoins;
                    milkDrops += DayThreeBonusMilkDrops;
                    break;
                case CheeseStarDeliveryBonusKind.DaySeven:
                    milkCoins += DaySevenBonusMilkCoins;
                    milkDrops += DaySevenBonusMilkDrops;
                    if (starRouteUnlocked)
                    {
                        starDrops += DaySevenBonusStarDrops;
                        fantasyPowder += DaySevenBonusFantasyPowder;
                    }
                    break;
            }

            return new CheeseStarDeliveryReward(
                milkCoins,
                milkDrops,
                starDrops,
                fantasyPowder);
        }

        private static CheeseStarDeliveryClaimStatus MapClaimStatus(
            CheeseStarDeliveryOfferStatus status)
        {
            return status switch
            {
                CheeseStarDeliveryOfferStatus.AlreadyClaimed =>
                    CheeseStarDeliveryClaimStatus.AlreadyClaimed,
                CheeseStarDeliveryOfferStatus.ClockRollback =>
                    CheeseStarDeliveryClaimStatus.ClockRollback,
                _ => CheeseStarDeliveryClaimStatus.InvalidSaveData
            };
        }

        private static string ToDateKey(DateTimeOffset value)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static bool TryParseDateKey(string value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }
    }
}
