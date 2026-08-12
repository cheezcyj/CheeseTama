using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public sealed class MilkGrowthSystem
    {
        private const int PointsPerGrowthLevel = 10;
        private const int MaxGrowthLevel = 5;
        private readonly Dictionary<string, int> growthPoints = new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> GrowthLevels => BuildGrowthLevels();

        public void AddGrowthPoints(string milkId, int points)
        {
            if (string.IsNullOrWhiteSpace(milkId) || points <= 0)
            {
                return;
            }

            growthPoints.TryGetValue(milkId, out var current);
            growthPoints[milkId] = current + points;
        }

        public int GetGrowthLevel(string milkId)
        {
            if (string.IsNullOrWhiteSpace(milkId) || !growthPoints.TryGetValue(milkId, out var points))
            {
                return 0;
            }

            return System.Math.Min(MaxGrowthLevel, points / PointsPerGrowthLevel + 1);
        }

        public MilkGrowthSaveEntry AddGrowthPoints(IList<MilkGrowthSaveEntry> entries, string milkId, int points)
        {
            if (entries == null || string.IsNullOrWhiteSpace(milkId) || points <= 0)
            {
                return null;
            }

            var entry = GetOrCreateEntry(entries, milkId);
            entry.growthPoints += points;
            entry.growthLevel = CalculateGrowthLevel(entry.growthPoints);
            return entry;
        }

        public MilkGrowthSaveEntry FindEntry(IList<MilkGrowthSaveEntry> entries, string milkId)
        {
            if (entries == null || string.IsNullOrWhiteSpace(milkId))
            {
                return null;
            }

            foreach (var entry in entries)
            {
                if (entry != null && entry.milkId == milkId)
                {
                    entry.growthLevel = CalculateGrowthLevel(entry.growthPoints);
                    return entry;
                }
            }

            return null;
        }

        private Dictionary<string, int> BuildGrowthLevels()
        {
            var levels = new Dictionary<string, int>();
            foreach (var entry in growthPoints)
            {
                levels[entry.Key] = GetGrowthLevel(entry.Key);
            }

            return levels;
        }

        private static MilkGrowthSaveEntry GetOrCreateEntry(IList<MilkGrowthSaveEntry> entries, string milkId)
        {
            foreach (var entry in entries)
            {
                if (entry != null && entry.milkId == milkId)
                {
                    return entry;
                }
            }

            var created = new MilkGrowthSaveEntry
            {
                milkId = milkId,
                growthLevel = 0,
                growthPoints = 0
            };
            entries.Add(created);
            return created;
        }

        private static int CalculateGrowthLevel(int points)
        {
            if (points <= 0)
            {
                return 0;
            }

            return System.Math.Min(MaxGrowthLevel, points / PointsPerGrowthLevel + 1);
        }
    }
}
