namespace CheeseTama.Gameplay.Feeding
{
    public readonly struct FeedingResult
    {
        public readonly bool success;
        public readonly string itemId;
        public readonly string penaltyId;

        public FeedingResult(bool success, string itemId, string penaltyId)
        {
            this.success = success;
            this.itemId = itemId;
            this.penaltyId = penaltyId;
        }
    }
}

