namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.UI.View;

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
