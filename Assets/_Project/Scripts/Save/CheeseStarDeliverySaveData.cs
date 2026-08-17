using System;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class CheeseStarDeliverySaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string latestObservedDateKey = string.Empty;
        public string lastClaimedDateKey = string.Empty;
        public string lastClaimedAtIso = string.Empty;
        public int currentStreakDays;
        public int totalClaims;

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            changed |= NormalizeDateKey(ref latestObservedDateKey);
            changed |= NormalizeDateKey(ref lastClaimedDateKey);
            changed |= NormalizeTimestamp(ref lastClaimedAtIso);

            var normalizedStreak = Math.Max(0, currentStreakDays);
            if (normalizedStreak != currentStreakDays)
            {
                currentStreakDays = normalizedStreak;
                changed = true;
            }

            var normalizedTotalClaims = Math.Max(0, totalClaims);
            if (normalizedTotalClaims != totalClaims)
            {
                totalClaims = normalizedTotalClaims;
                changed = true;
            }

            if (string.IsNullOrEmpty(lastClaimedDateKey))
            {
                if (currentStreakDays != 0)
                {
                    currentStreakDays = 0;
                    changed = true;
                }

                if (!string.IsNullOrEmpty(lastClaimedAtIso))
                {
                    lastClaimedAtIso = string.Empty;
                    changed = true;
                }
            }
            else
            {
                if (currentStreakDays == 0)
                {
                    currentStreakDays = 1;
                    changed = true;
                }

                if (totalClaims == 0)
                {
                    totalClaims = 1;
                    changed = true;
                }

                if (TryParseDateKey(lastClaimedDateKey, out var claimedDate)
                    && (!TryParseDateKey(latestObservedDateKey, out var observedDate)
                        || observedDate < claimedDate))
                {
                    latestObservedDateKey = lastClaimedDateKey;
                    changed = true;
                }
            }

            if (totalClaims < currentStreakDays)
            {
                totalClaims = currentStreakDays;
                changed = true;
            }

            return changed;
        }

        private static bool NormalizeDateKey(ref string value)
        {
            if (!TryParseDateKey(value, out var parsed))
            {
                if (value == null)
                {
                    value = string.Empty;
                    return true;
                }

                if (value.Length == 0)
                {
                    return false;
                }

                value = string.Empty;
                return true;
            }

            var canonical = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (string.Equals(value, canonical, StringComparison.Ordinal))
            {
                return false;
            }

            value = canonical;
            return true;
        }

        private static bool NormalizeTimestamp(ref string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (value == string.Empty)
                {
                    return false;
                }

                value = string.Empty;
                return true;
            }

            if (!DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                value = string.Empty;
                return true;
            }

            var canonical = parsed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            if (string.Equals(value, canonical, StringComparison.Ordinal))
            {
                return false;
            }

            value = canonical;
            return true;
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
