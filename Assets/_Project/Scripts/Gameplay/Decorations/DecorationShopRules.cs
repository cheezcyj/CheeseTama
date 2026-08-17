using System;
using System.Collections.Generic;

namespace CheeseTama.Gameplay.Decorations
{
    public enum DecorationTransactionStatus
    {
        Success = 0,
        ItemNotFound = 1,
        AlreadyOwned = 2,
        InsufficientCurrency = 3,
        NotOwned = 4,
        AlreadyEquipped = 5
    }

    public sealed class DecorationShopSnapshot
    {
        private readonly string[] ownedItemIds;

        public DecorationShopSnapshot(
            int milkCoins,
            int milkDrops,
            IEnumerable<string> ownedIds,
            string equippedWallId,
            string equippedFloorId,
            string equippedAccentId)
            : this(milkCoins, milkDrops, ownedIds, equippedWallId, equippedFloorId, equippedAccentId, null, null, null)
        {
        }

        public DecorationShopSnapshot(
            int milkCoins,
            int milkDrops,
            IEnumerable<string> ownedIds,
            string equippedWallId,
            string equippedFloorId,
            string equippedAccentId,
            string equippedWindowId,
            string equippedShelfId,
            string equippedBedsideId)
        {
            this.milkCoins = Math.Max(0, milkCoins);
            this.milkDrops = Math.Max(0, milkDrops);
            ownedItemIds = NormalizeOwnedIds(ownedIds);
            this.equippedWallId = ResolveEquipped(
                DecorationSlot.Wall,
                equippedWallId,
                ownedItemIds);
            this.equippedFloorId = ResolveEquipped(
                DecorationSlot.Floor,
                equippedFloorId,
                ownedItemIds);
            this.equippedAccentId = ResolveEquipped(
                DecorationSlot.Accent,
                equippedAccentId,
                ownedItemIds);
            this.equippedWindowId = ResolveEquipped(DecorationSlot.Window, equippedWindowId, ownedItemIds);
            this.equippedShelfId = ResolveEquipped(DecorationSlot.Shelf, equippedShelfId, ownedItemIds);
            this.equippedBedsideId = ResolveEquipped(DecorationSlot.Bedside, equippedBedsideId, ownedItemIds);
        }

        public int milkCoins { get; }
        public int milkDrops { get; }
        public IReadOnlyList<string> OwnedItemIds => ownedItemIds;
        public string equippedWallId { get; }
        public string equippedFloorId { get; }
        public string equippedAccentId { get; }
        public string equippedWindowId { get; }
        public string equippedShelfId { get; }
        public string equippedBedsideId { get; }

        public static DecorationShopSnapshot CreateDefault(int milkCoins = 0, int milkDrops = 0)
        {
            return new DecorationShopSnapshot(
                milkCoins,
                milkDrops,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        public bool Owns(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            return Array.IndexOf(ownedItemIds, itemId) >= 0;
        }

        public string GetEquippedId(DecorationSlot slot)
        {
            return slot switch
            {
                DecorationSlot.Floor => equippedFloorId,
                DecorationSlot.Accent => equippedAccentId,
                DecorationSlot.Window => equippedWindowId,
                DecorationSlot.Shelf => equippedShelfId,
                DecorationSlot.Bedside => equippedBedsideId,
                _ => equippedWallId
            };
        }

        internal DecorationShopSnapshot WithPurchase(DecorationDefinition item)
        {
            var nextOwnedIds = new string[ownedItemIds.Length + 1];
            Array.Copy(ownedItemIds, nextOwnedIds, ownedItemIds.Length);
            nextOwnedIds[nextOwnedIds.Length - 1] = item.id;
            return new DecorationShopSnapshot(
                milkCoins - item.milkCoinCost,
                milkDrops - item.milkDropCost,
                nextOwnedIds,
                equippedWallId,
                equippedFloorId,
                equippedAccentId,
                equippedWindowId,
                equippedShelfId,
                equippedBedsideId);
        }

        internal DecorationShopSnapshot WithEquipped(DecorationDefinition item)
        {
            return new DecorationShopSnapshot(
                milkCoins,
                milkDrops,
                ownedItemIds,
                item.slot == DecorationSlot.Wall ? item.id : equippedWallId,
                item.slot == DecorationSlot.Floor ? item.id : equippedFloorId,
                item.slot == DecorationSlot.Accent ? item.id : equippedAccentId,
                item.slot == DecorationSlot.Window ? item.id : equippedWindowId,
                item.slot == DecorationSlot.Shelf ? item.id : equippedShelfId,
                item.slot == DecorationSlot.Bedside ? item.id : equippedBedsideId);
        }

        private static string[] NormalizeOwnedIds(IEnumerable<string> ownedIds)
        {
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (ownedIds != null)
            {
                foreach (var itemId in ownedIds)
                {
                    if (!string.IsNullOrWhiteSpace(itemId) && seen.Add(itemId))
                    {
                        // Unknown IDs are deliberately retained for forward-compatible saves.
                        normalized.Add(itemId);
                    }
                }
            }

            foreach (var item in DecorationCatalog.All)
            {
                if (item != null && item.defaultOwned && seen.Add(item.id))
                {
                    normalized.Add(item.id);
                }
            }

            return normalized.ToArray();
        }

        private static string ResolveEquipped(
            DecorationSlot slot,
            string requestedItemId,
            IReadOnlyCollection<string> ownedIds)
        {
            var requested = DecorationCatalog.Find(requestedItemId);
            if (requested != null
                && requested.slot == slot
                && Contains(ownedIds, requested.id))
            {
                return requested.id;
            }

            return DecorationCatalog.GetDefault(slot)?.id ?? string.Empty;
        }

        private static bool Contains(IReadOnlyCollection<string> itemIds, string itemId)
        {
            if (itemIds == null)
            {
                return false;
            }

            foreach (var value in itemIds)
            {
                if (value == itemId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class DecorationTransactionResult
    {
        public DecorationTransactionResult(
            DecorationTransactionStatus status,
            DecorationDefinition item,
            DecorationShopSnapshot snapshot,
            string message)
        {
            this.status = status;
            this.item = item;
            this.snapshot = snapshot ?? DecorationShopSnapshot.CreateDefault();
            this.message = message ?? string.Empty;
        }

        public DecorationTransactionStatus status { get; }
        public DecorationDefinition item { get; }
        public DecorationShopSnapshot snapshot { get; }
        public string message { get; }
        public bool Succeeded => status == DecorationTransactionStatus.Success;
    }

    public static class DecorationShopRules
    {
        public static bool CanPurchase(DecorationDefinition item, DecorationShopSnapshot snapshot)
        {
            return item != null
                && snapshot != null
                && !snapshot.Owns(item.id)
                && snapshot.milkCoins >= item.milkCoinCost
                && snapshot.milkDrops >= item.milkDropCost;
        }

        public static bool CanEquip(DecorationDefinition item, DecorationShopSnapshot snapshot)
        {
            return item != null
                && snapshot != null
                && snapshot.Owns(item.id)
                && snapshot.GetEquippedId(item.slot) != item.id;
        }

        public static DecorationTransactionResult Purchase(
            string itemId,
            DecorationShopSnapshot snapshot)
        {
            snapshot ??= DecorationShopSnapshot.CreateDefault();
            var item = DecorationCatalog.Find(itemId);
            if (item == null)
            {
                return Result(
                    DecorationTransactionStatus.ItemNotFound,
                    null,
                    snapshot,
                    "장식 정보를 찾을 수 없어요.");
            }

            if (snapshot.Owns(item.id))
            {
                return Result(
                    DecorationTransactionStatus.AlreadyOwned,
                    item,
                    snapshot,
                    $"{item.displayName}은(는) 이미 가지고 있어요.");
            }

            if (!CanPurchase(item, snapshot))
            {
                return Result(
                    DecorationTransactionStatus.InsufficientCurrency,
                    item,
                    snapshot,
                    "재화가 부족해요.");
            }

            return Result(
                DecorationTransactionStatus.Success,
                item,
                snapshot.WithPurchase(item),
                $"{item.displayName}을(를) 구매했어요.");
        }

        public static DecorationTransactionResult Equip(
            string itemId,
            DecorationShopSnapshot snapshot)
        {
            snapshot ??= DecorationShopSnapshot.CreateDefault();
            var item = DecorationCatalog.Find(itemId);
            if (item == null)
            {
                return Result(
                    DecorationTransactionStatus.ItemNotFound,
                    null,
                    snapshot,
                    "장식 정보를 찾을 수 없어요.");
            }

            if (!snapshot.Owns(item.id))
            {
                return Result(
                    DecorationTransactionStatus.NotOwned,
                    item,
                    snapshot,
                    "먼저 장식을 구매해 주세요.");
            }

            if (!CanEquip(item, snapshot))
            {
                return Result(
                    DecorationTransactionStatus.AlreadyEquipped,
                    item,
                    snapshot,
                    $"{item.displayName}은(는) 이미 장착 중이에요.");
            }

            return Result(
                DecorationTransactionStatus.Success,
                item,
                snapshot.WithEquipped(item),
                $"{item.displayName}을(를) {GetSlotName(item.slot)}에 장착했어요.");
        }

        public static string FormatPrice(DecorationDefinition item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            if (item.IsFree)
            {
                return "기본 제공";
            }

            if (item.milkDropCost <= 0)
            {
                return $"코인 {item.milkCoinCost}";
            }

            if (item.milkCoinCost <= 0)
            {
                return $"우유방울 {item.milkDropCost}";
            }

            return $"코인 {item.milkCoinCost} · 우유방울 {item.milkDropCost}";
        }

        public static string GetSlotName(DecorationSlot slot)
        {
            return slot switch
            {
                DecorationSlot.Floor => "바닥",
                DecorationSlot.Accent => "포인트 장식",
                DecorationSlot.Window => "창가",
                DecorationSlot.Shelf => "선반",
                DecorationSlot.Bedside => "침대 곁",
                _ => "벽"
            };
        }

        private static DecorationTransactionResult Result(
            DecorationTransactionStatus status,
            DecorationDefinition item,
            DecorationShopSnapshot snapshot,
            string message)
        {
            return new DecorationTransactionResult(status, item, snapshot, message);
        }
    }
}
