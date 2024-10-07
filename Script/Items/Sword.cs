namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class Sword(string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, string resourcePath, Texture2D icon, int stackSize, int quantity)
        : Weapon(weaponName, rarity, minDamage, maxDamage, resourcePath, icon, stackSize, quantity)
    {
    }
}
