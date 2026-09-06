using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Decorations
{
    public sealed class DecorationWorkshopVariantDefinition
    {
        public DecorationWorkshopVariantDefinition(
            string id,
            string displayName,
            DecorationSlot slot,
            int coinCost,
            int milkDropCost,
            int collectionFragmentCost,
            string materialKey,
            string colorKey,
            string tintHex)
        {
            Id = Normalize(id);
            DisplayName = Normalize(displayName);
            Slot = slot;
            CoinCost = Math.Max(0, coinCost);
            MilkDropCost = Math.Max(0, milkDropCost);
            CollectionFragmentCost = Math.Max(0, collectionFragmentCost);
            MaterialKey = Normalize(materialKey);
            ColorKey = Normalize(colorKey);
            TintHex = Normalize(tintHex).TrimStart('#').ToUpperInvariant();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public DecorationSlot Slot { get; }
        public int CoinCost { get; }
        public int MilkDropCost { get; }
        public int CollectionFragmentCost { get; }
        public string MaterialKey { get; }
        public string ColorKey { get; }
        public string TintHex { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class DecorationWorkshopCatalog
    {
        public const string WallVanillaGlazeId = "workshop_wall_vanilla_glaze";
        public const string WallBerryMilkId = "workshop_wall_berry_milk";
        public const string FloorHoneyCheckerId = "workshop_floor_honey_checker";
        public const string FloorMintFoamId = "workshop_floor_mint_foam";
        public const string AccentGoldLacquerId = "workshop_accent_gold_lacquer";
        public const string AccentStarryBlueId = "workshop_accent_starry_blue";
        public const string WindowSunriseSheerId = "workshop_window_sunrise_sheer";
        public const string WindowMidnightVelvetId = "workshop_window_midnight_velvet";
        public const string ShelfWarmMapleId = "workshop_shelf_warm_maple";
        public const string ShelfCoolMilkId = "workshop_shelf_cool_milk";
        public const string BedsidePeachFabricId = "workshop_bedside_peach_fabric";
        public const string BedsideNightFabricId = "workshop_bedside_night_fabric";

        private static readonly DecorationWorkshopVariantDefinition[] Definitions =
        {
            new DecorationWorkshopVariantDefinition(
                WallVanillaGlazeId,
                "바닐라 글레이즈",
                DecorationSlot.Wall,
                45,
                2,
                1,
                "decor_wall_matte",
                "vanilla_cream",
                "F3D69A"),
            new DecorationWorkshopVariantDefinition(
                WallBerryMilkId,
                "베리 밀크",
                DecorationSlot.Wall,
                70,
                3,
                2,
                "decor_wall_matte",
                "berry_milk",
                "E9A8B8"),
            new DecorationWorkshopVariantDefinition(
                FloorHoneyCheckerId,
                "허니 체크",
                DecorationSlot.Floor,
                50,
                2,
                1,
                "decor_floor_checker",
                "honey_gold",
                "D9A441"),
            new DecorationWorkshopVariantDefinition(
                FloorMintFoamId,
                "민트 폼",
                DecorationSlot.Floor,
                75,
                4,
                2,
                "decor_floor_soft",
                "mint_foam",
                "A9D8C7"),
            new DecorationWorkshopVariantDefinition(
                AccentGoldLacquerId,
                "골드 래커",
                DecorationSlot.Accent,
                60,
                3,
                1,
                "decor_accent_lacquer",
                "warm_gold",
                "E3B54B"),
            new DecorationWorkshopVariantDefinition(
                AccentStarryBlueId,
                "별밤 블루",
                DecorationSlot.Accent,
                90,
                5,
                2,
                "decor_accent_lacquer",
                "starry_blue",
                "6679B8"),
            new DecorationWorkshopVariantDefinition(
                WindowSunriseSheerId,
                "햇살 시어",
                DecorationSlot.Window,
                55,
                2,
                1,
                "decor_window_sheer",
                "sunrise_peach",
                "F1B58B"),
            new DecorationWorkshopVariantDefinition(
                WindowMidnightVelvetId,
                "한밤 벨벳",
                DecorationSlot.Window,
                85,
                4,
                2,
                "decor_window_velvet",
                "midnight_indigo",
                "4D4E85"),
            new DecorationWorkshopVariantDefinition(
                ShelfWarmMapleId,
                "따뜻한 단풍나무",
                DecorationSlot.Shelf,
                50,
                3,
                1,
                "decor_shelf_wood",
                "warm_maple",
                "B87743"),
            new DecorationWorkshopVariantDefinition(
                ShelfCoolMilkId,
                "차분한 밀크",
                DecorationSlot.Shelf,
                80,
                4,
                2,
                "decor_shelf_painted",
                "cool_milk",
                "D9E2E8"),
            new DecorationWorkshopVariantDefinition(
                BedsidePeachFabricId,
                "복숭아 패브릭",
                DecorationSlot.Bedside,
                55,
                3,
                1,
                "decor_bedside_fabric",
                "peach_fabric",
                "EAB09E"),
            new DecorationWorkshopVariantDefinition(
                BedsideNightFabricId,
                "밤하늘 패브릭",
                DecorationSlot.Bedside,
                85,
                5,
                2,
                "decor_bedside_fabric",
                "night_fabric",
                "59638F")
        };

        public static IReadOnlyList<DecorationWorkshopVariantDefinition> All => Definitions;

        public static DecorationWorkshopVariantDefinition Find(string variantId)
        {
            var normalizedId = Normalize(variantId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                return null;
            }

            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (string.Equals(
                        Definitions[index].Id,
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }

        public static IReadOnlyList<DecorationWorkshopVariantDefinition> GetForSlot(
            DecorationSlot slot)
        {
            var matches = new List<DecorationWorkshopVariantDefinition>(2);
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (Definitions[index].Slot == slot)
                {
                    matches.Add(Definitions[index]);
                }
            }

            return matches;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct DecorationWorkshopWalletSnapshot
    {
        public DecorationWorkshopWalletSnapshot(
            int coins,
            int milkDrops,
            int collectionFragments)
        {
            Coins = Math.Max(0, coins);
            MilkDrops = Math.Max(0, milkDrops);
            CollectionFragments = Math.Max(0, collectionFragments);
        }

        public int Coins { get; }
        public int MilkDrops { get; }
        public int CollectionFragments { get; }

        internal DecorationWorkshopWalletSnapshot Spend(
            DecorationWorkshopVariantDefinition definition)
        {
            if (definition == null)
            {
                return this;
            }

            return new DecorationWorkshopWalletSnapshot(
                Coins - definition.CoinCost,
                MilkDrops - definition.MilkDropCost,
                CollectionFragments - definition.CollectionFragmentCost);
        }
    }

    public enum DecorationWorkshopQuoteStatus
    {
        Available = 0,
        MissingState = 1,
        UnknownVariant = 2,
        AlreadyOwned = 3
    }

    public sealed class DecorationWorkshopQuote
    {
        public DecorationWorkshopQuote(
            DecorationWorkshopQuoteStatus status,
            DecorationWorkshopVariantDefinition definition,
            DecorationWorkshopWalletSnapshot wallet)
        {
            Status = status;
            Definition = definition;
            Wallet = wallet;
            MissingCoins = Math.Max(0, (definition?.CoinCost ?? 0) - wallet.Coins);
            MissingMilkDrops = Math.Max(
                0,
                (definition?.MilkDropCost ?? 0) - wallet.MilkDrops);
            MissingCollectionFragments = Math.Max(
                0,
                (definition?.CollectionFragmentCost ?? 0) - wallet.CollectionFragments);
        }

        public DecorationWorkshopQuoteStatus Status { get; }
        public DecorationWorkshopVariantDefinition Definition { get; }
        public DecorationWorkshopWalletSnapshot Wallet { get; }
        public int MissingCoins { get; }
        public int MissingMilkDrops { get; }
        public int MissingCollectionFragments { get; }
        public bool CanCraft => Status == DecorationWorkshopQuoteStatus.Available
            && MissingCoins == 0
            && MissingMilkDrops == 0
            && MissingCollectionFragments == 0;
    }

    public enum DecorationWorkshopCraftStatus
    {
        Applied = 0,
        MissingState = 1,
        InvalidReceipt = 2,
        AlreadyApplied = 3,
        UnknownVariant = 4,
        AlreadyOwned = 5,
        InsufficientCurrency = 6,
        TrackingCapacityFull = 7
    }

    public sealed class DecorationWorkshopCraftResult
    {
        public DecorationWorkshopCraftResult(
            DecorationWorkshopCraftStatus status,
            string receiptKey,
            DecorationWorkshopVariantDefinition definition,
            DecorationWorkshopWalletSnapshot walletBefore,
            DecorationWorkshopWalletSnapshot walletAfter)
        {
            Status = status;
            ReceiptKey = Normalize(receiptKey);
            Definition = definition;
            WalletBefore = walletBefore;
            WalletAfter = status == DecorationWorkshopCraftStatus.Applied
                ? walletAfter
                : walletBefore;
        }

        public DecorationWorkshopCraftStatus Status { get; }
        public string ReceiptKey { get; }
        public DecorationWorkshopVariantDefinition Definition { get; }
        public DecorationWorkshopWalletSnapshot WalletBefore { get; }
        public DecorationWorkshopWalletSnapshot WalletAfter { get; }
        public bool Applied => Status == DecorationWorkshopCraftStatus.Applied;
        public bool DuplicateReceipt => Status == DecorationWorkshopCraftStatus.AlreadyApplied;
        public int SpentCoins => Applied ? Definition?.CoinCost ?? 0 : 0;
        public int SpentMilkDrops => Applied ? Definition?.MilkDropCost ?? 0 : 0;
        public int SpentCollectionFragments =>
            Applied ? Definition?.CollectionFragmentCost ?? 0 : 0;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum DecorationWorkshopSelectionStatus
    {
        Applied = 0,
        ResetToBase = 1,
        MissingState = 2,
        InvalidSlot = 3,
        UnknownVariant = 4,
        SlotMismatch = 5,
        NotOwned = 6,
        AlreadySelected = 7
    }

    public readonly struct DecorationWorkshopSelectionResult
    {
        public DecorationWorkshopSelectionResult(
            DecorationWorkshopSelectionStatus status,
            DecorationSlot slot,
            string variantId)
        {
            Status = status;
            Slot = slot;
            VariantId = string.IsNullOrWhiteSpace(variantId)
                ? string.Empty
                : variantId.Trim();
        }

        public DecorationWorkshopSelectionStatus Status { get; }
        public DecorationSlot Slot { get; }
        public string VariantId { get; }
        public bool Changed => Status == DecorationWorkshopSelectionStatus.Applied
            || Status == DecorationWorkshopSelectionStatus.ResetToBase;
    }

    public sealed class DecorationWorkshopRenderEntry
    {
        public DecorationWorkshopRenderEntry(
            DecorationSlot slot,
            DecorationWorkshopVariantDefinition definition)
        {
            Slot = slot;
            VariantId = definition?.Id ?? string.Empty;
            MaterialKey = definition?.MaterialKey ?? string.Empty;
            ColorKey = definition?.ColorKey ?? string.Empty;
            TintHex = definition?.TintHex ?? string.Empty;
        }

        public DecorationSlot Slot { get; }
        public string VariantId { get; }
        public string MaterialKey { get; }
        public string ColorKey { get; }
        public string TintHex { get; }
        public bool HasVariant => !string.IsNullOrEmpty(VariantId);
    }

    public sealed class DecorationWorkshopRenderSnapshot
    {
        private readonly DecorationWorkshopRenderEntry[] entries;

        public DecorationWorkshopRenderSnapshot(
            IEnumerable<DecorationWorkshopRenderEntry> entries)
        {
            var normalized = new List<DecorationWorkshopRenderEntry>();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry != null)
                    {
                        normalized.Add(entry);
                    }
                }
            }

            this.entries = normalized.ToArray();
        }

        public IReadOnlyList<DecorationWorkshopRenderEntry> Entries => entries;

        public DecorationWorkshopRenderEntry Find(DecorationSlot slot)
        {
            for (var index = 0; index < entries.Length; index += 1)
            {
                if (entries[index].Slot == slot)
                {
                    return entries[index];
                }
            }

            return null;
        }
    }

    public sealed class DecorationWorkshopSystem
    {
        public DecorationWorkshopQuote BuildQuote(
            DecorationWorkshopSaveData state,
            DecorationWorkshopWalletSnapshot wallet,
            string variantId)
        {
            var definition = DecorationWorkshopCatalog.Find(variantId);
            if (state == null)
            {
                return new DecorationWorkshopQuote(
                    DecorationWorkshopQuoteStatus.MissingState,
                    definition,
                    wallet);
            }

            NormalizeState(state);
            if (definition == null)
            {
                return new DecorationWorkshopQuote(
                    DecorationWorkshopQuoteStatus.UnknownVariant,
                    null,
                    wallet);
            }

            return new DecorationWorkshopQuote(
                state.Owns(definition.Id)
                    ? DecorationWorkshopQuoteStatus.AlreadyOwned
                    : DecorationWorkshopQuoteStatus.Available,
                definition,
                wallet);
        }

        public DecorationWorkshopCraftResult TryCraft(
            DecorationWorkshopSaveData state,
            DecorationWorkshopWalletSnapshot wallet,
            string variantId,
            string receiptKey)
        {
            var normalizedReceipt = Normalize(receiptKey);
            if (state == null)
            {
                return CraftFailure(
                    DecorationWorkshopCraftStatus.MissingState,
                    normalizedReceipt,
                    null,
                    wallet);
            }

            NormalizeState(state);
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return CraftFailure(
                    DecorationWorkshopCraftStatus.InvalidReceipt,
                    string.Empty,
                    null,
                    wallet);
            }

            if (state.HasAppliedCraftReceipt(normalizedReceipt))
            {
                return CraftFailure(
                    DecorationWorkshopCraftStatus.AlreadyApplied,
                    normalizedReceipt,
                    DecorationWorkshopCatalog.Find(variantId),
                    wallet);
            }

            var definition = DecorationWorkshopCatalog.Find(variantId);
            if (definition == null)
            {
                return CraftFailure(
                    DecorationWorkshopCraftStatus.UnknownVariant,
                    normalizedReceipt,
                    null,
                    wallet);
            }

            if (state.Owns(definition.Id))
            {
                return CraftFailure(
                    DecorationWorkshopCraftStatus.AlreadyOwned,
                    normalizedReceipt,
                    definition,
                    wallet);
            }

            var quote = BuildQuote(state, wallet, definition.Id);
            if (!quote.CanCraft)
            {
                return CraftFailure(
                    DecorationWorkshopCraftStatus.InsufficientCurrency,
                    normalizedReceipt,
                    definition,
                    wallet);
            }

            if (!state.CanAddOwnedVariant(definition.Id)
                || !state.CanAddCraftReceipt(normalizedReceipt))
            {
                return CraftFailure(
                    DecorationWorkshopCraftStatus.TrackingCapacityFull,
                    normalizedReceipt,
                    definition,
                    wallet);
            }

            state.AddOwnedVariant(definition.Id);
            state.AddCraftReceipt(normalizedReceipt);
            return new DecorationWorkshopCraftResult(
                DecorationWorkshopCraftStatus.Applied,
                normalizedReceipt,
                definition,
                wallet,
                wallet.Spend(definition));
        }

        public DecorationWorkshopSelectionResult TrySelect(
            DecorationWorkshopSaveData state,
            DecorationSlot slot,
            string variantId)
        {
            if (state == null)
            {
                return new DecorationWorkshopSelectionResult(
                    DecorationWorkshopSelectionStatus.MissingState,
                    slot,
                    string.Empty);
            }

            if (!IsKnownSlot(slot))
            {
                return new DecorationWorkshopSelectionResult(
                    DecorationWorkshopSelectionStatus.InvalidSlot,
                    slot,
                    string.Empty);
            }

            NormalizeState(state);
            var normalizedVariantId = Normalize(variantId);
            if (string.IsNullOrEmpty(normalizedVariantId))
            {
                var reset = state.SetSelectedVariant((int)slot, string.Empty);
                return new DecorationWorkshopSelectionResult(
                    reset
                        ? DecorationWorkshopSelectionStatus.ResetToBase
                        : DecorationWorkshopSelectionStatus.AlreadySelected,
                    slot,
                    string.Empty);
            }

            var definition = DecorationWorkshopCatalog.Find(normalizedVariantId);
            if (definition == null)
            {
                return new DecorationWorkshopSelectionResult(
                    DecorationWorkshopSelectionStatus.UnknownVariant,
                    slot,
                    string.Empty);
            }

            if (definition.Slot != slot)
            {
                return new DecorationWorkshopSelectionResult(
                    DecorationWorkshopSelectionStatus.SlotMismatch,
                    slot,
                    definition.Id);
            }

            if (!state.Owns(definition.Id))
            {
                return new DecorationWorkshopSelectionResult(
                    DecorationWorkshopSelectionStatus.NotOwned,
                    slot,
                    definition.Id);
            }

            if (string.Equals(
                    state.GetSelectedVariantId((int)slot),
                    definition.Id,
                    StringComparison.Ordinal))
            {
                return new DecorationWorkshopSelectionResult(
                    DecorationWorkshopSelectionStatus.AlreadySelected,
                    slot,
                    definition.Id);
            }

            state.SetSelectedVariant((int)slot, definition.Id);
            return new DecorationWorkshopSelectionResult(
                DecorationWorkshopSelectionStatus.Applied,
                slot,
                definition.Id);
        }

        public DecorationWorkshopRenderSnapshot BuildRenderSnapshot(
            DecorationWorkshopSaveData state)
        {
            NormalizeState(state);
            var entries = new List<DecorationWorkshopRenderEntry>(6);
            foreach (DecorationSlot slot in Enum.GetValues(typeof(DecorationSlot)))
            {
                var selectedVariantId = state?.GetSelectedVariantId((int)slot);
                var definition = DecorationWorkshopCatalog.Find(selectedVariantId);
                entries.Add(new DecorationWorkshopRenderEntry(
                    slot,
                    definition != null && definition.Slot == slot
                        ? definition
                        : null));
            }

            return new DecorationWorkshopRenderSnapshot(entries);
        }

        public bool NormalizeState(DecorationWorkshopSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            var changed = state.EnsureRuntimeDefaults();
            var validSelections = new List<DecorationWorkshopSelectionSaveEntry>(6);
            for (var index = 0; index < state.selectedVariants.Count; index += 1)
            {
                var entry = state.selectedVariants[index];
                var definition = DecorationWorkshopCatalog.Find(entry?.variantId);
                if (entry == null
                    || definition == null
                    || (int)definition.Slot != entry.slot
                    || !state.Owns(definition.Id))
                {
                    changed = true;
                    continue;
                }

                validSelections.Add(new DecorationWorkshopSelectionSaveEntry
                {
                    slot = entry.slot,
                    variantId = definition.Id
                });
            }

            changed |= state.ReplaceSelections(validSelections);
            return changed;
        }

        public static string FormatCost(DecorationWorkshopVariantDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            return $"코인 {definition.CoinCost} · 우유방울 {definition.MilkDropCost} · 도감조각 {definition.CollectionFragmentCost}";
        }

        private static DecorationWorkshopCraftResult CraftFailure(
            DecorationWorkshopCraftStatus status,
            string receiptKey,
            DecorationWorkshopVariantDefinition definition,
            DecorationWorkshopWalletSnapshot wallet)
        {
            return new DecorationWorkshopCraftResult(
                status,
                receiptKey,
                definition,
                wallet,
                wallet);
        }

        private static bool IsKnownSlot(DecorationSlot slot)
        {
            return slot >= DecorationSlot.Wall && slot <= DecorationSlot.Bedside;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
