namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using LastBreath.Script.Enums;
    using LastBreath.Script.LootGenerator;

    public class Rarity : GenericObject
    {
        public Rarity(GlobalRarity rarity)
        {
            Rarity = rarity;
            ProbabilityWeight = ConvertGlobalRarity.rarityWeights[rarity];
        }
    }
}
