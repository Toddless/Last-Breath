namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using LastBreath.Script.LootGenerator;

    public class Rarity : GenericObject
    {
        public Rarity(Core.Enums.Rarity rarity)
        {
            Rarity = rarity;
            ProbabilityWeight = ConvertGlobalRarity.rarityWeights[rarity];
        }
    }
}
