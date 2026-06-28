using UnityEngine;

namespace CheeseTama.Data
{
    public sealed class DataRegistry : MonoBehaviour
    {
        [SerializeField] private MilkItemDefinition[] milkItems;
        [SerializeField] private FoodItemDefinition[] foodItems;
        [SerializeField] private EvolutionDefinition[] evolutions;
        [SerializeField] private GameEventDefinition[] events;
        [SerializeField] private HiddenCollectionDefinition[] hiddenCollections;

        public MilkItemDefinition[] MilkItems => milkItems;
        public FoodItemDefinition[] FoodItems => foodItems;
        public EvolutionDefinition[] Evolutions => evolutions;
        public GameEventDefinition[] Events => events;
        public HiddenCollectionDefinition[] HiddenCollections => hiddenCollections;
    }
}

