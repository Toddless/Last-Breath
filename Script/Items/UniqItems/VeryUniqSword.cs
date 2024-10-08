namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class VeryUniqSword : Sword
    {
        private static VeryUniqSword instance = null;
        private VeryUniqSword(
            string weaponName,
            GlobalRarity rarity,
            float minDamage,
            float maxDamage,
            string resourcePath,
            Texture2D icon,
            int stackSize, 
            int quantity) 
            : base(weaponName, rarity, minDamage, maxDamage, resourcePath, icon, stackSize, quantity)
        {
        }

        public static VeryUniqSword Instance
        {
            get
            {
                instance ??= new VeryUniqSword(StringHelper.SwordMythic, GlobalRarity.Mythic, 600, 950, "res://Resource/SwordMythic.tres", GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordMythic.png"), 1, 1);
                return instance;
            }
        }
    }
}
