using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items
{
    public partial class Weapon(string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, string resourcePath, Texture2D icon, int stackSize, int quantity) 
        : Item(weaponName, rarity, resourcePath, icon, stackSize, quantity)
    {
        public float MinDamage = minDamage;

        public float MaxDamage = maxDamage;

        public virtual float DoDamage()
        {
            var randomMaxDamage = RandomNumberGenerator.RandfRange(MinDamage, MaxDamage);
            return randomMaxDamage;
        }
    }
}
