using System.Collections.Generic;

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

        private Dictionary<string, int> BuildGrowthLevels()
        {
            var levels = new Dictionary<string, int>();
            foreach (var entry in growthPoints)
            {
                levels[entry.Key] = GetGrowthLevel(entry.Key);
            }

            return levels;
        }
    }
}

