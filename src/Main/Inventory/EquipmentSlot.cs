namespace LastBreath.Inventory
{
    using System;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Godot;
    using Godot.Collections;

    /// <summary>
    /// One paperdoll slot of the equipment panel: shows the equipped piece (icon + rarity-coloured
    /// frame), accepts a matching item dragged from the bag, serves as a drag source for
    /// unequipping into the bag and unequips on right-click. The window supplies the checks and
    /// actions — the slot itself knows nothing about the inventory.
    /// </summary>
    public partial class EquipmentSlot : PanelContainer
    {
        private IEquipItem? _equipped;
        private Func<string, bool>? _canAcceptInstance;
        private Action<string>? _acceptInstance;
        private Action<EquipmentPiece>? _unequip;
        private StyleBox? _emptyStyle;

        [Export] public EquipmentPiece Piece { get; set; }
        [Export] private TextureRect? _icon;

        public override void _Ready() => _emptyStyle = GetThemeStylebox("panel");

        public void Bind(Func<string, bool> canAcceptInstance, Action<string> acceptInstance, Action<EquipmentPiece> unequip)
        {
            _canAcceptInstance = canAcceptInstance;
            _acceptInstance = acceptInstance;
            _unequip = unequip;
        }

        public void SetEquipped(IEquipItem? item)
        {
            _equipped = item;
            _icon?.Texture = item?.Icon;
            ApplyStyle();
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } || _equipped == null) return;
            _unequip?.Invoke(Piece);
            AcceptEvent();
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (_equipped == null) return new Variant();

            var preview = new TextureRect
            {
                Texture = _equipped.Icon,
                MouseFilter = MouseFilterEnum.Ignore,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                CustomMinimumSize = new Vector2(60, 60),
            };
            SetDragPreview(preview);
            return new Dictionary { ["EquipmentPiece"] = (int)Piece };
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (data.VariantType != Variant.Type.Dictionary) return false;
            var payload = data.AsGodotDictionary();
            if (!payload.ContainsKey("Instance")) return false;
            return _canAcceptInstance?.Invoke(payload["Instance"].AsString()) ?? false;
        }

        public override void _DropData(Vector2 atPosition, Variant data) =>
            _acceptInstance?.Invoke(data.AsGodotDictionary()["Instance"].AsString());

        /// <summary>A filled slot borrows the rarity for its frame and a faint tint of its fill;
        /// an empty one falls back to the scene's neutral stylebox.</summary>
        private void ApplyStyle()
        {
            if (_emptyStyle == null) return;
            if (_equipped == null || _emptyStyle is not StyleBoxFlat flat)
            {
                AddThemeStyleboxOverride("panel", _emptyStyle);
                return;
            }

            var rarity = Color.FromHtml(TextPalette.RarityColor(_equipped.Rarity));
            var filled = (StyleBoxFlat)flat.Duplicate();
            filled.BorderColor = rarity;
            filled.BgColor = flat.BgColor.Lerp(rarity, 0.14f);
            AddThemeStyleboxOverride("panel", filled);
        }
    }
}
