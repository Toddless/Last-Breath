namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public interface IHaveWeight
    {
        float Weight
        {
            get;
        }

        float RangeFrom
        {
            get; set;
        }

        float RangeTo
        {
            get; set;
        }

        GlobalRarity Rarity
        {
            get;
        }
    }
}
