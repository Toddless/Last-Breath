namespace Battle.Source.UIElements
{
    using Core.Interfaces.UI;
    using Godot;

    [GlobalClass]
    [Tool]
    public partial class QueueSlot : Control, IInitializable
    {
        private const string UID = "uid://bt5ovr71f3vu1";
        [Export] private TextureRect? Icon { get; set; }

        public void SetIcon(Texture2D icon) => Icon?.Texture = icon;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
