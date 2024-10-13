namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class Weapon(string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, float criticalStrikeChance, string resourcePath, Texture2D icon, int stackSize, int quantity) 
        : Item(weaponName, rarity, resourcePath, icon, stackSize, quantity)
    {
        public float MinDamage = minDamage;

        public float MaxDamage = maxDamage;

        public float CriticalStrikeChance = criticalStrikeChance;
    }
}
