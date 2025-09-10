namespace Crafting.Source.UIElements
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class MainResource : BaseResource
    {
        private const string UID = "uid://cnsayfpcqo753";

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
