namespace Playground.Script.Items.UniqItems
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    partial class VeryUniqBodyArmor : BodyArmor
    {
        private static VeryUniqBodyArmor? _instance = null;
        private VeryUniqBodyArmor(string itemName, GlobalRarity rarity, float defence, float bonusHealth, string resourcePath, Texture2D? icon, int stackSize, int quantity)
            : base(itemName, rarity, defence, bonusHealth, resourcePath, icon, stackSize, quantity)
        {
        }
        public static VeryUniqBodyArmor Instance
        {
            get
            {
                _instance ??= new VeryUniqBodyArmor(StringHelper.BodyArmorMythic, GlobalRarity.Mythic, 900, 600, "res://Resource/BodyArmorUniq.tres", GD.Load<Texture2D>("res://Assets/BodyArmor/Uniq.png"), 1, 1);
                return _instance;
            }
        }
    }
}
