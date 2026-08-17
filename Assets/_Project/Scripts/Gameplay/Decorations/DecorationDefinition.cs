namespace CheeseTama.Gameplay.Decorations
{
    public enum DecorationSlot
    {
        Wall = 0,
        Floor = 1,
        Accent = 2,
        Window = 3,
        Shelf = 4,
        Bedside = 5
    }

    public sealed class DecorationDefinition
    {
        public DecorationDefinition(
            string id,
            string displayName,
            string description,
            DecorationSlot slot,
            int milkCoinCost,
            int milkDropCost,
            bool defaultOwned,
            string visualKey)
        {
            this.id = id ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.description = description ?? string.Empty;
            this.slot = slot;
            this.milkCoinCost = milkCoinCost < 0 ? 0 : milkCoinCost;
            this.milkDropCost = milkDropCost < 0 ? 0 : milkDropCost;
            this.defaultOwned = defaultOwned;
            this.visualKey = visualKey ?? string.Empty;
        }

        public readonly string id;
        public readonly string displayName;
        public readonly string description;
        public readonly DecorationSlot slot;
        public readonly int milkCoinCost;
        public readonly int milkDropCost;
        public readonly bool defaultOwned;

        // Presentation code can map this stable key to a material, sprite, or prop.
        public readonly string visualKey;

        public bool IsFree => milkCoinCost == 0 && milkDropCost == 0;
    }
}
