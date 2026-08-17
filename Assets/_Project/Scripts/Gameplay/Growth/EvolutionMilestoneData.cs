namespace CheeseTama.Gameplay.Growth
{
    public sealed class EvolutionMilestoneData
    {
        public EvolutionMilestoneData(string occurrenceId, NormalEvolutionResult result, int level)
        {
            this.occurrenceId = occurrenceId ?? string.Empty;
            this.result = result;
            this.level = level < 1 ? 1 : level;
        }

        public string occurrenceId { get; }
        public NormalEvolutionResult result { get; }
        public int level { get; }
    }
}
