using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Gameplay.MiniGames;

namespace CheeseTama.Save
{
    /// <summary>
    /// Bounded, forward-compatible persistence for the first time each mini-game medal is earned.
    /// Scores remain authoritative in PlayMiniGameSaveData; these receipts never grant currency.
    /// The root save owner may add this DTO as a nullable field and assign the State returned by
    /// MiniGameMasterySystem.Reconcile when loading a legacy save.
    /// </summary>
    [Serializable]
    public sealed class MiniGameMasterySaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumReceiptCount = 64;
        public const int MaximumSupportedReceiptCount = 12;
        public const int MaximumForwardCompatibleReceiptCount =
            MaximumReceiptCount - MaximumSupportedReceiptCount;
        public const int MaximumTokenLength = 100;

        public const string BronzeMedalId = "bronze";
        public const string SilverMedalId = "silver";
        public const string GoldMedalId = "gold";

        public int schemaVersion = CurrentSchemaVersion;
        public List<MiniGameMasteryReceiptSaveData> medalReceipts =
            new List<MiniGameMasteryReceiptSaveData>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            var source = medalReceipts;
            if (source == null)
            {
                medalReceipts = new List<MiniGameMasteryReceiptSaveData>();
                return true;
            }

            var normalized = new List<MiniGameMasteryReceiptSaveData>(
                Math.Min(source.Count, MaximumReceiptCount));
            var receiptIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            var forwardCompatibleCount = 0;
            for (var index = 0; index < source.Count; index += 1)
            {
                var receipt = source[index];
                if (receipt == null)
                {
                    changed = true;
                    continue;
                }

                changed |= receipt.EnsureRuntimeDefaults();
                if (!receipt.HasValue
                    || !IsStableToken(receipt.receiptId)
                    || !IsStableToken(receipt.gameId)
                    || !IsStableToken(receipt.medalId))
                {
                    changed = true;
                    continue;
                }

                var supported = IsSupportedCombination(receipt.gameId, receipt.medalId);
                var receiptIdBelongsToSupportedCombination =
                    IsSupportedReceiptId(receipt.receiptId);
                if ((supported
                        && !string.Equals(
                            receipt.receiptId,
                            BuildReceiptId(receipt.gameId, receipt.medalId),
                            StringComparison.Ordinal))
                    || (!supported && receiptIdBelongsToSupportedCombination))
                {
                    changed = true;
                    continue;
                }

                if (receiptIndexes.TryGetValue(receipt.receiptId, out var existingIndex))
                {
                    changed = true;
                    var existing = normalized[existingIndex];
                    var existingSupported = IsSupportedCombination(
                        existing.gameId,
                        existing.medalId);
                    if ((supported && !existingSupported)
                        || (supported == existingSupported
                            && IsEarlier(receipt.achievedAtIso, existing.achievedAtIso)))
                    {
                        if (!existingSupported)
                        {
                            forwardCompatibleCount = Math.Max(
                                0,
                                forwardCompatibleCount - 1);
                        }

                        normalized[existingIndex] = receipt;
                    }

                    continue;
                }

                if (!supported
                    && forwardCompatibleCount >= MaximumForwardCompatibleReceiptCount)
                {
                    changed = true;
                    continue;
                }

                receiptIndexes.Add(receipt.receiptId, normalized.Count);
                normalized.Add(receipt);
                if (!supported)
                {
                    forwardCompatibleCount += 1;
                }
            }

            if (normalized.Count != source.Count)
            {
                changed = true;
            }

            medalReceipts = normalized;
            return changed;
        }

        public bool HasMasteryReceipt(string gameId, string medalId)
        {
            return FindMasteryReceipt(gameId, medalId) != null;
        }

        public MiniGameMasteryReceiptSaveData FindMasteryReceipt(
            string gameId,
            string medalId)
        {
            var normalizedGameId = NormalizeToken(gameId);
            var normalizedMedalId = NormalizeToken(medalId);
            if (!IsSupportedCombination(normalizedGameId, normalizedMedalId)
                || medalReceipts == null)
            {
                return null;
            }

            var receiptId = BuildReceiptId(normalizedGameId, normalizedMedalId);
            for (var index = medalReceipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = medalReceipts[index];
                if (receipt != null
                    && string.Equals(receipt.receiptId, receiptId, StringComparison.Ordinal)
                    && string.Equals(
                        receipt.gameId,
                        normalizedGameId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        receipt.medalId,
                        normalizedMedalId,
                        StringComparison.Ordinal))
                {
                    return receipt;
                }
            }

            return null;
        }

        internal bool TryRecordMasteryReceipt(
            string gameId,
            string medalId,
            DateTimeOffset achievedAt,
            out MiniGameMasteryReceiptSaveData receipt)
        {
            receipt = null;
            EnsureRuntimeDefaults();
            var normalizedGameId = NormalizeToken(gameId);
            var normalizedMedalId = NormalizeToken(medalId);
            if (achievedAt == default
                || !IsSupportedCombination(normalizedGameId, normalizedMedalId)
                || HasMasteryReceipt(normalizedGameId, normalizedMedalId)
                || medalReceipts.Count >= MaximumReceiptCount)
            {
                return false;
            }

            receipt = new MiniGameMasteryReceiptSaveData
            {
                receiptId = BuildReceiptId(normalizedGameId, normalizedMedalId),
                gameId = normalizedGameId,
                medalId = normalizedMedalId,
                achievedAtIso = FormatTimestamp(achievedAt)
            };
            medalReceipts.Add(receipt);
            return true;
        }

        public static string BuildReceiptId(string gameId, string medalId)
        {
            return "mini_game_mastery:"
                + NormalizeToken(gameId)
                + ":"
                + NormalizeToken(medalId);
        }

        internal static string NormalizeToken(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= MaximumTokenLength
                ? normalized
                : normalized.Substring(0, MaximumTokenLength);
        }

        internal static bool IsStableToken(string value)
        {
            var normalized = NormalizeToken(value);
            if (string.IsNullOrEmpty(normalized)
                || !string.Equals(normalized, value, StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = 0; index < normalized.Length; index += 1)
            {
                var character = normalized[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= 'A' && character <= 'Z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-'
                    || character == '.'
                    || character == ':';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TryParseTimestamp(
            string value,
            out DateTimeOffset parsed)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed);
        }

        internal static string FormatTimestamp(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        internal static bool IsSupportedCombination(string gameId, string medalId)
        {
            return IsSupportedGameId(gameId) && IsSupportedMedalId(medalId);
        }

        internal static bool IsSupportedMedalId(string medalId)
        {
            return string.Equals(medalId, BronzeMedalId, StringComparison.Ordinal)
                || string.Equals(medalId, SilverMedalId, StringComparison.Ordinal)
                || string.Equals(medalId, GoldMedalId, StringComparison.Ordinal);
        }

        private static bool IsSupportedGameId(string gameId)
        {
            return string.Equals(gameId, MiniGameRecordIds.MilkDrop, StringComparison.Ordinal)
                || string.Equals(gameId, MiniGameRecordIds.Cleaning, StringComparison.Ordinal)
                || string.Equals(gameId, MiniGameRecordIds.BouncyJump, StringComparison.Ordinal)
                || string.Equals(gameId, MiniGameRecordIds.BlueBall, StringComparison.Ordinal);
        }

        private static bool IsSupportedReceiptId(string receiptId)
        {
            return IsSupportedReceiptIdForGame(receiptId, MiniGameRecordIds.MilkDrop)
                || IsSupportedReceiptIdForGame(receiptId, MiniGameRecordIds.Cleaning)
                || IsSupportedReceiptIdForGame(receiptId, MiniGameRecordIds.BouncyJump)
                || IsSupportedReceiptIdForGame(receiptId, MiniGameRecordIds.BlueBall);
        }

        private static bool IsSupportedReceiptIdForGame(string receiptId, string gameId)
        {
            return string.Equals(
                    receiptId,
                    BuildReceiptId(gameId, BronzeMedalId),
                    StringComparison.Ordinal)
                || string.Equals(
                    receiptId,
                    BuildReceiptId(gameId, SilverMedalId),
                    StringComparison.Ordinal)
                || string.Equals(
                    receiptId,
                    BuildReceiptId(gameId, GoldMedalId),
                    StringComparison.Ordinal);
        }

        private static bool IsEarlier(string candidateIso, string existingIso)
        {
            return TryParseTimestamp(candidateIso, out var candidate)
                && TryParseTimestamp(existingIso, out var existing)
                && candidate < existing;
        }
    }

    [Serializable]
    public sealed class MiniGameMasteryReceiptSaveData
    {
        public string receiptId = string.Empty;
        public string gameId = string.Empty;
        public string medalId = string.Empty;
        public string achievedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrEmpty(receiptId)
            && !string.IsNullOrEmpty(gameId)
            && !string.IsNullOrEmpty(medalId)
            && !string.IsNullOrEmpty(achievedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = Normalize(ref receiptId);
            changed |= Normalize(ref gameId);
            changed |= Normalize(ref medalId);

            var originalTimestamp = achievedAtIso;
            if (!MiniGameMasterySaveData.TryParseTimestamp(
                    achievedAtIso,
                    out var achievedAt))
            {
                achievedAtIso = string.Empty;
            }
            else
            {
                achievedAtIso = MiniGameMasterySaveData.FormatTimestamp(achievedAt);
            }

            return changed
                || !string.Equals(
                    originalTimestamp,
                    achievedAtIso,
                    StringComparison.Ordinal);
        }

        private static bool Normalize(ref string value)
        {
            var normalized = MiniGameMasterySaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
