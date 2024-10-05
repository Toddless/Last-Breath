using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items
{
    public partial class BodyArmor(string itemName, GlobalRarity rarity, float defence, float bonusHealth, string resourcePath, Texture2D icon, int stackSize, int quantity)
        : Armor(itemName, rarity, defence, resourcePath, icon, stackSize, quantity)
    {
        public float BonusHealth = bonusHealth;

        public override void ReduceDamageTaken()
        {

        }
    }
}
