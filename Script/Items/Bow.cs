namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class Bow(string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, float criticalStrikeChance, string resourcePath, Texture2D icon, int stackSize, int quantity)
        : Weapon(weaponName, rarity, minDamage, maxDamage, criticalStrikeChance, resourcePath, icon, stackSize, quantity)
    {
    }
}
