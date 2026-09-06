using System;
using System.Collections.Generic;

namespace CheeseTama.Data
{
    public readonly struct ContentContractReference
    {
        public ContentContractReference(
            string targetCategory,
            string targetId,
            bool required = true)
        {
            TargetCategory = (targetCategory ?? string.Empty).Trim();
            TargetId = (targetId ?? string.Empty).Trim();
            Required = required;
        }

        public string TargetCategory { get; }
        public string TargetId { get; }
        public bool Required { get; }
    }

    public sealed class ContentContractEntry
    {
        private readonly ContentContractReference[] references;

        public ContentContractEntry(
            string source,
            string category,
            string id,
            params ContentContractReference[] references)
        {
            Source = (source ?? string.Empty).Trim();
            Category = (category ?? string.Empty).Trim();
            Id = (id ?? string.Empty).Trim();
            this.references = references ?? Array.Empty<ContentContractReference>();
        }

        public string Source { get; }
        public string Category { get; }
        public string Id { get; }
        public IReadOnlyList<ContentContractReference> References => references;
        public string QualifiedId => Category + ":" + Id;
    }

    public enum ContentContractIssueKind
    {
        MissingSource = 0,
        InvalidCategory = 1,
        InvalidId = 2,
        DuplicateId = 3,
        MissingReference = 4,
        SelfReference = 5
    }

    public sealed class ContentContractIssue
    {
        internal ContentContractIssue(
            ContentContractIssueKind kind,
            string source,
            string message)
        {
            Kind = kind;
            Source = source ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public ContentContractIssueKind Kind { get; }
        public string Source { get; }
        public string Message { get; }
    }

    public sealed class ContentContractReport
    {
        private readonly ContentContractEntry[] entries;
        private readonly ContentContractIssue[] issues;

        internal ContentContractReport(
            ContentContractEntry[] entries,
            ContentContractIssue[] issues)
        {
            this.entries = entries ?? Array.Empty<ContentContractEntry>();
            this.issues = issues ?? Array.Empty<ContentContractIssue>();
        }

        public IReadOnlyList<ContentContractEntry> Entries => entries;
        public IReadOnlyList<ContentContractIssue> Issues => issues;
        public bool IsValid => issues.Length == 0;
    }

    /// <summary>
    /// Deterministic, Unity-independent validation for authored catalog IDs and foreign keys.
    /// Editor adapters collect existing static catalogs and JSON documents into this model.
    /// </summary>
    public static class ContentContractValidator
    {
        public const int MaximumIdLength = 128;

        public static ContentContractReport Validate(
            IEnumerable<ContentContractEntry> sourceEntries)
        {
            var entries = new List<ContentContractEntry>();
            var issues = new List<ContentContractIssue>();
            var knownIds = new Dictionary<string, ContentContractEntry>(StringComparer.Ordinal);

            if (sourceEntries != null)
            {
                foreach (var entry in sourceEntries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    entries.Add(entry);
                    ValidateIdentity(entry, knownIds, issues);
                }
            }

            for (var entryIndex = 0; entryIndex < entries.Count; entryIndex += 1)
            {
                var entry = entries[entryIndex];
                for (var referenceIndex = 0;
                    referenceIndex < entry.References.Count;
                    referenceIndex += 1)
                {
                    var reference = entry.References[referenceIndex];
                    if (!reference.Required && string.IsNullOrEmpty(reference.TargetId))
                    {
                        continue;
                    }

                    var targetKey = BuildQualifiedId(
                        reference.TargetCategory,
                        reference.TargetId);
                    if (string.Equals(targetKey, entry.QualifiedId, StringComparison.Ordinal))
                    {
                        issues.Add(new ContentContractIssue(
                            ContentContractIssueKind.SelfReference,
                            entry.Source,
                            $"콘텐츠가 자신을 참조합니다: {targetKey}"));
                    }
                    else if (!knownIds.ContainsKey(targetKey))
                    {
                        issues.Add(new ContentContractIssue(
                            ContentContractIssueKind.MissingReference,
                            entry.Source,
                            $"참조 대상을 찾을 수 없습니다: {targetKey}"));
                    }
                }
            }

            return new ContentContractReport(entries.ToArray(), issues.ToArray());
        }

        public static bool IsStableId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaximumIdLength)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var character = value[index];
                var accepted = character >= 'a' && character <= 'z'
                    || character >= '0' && character <= '9'
                    || character == '_'
                    || character == '-'
                    || character == '.'
                    || character == ':';
                if (!accepted)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateIdentity(
            ContentContractEntry entry,
            IDictionary<string, ContentContractEntry> knownIds,
            ICollection<ContentContractIssue> issues)
        {
            if (string.IsNullOrEmpty(entry.Source))
            {
                issues.Add(new ContentContractIssue(
                    ContentContractIssueKind.MissingSource,
                    string.Empty,
                    "콘텐츠 출처가 비어 있습니다."));
            }

            if (!IsStableId(entry.Category))
            {
                issues.Add(new ContentContractIssue(
                    ContentContractIssueKind.InvalidCategory,
                    entry.Source,
                    $"콘텐츠 카테고리가 올바르지 않습니다: {entry.Category}"));
                return;
            }

            if (!IsStableId(entry.Id))
            {
                issues.Add(new ContentContractIssue(
                    ContentContractIssueKind.InvalidId,
                    entry.Source,
                    $"콘텐츠 ID가 올바르지 않습니다: {entry.Id}"));
                return;
            }

            if (knownIds.ContainsKey(entry.QualifiedId))
            {
                issues.Add(new ContentContractIssue(
                    ContentContractIssueKind.DuplicateId,
                    entry.Source,
                    $"중복 콘텐츠 ID입니다: {entry.QualifiedId}"));
                return;
            }

            knownIds.Add(entry.QualifiedId, entry);
        }

        private static string BuildQualifiedId(string category, string id)
        {
            return (category ?? string.Empty).Trim()
                + ":"
                + (id ?? string.Empty).Trim();
        }
    }
}
