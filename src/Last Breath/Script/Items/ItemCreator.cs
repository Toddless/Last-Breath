namespace LastBreath.Script.Items
{
    using Contracts.Enums;
    using Godot;

    public abstract class ItemCreator
    {
        protected RandomNumberGenerator Rnd = new();

        public abstract Item GenerateItem(GlobalRarity rarity);
    }
}
