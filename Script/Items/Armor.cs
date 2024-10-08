namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public abstract partial class Armor(string itemName, GlobalRarity rarity, float defence, string resourcePath, Texture2D icon, int stackSize, int quantity)
        : Item(itemName, rarity, resourcePath, icon, stackSize, quantity)
    {
        public float Defence = defence;

        public abstract void ReduceDamageTaken();
    }
}
