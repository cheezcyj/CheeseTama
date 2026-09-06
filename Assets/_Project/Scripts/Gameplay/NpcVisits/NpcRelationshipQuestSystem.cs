using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.NpcVisits
{
    public enum NpcRelationshipTier
    {
        NewFace = 0,
        Familiar = 1,
        Friend = 2,
        TrustedFriend = 3
    }

    public readonly struct NpcRelationshipSnapshot
    {
        public NpcRelationshipSnapshot(
            bool knownNpc,
            string npcId,
            int visits,
            int affinity,
            NpcRelationshipTier tier)
        {
            KnownNpc = knownNpc;
            NpcId = npcId ?? string.Empty;
            Visits = Math.Max(0, visits);
            Affinity = Math.Max(0, Math.Min(99, affinity));
            Tier = tier;
        }

        public bool KnownNpc { get; }
        public string NpcId { get; }
        public int Visits { get; }
        public int Affinity { get; }
        public NpcRelationshipTier Tier { get; }
    }

    public readonly struct NpcQuestCost
    {
        public NpcQuestCost(
            int milkCoins = 0,
            int milkDrops = 0,
            int collectionFragments = 0,
            string snackId = "",
            int snackQuantity = 0)
        {
            MilkCoins = Math.Max(0, milkCoins);
            MilkDrops = Math.Max(0, milkDrops);
            CollectionFragments = Math.Max(0, collectionFragments);
            SnackId = (snackId ?? string.Empty).Trim();
            SnackQuantity = string.IsNullOrEmpty(SnackId) ? 0 : Math.Max(0, snackQuantity);
        }

        public int MilkCoins { get; }
        public int MilkDrops { get; }
        public int CollectionFragments { get; }
        public string SnackId { get; }
        public int SnackQuantity { get; }
    }

    public readonly struct NpcQuestReward
    {
        public NpcQuestReward(
            int milkCoins,
            int milkDrops,
            int collectionFragments,
            int affinity)
        {
            MilkCoins = Math.Max(0, milkCoins);
            MilkDrops = Math.Max(0, milkDrops);
            CollectionFragments = Math.Max(0, collectionFragments);
            Affinity = Math.Max(0, affinity);
        }

        public int MilkCoins { get; }
        public int MilkDrops { get; }
        public int CollectionFragments { get; }
        public int Affinity { get; }
    }

    public sealed class NpcRelationshipQuestDefinition
    {
        public NpcRelationshipQuestDefinition(
            string id,
            string npcId,
            string title,
            string description,
            int minimumAffinity,
            NpcQuestCost cost,
            NpcQuestReward reward)
        {
            Id = (id ?? string.Empty).Trim();
            NpcId = (npcId ?? string.Empty).Trim();
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            MinimumAffinity = Math.Max(0, Math.Min(99, minimumAffinity));
            Cost = cost;
            Reward = reward;
        }

        public string Id { get; }
        public string NpcId { get; }
        public string Title { get; }
        public string Description { get; }
        public int MinimumAffinity { get; }
        public NpcQuestCost Cost { get; }
        public NpcQuestReward Reward { get; }
    }

    public enum NpcQuestActivationStatus
    {
        Activated,
        MissingState,
        MissingRelationshipState,
        InvalidOfferId,
        UnknownNpc,
        UnknownQuest,
        QuestNpcMismatch,
        RelationshipLocked,
        AlreadyActive,
        AlreadyClaimedOffer
    }

    public readonly struct NpcQuestActivationResult
    {
        public NpcQuestActivationResult(
            NpcQuestActivationStatus status,
            NpcRelationshipQuestDefinition quest,
            string offerId,
            DateTimeOffset expiresAt,
            DateTimeOffset graceEndsAt)
        {
            Status = status;
            Quest = quest;
            OfferId = offerId ?? string.Empty;
            ExpiresAt = expiresAt;
            GraceEndsAt = graceEndsAt;
        }

        public NpcQuestActivationStatus Status { get; }
        public NpcRelationshipQuestDefinition Quest { get; }
        public string OfferId { get; }
        public DateTimeOffset ExpiresAt { get; }
        public DateTimeOffset GraceEndsAt { get; }
        public bool Applied => Status == NpcQuestActivationStatus.Activated;
    }

    public enum NpcQuestWindowStatus
    {
        None,
        Active,
        Grace,
        Expired,
        ClockRollback,
        UnknownQuest
    }

    public readonly struct NpcQuestWindowSnapshot
    {
        public NpcQuestWindowSnapshot(
            NpcQuestWindowStatus status,
            NpcRelationshipQuestDefinition quest,
            string offerId,
            DateTimeOffset expiresAt,
            DateTimeOffset graceEndsAt)
        {
            Status = status;
            Quest = quest;
            OfferId = offerId ?? string.Empty;
            ExpiresAt = expiresAt;
            GraceEndsAt = graceEndsAt;
        }

        public NpcQuestWindowStatus Status { get; }
        public NpcRelationshipQuestDefinition Quest { get; }
        public string OfferId { get; }
        public DateTimeOffset ExpiresAt { get; }
        public DateTimeOffset GraceEndsAt { get; }
        public bool CanDeliver => Status == NpcQuestWindowStatus.Active
            || Status == NpcQuestWindowStatus.Grace;
        public bool IsGrace => Status == NpcQuestWindowStatus.Grace;
    }

    public enum NpcQuestDeliveryStatus
    {
        Applied,
        DuplicateClaim,
        MissingState,
        MissingRelationshipState,
        MissingEconomy,
        MissingInventory,
        InvalidClaimReceipt,
        NoActiveQuest,
        UnknownQuest,
        ClockRollback,
        Expired,
        InsufficientResources,
        RewardCapacityFull
    }

    public readonly struct NpcQuestDeliveryResult
    {
        public NpcQuestDeliveryResult(
            NpcQuestDeliveryStatus status,
            NpcRelationshipQuestDefinition quest,
            string claimReceiptId,
            bool usedGrace,
            int affinityBefore,
            int affinityAfter,
            NpcRelationshipTier tierBefore,
            NpcRelationshipTier tierAfter)
        {
            Status = status;
            Quest = quest;
            ClaimReceiptId = claimReceiptId ?? string.Empty;
            UsedGrace = usedGrace;
            AffinityBefore = Math.Max(0, Math.Min(99, affinityBefore));
            AffinityAfter = Math.Max(0, Math.Min(99, affinityAfter));
            TierBefore = tierBefore;
            TierAfter = tierAfter;
        }

        public NpcQuestDeliveryStatus Status { get; }
        public NpcRelationshipQuestDefinition Quest { get; }
        public string ClaimReceiptId { get; }
        public bool UsedGrace { get; }
        public int AffinityBefore { get; }
        public int AffinityAfter { get; }
        public NpcRelationshipTier TierBefore { get; }
        public NpcRelationshipTier TierAfter { get; }
        public bool Applied => Status == NpcQuestDeliveryStatus.Applied;
        public bool TierAdvanced => Applied && (int)TierAfter > (int)TierBefore;
    }

    public sealed class NpcRelationshipQuestSystem
    {
        public const int FamiliarAffinityThreshold = 10;
        public const int FriendAffinityThreshold = 25;
        public const int TrustedFriendAffinityThreshold = 50;
        public const int ActiveDurationDays = 3;
        public const int GraceDurationDays = 2;

        private static readonly NpcRelationshipQuestDefinition[] Definitions =
        {
            new NpcRelationshipQuestDefinition(
                "doctor_warm_soup",
                NpcVisitSystem.MilkyDoctorId,
                "따뜻한 한 그릇",
                "밀키 박사가 돌봄 기록을 위해 따뜻한 우유 수프 한 그릇을 부탁했어요.",
                0,
                new NpcQuestCost(snackId: SnackCatalog.WarmMilkSoupId, snackQuantity: 1),
                new NpcQuestReward(0, 2, 1, 5)),
            new NpcRelationshipQuestDefinition(
                "doctor_care_notes",
                NpcVisitSystem.MilkyDoctorId,
                "돌봄 수첩 정리",
                "오래된 돌봄 수첩을 정리할 재료를 함께 준비해 달라고 부탁했어요.",
                FamiliarAffinityThreshold,
                new NpcQuestCost(milkCoins: 20, milkDrops: 2),
                new NpcQuestReward(0, 0, 3, 7)),
            new NpcRelationshipQuestDefinition(
                "fairy_yogurt_bowl",
                NpcVisitSystem.FermentationFairyId,
                "향기로운 요거트볼",
                "발효요정이 오늘의 향을 기록할 발효우유 요거트볼을 기다려요.",
                0,
                new NpcQuestCost(snackId: SnackCatalog.FermentedYogurtBowlId, snackQuantity: 1),
                new NpcQuestReward(0, 3, 1, 5)),
            new NpcRelationshipQuestDefinition(
                "fairy_patient_fermentation",
                NpcVisitSystem.FermentationFairyId,
                "천천히 익는 선물",
                "발효요정이 오래 숙성할 작은 선물 꾸러미를 준비하고 있어요.",
                FamiliarAffinityThreshold,
                new NpcQuestCost(milkDrops: 3, collectionFragments: 1),
                new NpcQuestReward(25, 0, 0, 7)),
            new NpcRelationshipQuestDefinition(
                "cat_cold_pudding",
                NpcVisitSystem.MilkCatId,
                "산뜻한 푸딩 찾기",
                "밀크냥이 함께 나눠 먹을 차가운 우유 푸딩을 찾고 있어요.",
                0,
                new NpcQuestCost(snackId: SnackCatalog.ColdMilkPuddingId, snackQuantity: 1),
                new NpcQuestReward(15, 0, 1, 5)),
            new NpcRelationshipQuestDefinition(
                "cat_shiny_trade",
                NpcVisitSystem.MilkCatId,
                "반짝이는 교환",
                "밀크냥이 지도 조각과 바꿀 반짝이는 물건을 모으고 있어요.",
                FamiliarAffinityThreshold,
                new NpcQuestCost(milkCoins: 25, collectionFragments: 1),
                new NpcQuestReward(0, 5, 0, 7))
        };

        public IReadOnlyList<NpcRelationshipQuestDefinition> All => Definitions;

        public NpcRelationshipQuestDefinition Find(string questId)
        {
            var normalized = Normalize(questId);
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (string.Equals(Definitions[index].Id, normalized, StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }

        public IReadOnlyList<NpcRelationshipQuestDefinition> GetEligibleQuests(
            string npcId,
            int affinity)
        {
            var normalizedNpc = Normalize(npcId);
            var safeAffinity = Math.Max(0, Math.Min(99, affinity));
            var result = new List<NpcRelationshipQuestDefinition>(2);
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var quest = Definitions[index];
                if (string.Equals(quest.NpcId, normalizedNpc, StringComparison.Ordinal)
                    && safeAffinity >= quest.MinimumAffinity)
                {
                    result.Add(quest);
                }
            }

            return result;
        }

        public NpcRelationshipSnapshot ObserveRelationship(
            NpcVisitSaveData relationships,
            string npcId)
        {
            var normalizedNpc = Normalize(npcId);
            if (!IsKnownNpc(normalizedNpc))
            {
                return new NpcRelationshipSnapshot(false, normalizedNpc, 0, 0, NpcRelationshipTier.NewFace);
            }

            relationships?.EnsureRuntimeDefaults();
            var entry = FindRelationship(relationships, normalizedNpc);
            var visits = entry?.visits ?? 0;
            var affinity = entry?.affinity ?? 0;
            return new NpcRelationshipSnapshot(
                true,
                normalizedNpc,
                visits,
                affinity,
                ResolveTier(affinity));
        }

        public static NpcRelationshipTier ResolveTier(int affinity)
        {
            var safeAffinity = Math.Max(0, Math.Min(99, affinity));
            if (safeAffinity >= TrustedFriendAffinityThreshold)
            {
                return NpcRelationshipTier.TrustedFriend;
            }

            if (safeAffinity >= FriendAffinityThreshold)
            {
                return NpcRelationshipTier.Friend;
            }

            return safeAffinity >= FamiliarAffinityThreshold
                ? NpcRelationshipTier.Familiar
                : NpcRelationshipTier.NewFace;
        }

        public NpcQuestActivationResult TryActivate(
            NpcRelationshipQuestSaveData state,
            NpcVisitSaveData relationships,
            string npcId,
            string questId,
            string offerId,
            DateTimeOffset now)
        {
            var normalizedOffer = Normalize(offerId);
            if (state == null)
            {
                return ActivationFailure(NpcQuestActivationStatus.MissingState, normalizedOffer);
            }

            if (relationships == null)
            {
                return ActivationFailure(NpcQuestActivationStatus.MissingRelationshipState, normalizedOffer);
            }

            NormalizeState(state);
            NormalizeRelationships(relationships);
            if (string.IsNullOrEmpty(normalizedOffer))
            {
                return ActivationFailure(NpcQuestActivationStatus.InvalidOfferId, normalizedOffer);
            }

            var normalizedNpc = Normalize(npcId);
            if (!IsKnownNpc(normalizedNpc))
            {
                return ActivationFailure(NpcQuestActivationStatus.UnknownNpc, normalizedOffer);
            }

            var quest = Find(questId);
            if (quest == null)
            {
                return ActivationFailure(NpcQuestActivationStatus.UnknownQuest, normalizedOffer);
            }

            if (!string.Equals(quest.NpcId, normalizedNpc, StringComparison.Ordinal))
            {
                return ActivationFailure(NpcQuestActivationStatus.QuestNpcMismatch, normalizedOffer, quest);
            }

            if (state.HasClaimedOffer(normalizedOffer))
            {
                return ActivationFailure(NpcQuestActivationStatus.AlreadyClaimedOffer, normalizedOffer, quest);
            }

            if (state.activeQuest.HasValue)
            {
                var activeWindow = ObserveActive(state, now);
                if (activeWindow.Status == NpcQuestWindowStatus.Expired
                    || activeWindow.Status == NpcQuestWindowStatus.UnknownQuest)
                {
                    state.activeQuest.Clear();
                }
                else
                {
                    return ActivationFailure(NpcQuestActivationStatus.AlreadyActive, normalizedOffer, quest);
                }
            }

            var relationship = ObserveRelationship(relationships, normalizedNpc);
            if (relationship.Affinity < quest.MinimumAffinity)
            {
                return ActivationFailure(NpcQuestActivationStatus.RelationshipLocked, normalizedOffer, quest);
            }

            var expiresAt = now.AddDays(ActiveDurationDays);
            var graceEndsAt = expiresAt.AddDays(GraceDurationDays);
            state.activeQuest.Set(
                normalizedOffer,
                normalizedNpc,
                quest.Id,
                now,
                expiresAt,
                graceEndsAt);
            return new NpcQuestActivationResult(
                NpcQuestActivationStatus.Activated,
                quest,
                normalizedOffer,
                expiresAt,
                graceEndsAt);
        }

        public NpcQuestWindowSnapshot ObserveActive(
            NpcRelationshipQuestSaveData state,
            DateTimeOffset now)
        {
            if (state == null)
            {
                return default;
            }

            NormalizeState(state);
            var active = state.activeQuest;
            if (active == null || !active.HasValue)
            {
                return default;
            }

            var quest = Find(active.questId);
            if (quest == null
                || !string.Equals(quest.NpcId, active.npcId, StringComparison.Ordinal))
            {
                return new NpcQuestWindowSnapshot(
                    NpcQuestWindowStatus.UnknownQuest,
                    null,
                    active.offerId,
                    default,
                    default);
            }

            if (!active.TryGetTimes(out var startedAt, out var expiresAt, out var graceEndsAt))
            {
                return new NpcQuestWindowSnapshot(
                    NpcQuestWindowStatus.UnknownQuest,
                    quest,
                    active.offerId,
                    default,
                    default);
            }

            if (active.terminalExpired || now > graceEndsAt)
            {
                active.terminalExpired = true;
                return new NpcQuestWindowSnapshot(
                    NpcQuestWindowStatus.Expired,
                    quest,
                    active.offerId,
                    expiresAt,
                    graceEndsAt);
            }

            var status = now < startedAt
                ? NpcQuestWindowStatus.ClockRollback
                : now <= expiresAt
                    ? NpcQuestWindowStatus.Active
                    : NpcQuestWindowStatus.Grace;
            return new NpcQuestWindowSnapshot(
                status,
                quest,
                active.offerId,
                expiresAt,
                graceEndsAt);
        }

        public NpcQuestDeliveryResult TryDeliver(
            NpcRelationshipQuestSaveData state,
            NpcVisitSaveData relationships,
            EconomySaveData economy,
            IList<SnackInventorySaveEntry> snackInventory,
            DateTimeOffset now,
            string claimReceiptId)
        {
            var normalizedReceipt = Normalize(claimReceiptId);
            if (state == null)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.MissingState, normalizedReceipt);
            }

            NormalizeState(state);
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.InvalidClaimReceipt, normalizedReceipt);
            }

            if (state.HasClaimReceipt(normalizedReceipt))
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.DuplicateClaim, normalizedReceipt);
            }

            if (relationships == null)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.MissingRelationshipState, normalizedReceipt);
            }

            if (economy == null)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.MissingEconomy, normalizedReceipt);
            }

            if (snackInventory == null)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.MissingInventory, normalizedReceipt);
            }

            NormalizeRelationships(relationships);
            var window = ObserveActive(state, now);
            if (window.Status == NpcQuestWindowStatus.None)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.NoActiveQuest, normalizedReceipt);
            }

            if (window.Status == NpcQuestWindowStatus.UnknownQuest)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.UnknownQuest, normalizedReceipt);
            }

            if (window.Status == NpcQuestWindowStatus.ClockRollback)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.ClockRollback, normalizedReceipt, window.Quest);
            }

            if (window.Status == NpcQuestWindowStatus.Expired)
            {
                return DeliveryFailure(NpcQuestDeliveryStatus.Expired, normalizedReceipt, window.Quest);
            }

            var quest = window.Quest;
            var cost = quest.Cost;
            var reward = quest.Reward;
            var snackEntry = FindSnack(snackInventory, cost.SnackId);
            var snackQuantity = Math.Max(0, snackEntry?.quantity ?? 0);
            if (economy.milkCoins < cost.MilkCoins
                || economy.milkDrops < cost.MilkDrops
                || economy.collectionFragments < cost.CollectionFragments
                || snackQuantity < cost.SnackQuantity)
            {
                return DeliveryFailure(
                    NpcQuestDeliveryStatus.InsufficientResources,
                    normalizedReceipt,
                    quest);
            }

            if (!TryCalculateBalance(economy.milkCoins, cost.MilkCoins, reward.MilkCoins, out var milkCoins)
                || !TryCalculateBalance(economy.milkDrops, cost.MilkDrops, reward.MilkDrops, out var milkDrops)
                || !TryCalculateBalance(
                    economy.collectionFragments,
                    cost.CollectionFragments,
                    reward.CollectionFragments,
                    out var fragments))
            {
                return DeliveryFailure(
                    NpcQuestDeliveryStatus.RewardCapacityFull,
                    normalizedReceipt,
                    quest);
            }

            var relationship = GetOrCreateRelationship(relationships, quest.NpcId);
            var affinityBefore = Math.Max(0, Math.Min(99, relationship.affinity));
            var affinityAfter = Math.Min(99, SaturatingAdd(affinityBefore, reward.Affinity));
            var tierBefore = ResolveTier(affinityBefore);
            var tierAfter = ResolveTier(affinityAfter);

            // All validation is complete. Apply every cost and reward as one mutation block.
            economy.milkCoins = milkCoins;
            economy.milkDrops = milkDrops;
            economy.collectionFragments = fragments;
            if (cost.SnackQuantity > 0)
            {
                snackEntry.quantity = snackQuantity - cost.SnackQuantity;
            }

            relationship.affinity = affinityAfter;
            relationship.storyStep = Math.Max(
                relationship.storyStep,
                (int)tierAfter >= (int)NpcRelationshipTier.TrustedFriend
                    ? 2
                    : (int)tierAfter >= (int)NpcRelationshipTier.Friend ? 1 : 0);
            state.claimReceipts.Add(new NpcRelationshipQuestClaimReceiptSaveData
            {
                claimReceiptId = normalizedReceipt,
                offerId = state.activeQuest.offerId,
                npcId = quest.NpcId,
                questId = quest.Id,
                claimedAtIso = now.ToString("O", CultureInfo.InvariantCulture)
            });
            while (state.claimReceipts.Count > NpcRelationshipQuestSaveData.MaximumClaimReceipts)
            {
                state.claimReceipts.RemoveAt(0);
            }

            state.activeQuest.Clear();
            return new NpcQuestDeliveryResult(
                NpcQuestDeliveryStatus.Applied,
                quest,
                normalizedReceipt,
                window.IsGrace,
                affinityBefore,
                affinityAfter,
                tierBefore,
                tierAfter);
        }

        public bool NormalizeState(NpcRelationshipQuestSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            var changed = state.EnsureRuntimeDefaults();
            if (state.activeQuest.HasValue)
            {
                var activeQuest = Find(state.activeQuest.questId);
                if (activeQuest == null
                    || !IsKnownNpc(state.activeQuest.npcId)
                    || !string.Equals(activeQuest.NpcId, state.activeQuest.npcId, StringComparison.Ordinal))
                {
                    state.activeQuest.Clear();
                    changed = true;
                }
            }

            for (var index = state.claimReceipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = state.claimReceipts[index];
                var quest = Find(receipt?.questId);
                if (receipt == null
                    || quest == null
                    || !string.Equals(quest.NpcId, receipt.npcId, StringComparison.Ordinal))
                {
                    state.claimReceipts.RemoveAt(index);
                    changed = true;
                }
            }

            return changed;
        }

        public bool NormalizeRelationships(NpcVisitSaveData relationships)
        {
            if (relationships == null)
            {
                return false;
            }

            var changed = relationships.EnsureRuntimeDefaults();
            for (var index = relationships.relationships.Count - 1; index >= 0; index -= 1)
            {
                var entry = relationships.relationships[index];
                if (entry == null || !IsKnownNpc(entry.npcId))
                {
                    relationships.relationships.RemoveAt(index);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool IsKnownNpc(string npcId)
        {
            return string.Equals(npcId, NpcVisitSystem.MilkyDoctorId, StringComparison.Ordinal)
                || string.Equals(npcId, NpcVisitSystem.FermentationFairyId, StringComparison.Ordinal)
                || string.Equals(npcId, NpcVisitSystem.MilkCatId, StringComparison.Ordinal);
        }

        private static NpcRelationshipSaveEntry FindRelationship(
            NpcVisitSaveData state,
            string npcId)
        {
            if (state?.relationships == null)
            {
                return null;
            }

            for (var index = 0; index < state.relationships.Count; index += 1)
            {
                var entry = state.relationships[index];
                if (entry != null && string.Equals(entry.npcId, npcId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static NpcRelationshipSaveEntry GetOrCreateRelationship(
            NpcVisitSaveData state,
            string npcId)
        {
            var existing = FindRelationship(state, npcId);
            if (existing != null)
            {
                return existing;
            }

            var created = new NpcRelationshipSaveEntry { npcId = npcId };
            state.relationships.Add(created);
            return created;
        }

        private static SnackInventorySaveEntry FindSnack(
            IList<SnackInventorySaveEntry> inventory,
            string snackId)
        {
            if (inventory == null || string.IsNullOrEmpty(snackId))
            {
                return null;
            }

            for (var index = 0; index < inventory.Count; index += 1)
            {
                var entry = inventory[index];
                if (entry != null && string.Equals(entry.snackId, snackId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool TryCalculateBalance(
            int current,
            int cost,
            int reward,
            out int result)
        {
            var calculated = (long)Math.Max(0, current) - Math.Max(0, cost) + Math.Max(0, reward);
            if (calculated < 0 || calculated > int.MaxValue)
            {
                result = current;
                return false;
            }

            result = (int)calculated;
            return true;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            var result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static NpcQuestActivationResult ActivationFailure(
            NpcQuestActivationStatus status,
            string offerId,
            NpcRelationshipQuestDefinition quest = null)
        {
            return new NpcQuestActivationResult(status, quest, offerId, default, default);
        }

        private static NpcQuestDeliveryResult DeliveryFailure(
            NpcQuestDeliveryStatus status,
            string claimReceiptId,
            NpcRelationshipQuestDefinition quest = null)
        {
            return new NpcQuestDeliveryResult(
                status,
                quest,
                claimReceiptId,
                false,
                0,
                0,
                NpcRelationshipTier.NewFace,
                NpcRelationshipTier.NewFace);
        }
    }
}
