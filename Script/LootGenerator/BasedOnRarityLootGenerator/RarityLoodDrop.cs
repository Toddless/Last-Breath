namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    public class RarityLoodDrop : GenericObject<Rarity>
    {
        public RarityLoodDrop(Rarity rarity, GlobalRarity globalRarity)
        {
            Item = rarity;
            probabilityWeight = ConvertGlobalRarity.rarityWeights[globalRarity];
            Rarity = globalRarity;
        }
    }
}
