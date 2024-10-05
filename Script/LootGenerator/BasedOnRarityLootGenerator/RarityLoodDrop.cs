namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    public class RarityLoodDrop : GenericObject<Rarity>
    {
        public RarityLoodDrop(Rarity rarity, GlobalRarity globalRarity)
        {
            Item = rarity;
            probabilityWeight = GlobalRarityToWeight.rarityWeights[globalRarity];
            Rarity = globalRarity;
        }
    }
}
