namespace Battle.Source.UIElements
{
    using Godot;
    using Core.Interfaces.UI;

    [GlobalClass]
    public partial class StatLine : Control, IInitializable
    {
        private const string UID = "uid://b1p8rydqpxk8b";
        [Export] private Label? _stat, _value;

        public void SetStatLineText(string stat, string value)
        {
            _stat?.Text = stat;
            _value?.Text = value;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
