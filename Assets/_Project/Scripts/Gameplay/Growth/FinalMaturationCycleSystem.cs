using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public enum FinalMaturationProgressStatus
    {
        Applied,
        AlreadyApplied,
        MissingState,
        MissingTama,
        InvalidReceipt,
        InvalidAmount,
        NotFinalLevel,
        RewardQueueFull
    }

    public enum FinalMaturationClaimStatus
    {
        Applied,
        AlreadyApplied,
        MissingState,
        MissingEconomy,
        InvalidReceipt,
        NoPendingReward,
        MissingFantasyPowderState,
        RewardCapacityFull
    }

    public readonly struct FinalMaturationReward
    {
        public FinalMaturationReward(
            string rewardId,
            int cycleNumber,
            int milkCoins,
            int milkDrops,
            int starDrops,
            int fantasyPowder)
        {
            this.rewardId = rewardId ?? string.Empty;
            this.cycleNumber = Math.Max(0, cycleNumber);
            this.milkCoins = Math.Max(0, milkCoins);
            this.milkDrops = Math.Max(0, milkDrops);
            this.starDrops = Math.Max(0, starDrops);
            this.fantasyPowder = Math.Max(0, fantasyPowder);
        }

        public string rewardId { get; }
        public int cycleNumber { get; }
        public int milkCoins { get; }
        public int milkDrops { get; }
        public int starDrops { get; }
        public int fantasyPowder { get; }
        public bool isEmpty => milkCoins == 0
            && milkDrops == 0
            && starDrops == 0
            && fantasyPowder == 0;

        public FinalMaturationRewardSaveEntry ToSaveEntry()
        {
            return new FinalMaturationRewardSaveEntry
            {
                rewardId = rewardId,
                cycleNumber = cycleNumber,
                milkCoins = milkCoins,
                milkDrops = milkDrops,
                starDrops = starDrops,
                fantasyPowder = fantasyPowder
            };
        }

        public static FinalMaturationReward FromSaveEntry(FinalMaturationRewardSaveEntry entry)
        {
            return entry == null
                ? default
                : new FinalMaturationReward(
                    entry.rewardId,
                    entry.cycleNumber,
                    entry.milkCoins,
                    entry.milkDrops,
                    entry.starDrops,
                    entry.fantasyPowder);
        }
    }

    public readonly struct FinalMaturationProgressResult
    {
        public FinalMaturationProgressResult(
            FinalMaturationProgressStatus status,
            string receiptKey,
            int previousProgress,
            int currentProgress,
            int completedCycles,
            IReadOnlyList<FinalMaturationReward> generatedRewards)
        {
            this.status = status;
            this.receiptKey = receiptKey ?? string.Empty;
            this.previousProgress = Math.Max(0, previousProgress);
            this.currentProgress = Math.Max(0, currentProgress);
            this.completedCycles = Math.Max(0, completedCycles);
            this.generatedRewards = generatedRewards ?? Array.Empty<FinalMaturationReward>();
        }

        public FinalMaturationProgressStatus status { get; }
        public string receiptKey { get; }
        public int previousProgress { get; }
        public int currentProgress { get; }
        public int completedCycles { get; }
        public IReadOnlyList<FinalMaturationReward> generatedRewards { get; }
        public bool applied => status == FinalMaturationProgressStatus.Applied;
        public bool duplicateReceipt => status == FinalMaturationProgressStatus.AlreadyApplied;
    }

    public readonly struct FinalMaturationClaimResult
    {
        public FinalMaturationClaimResult(
            FinalMaturationClaimStatus status,
            string receiptKey,
            FinalMaturationReward reward)
        {
            this.status = status;
            this.receiptKey = receiptKey ?? string.Empty;
            this.reward = reward;
        }

        public FinalMaturationClaimStatus status { get; }
        public string receiptKey { get; }
        public FinalMaturationReward reward { get; }
        public bool applied => status == FinalMaturationClaimStatus.Applied;
        public bool duplicateReceipt => status == FinalMaturationClaimStatus.AlreadyApplied;
    }

    public readonly struct FinalMaturationCycleSnapshot
    {
        public FinalMaturationCycleSnapshot(
            int progress,
            int completedCycles,
            int claimedCycles,
            int pendingRewardCount,
            FinalMaturationReward nextReward)
        {
            this.progress = Math.Max(0, Math.Min(FinalMaturationCycleSystem.RequiredProgress - 1, progress));
            this.completedCycles = Math.Max(0, completedCycles);
            this.claimedCycles = Math.Max(0, Math.Min(this.completedCycles, claimedCycles));
            this.pendingRewardCount = Math.Max(0, pendingRewardCount);
            this.nextReward = nextReward;
        }

        public int progress { get; }
        public int requiredProgress => FinalMaturationCycleSystem.RequiredProgress;
        public int completedCycles { get; }
        public int claimedCycles { get; }
        public int pendingRewardCount { get; }
        public FinalMaturationReward nextReward { get; }
    }

    public sealed class FinalMaturationCycleSystem
    {
        public const int RequiredProgress = 100;
        public const int BaseMilkCoins = 60;
        public const int BaseMilkDrops = 10;
        public const int StarDropRewardInterval = 3;
        public const int FantasyPowderRewardInterval = 7;

        public FinalMaturationProgressResult AddProgress(
            CheeseTamaModel tama,
            FinalMaturationCycleSaveData state,
            int amount,
            string receiptKey,
            bool starRouteUnlocked,
            bool fantasyPowderEnabled)
        {
            var normalizedReceipt = Normalize(receiptKey);
            if (state == null)
            {
                return ProgressFailure(
                    FinalMaturationProgressStatus.MissingState,
                    normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return ProgressFailure(
                    FinalMaturationProgressStatus.InvalidReceipt,
                    normalizedReceipt,
                    state.progress);
            }

            if (state.HasAppliedProgressReceipt(normalizedReceipt))
            {
                return ProgressFailure(
                    FinalMaturationProgressStatus.AlreadyApplied,
                    normalizedReceipt,
                    state.progress);
            }

            if (tama == null)
            {
                return ProgressFailure(
                    FinalMaturationProgressStatus.MissingTama,
                    normalizedReceipt,
                    state.progress);
            }

            if (amount <= 0)
            {
                return ProgressFailure(
                    FinalMaturationProgressStatus.InvalidAmount,
                    normalizedReceipt,
                    state.progress);
            }

            if (tama.level < Math.Max(UnlockSystem.MaxLevel, tama.maxLevel))
            {
                return ProgressFailure(
                    FinalMaturationProgressStatus.NotFinalLevel,
                    normalizedReceipt,
                    state.progress);
            }

            var totalProgress = (long)state.progress + amount;
            var completedNow = totalProgress / RequiredProgress;
            if (completedNow > FinalMaturationCycleSaveData.MaximumPendingRewards
                || state.pendingRewards.Count + completedNow
                    > FinalMaturationCycleSaveData.MaximumPendingRewards
                || state.completedCycles > int.MaxValue - completedNow)
            {
                return ProgressFailure(
                    FinalMaturationProgressStatus.RewardQueueFull,
                    normalizedReceipt,
                    state.progress);
            }

            var generated = new FinalMaturationReward[(int)completedNow];
            for (var index = 0; index < generated.Length; index += 1)
            {
                var cycleNumber = state.completedCycles + index + 1;
                generated[index] = CreateReward(
                    cycleNumber,
                    starRouteUnlocked,
                    fantasyPowderEnabled);
            }

            var previousProgress = state.progress;
            state.progress = (int)(totalProgress % RequiredProgress);
            state.completedCycles += generated.Length;
            for (var index = 0; index < generated.Length; index += 1)
            {
                state.pendingRewards.Add(generated[index].ToSaveEntry());
            }

            state.AddAppliedProgressReceipt(normalizedReceipt);
            return new FinalMaturationProgressResult(
                FinalMaturationProgressStatus.Applied,
                normalizedReceipt,
                previousProgress,
                state.progress,
                generated.Length,
                generated);
        }

        public FinalMaturationClaimResult TryClaimNext(
            FinalMaturationCycleSaveData state,
            EconomySaveData economy,
            FantasyPowderSaveData fantasyPowder,
            string receiptKey)
        {
            var normalizedReceipt = Normalize(receiptKey);
            if (state == null)
            {
                return ClaimFailure(
                    FinalMaturationClaimStatus.MissingState,
                    normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return ClaimFailure(
                    FinalMaturationClaimStatus.InvalidReceipt,
                    normalizedReceipt);
            }

            if (state.HasAppliedClaimReceipt(normalizedReceipt))
            {
                return ClaimFailure(
                    FinalMaturationClaimStatus.AlreadyApplied,
                    normalizedReceipt);
            }

            if (economy == null)
            {
                return ClaimFailure(
                    FinalMaturationClaimStatus.MissingEconomy,
                    normalizedReceipt);
            }

            if (state.pendingRewards.Count == 0)
            {
                return ClaimFailure(
                    FinalMaturationClaimStatus.NoPendingReward,
                    normalizedReceipt);
            }

            var entry = state.pendingRewards[0];
            var reward = FinalMaturationReward.FromSaveEntry(entry);
            if (reward.fantasyPowder > 0 && fantasyPowder == null)
            {
                return ClaimFailure(
                    FinalMaturationClaimStatus.MissingFantasyPowderState,
                    normalizedReceipt);
            }

            if (!CanAdd(economy.milkCoins, reward.milkCoins)
                || !CanAdd(economy.milkDrops, reward.milkDrops)
                || !CanAdd(economy.starDrops, reward.starDrops)
                || (reward.fantasyPowder > 0
                    && !CanAdd(fantasyPowder.powderQuantity, reward.fantasyPowder)))
            {
                return ClaimFailure(
                    FinalMaturationClaimStatus.RewardCapacityFull,
                    normalizedReceipt);
            }

            if (fantasyPowder != null)
            {
                fantasyPowder.EnsureRuntimeDefaults();
            }

            economy.milkCoins += reward.milkCoins;
            economy.milkDrops += reward.milkDrops;
            economy.starDrops += reward.starDrops;
            if (reward.fantasyPowder > 0)
            {
                fantasyPowder.powderQuantity += reward.fantasyPowder;
            }

            state.pendingRewards.RemoveAt(0);
            state.claimedCycles = Math.Max(state.claimedCycles, reward.cycleNumber);
            state.AddAppliedClaimReceipt(normalizedReceipt);
            return new FinalMaturationClaimResult(
                FinalMaturationClaimStatus.Applied,
                normalizedReceipt,
                reward);
        }

        public FinalMaturationCycleSnapshot BuildSnapshot(FinalMaturationCycleSaveData state)
        {
            if (state == null)
            {
                return new FinalMaturationCycleSnapshot(0, 0, 0, 0, default);
            }

            state.EnsureRuntimeDefaults();
            var nextReward = state.pendingRewards.Count > 0
                ? FinalMaturationReward.FromSaveEntry(state.pendingRewards[0])
                : default;
            return new FinalMaturationCycleSnapshot(
                state.progress,
                state.completedCycles,
                state.claimedCycles,
                state.pendingRewards.Count,
                nextReward);
        }

        public FinalMaturationReward CreateReward(
            int cycleNumber,
            bool starRouteUnlocked,
            bool fantasyPowderEnabled)
        {
            var normalizedCycle = Math.Max(1, cycleNumber);
            return new FinalMaturationReward(
                $"final_maturation_{normalizedCycle:D8}",
                normalizedCycle,
                BaseMilkCoins,
                BaseMilkDrops,
                starRouteUnlocked && normalizedCycle % StarDropRewardInterval == 0 ? 1 : 0,
                fantasyPowderEnabled && normalizedCycle % FantasyPowderRewardInterval == 0 ? 1 : 0);
        }

        private static bool CanAdd(int current, int amount)
        {
            return current >= 0
                && amount >= 0
                && (long)current + amount <= int.MaxValue;
        }

        private static FinalMaturationProgressResult ProgressFailure(
            FinalMaturationProgressStatus status,
            string receiptKey,
            int progress = 0)
        {
            return new FinalMaturationProgressResult(
                status,
                receiptKey,
                progress,
                progress,
                0,
                Array.Empty<FinalMaturationReward>());
        }

        private static FinalMaturationClaimResult ClaimFailure(
            FinalMaturationClaimStatus status,
            string receiptKey)
        {
            return new FinalMaturationClaimResult(status, receiptKey, default);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
