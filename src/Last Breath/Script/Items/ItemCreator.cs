namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Enums;

    public abstract class ItemCreator
    {
        protected RandomNumberGenerator? RandomNumberGenerator;

        public abstract Item? GenerateItem(GlobalRarity rarity);
    }
}
