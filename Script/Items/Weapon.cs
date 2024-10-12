namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class Weapon(string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, string resourcePath, Texture2D icon, int stackSize, int quantity) 
        : Item(weaponName, rarity, resourcePath, icon, stackSize, quantity)
    {

        [Signal]
        public delegate void OnWeaponEquipEventHandler(Weapon weapon);

        public float MinDamage = minDamage;

        public float MaxDamage = maxDamage;
    }
}
