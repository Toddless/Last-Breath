namespace Playground.DebugTools
{
    using System;
    using Godot;

    public partial class Variable : HBoxContainer
    {
        private const string Path = "res://DebugTools/Variable.tscn";
        private Button? _add, _remove;
        private SpinBox? _spin;
        private Label? _text;

        public event Action<double>? Add, Remove;

        public override void _Ready()
        {
            _add = GetNode<Button>("Add");
            _remove = GetNode<Button>("Remove");
            _spin = GetNode<SpinBox>(nameof(SpinBox));
            _text = GetNode<Label>(nameof(Label));
            SetEvents();
        }

        public void Initialize(string text)
        {
            _text!.Text = text;
        }

        private void SetEvents()
        {
            _add!.Pressed += () => Add?.Invoke(_spin!.Value);
            _remove!.Pressed += () => Remove?.Invoke(_spin!.Value);
        }

        public static PackedScene InitializeAsPackedScene() => ResourceLoader.Load<PackedScene>(Path);

        public override void _ExitTree()
        {
            Add = null;
            Remove = null;
            base._ExitTree();
        }
    }
}
