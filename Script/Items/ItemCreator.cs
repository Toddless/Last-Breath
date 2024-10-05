using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items
{
    public abstract class ItemCreator
    {
        protected RandomNumberGenerator RandomNumberGenerator = new();

        public abstract Item GenerateItem(GlobalRarity rarity);
    }
}
