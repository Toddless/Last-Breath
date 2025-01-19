namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using Playground.Script.Enums;

    public class Rarity : GenericObject
    {
        public Rarity(GlobalRarity rarity)
        {
            Rarity = rarity;
            ProbabilityWeight = ConvertGlobalRarity.rarityWeights[rarity];
        }
    }
}
