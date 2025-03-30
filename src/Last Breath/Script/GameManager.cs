namespace Playground.Script
{
    using Godot;

    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; } = new();
        public Player Player { get; set; }

        public override void _EnterTree() => Instance ??= this;
    }
}
