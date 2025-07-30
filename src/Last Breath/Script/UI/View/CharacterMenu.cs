namespace LastBreath.Script.UI
{
    using Godot;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.UI.View;

    public partial class CharacterMenu : Control
    {
        private VBoxContainer? _ranks;
        private FractionReputation? _fractionReputation;

        public override void _Ready()
        {
            _ranks = (VBoxContainer?)NodeFinder.FindBFSCached(this, "Ranks");
        }
    }
}
