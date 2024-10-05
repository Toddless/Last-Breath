using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items
{
    public partial class VeryUniqDagger : Dagger
    {
        private static VeryUniqDagger instance = null;
        private VeryUniqDagger(
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

        public static VeryUniqDagger Instance
        {
            get
            {
                instance ??= new VeryUniqDagger("Shadow Strike", GlobalRarity.Mythic, 600, 950, string.Empty, null, 1, 1);
                return instance;
            }
        }
    }
}
