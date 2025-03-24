namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Enums;

    public partial class BodyArmor(string itemName, GlobalRarity rarity, float defence, float bonusHealth, string resourcePath, Texture2D? icon, int stackSize, int quantity, string descriptionKey)
        : Armor(itemName, rarity, defence, resourcePath, icon, stackSize, quantity, descriptionKey)
    {
        public float BonusHealth = bonusHealth;

        public override void ReduceDamageTaken()
        {
        }
    }
}
