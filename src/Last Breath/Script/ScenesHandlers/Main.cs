namespace LastBreath.Script.ScenesHandlers
{
    using Core.Interfaces.UI;
    using Godot;

    public partial class Main : Node2D, IInitializable
    {
        private const string UID = "uid://drgs10sgp405d";

        [Export] private MainWorld? _mainWorld;


        public override void _Ready()
        {
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
