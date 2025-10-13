namespace Crafting.Source.UIElements
{
    using Core.Interfaces.UI;
    using Godot;

    public partial class PopupWindow : PanelContainer, IInitializable
    {
        private const string UID = "uid://dqrub1rhhnkqp";
        [Export] private VBoxContainer? _childContainer;

        public void AddItem(Node item) => _childContainer?.AddChild(item);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
