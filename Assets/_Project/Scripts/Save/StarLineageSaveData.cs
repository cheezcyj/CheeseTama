using System;
using System.Collections.Generic;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class StarLineageSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumRecords = 16;
        public const int MaximumReceiptIds = 64;

        public int schemaVersion = CurrentSchemaVersion;
        public List<StarGenerationRecordSaveData> records =
            new List<StarGenerationRecordSaveData>();
        public int pendingTraitGenerationNumber;
        public string activeTraitId = string.Empty;
        public int activeTraitGenerationNumber;
        public List<string> appliedReceiptIds = new List<string>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            if (records == null)
            {
                records = new List<StarGenerationRecordSaveData>();
                changed = true;
            }

            if (appliedReceiptIds == null)
            {
                appliedReceiptIds = new List<string>();
                changed = true;
            }

            changed |= NormalizeRecords();
            changed |= NormalizeIds(appliedReceiptIds, MaximumReceiptIds);
            changed |= Normalize(ref activeTraitId);

            if (pendingTraitGenerationNumber < 0)
            {
                pendingTraitGenerationNumber = 0;
                changed = true;
            }

            if (activeTraitGenerationNumber < 0)
            {
                activeTraitGenerationNumber = 0;
                changed = true;
            }

            if (string.IsNullOrEmpty(activeTraitId) && activeTraitGenerationNumber != 0)
            {
                activeTraitGenerationNumber = 0;
                changed = true;
            }

            return changed;
        }

        public bool HasReceipt(string receiptId)
        {
            var normalized = NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            for (var index = 0; index < appliedReceiptIds.Count; index += 1)
            {
                if (string.Equals(appliedReceiptIds[index], normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanRecordReceipt()
        {
            return appliedReceiptIds != null && appliedReceiptIds.Count < MaximumReceiptIds;
        }

        public void RecordReceipt(string receiptId)
        {
            var normalized = NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalized) || HasReceipt(normalized))
            {
                return;
            }

            appliedReceiptIds.Add(normalized);
            while (appliedReceiptIds.Count > MaximumReceiptIds)
            {
                appliedReceiptIds.RemoveAt(0);
            }
        }

        private bool NormalizeRecords()
        {
            var changed = false;
            var normalized = new List<StarGenerationRecordSaveData>(
                Math.Min(records.Count, MaximumRecords));
            var recordIds = new HashSet<string>(StringComparer.Ordinal);
            var transitionNumbers = new HashSet<int>();

            for (var index = records.Count - 1; index >= 0; index -= 1)
            {
                var record = records[index];
                if (record == null)
                {
                    changed = true;
                    continue;
                }

                changed |= record.EnsureRuntimeDefaults();
                if (!record.IsValid
                    || !recordIds.Add(record.recordId)
                    || !transitionNumbers.Add(record.nextGenerationNumber))
                {
                    changed = true;
                    continue;
                }

                if (normalized.Count >= MaximumRecords)
                {
                    changed = true;
                    continue;
                }

                normalized.Add(record);
            }

            normalized.Reverse();
            changed |= normalized.Count != records.Count;
            records = normalized;
            return changed;
        }

        private static bool NormalizeIds(List<string> values, int maximum)
        {
            var changed = false;
            var normalized = new List<string>(Math.Min(values.Count, maximum));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = values.Count - 1; index >= 0; index -= 1)
            {
                var value = NormalizeToken(values[index]);
                if (string.IsNullOrEmpty(value) || !seen.Add(value))
                {
                    changed = true;
                    continue;
                }

                if (normalized.Count >= maximum)
                {
                    changed = true;
                    continue;
                }

                normalized.Add(value);
            }

            normalized.Reverse();
            if (normalized.Count != values.Count)
            {
                changed = true;
            }

            values.Clear();
            values.AddRange(normalized);
            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = NormalizeToken(value);
            if (string.Equals(value, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            value = normalized;
            return true;
        }

        internal static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class StarGenerationRecordSaveData
    {
        public string recordId = string.Empty;
        public int nextGenerationNumber;
        public string tamaId = string.Empty;
        public string displayName = string.Empty;
        public string formId = string.Empty;
        public string evolutionId = string.Empty;
        public string primaryMilkId = string.Empty;
        public string careStyleId = string.Empty;
        public string completedAtIso = string.Empty;

        public bool IsValid => !string.IsNullOrEmpty(recordId)
            && nextGenerationNumber > 0
            && !string.IsNullOrEmpty(tamaId)
            && DateTimeOffset.TryParse(completedAtIso, out _);

        public bool EnsureRuntimeDefaults()
        {
            var changed = Normalize(ref recordId);
            changed |= Normalize(ref tamaId);
            changed |= Normalize(ref displayName);
            changed |= Normalize(ref formId);
            changed |= Normalize(ref evolutionId);
            changed |= Normalize(ref primaryMilkId);
            changed |= Normalize(ref careStyleId);
            changed |= Normalize(ref completedAtIso);
            if (nextGenerationNumber < 0)
            {
                nextGenerationNumber = 0;
                changed = true;
            }

            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = StarLineageSaveData.NormalizeToken(value);
            if (string.Equals(value, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            value = normalized;
            return true;
        }
    }
}
