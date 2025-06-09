namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.UI.View;

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
