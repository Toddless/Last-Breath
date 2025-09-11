namespace Crafting.Source.UIElements
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class SelectableItem : Panel
    {
        private bool _selected, _hovered, _selectable = true;
        private int _selectedIndex;
        private Variant _metaData;
        private StyleBoxFlat? _styleSelected, _styleHovered, _styleNormal;

        [Signal] public delegate void SelectedEventHandler(int index);

        protected bool ItemSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                UpdateState();
            }
        }

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
            _styleSelected = UIResourcesProvider.Instance?.GetResource("ItemSelectedStyle") as StyleBoxFlat;
            _styleHovered = UIResourcesProvider.Instance?.GetResource("ItemHoveredStyle") as StyleBoxFlat;
            _styleNormal = new StyleBoxFlat() { BgColor = new(0, 0, 0, 0.1f) };
            SizeFlagsHorizontal = SizeFlags.Fill;
            SizeFlagsVertical = SizeFlags.Fill;
            SizeFlagsStretchRatio = 1f;
            CustomMinimumSize = new(0, 25);
        }


        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mb) return;
            if (mb.ButtonIndex != MouseButton.Left) return;
            if (mb.Pressed && _selectable)
            {
                if (ItemSelected)
                {
                    ItemSelected = false;
                    GD.Print("Item deselected");
                }
                else
                {
                    EmitSignal(SignalName.Selected, _selectedIndex);
                    ItemSelected = true;
                }
                AcceptEvent();
            }
        }

        public void SetIndex(int idx) => _selectedIndex = idx;
        public void SetSelectable(bool selectable) => _selectable = selectable;
        public void SetMetadata(Variant variant) => _metaData = variant;

        // not sure about this
        public void SetText(string text) => AddChild(new Label
        {
            Text = text,
            LabelSettings = UIResourcesProvider.Instance?.GetResource("TextSettings") as LabelSettings,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AnchorLeft = 0.03f,
            AnchorRight = 0.1f
        });
        public Variant GetMetadata() => _metaData;

        private void UpdateState()
        {
            switch (true)
            {
                case var _ when !ItemSelected && IsHovered:
                    CallDeferred(MethodName.AddThemeStyleboxOverride, "panel", _styleHovered!);
                    break;
                case var _ when ItemSelected:
                    CallDeferred(MethodName.AddThemeStyleboxOverride, "panel", _styleSelected!);
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
