namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class Gold(string itemName, GlobalRarity rarity, string resourcePath, Texture2D? icon, int stackSize, int quantity)
        : Item(itemName, rarity, resourcePath, icon, stackSize, quantity)
    {
    }
}
