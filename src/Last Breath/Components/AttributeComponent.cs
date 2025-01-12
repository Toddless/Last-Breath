namespace Playground.Components
{
    using Playground.Script.Attribute;
    using Godot;

    [GlobalClass]
    public partial class AttributeComponent : Node, IGameComponent
    {
        private Dexterity? _dexterity;
        private Strength? _strength;
        private Intelligence? _intelligence;


        public Dexterity? Dexterity
        {
            get => _dexterity;
        }

        public Strength? Strength
        {
            get => _strength;
        }

        public Intelligence? Intelligence
        {
            get => _intelligence;
        }

        public override void _Ready()
        {
            _dexterity = new Dexterity();
            _strength = new Strength();
            _intelligence = new Intelligence();
        }
    }
}
