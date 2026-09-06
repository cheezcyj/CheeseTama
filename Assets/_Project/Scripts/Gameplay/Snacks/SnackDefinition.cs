namespace CheeseTama.Gameplay.Snacks
{
    public sealed class SnackDefinition
    {
        public readonly string id;
        public readonly string eventId;
        public readonly string displayName;
        public readonly string description;
        public readonly int coinCost;
        public readonly int dropCost;
        public readonly int fragmentCost;
        public readonly bool requiresStarMilk;
        public readonly int hunger;
        public readonly int mood;
        public readonly int cleanliness;
        public readonly int sleepiness;
        public readonly int health;
        public readonly int affection;
        public readonly int maturation;
        public readonly int milkSatisfaction;
        public readonly int careProgress;
        public readonly string growthMilkId;
        public readonly int growthPoints;
        public readonly string reactionEventId;
        public readonly string resultMessage;

        public SnackDefinition(
            string id,
            string displayName,
            string description,
            int coinCost,
            int dropCost,
            int fragmentCost,
            bool requiresStarMilk,
            int hunger,
            int mood,
            int cleanliness,
            int sleepiness,
            int health,
            int affection,
            int maturation,
            int milkSatisfaction,
            int careProgress,
            string growthMilkId,
            int growthPoints,
            string reactionEventId)
        {
            this.id = id;
            eventId = id;
            this.displayName = displayName;
            this.description = description;
            this.coinCost = coinCost;
            this.dropCost = dropCost;
            this.fragmentCost = fragmentCost;
            this.requiresStarMilk = requiresStarMilk;
            this.hunger = hunger;
            this.mood = mood;
            this.cleanliness = cleanliness;
            this.sleepiness = sleepiness;
            this.health = health;
            this.affection = affection;
            this.maturation = maturation;
            this.milkSatisfaction = milkSatisfaction;
            this.careProgress = careProgress;
            this.growthMilkId = growthMilkId;
            this.growthPoints = growthPoints;
            this.reactionEventId = reactionEventId;
            resultMessage = $"{displayName} 1개가 간식 패널에 보관되었습니다.";
        }
    }
}
