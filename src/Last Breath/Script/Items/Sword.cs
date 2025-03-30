namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Enums;

    public partial class Sword(string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, float criticalStrikeChance, string resourcePath, Texture2D icon, int stackSize, int quantity,string descriptionKey)
        : Weapon(weaponName, rarity, minDamage, maxDamage, criticalStrikeChance, resourcePath, icon, stackSize, quantity, descriptionKey)
    {
    }
}
