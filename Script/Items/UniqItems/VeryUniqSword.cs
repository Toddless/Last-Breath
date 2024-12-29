namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public partial class VeryUniqSword : Sword
    {
        private static VeryUniqSword? _instance = null;
        private VeryUniqSword( string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, float criticalStrikeChance, string resourcePath, Texture2D icon, int stackSize, int quantity) 
            : base(weaponName, rarity, minDamage, maxDamage, criticalStrikeChance, resourcePath, icon, stackSize, quantity)
        {
        }

        public static VeryUniqSword Instance
        {
            get
            {
                _instance ??= new VeryUniqSword(StringHelper.SwordMythic, GlobalRarity.Mythic, 600, 950, 0.1f, "res://Resource/SwordMythic.tres", GD.Load<Texture2D>("res://Assets/Weapon/Swords/SwordMythic.png"), 1, 1);
                return _instance;
            }
        }
    }
}
