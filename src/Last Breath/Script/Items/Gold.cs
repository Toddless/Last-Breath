namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Enums;

    public partial class Gold(string itemName, GlobalRarity rarity, string resourcePath, Texture2D? icon, int stackSize, int quantity, string descriptionKey)
        : Item(itemName, rarity, resourcePath, icon, stackSize, quantity, descriptionKey)
    {
    }
}
