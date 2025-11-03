namespace Crafting.Source.UIElements
{
    using Godot;
    using Utilities;

    public static class TreeExtension
    {
        public static void UpdateText(this TreeItem treeItem, int amount)
        {
            var id = treeItem.GetMetadata(0).AsString();
            var text = $"{Localizator.Localize(id)}";
            if (amount > 0)
                text += $" ({amount})";
            treeItem.SetText(0, text);
        }
    }
}
