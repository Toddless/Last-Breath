namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public abstract class ItemCreator
    {
        protected RandomNumberGenerator RandomNumberGenerator = new();

        public abstract Item? GenerateItem(GlobalRarity rarity);
    }
}
