namespace Playground.Script.Items
{
    using Playground.Script.Enums;

    public interface IItemCreator
    {
        Item? GenerateItem(GlobalRarity rarity);
    }
}