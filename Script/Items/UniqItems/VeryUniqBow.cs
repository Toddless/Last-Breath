namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class VeryUniqBow : Bow
    {
        private static VeryUniqBow? _instance = null;

        private VeryUniqBow( string name, GlobalRarity rarity, float mindamage, float maxdamage, float criticalStrikeChance, string resourcePath, Texture2D icon, int stackSize, int quantity)
            : base(name, rarity, mindamage, maxdamage, criticalStrikeChance, resourcePath, icon, stackSize, quantity)
        {
        }

        public static VeryUniqBow Instance
        {
            get
            {
                _instance ??= new VeryUniqBow(StringHelper.BowMythic, GlobalRarity.Mythic, 600, 900, 0.1f, "res://Resource/BowMythic.tres", GD.Load<Texture2D>("res://Assets/Weapon/Bows/BowMythic.png"), 1, 1);
                return _instance;
            }
        }
    }
}
