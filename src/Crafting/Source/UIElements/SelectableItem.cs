namespace Crafting.Source.UIElements
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class SelectableItem : Panel
    {
        private bool _hovered, _selectable = true;
        private int _index;
        private Variant _metaData;
        private StyleBoxFlat? _styleHovered, _styleNormal;

        private Label? _currentText;

        [Signal] public delegate void SelectedEventHandler(int index);

        protected bool IsHovered
        {
            get => _hovered;
            set
            {
                _hovered = value;
                UpdateState();
            }
        }

        public override void _Ready()
        {
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
            _styleHovered = UIResourcesProvider.Instance?.GetResource("ItemHoveredStyle") as StyleBoxFlat;
            _styleNormal = new StyleBoxFlat() { BgColor = new(0, 0, 0, 0) };
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsStretchRatio = 1f;
            CustomMinimumSize = new(0, 25);
            AddThemeStyleboxOverride("panel", _styleNormal);
        }


        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mb) return;
            if (mb.ButtonIndex == MouseButton.Left && mb.Pressed && _selectable)
            {
                EmitSignal(SignalName.Selected, _index);
                AcceptEvent();
            }
        }

        public void SetIndex(int idx) => _index = idx;
        public void SetSelectable(bool selectable) => _selectable = selectable;
        public void SetMetadata(Variant variant) => _metaData = variant;

        // not sure about this
        public void SetText(string text)
        {
            if (_currentText != null) GetChild(0).QueueFree();
            _currentText = new Label
            {
                Text = text,
                LabelSettings = UIResourcesProvider.Instance?.GetResource("TextSettings") as LabelSettings,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _currentText.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_currentText);
        }

        public Variant GetMetadata() => _metaData;

        private void UpdateState()
        {
            switch (true)
            {
                case var _ when IsHovered && _selectable:
                    CallDeferred(MethodName.AddThemeStyleboxOverride, "panel", _styleHovered!);
                    break;
                case var _ when !IsHovered:
                    CallDeferred(MethodName.AddThemeStyleboxOverride, "panel", _styleNormal!);
                    break;
            }
        }

        private void OnMouseEntered() => IsHovered = true;

        private void OnMouseExited() => IsHovered = false;

        public override void _ExitTree()
        {
            MouseEntered -= OnMouseEntered;
            MouseExited -= OnMouseExited;
        }
    }
}
