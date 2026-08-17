using System;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public enum LateLevelProgressMigrationStatus
    {
        NotApplicable = 0,
        MissingTarget = 1,
        InitializedFromLegacyPercent = 2,
        RepairedCurrentState = 3,
        AlreadyCurrent = 4,
        UnsupportedFutureVersion = 5
    }

    public readonly struct LateLevelProgressMigrationResult
    {
        public LateLevelProgressMigrationResult(
            LateLevelProgressMigrationStatus status,
            int legacyPercent,
            int progressUnits,
            int requiredProgressUnits,
            int displayPercent,
            bool changed)
        {
            Status = status;
            LegacyPercent = Math.Max(0, Math.Min(99, legacyPercent));
            ProgressUnits = Math.Max(0, progressUnits);
            RequiredProgressUnits = Math.Max(0, requiredProgressUnits);
            DisplayPercent = Math.Max(0, Math.Min(100, displayPercent));
            Changed = changed;
        }

        public LateLevelProgressMigrationStatus Status { get; }
        public int LegacyPercent { get; }
        public int ProgressUnits { get; }
        public int RequiredProgressUnits { get; }
        public int DisplayPercent { get; }
        public bool Changed { get; }
    }

    /// <summary>
    /// Migration contract:
    /// - Existing CheeseTamaModel.levelProgress remains a 0..99 compatibility mirror.
    /// - On first migration, p percent becomes p/100 of the new raw requirement.
    /// - Requirements are whole multiples of 100, so every legacy integer percent
    ///   maps exactly and the serialized mirror remains byte-for-byte equivalent.
    /// - A full but gate-blocked bar is mirrored as 99 to preserve LevelSystem's
    ///   historical "less than 100 until level-up" invariant; DisplayPercent is 100.
    /// - Existing level, form, and evolutionId values are never changed here.
    /// </summary>
    public static class LateLevelProgressMigration
    {
        public static LateLevelProgressMigrationResult EnsureCurrent(
            CheeseTamaModel tama,
            LateLevelGrowthSaveData state)
        {
            if (tama == null || state == null)
            {
                return new LateLevelProgressMigrationResult(
                    LateLevelProgressMigrationStatus.MissingTarget,
                    tama?.levelProgress ?? 0,
                    state?.progressUnits ?? 0,
                    0,
                    0,
                    false);
            }

            if (!LateLevelGrowthCatalog.TryGetForCurrentLevel(
                    tama.level,
                    out var requirement))
            {
                return new LateLevelProgressMigrationResult(
                    LateLevelProgressMigrationStatus.NotApplicable,
                    tama.levelProgress,
                    state.progressUnits,
                    0,
                    0,
                    false);
            }

            if (state.schemaVersion > LateLevelGrowthSaveData.CurrentSchemaVersion)
            {
                return new LateLevelProgressMigrationResult(
                    LateLevelProgressMigrationStatus.UnsupportedFutureVersion,
                    tama.levelProgress,
                    state.progressUnits,
                    requirement.RequiredProgressUnits,
                    GetDisplayPercent(state.progressUnits, requirement.RequiredProgressUnits),
                    false);
            }

            var legacyPercent = Math.Max(0, Math.Min(99, tama.levelProgress));
            if (!state.initialized
                || state.schemaVersion <= 0
                || state.trackedLevel != tama.level)
            {
                state.schemaVersion = LateLevelGrowthSaveData.CurrentSchemaVersion;
                state.initialized = true;
                state.migratedFromLegacyPercent = true;
                state.trackedLevel = tama.level;
                state.progressUnits = ConvertLegacyPercentToUnits(
                    legacyPercent,
                    requirement.RequiredProgressUnits);
                tama.levelProgress = legacyPercent;
                return new LateLevelProgressMigrationResult(
                    LateLevelProgressMigrationStatus.InitializedFromLegacyPercent,
                    legacyPercent,
                    state.progressUnits,
                    requirement.RequiredProgressUnits,
                    GetDisplayPercent(state.progressUnits, requirement.RequiredProgressUnits),
                    true);
            }

            var changed = false;
            if (state.schemaVersion != LateLevelGrowthSaveData.CurrentSchemaVersion)
            {
                state.schemaVersion = LateLevelGrowthSaveData.CurrentSchemaVersion;
                changed = true;
            }

            var clampedUnits = Math.Max(
                0,
                Math.Min(requirement.RequiredProgressUnits, state.progressUnits));
            if (state.progressUnits != clampedUnits)
            {
                state.progressUnits = clampedUnits;
                changed = true;
            }

            var compatibilityPercent = GetCompatibilityPercent(
                state.progressUnits,
                requirement.RequiredProgressUnits);
            if (tama.levelProgress != compatibilityPercent)
            {
                tama.levelProgress = compatibilityPercent;
                changed = true;
            }

            return new LateLevelProgressMigrationResult(
                changed
                    ? LateLevelProgressMigrationStatus.RepairedCurrentState
                    : LateLevelProgressMigrationStatus.AlreadyCurrent,
                compatibilityPercent,
                state.progressUnits,
                requirement.RequiredProgressUnits,
                GetDisplayPercent(state.progressUnits, requirement.RequiredProgressUnits),
                changed);
        }

        public static bool SyncCompatibilityPercent(
            CheeseTamaModel tama,
            LateLevelGrowthSaveData state)
        {
            if (tama == null
                || state == null
                || !LateLevelGrowthCatalog.TryGetForCurrentLevel(
                    tama.level,
                    out var requirement)
                || state.schemaVersion != LateLevelGrowthSaveData.CurrentSchemaVersion
                || !state.initialized
                || state.trackedLevel != tama.level)
            {
                return false;
            }

            var clampedUnits = Math.Max(
                0,
                Math.Min(requirement.RequiredProgressUnits, state.progressUnits));
            var percent = GetCompatibilityPercent(
                clampedUnits,
                requirement.RequiredProgressUnits);
            var changed = state.progressUnits != clampedUnits
                || tama.levelProgress != percent;
            state.progressUnits = clampedUnits;
            tama.levelProgress = percent;
            return changed;
        }

        public static int ConvertLegacyPercentToUnits(int legacyPercent, int requiredProgressUnits)
        {
            var percent = Math.Max(0, Math.Min(99, legacyPercent));
            var required = Math.Max(1, requiredProgressUnits);
            return (int)(((long)percent * required) / 100L);
        }

        public static int GetDisplayPercent(int progressUnits, int requiredProgressUnits)
        {
            var required = Math.Max(1, requiredProgressUnits);
            var progress = Math.Max(0, Math.Min(required, progressUnits));
            return (int)Math.Min(100L, ((long)progress * 100L) / required);
        }

        public static int GetCompatibilityPercent(int progressUnits, int requiredProgressUnits)
        {
            return Math.Min(99, GetDisplayPercent(progressUnits, requiredProgressUnits));
        }
    }
}
