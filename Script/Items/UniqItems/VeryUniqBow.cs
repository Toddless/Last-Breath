using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.Items
{
    public partial class VeryUniqBow : Bow
    {
        private static VeryUniqBow instance = null;

        private VeryUniqBow(
            string name,
            GlobalRarity rarity,
            float mindamage,
            float maxdamage,
            string resourcePath,
            Texture2D icon,
            int stackSize,
            int quantity)
            : base(name, rarity, mindamage, maxdamage, resourcePath, icon, stackSize, quantity)
        {
        }

        public static VeryUniqBow Instance
        {
            get
            {
                instance ??= new VeryUniqBow("Zeus`s Bow", GlobalRarity.Mythic, 600, 900, string.Empty, null, 1, 1);
                return instance;
            }
        }
    }
}
