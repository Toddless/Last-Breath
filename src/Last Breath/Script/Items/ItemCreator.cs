namespace LastBreath.Script.Items
{
    using Core.Enums;
    using Godot;

    public abstract class ItemCreator
    {
        protected RandomNumberGenerator Rnd = new();

        public abstract Item GenerateItem(Rarity rarity);
    }
}
