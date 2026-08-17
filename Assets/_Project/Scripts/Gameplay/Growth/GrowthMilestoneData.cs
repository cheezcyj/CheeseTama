namespace CheeseTama.Gameplay.Growth
{
    public sealed class GrowthMilestoneData
    {
        public GrowthMilestoneData(string id, CheeseTamaGrowthStage stage, int level)
        {
            this.id = id ?? string.Empty;
            this.stage = stage;
            this.level = level;
        }

        public string id { get; }
        public CheeseTamaGrowthStage stage { get; }
        public int level { get; }
    }
}
