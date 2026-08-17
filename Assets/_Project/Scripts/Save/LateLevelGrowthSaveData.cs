using System;

namespace CheeseTama.Save
{
    /// <summary>
    /// Raw late-level progress. CheeseTamaModel.levelProgress remains the public
    /// 0..99 compatibility mirror; this DTO owns the larger Lv.31-33 unit totals.
    /// It is intentionally separate so old evolution ids and serialized fields do
    /// not need to change.
    /// </summary>
    [Serializable]
    public sealed class LateLevelGrowthSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public bool initialized;
        public bool migratedFromLegacyPercent;
        public int trackedLevel;
        public int progressUnits;

        public void BeginLevel(int level, int startingProgressUnits = 0)
        {
            schemaVersion = CurrentSchemaVersion;
            initialized = true;
            trackedLevel = Math.Max(0, level);
            progressUnits = Math.Max(0, startingProgressUnits);
        }
    }
}
