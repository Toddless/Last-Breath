namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using LastBreath.Script.LootGenerator;

    public class Rarity : GenericObject
    {
        public Rarity(Contracts.Enums.Rarity rarity)
        {
            Rarity = rarity;
            ProbabilityWeight = ConvertGlobalRarity.rarityWeights[rarity];
        }
    }
}
