namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class Weapon : Item
    {
        private float _minDamage;
        private float _maxDamage;
        private float _criticalStrikeChance;
        private RandomNumberGenerator _rnd = new();


        public float MinDamage
        {
            get => _minDamage;
            set => _minDamage = value;
        }

        public float MaxDamage
        {
            get => _maxDamage;
            set => _maxDamage = value;
        }

        public float CriticalStrikeChance
        {
            get => _criticalStrikeChance;
            set => _criticalStrikeChance = value;
        }


        public Weapon(string weaponName, GlobalRarity rarity, float minDamage, float maxDamage, float criticalStrikeChance, string resourcePath, Texture2D icon, int stackSize, int quantity) : base(weaponName, rarity, resourcePath, icon, stackSize, quantity)
        {
            _minDamage = minDamage;
            _maxDamage = maxDamage;
            _criticalStrikeChance = criticalStrikeChance;
        }
    }
}
