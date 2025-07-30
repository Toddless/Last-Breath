namespace LastBreath.Script.Items
{
    using Godot;
    using LastBreath.Script.Enums;

    public abstract class ItemCreator
    {
        protected RandomNumberGenerator Rnd = new();

        public abstract Item GenerateItem(GlobalRarity rarity);
    }
}
