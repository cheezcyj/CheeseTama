namespace CheeseTama.Gameplay.Milk
{
    public sealed class MilkDefinition
    {
        public MilkDefinition(
            string id,
            string displayName,
            string rarity,
            string description,
            string actionId,
            string requiredMilkId,
            int requiredMilkLevel,
            int growthPoints,
            int careProgress,
            int hunger,
            int mood,
            int cleanliness,
            int sleepiness,
            int health,
            int maturation,
            int affection,
            int milkSatisfaction)
        {
            this.id = id;
            this.displayName = displayName;
            this.rarity = rarity;
            this.description = description;
            this.actionId = actionId;
            this.requiredMilkId = requiredMilkId;
            this.requiredMilkLevel = requiredMilkLevel;
            this.growthPoints = growthPoints;
            this.careProgress = careProgress;
            this.hunger = hunger;
            this.mood = mood;
            this.cleanliness = cleanliness;
            this.sleepiness = sleepiness;
            this.health = health;
            this.maturation = maturation;
            this.affection = affection;
            this.milkSatisfaction = milkSatisfaction;
        }

        public string id;
        public string displayName;
        public string rarity;
        public string description;
        public string actionId;
        public string requiredMilkId;
        public int requiredMilkLevel;
        public int growthPoints;
        public int careProgress;
        public int hunger;
        public int mood;
        public int cleanliness;
        public int sleepiness;
        public int health;
        public int maturation;
        public int affection;
        public int milkSatisfaction;

        public bool IsUnlocked(int requiredMilkGrowthLevel)
        {
            return string.IsNullOrWhiteSpace(requiredMilkId) || requiredMilkGrowthLevel >= requiredMilkLevel;
        }
    }
}
