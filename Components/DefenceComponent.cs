namespace Playground.Components
{
    using Godot;

    [GlobalClass]
    public partial class DefenceComponent : Node
    {
        private float _defence;
        private float _chanceToEvade;


        public float Defence
        {
            get => _defence;
            set => _defence = value;
        }

        public float ChanceToEvade
        {
            get => _chanceToEvade;
            set => _chanceToEvade = value;
        }
    }
}
