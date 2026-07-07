namespace LastBreath.UI.View
{
    using Core.Data;
    using Core.Views.UI;
    using Godot;

    public partial class CharacterWindow : Control, IWindow
    {
        private const string UID = "uid://b7ndt5b1q2dif";

        [Export] private VBoxContainer? _ranks;
        private FractionReputation? _fractionReputation;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
        }

        public void Close() => GetParent().RemoveChild(this);
        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void InjectServices(IGameServiceProvider provider)
        {
        }
    }
}
