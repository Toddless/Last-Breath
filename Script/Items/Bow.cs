using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items
{
    public partial class Bow(
        string weaponName,
        GlobalRarity rarity,
        float minDamage,
        float maxDamage,
        string resourcePath,
        Texture2D icon,
        int stackSize,
        int quantity) : Weapon(weaponName, rarity, minDamage, maxDamage, resourcePath, icon, stackSize, quantity)
    {
    }
}
