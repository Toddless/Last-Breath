namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Enums;

    public abstract partial class Armor(string itemName, GlobalRarity rarity, float defence, string resourcePath, Texture2D? icon, int stackSize, int quantity, string descriptionKey)
        : Item(itemName, rarity, resourcePath, icon, stackSize, quantity, descriptionKey)
    {
        public float Defence = defence;

        public abstract void ReduceDamageTaken();
    }
}
