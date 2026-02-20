namespace LastBreath.Source.UI.View
{
    using Core.Data;
    using Core.Interfaces.UI;
    using Godot;
    using Script.Helpers;
    using Window = Window;

    public partial class CharacterWindow : Window, IInitializable
    {
        private const string UID = "uid://b7ndt5b1q2dif";

        private VBoxContainer? _ranks;
        private FractionReputation? _fractionReputation;

        public override void _Ready()
        {
            _ranks = (VBoxContainer?)NodeFinder.FindBFSCached(this, "Ranks");
        }


        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void InjectServices(IGameServiceProvider provider)
        {

        }

    }
}
